﻿using BepInEx.Configuration;
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
using System.Collections;
using RoR2.Projectile;

namespace vanillaVoid.Items
{
    public class ExeBlade : ItemBase<ExeBlade>
    {
        public ConfigEntry<float> additionalProcs;

        public ConfigEntry<float> deathDelay;

        public ConfigEntry<float> additionalDuration;

        public ConfigEntry<float> baseDamageAOEExe;

        public ConfigEntry<float> aoeRangeBaseExe;

        public ConfigEntry<float> bladeCoefficient;

        //public ConfigEntry<bool> enableOnDeathDamage;

        //public ConfigEntry<float> aoeRangeStackingExe;

        public override string ItemName => "Executioner's Burden";

        public override string ItemLangTokenName => "EXEBLADE_ITEM";

        public override string ItemPickupDesc => $"Your 'On-Kill' effects occur an additional time upon killing an elite." + (aoeRangeBaseExe.Value != 0 && baseDamageAOEExe.Value != 0 ? $" Additionally causes a damaging AOE upon elite kill." : "") + $" <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"Your <style=cIsDamage>On-Kill</style> effects occur <style=cIsDamage>{additionalProcs.Value}</style>" + (additionalProcs.Value != 0 ? $" <style=cStack>(+{additionalProcs.Value} per stack)</style>" : "") + $" additional times upon killing an elite." + (aoeRangeBaseExe.Value != 0 && baseDamageAOEExe.Value != 0 ? $" Additionally causes a <style=cIsDamage>{aoeRangeBaseExe.Value}m</style> explosion, dealing <style=cIsDamage>{baseDamageAOEExe.Value * 100}%</style> base damage." : "") + $" <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"<style=cMono>//-- AUTO-TRANSCRIPTION FROM CARGO BAY 14 OF UES [Redacted] --//</style>" +
            "\n\n\"Hey Joe, how are things g....what is all that. Why do you have so many swords.\"" +
            "\n\n\"Oh hi! Remember when I started, uh, trading with the void? Found a new candidate. Those rusty guillitones? Void loves 'em.\"" +
            "\n\n\"I..sure. Wait a sec, how many guillotines did you have?\"" +
            "\n\n\"...enough? Why's it matter?\"\n\n\"No no it's just that like, how are you supposed to use like, more than one of these things? Doesn't seem as applicable than having a guillotine strapped to everything.\"" +
            "\n\n\"That doesn't make any sense either though! Why did that ship have so many [REDACTED] guillotines anyway!\"";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlBladePickupFinal.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("bladeIcon512.png");

        public static GameObject Projectile;

        public static GameObject ItemBodyModelPrefab;

        private static readonly SphereSearch exeBladeSphereSearch = new SphereSearch();
        private static readonly List<HurtBox> exeBladeHurtBoxBuffer = new List<HurtBox>();

        public static GameObject bladeObject;
        //string tempItemPickupDesc;
        //string tempItemFullDescription;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Damage, ItemTag.AIBlacklist };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            //VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            CreateObject();
            Hooks(); 
        }

        public override void CreateConfig(ConfigFile config)
        {
            string name = ItemName == "Executioner's Burden" ? "Executioners Burden" : ItemName;

            //luckBonus = config.Bind<float>("Item: " + name, "Luck Bonus", 1f, "Adjust the luck added to the hit that kills an elite.");
            additionalProcs = config.Bind<float>("Item: " + name, "Number of Addtional Procs", 1f, "Adjust the number of additional times on kill effects occur per stack.");
            deathDelay = config.Bind<float>("Item: " + name, "Time between Extra Procs", .3f, "Adjust the amount of time between each additional on-kill proc.");
            additionalDuration = config.Bind<float>("Item: " + name, "Additional Duration", 2.5f, "Adjust the amount of time the sword exists after the on-kill procs are finished.");

            //enableOnDeathDamage = config.Bind<bool>("Item: " + name, "Enable Damage AOE", true, "Enable or disable the additional AOE without having to set the next two configs to zero. ");
            baseDamageAOEExe = config.Bind<float>("Item: " + name, "Percent Base Damage", 1f, "Adjust the percent base damage the AOE does.");
            aoeRangeBaseExe = config.Bind<float>("Item: " + name, "Range of AOE", 12f, "Adjust the range of the damaging AOE on the first stack.");
            bladeCoefficient = config.Bind<float>("Item: " + name, "Proc Coefficient", .5f, "Adjust the proc coefficient for the item's damage AOE. (0 is no procs, 1 is normal proc rate)");

            voidPair = config.Bind<string>("Item: " + name, "Item to Corrupt", "ExecuteLowHealthElite", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlBladeDisplay.prefab");
            //string orbTransp = "RoR2/DLC1/voidraid/matVoidRaidPlanetPurpleWave.mat"; 
            //string orbCore = "RoR2/DLC1/voidstage/matVoidCoralPlatformPurple.mat";

            //string orbTransp = "RoR2/DLC1/voidraid/matVoidRaidPlanetPurpleWave.mat";
            //string orbCore = "RoR2/DLC1/voidstage/matVoidFoam.mat";
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
            mpp.minDistance = 3f;
            mpp.maxDistance = 6f;
            mpp.modelRotation = Quaternion.Euler(new Vector3(0, 0, 0));

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.05684097f, 0.3799726f, -0.1878726f),
                    localAngles = new Vector3(13.62447f, 90.2953f, 38.50036f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1555227f, 0.1579392f, -0.06899721f),
                    localAngles = new Vector3(2.563096f, 34.56156f, 7.501457f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0.1920169f, -0.04241098f, -0.02298578f),
                    localAngles = new Vector3(60.66765f, 200.7879f, 26.70174f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(1.890986f, 0.6897769f, -1.422848f),
                    localAngles = new Vector3(331.613f, 356.0543f, 7.945525f),
                    localScale = new Vector3(.5f, .5f, .5f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00722686f, 0.3719998f, -0.2702792f),
                    localAngles = new Vector3(349.887f, 86.67003f, 27.15041f),
                    localScale = new Vector3(0.175f, 0.175f, 0.175f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.1254412f, 0.02308899f, 0.1981006f),
                    localAngles = new Vector3(62.12778f, 82.70875f, 358.3321f),
                    localScale = new Vector3(.25f, .25f, .25f)

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
                    localPos = new Vector3(-0.01370448f, 0.1987826f, -0.2920466f),
                    localAngles = new Vector3(354.5892f, 82.3866f, 18.983f),
                    localScale = new Vector3(.12f, .12f, .12f)
                }
                
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.04337082f, -0.0548192f, -0.174685f),
                    localAngles = new Vector3(55.62988f, 21.04336f, 295.0692f),
                    localScale = new Vector3(0.115f, 0.115f, 0.115f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "FootBackR",
                    localPos = new Vector3(-0.0001340785f, 0.4513009f, -0.08311531f),
                    localAngles = new Vector3(12.71165f, 95.05572f, 190.8498f),
                    localScale = new Vector3(0.2f, 0.2f, 0.2f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.002651015f, -0.04257223f, -0.3807302f),
                    localAngles = new Vector3(349.9466f, 84.35776f, 18.38718f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.0140248f, -0.6587424f, 4.050778f),
                    localAngles = new Vector3(344.4937f, 89.13557f, 32.49933f),
                    localScale = new Vector3(1.1f, 1.1f, 1.1f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.007421299f, 0.0434005f, -0.2499133f),
                    localAngles = new Vector3(347.7402f, 95.98297f, 4.725613f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "TopRail",
                    localPos = new Vector3(-0.0006022891f, 0.6622809f, 0.02868086f),
                    localAngles = new Vector3(11.65705f, 357.5795f, 186.7869f),
                    localScale = new Vector3(0.125f, 0.125f, 0.125f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmR",
                    localPos = new Vector3(0.2549843f, 0.3083106f, -0.1155708f),
                    localAngles = new Vector3(5.404367f, 219.7214f, 190.5286f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.01045F, 0.23457F, -0.19368F),
                    localAngles = new Vector3(305.1918F, 73.05267F, 23.90182F),
                    localScale = new Vector3(0.11F, 0.11F, 0.11F)
                }
            });
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.28857F, -0.36503F, -0.00765F),
                    localAngles = new Vector3(79.21353F, 186.0065F, 359.0951F),
                    localScale = new Vector3(0.15F, 0.15F, 0.15F)
                }
            });
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.4311F, 0.40855F, -0.37673F),
                    localAngles = new Vector3(331.1584F, 155.745F, 335.6807F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(-1.861564f, 13.43366f, 4.458276f),
                    localAngles = new Vector3(345.1214f, 245.0656f, 9.101425f),
                    localScale = new Vector3(2f, 2f, 2f)
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
                    localPos =   new Vector3(0.4780795f, -0.6616167f, 0.5085409f),
                    localAngles = new Vector3(327.1772f, 30.54325f, 268.9095f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos =   new Vector3(0.007075419f, 0.01237386f, 0.005717664f),
                    localAngles = new Vector3(283.9196f, 224.3569f, 144.0466f),
                    localScale = new Vector3(.01f, .009f, .01f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] //these ones don't work for some reason!
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos =   new Vector3(-0.1096776f, 0.07879563f, -0.2880353f),
                    localAngles = new Vector3(13.29552f, 102.2941f, 170.1359f),
                    localScale = new Vector3(.175f, .15f, .175f)
                }
            });
            //rules.Add("mdlCHEF", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Door",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(0f, 0f, 0f)
            //    }
            //});
            rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LegR",
                    localPos =   new Vector3(0f, 0f, 0.001307127f),
                    localAngles = new Vector3(12.1725f, 95.70362f, 186.0707f),
                    localScale = new Vector3(.0015f, .0015f, .0015f)
                }
            });
            rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.0143426f, 0.4754811f, -0.1153371f),
                    localAngles = new Vector3(352.7357f, 98.62195f, 325.0519f),
                    localScale = new Vector3(.20f, .20f, .20f)
                }
            });
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos =   new Vector3(-0.1375525f, 0.06662301f, 0.1021552f),
                    localAngles = new Vector3(19.61578f, 27.82838f, 181.4652f),
                    localScale = new Vector3(.13f, .13f, .13f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Sheath",
                    localPos =   new Vector3(0.003468025f, 0.5852519f, 0.14202f),
                    localAngles = new Vector3(14.6782f, 356.295f, 188.6961f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }
            });
            rules.Add("mdlExecutioner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.0008111957f, -0.0005103727f, -0.001211213f),
                    localAngles = new Vector3(358.2127f, 67.5713f, 333.6733f),
                    localScale = new Vector3(0.0010f, 0.0010f, 0.0010f)
                }
            });
            rules.Add("mdlNemmando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Sword",
                    localPos = new Vector3(-0.0005331703f, 0.01267182f, 0.00001262315f),
                    localAngles = new Vector3(12.08878f, 117.7268f, 185.4063f),
                    localScale = new Vector3(0.0010f, 0.0010f, 0.0010f)
                }
            });
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(0.05391715f, 0.3403379f, -0.01506396f),
                    localAngles = new Vector3(7.134398f, 180.3909f, 180.3603f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(-0.09054253f, 0.05356936f, 0.024171f),
                    localAngles = new Vector3(14.07468f, 179.7917f, 192.2729f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
            });
            rules.Add("mdlHANDOverclocked", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(-0.6455446f, 0.7129821f, 0.4003462f),
                    localAngles = new Vector3(14.33627f, 32.17481f, 189.0975f),
                    localScale = new Vector3(.75f, .75f, .75f)
                }
            });
            rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Exhaust",
                    localPos = new Vector3(-0.2132205f, 0.03606546f, -0.07759695f),
                    localAngles = new Vector3(295.5585f, 86.91768f, 10.09711f),
                    localScale = new Vector3(.2f, .2f, .2f)
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
                    childName = "LowerArmL",
                    localPos = new Vector3(0.02459F, 1.62932F, 1.02468F),
                    localAngles = new Vector3(13.42376F, 91.81984F, 187.1991F),
                    localScale = new Vector3(0.9F, 0.9F, 0.9F)
                }
            });
            //rules.Add("Spearman", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "chest",
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
                    childName = "stomach_bone",
                    localPos = new Vector3(0.48736F, 0.40121F, -0.21725F),
                    localAngles = new Vector3(357.6963F, 179.8225F, 15.71513F),
                    localScale = new Vector3(0.25F, 0.25F, 0.25F)
                }
            });
            rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandR",
                    localPos = new Vector3(-0.00259F, 0.08872F, 0.04364F),
                    localAngles = new Vector3(6.34987F, 264.9246F, 182.9005F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                }
            });
            rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0.33938F, 0.1103F, 0.71752F),
                    localAngles = new Vector3(12.18466F, 280.9303F, 180.0076F),
                    localScale = new Vector3(0.35F, 0.35F, 0.35F)
                }
            });
            rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Knife",
                    localPos = new Vector3(0.007854097f, 0.2381452f, -0.0714934f),
                    localAngles = new Vector3(11.66264f, 359.8306f, 188.8765f),
                    localScale = new Vector3(.05f, .05f, .05f)
                }
            });
            rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperLegB",
                    localPos = new Vector3(-0.20212F, 0.15023F, 0.24092F),
                    localAngles = new Vector3(352.1156F, 7.08063F, 190.1481F),
                    localScale = new Vector3(0.2F, 0.2F, 0.2F)
                }
            });
            //rules.Add("RobDriverBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0, 0, -0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
            rules.Add("mdlTeslaTrooper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.07703F, 0.13181F, 0.00804F),
                    localAngles = new Vector3(274.092F, 299.6003F, 300.7438F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.04088F, 0.1637F, 0.0234F),
                    localAngles = new Vector3(46.89203F, 110.4791F, 83.16012F),
                    localScale = new Vector3(0.105F, 0.105F, 0.105F)
                }
            });
            rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.04748F, 0.45498F, -0.12995F),
                    localAngles = new Vector3(14.24262F, 182.636F, 359.6417F),
                    localScale = new Vector3(0.085F, 0.085F, 0.085F)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.05034F, 0.45041F, -0.13329F),
                    localAngles = new Vector3(14.24262F, 182.636F, 22.40773F),
                    localScale = new Vector3(0.085F, 0.085F, 0.085F)
                }
            });


            rules.Add("RA2ChronoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "weapon_base",
                    localPos = new Vector3(-0.50457F, 0.1861F, -0.32807F),
                    localAngles = new Vector3(43.41581F, 190.6077F, 212.5559F),
                    localScale = new Vector3(0.125F, 0.125F, 0.125F)
                }
            });
            rules.Add("RobRavagerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.05684097f, 0.3799726f, -0.1878726f),
                    localAngles = new Vector3(13.62447f, 90.2953f, 38.50036f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlMorris", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfR",
                    localPos = new Vector3(0.14739F, 0.44559F, 0.00475F),
                    localAngles = new Vector3(13.38404F, 187.9133F, 181.616F),
                    localScale = new Vector3(0.175F, 0.175F, 0.175F)
                }
            });

            return rules;

        }

        public override void Hooks(){
            //On.RoR2.GlobalEventManager.OnHitEnemy += HitProcBonus;
            //On.RoR2.GlobalEventManager.OnCharacterDeath += DeathProcBonus;
            GlobalEventManager.onCharacterDeathGlobal += ExeBladeExtraDeath;
        }

        public void CreateObject(){
            bladeObject = MainAssets.LoadAsset<GameObject>("mdlBladeWorldObject.prefab");
            bladeObject.AddComponent<TeamFilter>();
            bladeObject.AddComponent<HealthComponent>();
            bladeObject.AddComponent<NetworkIdentity>();
            bladeObject.AddComponent<BoxCollider>();
            bladeObject.AddComponent<Rigidbody>();

            PrefabAPI.RegisterNetworkPrefab(bladeObject);
        }

        private void ExeBladeExtraDeath(DamageReport dmgReport){
            if (!dmgReport.attacker || !dmgReport.attackerBody || !dmgReport.victim || !dmgReport.victimBody || !dmgReport.victimIsElite){ return; } //end func if death wasn't killed by something real enough
            
            var exeComponent = dmgReport.victimBody.GetComponent<ExeToken>();
            if (exeComponent) { return; } //prevent game crash  

            if (!NetworkServer.active) { return; }

            CharacterBody victimBody = dmgReport.victimBody;
            dmgReport.victimBody.gameObject.AddComponent<ExeToken>();
            CharacterBody attackerBody = dmgReport.attackerBody;
            if (attackerBody.inventory && NetworkServer.active)
            {
                var bladeCount = attackerBody.inventory.GetItemCount(ItemBase<ExeBlade>.instance.ItemDef);
                if (bladeCount > 0){
                    var tempBlade = GameObject.Instantiate(bladeObject, victimBody.corePosition, Quaternion.Euler(0, 180, 0));

                    //Debug.Log("temp blade instantiated");
                    tempBlade.GetComponent<TeamFilter>().teamIndex = attackerBody.teamComponent.teamIndex;
                    tempBlade.transform.position = victimBody.corePosition;
                    //Debug.Log("post teamfilter");

                    var bladeRigid = tempBlade.GetComponent<Rigidbody>();
                    //Debug.Log("blade rigid got " + bladeRigid);
                    
                    var bladeCollider = bladeRigid.GetComponent<BoxCollider>(); // default size = (0.8, 4.3, 1.8)
                    //Debug.Log("bladeCollider made " + bladeCollider);
                    bladeRigid.drag = .5f;

                    float randomHeight = UnityEngine.Random.Range(2.45f, 2.95f);
                    bladeCollider.size = new Vector3(0.1f, randomHeight, 0.1f);

                    bladeRigid.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;

                    float randomX = UnityEngine.Random.Range(-20, 10);
                    float randomY = UnityEngine.Random.Range(0, 360);
                    float randomZ = UnityEngine.Random.Range(-20, 20);
                    Quaternion rotrand = Quaternion.Euler(randomX, randomY, randomZ);
                    bladeRigid.transform.SetPositionAndRotation(bladeRigid.transform.position, rotrand);

                    var token = tempBlade.AddComponent<SwordToken>();
                    token.PrepExecutions(exeBladeSphereSearch, exeBladeHurtBoxBuffer, bladeCount, dmgReport);

                    NetworkServer.Spawn(tempBlade);

                    EffectData effectData = new EffectData{ origin = victimBody.corePosition };
                    effectData.SetNetworkedObjectReference(tempBlade);
                    EffectManager.SpawnEffect(HealthComponent.AssetReferences.executeEffectPrefab, effectData, transmit: true);
                    //StartCoroutine(ExeBladeDelayedExecutions(bladeCount, tempBlade, dmgReport));
                }
            }
        }
    }

    public class ExeToken : MonoBehaviour { }

    public class SwordToken : MonoBehaviour{
        
        public void PrepExecutions(SphereSearch search, List<HurtBox> buffer, int count, DamageReport dmgRep){
            StartCoroutine(ExeBladeDelayedExecutions(search, buffer, count, this.gameObject, dmgRep));
        }

        IEnumerator ExeBladeDelayedExecutions(SphereSearch exeBladeSphereSearch, List<HurtBox> exeBladeHurtBoxBuffer, int bladeCount, GameObject bladeObject, DamageReport dmgReport){
            var damage = dmgReport.damageInfo.damage;
            var cmbHP = dmgReport.victim.combinedHealth;
            var bladeObjHPC = bladeObject.GetComponent<HealthComponent>();
            CharacterBody attackerBody = dmgReport.attackerBody;

            float effectiveRadius = ItemBase<ExeBlade>.instance.aoeRangeBaseExe.Value;
            float AOEDamageMult = ItemBase<ExeBlade>.instance.baseDamageAOEExe.Value;

            if (attackerBody){
                for (int i = 0; i < (bladeCount * ItemBase<ExeBlade>.instance.additionalProcs.Value); ++i){
                    //Debug.Log("bladeCount * ItemBase<ExeBlade>.instance.additionalProcs.Value: " + bladeCount * ItemBase<ExeBlade>.instance.additionalProcs.Value + " | " + i);
                    yield return new WaitForSeconds(ItemBase<ExeBlade>.instance.deathDelay.Value);
                    DamageInfo damageInfoDeath = new DamageInfo{
                        attacker = attackerBody.gameObject,
                        crit = attackerBody.RollCrit(),
                        damage = 1,
                        position = bladeObject.transform.position,
                        procCoefficient = ItemBase<ExeBlade>.instance.bladeCoefficient.Value,
                        damageType = DamageType.AOE,
                        damageColorIndex = DamageColorIndex.Default,
                    };

                    DamageReport damageReport = new DamageReport(damageInfoDeath, bladeObjHPC, damage, cmbHP);
                    GlobalEventManager.instance.OnCharacterDeath(damageReport);

                    EffectData effectDataPulse = new EffectData { origin = bladeObject.transform.position };
                    effectDataPulse.SetNetworkedObjectReference(bladeObject);

                    if (ItemBase<ExeBlade>.instance.aoeRangeBaseExe.Value != 0 && ItemBase<ExeBlade>.instance.baseDamageAOEExe.Value != 0){
                        EffectManager.SpawnEffect(HealthComponent.AssetReferences.executeEffectPrefab, effectDataPulse, true);
                        float AOEDamage = dmgReport.attackerBody.damage * AOEDamageMult;
                        Vector3 corePosition = bladeObject.transform.position;

                        exeBladeSphereSearch.origin = corePosition;
                        exeBladeSphereSearch.mask = LayerIndex.entityPrecise.mask;
                        exeBladeSphereSearch.radius = effectiveRadius;
                        exeBladeSphereSearch.RefreshCandidates();
                        exeBladeSphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(dmgReport.attackerBody.teamComponent.teamIndex));
                        exeBladeSphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                        exeBladeSphereSearch.OrderCandidatesByDistance();
                        exeBladeSphereSearch.GetHurtBoxes(exeBladeHurtBoxBuffer);
                        exeBladeSphereSearch.ClearCandidates();

                        for (int j = 0; j < exeBladeHurtBoxBuffer.Count; j++){
                            HurtBox hurtBox = exeBladeHurtBoxBuffer[j];
                            if (hurtBox.healthComponent && hurtBox.healthComponent.body && hurtBox.healthComponent != bladeObjHPC){
                                DamageInfo damageInfoAOE = new DamageInfo{
                                    attacker = attackerBody.gameObject,
                                    crit = attackerBody.RollCrit(),
                                    damage = AOEDamage,
                                    position = corePosition,
                                    procCoefficient = ItemBase<ExeBlade>.instance.bladeCoefficient.Value,
                                    damageType = DamageType.AOE,
                                    damageColorIndex = DamageColorIndex.Item,
                                };
                                hurtBox.healthComponent.TakeDamage(damageInfoAOE);
                            }
                        }
                        exeBladeHurtBoxBuffer.Clear();
                    }
                }
            }

            yield return new WaitForSeconds(ItemBase<ExeBlade>.instance.additionalDuration.Value);
            EffectData effectData = new EffectData{
                origin = bladeObject.transform.position
            };
            effectData.SetNetworkedObjectReference(bladeObject); //pulverizedEffectPrefab
            EffectManager.SpawnEffect(HealthComponent.AssetReferences.permanentDebuffEffectPrefab, effectData, transmit: true);

            Destroy(bladeObject);
        }
    }

}
