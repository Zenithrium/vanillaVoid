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

        public override string ItemFullDescription => $"Slow effects apply drown, dealing <style=cIsDamage>{baseDamageDot.Value * 100}%</style> (+{stackingDamageDot.Value * 100}% per stack) damage per 10% slow. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"The horngus of a dongfish is attached by a scungle to a kind of dillsack (the nutte sac).";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCoreFinalPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("coreIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.Damage };

        public BuffDef drownBuff { get; private set; }
        public DotController.DotIndex drownDotIndex;
        ModdedDamageType drownDamage;

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            CreateBuff();

            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            Hooks();
        }

        public void CreateBuff()
        {
            drownDamage = ReserveDamageType();
            drownBuff = ScriptableObject.CreateInstance<BuffDef>();
            drownBuff.buffColor = Color.magenta;
            drownBuff.canStack = false;
            drownBuff.isDebuff = false;
            drownBuff.name = "DmVV" + "drownDot";
            drownBuff.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("drownDot");
            ContentAddition.AddBuffDef(drownBuff);

            DotAPI.CustomDotBehaviour drownDotBehavior = DrownDotBehavior;
            drownDotIndex = DotAPI.RegisterDotDef(0.2f, (1 * 0.2f), DamageColorIndex.Void, drownBuff, DrownDotBehavior, null);
        }

        public void DrownDotBehavior(DotController self, DotController.DotStack dotStack)
        {
            if (dotStack.dotIndex == drownDotIndex){
                CharacterBody attacker = dotStack.attackerObject.GetComponent<CharacterBody>();
                int count = 1;
                if (attacker.inventory){
                    count = GetCount(attacker);
                }

                float mult = 0;
                var comp = self.victimBody.gameObject.GetComponent<CorrosiveCounter>();
                if (comp){
                    mult = comp.slowAmount;
                }

                if(mult > 1){
                    Debug.Log("Removing old debuff");

                    for(int i = 0; i < self.dotStackList.Count; ++i){
                        if(self.dotStackList[i].dotIndex == drownDotIndex){
                            self.RemoveDotStackAtServer(i);
                            break;
                        }
                    }
                    //self.RemoveDotStackAtServer()

                    Debug.Log("The j: " + baseDamageDot.Value * attacker.damage + (stackingDamageDot.Value * (count - 1)));
                    dotStack.damage = mult * (baseDamageDot.Value * attacker.damage + (stackingDamageDot.Value * (count - 1)));
                    dotStack.AddModdedDamageType(drownDamage);
                }
                else
                {
                    Debug.Log("Mult is not high enough");
                }

                //float baseDotDamage = self.victimBody.maxHealth * burnDamagePercent.Value / 100f / burnDamageDuration.Value * myDotDef.interval;
                //float dotDamage = Math.Max(burnDamageMin.Value * attackerCharacterBody.damage, Math.Min(burnDamageMax.Value * attackerCharacterBody.damage, baseDotDamage)) / burnDamageDuration.Value * inventoryCount;
                //dotStack.damage = dotDamage;
            }
        }


        public override void CreateConfig(ConfigFile config)
        {
            baseDamageDot = config.Bind<float>("Item: " + ItemName, "Base Dot Damage", .3f, "Adjust the percent of base damage drown does per 10% slow.");
            stackingDamageDot = config.Bind<float>("Item: " + ItemName, "Stacking Dot Damage", .3f, "Adjust the damage percent drown does per 10% slow per stack.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "StrengthenBurn", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {

            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCoreDisplay.prefab");
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
                    localScale = new Vector3(1f, 1f, 1f)
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

            //Modded Chars 
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
            IL.RoR2.CharacterBody.RecalculateStats += CheckSlowAmount;
            On.RoR2.HealthComponent.TakeDamage += ApplySlowDot;

            On.RoR2.CharacterBody.OnBuffFinalStackLost += LostUpdateSlow;
            On.RoR2.CharacterBody.OnBuffFirstStackGained += GainUpdateSlow;
        }

        private void GainUpdateSlow(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);

            //CharacterBody victimBody = self.body;
            //if ()
            //{
            //    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            //
            //    if (attackerBody && attackerBody.inventory)
            //    {
            //        var stackCount = GetCount(attackerBody);
            //
            //        if (stackCount > 0)
            //        {
            //            var comp = self.body.gameObject.GetComponent<CorrosiveCounter>();
            //            if (comp.slowAmount != comp.slowAmountCurrent)
            //            {
            //                comp.slowAmountCurrent = comp.slowAmount;
            //                var dotInfo = new InflictDotInfo
            //                {
            //                    attackerObject = damageInfo.attacker,
            //                    victimObject = self.body.gameObject,
            //                    damageMultiplier = 1f,
            //                    dotIndex = drownDotIndex,
            //                    duration = 999999
            //                };
            //
            //                DotController.InflictDot(ref dotInfo);
            //            }
            //        }
            //    }
            //}
        }

        private void LostUpdateSlow(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            var token = self.gameObject.GetComponent<CorrosiveCounter>();
            if (token){
                Debug.Log("Before " + token.slowAmount);
            }

            orig(self, buffDef);

            if (token){
                Debug.Log("After " + token.slowAmount);
            }
        }

        private void CheckSlowAmount(ILContext il){
            ILCursor c = new ILCursor(il);

            bool ILFound = c.TryGotoNext(MoveType.Before,
            x => x.MatchLdloc(76),
            x => x.MatchLdloc(44),
            x => x.MatchConvR4(),
            x => x.MatchLdcR4(1)
            );

            if (ILFound){
                float a;
                c.Index += 1;
                c.Emit(OpCodes.Ldloc, 76);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<float, CharacterBody>>((slowAmount, self) =>
                {

                    //Debug.Log("slow amount: " + slowAmount);
                    //Debug.Log("self: " + self);
                    var token = self.gameObject.GetComponent<CorrosiveCounter>();
                    if (!token){
                        token = self.gameObject.AddComponent<CorrosiveCounter>();
                    }
                    token.slowAmount = slowAmount;

                    if(token.slowAmount == 1)
                    {
                        var dotCtrl = DotController.FindDotController(self.gameObject);
                        if (dotCtrl){
                            for (int i = 0; i < dotCtrl.dotStackList.Count; ++i){
                                if (dotCtrl.dotStackList[i].dotIndex == drownDotIndex){
                                    dotCtrl.RemoveDotStackAtServer(i);
                                    break;
                                }
                            }
                        }
                    }
                    Debug.Log("slow amount: " + slowAmount + " | " + self);
                });
            }else{
                Debug.Log("ah fuck");
            }
        }


        private void ApplySlowDot(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);

            if (damageInfo.attacker){
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

                if(attackerBody && attackerBody.inventory){
                    var stackCount = GetCount(attackerBody);

                    if(stackCount > 0){
                        var comp = self.body.gameObject.GetComponent<CorrosiveCounter>();
                        
                        if (comp && (comp.slowAmount != comp.slowAmountCurrent || GetCount(comp.recentPlayer) < GetCount(attackerBody)))
                        {
                            if(GetCount(comp.recentPlayer) < GetCount(attackerBody)){
                                comp.recentPlayer = attackerBody;
                            }

                            comp.slowAmountCurrent = comp.slowAmount;

                            var dotCtrl = DotController.FindDotController(self.gameObject);
                            if (dotCtrl){
                                for (int i = 0; i < dotCtrl.dotStackList.Count; ++i){
                                    if (dotCtrl.dotStackList[i].dotIndex == drownDotIndex){
                                        dotCtrl.RemoveDotStackAtServer(i);
                                        break;
                                    }
                                }
                            }

                            var dotInfo = new InflictDotInfo{
                                attackerObject = comp.recentPlayer.gameObject,
                                victimObject = self.body.gameObject,
                                damageMultiplier = 1f,
                                dotIndex = drownDotIndex,
                                duration = 999999
                            };

                            DotController.InflictDot(ref dotInfo);
                        }
                    }
                }
            }
            //float initialDmg = damageInfo.damage;
            //float mult = 0;
            //bool adjusted = false;
            //
            //if (damageInfo.attacker && damageInfo.attacker.GetComponent<CharacterBody>()){
            //    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            //    if (attackerBody.inventory){
            //        var stackCount = GetCount(attackerBody);
            //
            //        if (stackCount > 0){
            //            var healthFraction = Mathf.Clamp((1 - self.combinedHealthFraction), 0f, 1f);
            //            mult = healthFraction * (baseDamageBuff.Value + (stackingBuff.Value * (stackCount - 1)));
            //
            //            damageInfo.damage *= (1 + mult);
            //            float maxDamage = initialDmg + (initialDmg * (baseDamageBuff.Value + (stackingBuff.Value * (stackCount - 1))));
            //
            //            damageInfo.damage = Mathf.Min(damageInfo.damage, maxDamage);
            //            adjusted = true;
            //        }
            //    }
            //}
            //
            //if (adjusted){
            //    damageInfo.damage /= (1 + mult);
            //    //damageInfo.damage = initialDmg; //this also works
            //}

        }
    }


    public class CorrosiveCounter : MonoBehaviour
    {
        public float slowAmount = 1;
        public float slowAmountCurrent = 1;

        public CharacterBody highestCountPlayer;

        public CharacterBody recentPlayer;
        public CharacterBody currentPlayer;

    }

}