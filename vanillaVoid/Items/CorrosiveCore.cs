using BepInEx.Configuration;
using R2API;
using RoR2;
using vanillaVoid.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using static vanillaVoid.vanillaVoidPlugin;
using On.RoR2.Items;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using static R2API.DamageAPI;

namespace vanillaVoid.Items
{
    public class CorrosiveCore : ItemBase<CorrosiveCore>
    {
        public ConfigEntry<float> baseDamageDot;

        public ConfigEntry<float> stackingDamageDot;

        public override string ItemName => "Corrosive Core";

        public override string ItemLangTokenName => "CORE_ITEM";

        public override string ItemPickupDesc => $"Slowed enemies take damage over time. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"<style=cIsUtility>Slow</style> effects apply <style=cIsDamage>drown</style>, dealing <style=cIsDamage>{baseDamageDot.Value * 100}%</style> <style=cStack>(+{stackingDamageDot.Value * 100}% per stack)</style> base damage per <style=cIsUtility>10% slow</style>. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"The horngus of a dongfish is attached by a scungle to a kind of dillsack (the nutte sac).";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCorePickupAnim.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("coreIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.Damage };

        public BuffDef drownBuff { get; private set; }
        public DotController.DotIndex drownDotIndex;
        ModdedDamageType drownDamage;

        public GameObject drownVFX;

        public override void Init(ConfigFile config){
            CreateConfig(config);
            CreateLang();
            CreateItem();
            CreateBuff();

            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            Hooks();

            drownVFX = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("CoreDrownVFX.prefab");

            var destroy = drownVFX.AddComponent<DestroyOnTimer>();
            destroy.duration = .2f;
            destroy.enabled = false;

            var tempvfx = drownVFX.AddComponent<TemporaryVisualEffect>();
            tempvfx.radius = 1;
            tempvfx.visualTransform = drownVFX.transform.Find("VisualEffect");

            var exits = new MonoBehaviour[1];
            exits[0] = destroy;

            tempvfx.exitComponents = exits;

            //TempVisualEffectAPI.AddTemporaryVisualEffect()

            TempVisualEffectAPI.AddTemporaryVisualEffect(drownVFX.InstantiateClone("DrownDotVFX", false), (CharacterBody body) => { return body.HasBuff(drownBuff); }, true, "Head");
        }

        public void CreateBuff(){
            drownDamage = ReserveDamageType();
            drownBuff = ScriptableObject.CreateInstance<BuffDef>();
            drownBuff.buffColor = Color.magenta;
            drownBuff.canStack = false;
            drownBuff.isDebuff = false;
            drownBuff.name = "DmVV" + "drownDot";
            drownBuff.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("DrownDot");
            ContentAddition.AddBuffDef(drownBuff);

            //DotAPI.CustomDotBehaviour drownDotBehavior = DrownDotBehavior;
            drownDotIndex = DotAPI.RegisterDotDef(0.33f, (1 * 0.33f), DamageColorIndex.Void, drownBuff, null, null);
        }

        //public void DrownDotBehavior(DotController self, DotController.DotStack dotStack){
        //    if (dotStack.dotIndex == drownDotIndex){
        //        CharacterBody attacker = dotStack.attackerObject.GetComponent<CharacterBody>();
        //        int count = 1;
        //        if (attacker.inventory){
        //            count = GetCount(attacker);
        //        }
        //
        //        float mult = 0;
        //        var comp = self.victimBody.gameObject.GetComponent<CorrosiveCounter>();
        //        if (comp) {
        //            mult = comp.slowAmount + (comp.isFrozen ? 1.5f : (comp.isStopped ? 1 : 0)) + (self.victimBody.HasBuff(RoR2Content.Buffs.Weak) ? .4f : 0); //shoutout to rex weaken being the only one done differently
        //
        //            //self.victimBody.gameObject.GetComponent<SetStateOnHurt>().targetStateMachine
        //        }
        //
        //        if(mult > 1){
        //            Debug.Log("Removing old debuff");
        //
        //            for(int i = 0; i < self.dotStackList.Count; ++i){
        //                if(self.dotStackList[i].dotIndex == drownDotIndex){
        //                    self.RemoveDotStackAtServer(i);
        //                    break;
        //                }
        //            }
        //            //self.RemoveDotStackAtServer()
        //
        //           // Debug.Log("The j: " + baseDamageDot.Value * attacker.damage + (stackingDamageDot.Value * (count - 1)));
        //            dotStack.damage = mult * (baseDamageDot.Value * attacker.damage + (stackingDamageDot.Value * (count - 1)));
        //            dotStack.AddModdedDamageType(drownDamage);
        //        }
        //        else
        //        {
        //            //Debug.Log("Mult is not high enough");
        //        }
        //
        //        //float baseDotDamage = self.victimBody.maxHealth * burnDamagePercent.Value / 100f / burnDamageDuration.Value * myDotDef.interval;
        //        //float dotDamage = Math.Max(burnDamageMin.Value * attackerCharacterBody.damage, Math.Min(burnDamageMax.Value * attackerCharacterBody.damage, baseDotDamage)) / burnDamageDuration.Value * inventoryCount;
        //        //dotStack.damage = dotDamage;
        //    }
        //}


        public override void CreateConfig(ConfigFile config)
        {
            baseDamageDot = config.Bind<float>("Item: " + ItemName, "Base Dot Damage", .33f, "Adjust the percent of base damage drown does per 10% slow.");
            stackingDamageDot = config.Bind<float>("Item: " + ItemName, "Stacking Dot Damage", .33f, "Adjust the damage percent drown does per 10% slow per stack.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "StrengthenBurn", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {

            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCorePickupAnim.prefab");
            //string orbTransp = "RoR2/DLC1/voidraid/matVoidRaidPlanetPurpleWave.mat"; 
            //string orbCore = "RoR2/DLC1/voidstage/matVoidCoralPlatformPurple.mat";

            //string orbTransp = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorLightning.mat";
            //string orbCore = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorPod.mat";
            //
            //var adzeOrbsModelTransp = ItemModel.transform.Find("orbTransp").GetComponent<MeshRenderer>();
            //var adzeOrbsModelCore = ItemModel.transform.Find("orbCore").GetComponent<MeshRenderer>();
            //adzeOrbsModelTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            //adzeOrbsModelCore.material = Addressables.LoadAssetAsync<Material>(orbCore).WaitForCompletion();
            //
            //var adzeOrbsDisplayTransp = ItemBodyModelPrefab.transform.Find("orbTransp").GetComponent<MeshRenderer>();
            //var adzeOrbsDisplayCore = ItemBodyModelPrefab.transform.Find("orbCore").GetComponent<MeshRenderer>();
            //adzeOrbsDisplayTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            //adzeOrbsDisplayCore.material = Addressables.LoadAssetAsync<Material>(orbCore).WaitForCompletion();

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);



            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CannonHeadL",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
            
                    //localPos = new Vector3(0.3982559f, 0.5157748f, 1.197929f), //std turret
                    //localAngles = new Vector3(2.650187f, 268.003f, 247.601f),
                    //localScale = new Vector3(.25f, .25f, .25f)
                }
            });
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "PlatformBase",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MechBase",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Stomach",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Stomach",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            //
            ////Modded Chars 
            rules.Add("EnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName =  "Shield",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            //rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Door",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "PickL",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LeftForearm",
                    localPos = new Vector3(0f, 0f, 0f),
                    localAngles = new Vector3(0f, 0f, 0f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("ExecutionerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("NemmandoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlHANDOverclocked", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            //rules.Add("mdlDaredevil", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0, 0, 0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
            rules.Add("mdlRMOR", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            //rules.Add("Spearman", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0, 0, 0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
            rules.Add("mdlAssassin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "arm_bone2.L",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("RobDriverBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlTeslaTrooper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0, 0, 0),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(1, 1, 1)
                }
            });
            return rules;
        }

        public override void Hooks()
        {
            IL.RoR2.CharacterBody.RecalculateStats += CheckSlowAmount2; //HUGE
            On.RoR2.HealthComponent.TakeDamage += ApplySlowDot;

            On.RoR2.HealthComponent.TakeDamage += wawa;

            On.EntityStates.FrozenState.OnEnter += CheckFrozenState;
            On.EntityStates.FrozenState.OnExit += RemoveFrozenState;

            On.RoR2.Skills.SkillDef.OnExecute += Buh;

            //what remains:

            //0 speed affects (test this?)
            //freeze
        }

       

        private void Buh(On.RoR2.Skills.SkillDef.orig_OnExecute orig, RoR2.Skills.SkillDef self, GenericSkill skillSlot)
        {
            orig(self, skillSlot);
            Debug.Log("Skill fired " + self.skillNameToken);
        }

        private void CheckFrozenState(On.EntityStates.FrozenState.orig_OnEnter orig, EntityStates.FrozenState self)
        {
            orig(self);
            var comp = self.characterBody.gameObject.GetComponent<CorrosiveCounter>();

            if (comp)
            {
                comp.isFrozen = true;
                var mult = comp.slowAmount + (comp.isFrozen ? 1.5f : (comp.isStopped ? 1 : 0)) + (self.characterBody.HasBuff(RoR2Content.Buffs.Weak) ? .4f : 0); //shoutout to rex weaken being the only one done differently
                
                if (mult > 1 && !self.characterBody.HasBuff(drownBuff))
                {
                    Debug.Log("applying DOT from CheckFrozenState");
                    var dotInfo = new InflictDotInfo
                    {
                        attackerObject = comp.recentPlayer.gameObject,
                        victimObject = self.gameObject,
                        damageMultiplier = 1f,
                        dotIndex = drownDotIndex,
                        duration = Mathf.Infinity,
                    };
                    DotController.InflictDot(ref dotInfo);
                }
                else
                {
                    Debug.Log("Should remove dot");
                }
            }
        
        }

        private void RemoveFrozenState(On.EntityStates.FrozenState.orig_OnExit orig, EntityStates.FrozenState self)
        {
            orig(self);
            var comp = self.characterBody.gameObject.GetComponent<CorrosiveCounter>();
            if (comp)
            {
                comp.isFrozen = false;
                var mult = comp.slowAmount + (comp.isFrozen ? 1.5f : (comp.isStopped ? 1 : 0)) + (self.characterBody.HasBuff(RoR2Content.Buffs.Weak) ? .4f : 0); //shoutout to rex weaken being the only one done differently

                if (mult > 1 && self.characterBody.HasBuff(drownBuff))
                {
                    Debug.Log("removing DOT from CheckFrozenState");
                    var dotCtrl = DotController.FindDotController(self.gameObject);
                    if (dotCtrl)
                    {
                        for (int i = 0; i < dotCtrl.dotStackList.Count; ++i)
                        {
                            if (dotCtrl.dotStackList[i].dotIndex == drownDotIndex)
                            {
                                dotCtrl.RemoveDotStackAtServer(i);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("Should keep dot");
                }
            }
        }

        private void wawa(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            //Debug.Log("damagetype " + damageInfo.damageType + " | " + damageInfo.dotIndex);
            if (damageInfo.dotIndex == drownDotIndex) {
                float mult;
                var comp = self.body.gameObject.GetComponent<CorrosiveCounter>();
                if (comp) {
                    Debug.Log("comp.isFrozen: " + comp.isFrozen);
                    mult = comp.slowAmount + (comp.isFrozen ? 1.5f : (comp.isStopped ? 1 : 0)) + (self.body.HasBuff(RoR2Content.Buffs.Weak) ? .4f : 0); //shoutout to rex weaken being the only one done differently
                    
                    if (mult > 1){
                        Debug.Log("update damage");
                        //self.RemoveDotStackAtServer()
                        var cb = comp.recentPlayer;
                        var count = cb.inventory.GetItemCount(ItemDef);
                        // Debug.Log("The j: " + baseDamageDot.Value * attacker.damage + (stackingDamageDot.Value * (count - 1)));
                        damageInfo.damage = mult * (baseDamageDot.Value * cb.damage + (stackingDamageDot.Value * (count - 1)));
                        //dotStack.AddModdedDamageType(drownDamage);
                    }
                    else
                    {
                        Debug.Log("Removing DOT from TakeDamage");
                        if (self.body.HasBuff(drownBuff))
                        {
                            var dotCtrl = DotController.FindDotController(self.gameObject);
                            if (dotCtrl)
                            {
                                for (int i = 0; i < dotCtrl.dotStackList.Count; ++i)
                                {
                                    if (dotCtrl.dotStackList[i].dotIndex == drownDotIndex)
                                    {
                                        dotCtrl.RemoveDotStackAtServer(i);
                                        break;
                                    }
                                }
                            }
                        }
                        damageInfo.damage = 0;
                        damageInfo.rejected = true;


                    }
                    //self.victimBody.gameObject.GetComponent<SetStateOnHurt>().targetStateMachine
                }
            }
            orig(self, damageInfo);
        }

        private void CheckSlowAmount2(ILContext il){
            ILCursor c = new ILCursor(il);

            bool ILFound = c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0), //searches for the end of the recalc stats function
                x => x.MatchLdcI4(0),
                x => x.MatchStfld("RoR2.CharacterBody", "statsDirty") //final line before the ret
            );
            
            if (ILFound)
            {
                c.Emit(OpCodes.Ldloc, 86); //emits the slow variable, currently num72
                c.Emit(OpCodes.Ldarg_0); //emits characterbody for the token
                c.EmitDelegate<Action<float, CharacterBody>>((decrease, self) => { //eats the emitted variables with no return
                    var token = self.gameObject.GetComponent<CorrosiveCounter>();
                    if (!token){
                        token = self.gameObject.AddComponent<CorrosiveCounter>();
                    }
                    //Debug.Log("increase: " + );
                    //Debug.Log("decrease:" + decrease);
                    token.slowAmount = decrease + (self.HasBuff(RoR2Content.Buffs.Weak) ? .6f : 0);

                    if(self.moveSpeed == 0 && self.acceleration == 80){
                        token.isStopped = true;
                    }else{
                        token.isStopped = false;
                    }

                    token.isFrozen = self.healthComponent.isInFrozenState;
                    Debug.Log("RecalcStats : slowamount:" + token.slowAmount + " | " + token.isStopped + " | " + token.isFrozen);

                    if (token.recentPlayer && token.recentPlayer.inventory.GetItemCount(ItemDef) > 0){
                        if (!self.HasBuff(drownBuff)){
                            if(token.slowAmount > 1 || token.isFrozen || token.isStopped){
                                Debug.Log("applying DOT from stack gain");
                                var dotInfo = new InflictDotInfo {
                                    attackerObject = token.recentPlayer.gameObject,
                                    victimObject = self.gameObject,
                                    damageMultiplier = 1f,
                                    dotIndex = drownDotIndex,
                                    duration = Mathf.Infinity,
                                };
                                DotController.InflictDot(ref dotInfo);
                            }
                        }
                    }

                    if (token.slowAmount == 1 && !token.isStopped && !token.isFrozen){
                        var dotCtrl = DotController.FindDotController(self.gameObject);
                        if (dotCtrl)
                        {
                            for (int i = 0; i < dotCtrl.dotStackList.Count; ++i)
                            {
                                if (dotCtrl.dotStackList[i].dotIndex == drownDotIndex)
                                {
                                    dotCtrl.RemoveDotStackAtServer(i);
                                    break;
                                }
                            }
                        }
                    }
                    //Debug.Log("slow amount: " + slowAmount + " | " + self);
                });
            } else { Debug.Log("ah fuck (corrosive)"); }
        }

        private void ApplySlowDot(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo){
            orig(self, damageInfo);
            if (damageInfo.attacker){
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

                if(attackerBody && attackerBody.inventory){
                    var stackCount = GetCount(attackerBody);
                    Debug.Log("exists");
                    if(stackCount > 0){
                        var comp = self.body.gameObject.GetComponent<CorrosiveCounter>();
                        if (comp)
                        {
                            comp.isFrozen = self.isInFrozenState;
                            Debug.Log("TakeDamage : is slowamount > 1?: " + (comp.slowAmount > 1) + " | is new count greater than old count?: " + (GetCount(comp.recentPlayer) < GetCount(attackerBody)) + " | stopped: " + comp.isStopped + " | slowamnt: " + comp.slowAmount + " | frozen: " + comp.isFrozen);
                            comp.recentPlayer = attackerBody;
                        }
                        if (comp && (comp.slowAmount > 1 || GetCount(comp.recentPlayer) < GetCount(attackerBody) || comp.isStopped)){
                            if(GetCount(comp.recentPlayer) < GetCount(attackerBody)){
                                comp.recentPlayer = attackerBody;
                            }
                        }
                    }
                }
            }
        }
    }

    public class CorrosiveCounter : MonoBehaviour
    {
        public float slowAmount = 1;
        public float slowAmountCurrent = 1;

        public bool isStopped = false;
        public bool isFrozen = false;
        public CharacterBody highestCountPlayer;

        public CharacterBody recentPlayer;
        public CharacterBody currentPlayer;

    }

    public class CorrosiveToken : MonoBehaviour
    {
        public CharacterBody body;
    }
}