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

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCorePickupAnimPickup.prefab");

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

            var mpp = ItemModel.AddComponent<ModelPanelParameters>();
            mpp.focusPointTransform = ItemModel.transform.Find("Target");
            mpp.cameraPositionTransform = ItemModel.transform.Find("Source");
            mpp.minDistance = 4f;
            mpp.maxDistance = 8f;
            mpp.modelRotation = Quaternion.Euler(new Vector3(0, 0, 0));

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00118F, 0.04348F, 0.18291F),
                    localAngles = new Vector3(0F, 0F, 269.2369F),
                    localScale = new Vector3(0.05F, 0.05F, 0.05F)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfR",
                    localPos = new Vector3(0.12266F, 0.26412F, 0.01867F),
                    localAngles = new Vector3(356.7391F, 359.4713F, 190.1265F),
                    localScale = new Vector3(0.0475F, 0.0475F, 0.0475F)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MainWeapon",
                    localPos = new Vector3(-0.1446F, 0.54198F, 0.00309F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(0.0325F, 0.0325F, 0.0325F)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hip",
                    localPos = new Vector3(1.19634F, -0.13362F, 0.00001F),
                    localAngles = new Vector3(270F, 0.00001F, 0F),
                    localScale = new Vector3(0.51F, 0.51F, 0.51F)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.00053F, 0.11487F, -0.37182F),
                    localAngles = new Vector3(0F, 0F, 269.7339F),
                    localScale = new Vector3(0.065F, 0.065F, 0.065F)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00023F, 0.08593F, -0.45226F),
                    localAngles = new Vector3(88.47248F, 185.7477F, 95.74977F),
                    localScale = new Vector3(0.225F, 0.225F, 0.225F)
            
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
                    childName = "Chest",
                    localPos = new Vector3(-0.00165F, 0.08679F, -0.35124F),
                    localAngles = new Vector3(9.53182F, 0F, 0F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                }
            
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0.00128F, 0.02637F, 0.15752F),
                    localAngles = new Vector3(44.84497F, 359.7617F, 89.66206F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfBackR",
                    localPos = new Vector3(-0.00005F, 0.73238F, -0.29824F),
                    localAngles = new Vector3(0F, 17.8037F, 0F),
                    localScale = new Vector3(0.15F, 0.15F, 0.15F)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MechBase",
                    localPos = new Vector3(-0.00032F, -0.0516F, 0.44887F),
                    localAngles = new Vector3(334.9733F, 179.8491F, 270.3567F),
                    localScale = new Vector3(0.065F, 0.065F, 0.065F)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hip",
                    localPos = new Vector3(0F, 0.35496F, -1.62809F),
                    localAngles = new Vector3(10.25407F, 358.7614F, 89.98595F),
                    localScale = new Vector3(0.55F, 0.55F, 0.55F)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(-0.12273F, 0.25494F, 0.07167F),
                    localAngles = new Vector3(3.71931F, 85.17051F, 169.2675F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(-0.30914F, -0.19114F, -0.09036F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CannonEnd",
                    localPos = new Vector3(0.08971F, -0.02901F, 0.00486F),
                    localAngles = new Vector3(0.45401F, 0.02018F, 2.54483F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                }
            });
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.17851F, -0.07656F, -0.01408F),
                    localAngles = new Vector3(318.7987F, 255.6058F, 291.2871F),
                    localScale = new Vector3(0.055F, 0.055F, 0.055F)
                }
            });
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.37335F, -0.16081F, 0.13103F),
                    localAngles = new Vector3(52.63165F, 181.2988F, 91.45337F),
                    localScale = new Vector3(0.0625F, 0.0625F, 0.0625F)
                }
            });
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.41929F, 0.17111F, 0.1431F),
                    localAngles = new Vector3(12.21397F, 1.90596F, 318.4379F),
                    localScale = new Vector3(0.06F, 0.06F, 0.06F)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(-3.38261F, 13.41271F, 3.48508F),
                    localAngles = new Vector3(359.5338F, 51.73492F, 359.3961F),
                    localScale = new Vector3(1.5F, 1.5F, 1.5F)
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
                    childName = "ThighR",
                    localPos = new Vector3(-0.1359F, 0.24248F, 0.21037F),
                    localAngles = new Vector3(4.67129F, 0F, 180F),
                    localScale = new Vector3(0.05F, 0.05F, 0.05F)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hammer",
                    localPos = new Vector3(-0.00018F, -0.05274F, -0.00007F),
                    localAngles = new Vector3(0.84823F, 180F, 180F),
                    localScale = new Vector3(0.0009F, 0.0009F, 0.0009F)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(-0.24608F, 0.27776F, 0.04725F),
                    localAngles = new Vector3(357.6129F, 217.238F, 186.1909F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
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
                    childName = "Chest",
                    localPos = new Vector3(0F, 0.00255F, 0.00235F),
                    localAngles = new Vector3(0F, 0F, 268.9755F),
                    localScale = new Vector3(0.0005F, 0.0005F, 0.0005F)
                }
            });
            rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(0.12276F, 0.10388F, 0F),
                    localAngles = new Vector3(359.0975F, 180F, 180F),
                    localScale = new Vector3(0.05F, 0.05F, 0.05F)
                }
            });
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfR",
                    localPos = new Vector3(-0.16596F, 0.25509F, 0.00001F),
                    localAngles = new Vector3(0.01331F, 180F, 180F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "RightThigh",
                    localPos = new Vector3(0.00001F, 0.15492F, 0.12487F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(0.035F, 0.035F, 0.035F)
                }
            });
            //rules.Add("JavangleHouse", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "LeftForearm",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Stomach",
                    localPos = new Vector3(-0.04137F, 0.13049F, -0.0893F),
                    localAngles = new Vector3(335.8853F, 187.695F, 316.1041F),
                    localScale = new Vector3(0.045F, 0.045F, 0.045F)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.07563F, 0.21529F, -0.25773F),
                    localAngles = new Vector3(2.56352F, 357.1147F, 311.5867F),
                    localScale = new Vector3(0.055F, 0.055F, 0.055F)
                }
            });
            rules.Add("mdlMorris", new RoR2.ItemDisplayRule[]
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
                    childName = "Chest",
                    localPos = new Vector3(-0.00001F, -0.28992F, -1.38041F),
                    localAngles = new Vector3(0F, 0F, 90F),
                    localScale = new Vector3(0.275F, 0.275F, 0.275F)
                }
            });
            rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.1509F, -0.21049F, 0.30265F),
                    localAngles = new Vector3(358.929F, 0F, 0F),
                    localScale = new Vector3(0.06F, 0.06F, 0.06F)
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
            //rules.Add("mdlRMOR", new RoR2.ItemDisplayRule[]
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
                    childName = "leg_bone2.L",
                    localPos = new Vector3(-0.32678F, 0.17556F, 0.04668F),
                    localAngles = new Vector3(0.43776F, 27.16183F, 172.7489F),
                    localScale = new Vector3(0.09F, 0.09F, 0.09F)
                }
            });
            rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Gun",
                    localPos = new Vector3(0.26972F, 0.20634F, 0.00223F),
                    localAngles = new Vector3(1.66128F, 358.5773F, 319.4146F),
                    localScale = new Vector3(0.015F, 0.015F, 0.015F)
                }
            });
            rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.01177F, -0.14901F, -1.0662F),
                    localAngles = new Vector3(0F, 0F, 270F),
                    localScale = new Vector3(0.25F, 0.25F, 0.25F)
                }
            });
            rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.11027F, 0.01322F, -0.27688F),
                    localAngles = new Vector3(0F, 0F, 343.842F),
                    localScale = new Vector3(0.065F, 0.065F, 0.065F)
                }
            });
            rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0.29325F, 0.18803F, 0.00011F),
                    localAngles = new Vector3(12.3589F, 0F, 3.86741F),
                    localScale = new Vector3(0.1125F, 0.1125F, 0.1125F)
                }
            });
            rules.Add("RobDriverBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00118F, 0.04348F, 0.18291F),
                    localAngles = new Vector3(0F, 0F, 269.2369F),
                    localScale = new Vector3(0.05F, 0.05F, 0.05F)
                }
            });
            rules.Add("RobRavagerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00118F, 0.04348F, 0.18291F),
                    localAngles = new Vector3(0F, 0F, 269.2369F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                }
            });
            rules.Add("mdlTeslaTrooper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(-0.10288F, 0.26028F, 0.09561F),
                    localAngles = new Vector3(334.7432F, 182.6401F, 206.0909F),
                    localScale = new Vector3(0.05F, 0.05F, 0.05F)
                }
            });
            rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(-0.20642F, 0.322F, 0.07124F),
                    localAngles = new Vector3(323.9191F, 181.3133F, 188.4952F),
                    localScale = new Vector3(0.05F, 0.05F, 0.05F)
                }
            });
            rules.Add("RA2ChronoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfR",
                    localPos = new Vector3(0.0069F, 0.22536F, -0.22845F),
                    localAngles = new Vector3(2.5693F, 359.2332F, 163.3763F),
                    localScale = new Vector3(0.09F, 0.09F, 0.09F)
                }
            });
            rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.01147F, 0.20373F, -0.52839F),
                    localAngles = new Vector3(4.71942F, 0F, 0F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                }
            });
            //rules.Add("BastianBody", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlAmp", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlNemAmp", new RoR2.ItemDisplayRule[]
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
            return rules;
        }

        public override void Hooks()
        {
            IL.RoR2.CharacterBody.RecalculateStats += CheckSlowAmount2; //HUGE
            On.RoR2.HealthComponent.TakeDamage += ApplySlowDot;

            On.RoR2.HealthComponent.TakeDamage += wawa;

            On.EntityStates.FrozenState.OnEnter += CheckFrozenState;
            On.EntityStates.FrozenState.OnExit += RemoveFrozenState;
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
                    //Debug.Log("applying DOT from CheckFrozenState");
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
                else if(!(mult > 1) && self.characterBody.HasBuff(drownBuff))
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
                    //Debug.Log("removing DOT from CheckFrozenState");
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
                    //Debug.Log("Should keep dot");
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
                    //Debug.Log("comp.isFrozen: " + comp.isFrozen);
                    mult = comp.slowAmount + (comp.isFrozen ? 1.5f : (comp.isStopped ? 1 : 0)) + (self.body.HasBuff(RoR2Content.Buffs.Weak) ? .4f : 0); //shoutout to rex weaken being the only one done differently
                    
                    if (mult > 1){
                        //Debug.Log("update damage");
                        //self.RemoveDotStackAtServer()
                        var cb = comp.recentPlayer;
                        var count = cb.inventory.GetItemCount(ItemDef);
                        // Debug.Log("The j: " + baseDamageDot.Value * attacker.damage + (stackingDamageDot.Value * (count - 1)));
                        damageInfo.damage = mult * (baseDamageDot.Value * cb.damage + (stackingDamageDot.Value * (count - 1)));
                        //dotStack.AddModdedDamageType(drownDamage);
                    }
                    else
                    {
                        //Debug.Log("Removing DOT from TakeDamage");
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
                    //Debug.Log("RecalcStats : slowamount:" + token.slowAmount + " | " + token.isStopped + " | " + token.isFrozen);

                    if (token.recentPlayer && token.recentPlayer.inventory.GetItemCount(ItemDef) > 0){
                        if (!self.HasBuff(drownBuff)){
                            if(token.slowAmount > 1 || token.isFrozen || token.isStopped){
                                //Debug.Log("applying DOT from stack gain");
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
                    if(stackCount > 0){
                        var comp = self.body.gameObject.GetComponent<CorrosiveCounter>();
                        if (comp)
                        {
                            comp.isFrozen = self.isInFrozenState;
                            //Debug.Log("TakeDamage : is slowamount > 1?: " + (comp.slowAmount > 1) + " | is new count greater than old count?: " + (GetCount(comp.recentPlayer) < GetCount(attackerBody)) + " | stopped: " + comp.isStopped + " | slowamnt: " + comp.slowAmount + " | frozen: " + comp.isFrozen);
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