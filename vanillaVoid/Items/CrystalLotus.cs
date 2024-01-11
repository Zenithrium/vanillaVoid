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

namespace vanillaVoid.Items
{
    public class CrystalLotus : ItemBase<CrystalLotus>
    {
        //public ConfigEntry<int> LotusVariant;
        //
        //public ConfigEntry<float> LotusDuration;
        //
        //public ConfigEntry<float> LotusSlowPercent;

        public ConfigEntry<float> barrierAmount;

        public ConfigEntry<float> pulseCountStacking;

        public override string ItemName => "Crystalline Lotus";

        public override string ItemLangTokenName => "BARRIERLOTUS_ITEM";

        public override string ItemPickupDesc => tempItemPickupDesc;

        public override string ItemFullDescription => tempItemFullDescription;

        public override string ItemLore => tempLore;

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlFinalLotusPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("lotusIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        //public static BuffDef lotusSlow { get; private set; }

        public override ItemTag[] ItemTags => new ItemTag[5] { ItemTag.Utility, ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.CannotCopy, ItemTag.HoldoutZoneRelated };

        string tempItemPickupDesc;
        string tempItemFullDescription;
        string tempLore;

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);

            switch (LotusVariant.Value)
            { 

                case 1:
                    tempItemPickupDesc = $"Periodically release a barrier nova during the Teleporter event and 'Holdout Zones' such as the Void Fields. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    tempItemFullDescription = $"Release a <style=cIsHealing>barrier nova</style> during the Teleporter event, <style=cIsHealing>providing a barrier</style> to all nearby allies for <style=cIsHealing>{barrierAmount.Value * 100}%</style> of their max health. Occurs <style=cIsHealing>{pulseCountStacking.Value}</style> <style=cStack>(+{pulseCountStacking.Value} per stack)</style> times. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    tempLore = $"\"I've located an...interesting specimen. You know those inane myths and theories people have about healing crystals, magical herbs, all that nonsense? You'll never believe me, but uh...I found something that roughly matches those descriptions. There's no doubt it's a coincidence... but it makes me wonder. What if some of these objects...these.. discoveries... aren't so new?\"\n\n- Lost Journal, Recovered from Petrichor V";

                    break;

                default:
                    tempItemPickupDesc = $"Periodically release slowing pulses during the Teleporter event and 'Holdout Zones' such as the Void Fields. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    tempItemFullDescription = $"Release a <style=cIsUtility>slowing pulse</style> during the Teleporter event, <style=cIsUtility>slowing enemies and projectiles</style> by up to <style=cIsUtility>{(1 - LotusSlowPercent.Value) * 100}%</style> for {LotusDuration.Value} seconds. Occurs <style=cIsHealing>{pulseCountStacking.Value}</style> <style=cStack>(+{pulseCountStacking.Value} per stack)</style> times. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    tempLore = $"\"I've been holed up here for... god knows how long now. I thought... I thought these plants would be... valuable, that..that it would be fine if I went and just... grabbed one - nature wouldn't mind... right? But ever since I grabbed it...I just feel.. so sluggish.. What...did I do wrong?\"\n\n- Lost Recording, Recovered from Petrichor V";

                    break;                
            }

            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            //VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
           // CreateBuff();
            //lotusObject = MainAssets.LoadAsset<GameObject>("mdlLotusWorldObject.prefab"); //lmao it makes the pickup spin WILDLY if you use mdlBladePickup
            //lotusObject.AddComponent<TeamFilter>();
            //lotusObject.AddComponent<NetworkIdentity>();

            //lotusObject.AddComponent<HealthComponent>();
            //lotusObject.AddComponent<BoxCollider>();
            //lotusObject.AddComponent<Rigidbody>();

            Hooks(); 
        }

        public override void CreateConfig(ConfigFile config)
        {
            //LotusVariant = config.Bind<int>("Item: " + ItemName, "Variant of Item", 0, "Adjust which version of " + ItemName + " you'd prefer to use. Variant 0 releases slowing novas per pulse, which reduce enemy and projectile speed, while Variant 1 provides 50% barrier per pulse.");
            //LotusDuration = config.Bind<float>("Item: " + ItemName, "Slow Duration", 30f, "Adjust how long the slow should last per pulse. A given slow is replaced by the next slow, so with enough lotuses, the full duration won't get used. However, increasing this also decreases the rate at which the slow fades.");
            //LotusSlowPercent = config.Bind<float>("Item: " + ItemName, "Slow Percent", 0.075f, "Adjust the strongest slow percent (between 0 and 1). Increasing this also makes it so the slow 'feels' shorter, as high values (near 1) feel very minor.");

            barrierAmount = config.Bind<float>("Item: " + ItemName, "Percent Barrier Provided", .5f, "Variant 1: Adjust percent of health that the barrier pulse provides.");
            pulseCountStacking = config.Bind<float>("Item: " + ItemName, "Activations per Stack", 1f, "Variant 1: Adjust the number of pulses each stack provides.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "TPHealingNova", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlFinalLotusDisplay.prefab");

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1726241f, 0.2653433f, 0.212837f),
                    localAngles = new Vector3(6.086947f, 8.317881f, 2.610492f),
                    localScale = new Vector3(.2f, .2f, .2f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.05854443f, 0.1524417f, 0.1972835f),
                    localAngles = new Vector3(8.609409f, 349.0558f, 354.7103f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hat",
                    localPos = new Vector3(0.1022652f, -0.009553856f, -0.09913615f),
                    localAngles = new Vector3(320.1294f, 104f, 317.2372f),
                    localScale = new Vector3(.125f, .125f, .125f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(1.881902f, 1.444218f, 3.373417f),
                    localAngles = new Vector3(26.07172f, 353.373f, 5.523437f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.2419884f, 0.1126657f, 0.1895919f),
                    localAngles = new Vector3(44.81784f, 26.5198f, 12.03686f),
                    localScale = new Vector3(.2f, .2f, .2f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.003674691f, -0.6024528f, 0.2619569f),
                    localAngles = new Vector3(29.69731f, 353.7802f, 358.004f),
                    localScale = new Vector3(.5f, .5f, .5f)

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
                    localPos = new Vector3(0.07465521f, 0.1513727f, 0.1474627f),
                    localAngles = new Vector3(1.975409f, 0.1417529f, 12.61099f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1092708f, 0.1483147f, 0.1939543f),
                    localAngles = new Vector3(9.869252f, 7.184193f, 356.6933f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "PlatformBase",
                    localPos = new Vector3(0.6196117f, -0.1732304f, 0.1983203f),
                    localAngles = new Vector3(29.56468f, 112.3599f, 83.46249f),
                    localScale = new Vector3(.4f, .4f, .4f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.09584545f, 0.1744341f, 0.204406f),
                    localAngles = new Vector3(9.84764f, 345.7096f, 357.1768f),
                    localScale = new Vector3(.165f, .165f, .16f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.556223f, -1.21355f, -1.983067f),
                    localAngles = new Vector3(7.949906f, 192.6268f, 190.9715f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.09959833f, 0.1406649f, 0.2082557f),
                    localAngles = new Vector3(31.40045f, 1.59294f, 1.171426f),
                    localScale = new Vector3(.2f, .2f, .2f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(-0.02802477f, 0.3978654f, -0.117903f),
                    localAngles = new Vector3(296.3392f, 200.9765f, 160.3061f),
                    localScale = new Vector3(.125f, .125f, .125f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmL",
                    localPos = new Vector3(0.1217875f, -0.005072067f, 0.01755153f),
                    localAngles = new Vector3(51.58237f, 182.4142f, 194.6872f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-2.347142f, 0.02130795f, -9.950616f),
                    localAngles = new Vector3(300.3647f, 214.8425f, 165.5008f),
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
                    childName =  "Chest",
                    localPos = new Vector3(-0.2344948f, 0.3370056f, 0.006313583f),
                    localAngles = new Vector3(12.62754f, 266.1613f, 9.178808f),
                    localScale = new Vector3(.125f, .125f, .115f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.006118589f, 0.002633904f, -0.003834995f),
                    localAngles = new Vector3(29.91726f, 229.2298f, 342.4258f),
                    localScale = new Vector3(.005f, .005f, .005f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] 
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.02318558f, 0.2201249f, -0.1634783f),
                    localAngles = new Vector3(339.486f, 149.8592f, 275.7268f),
                    localScale = new Vector3(.2f, .2f, .2f)
                }
            });
            //rules.Add("mdlCHEF", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Door",
            //        localPos = new Vector3(1f, 1f, 1f),
            //        localAngles = new Vector3(1f, 1f, 1f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ShoulderL",
                    localPos = new Vector3(0.001272175f, 0f, 0.0004509923f),
                    localAngles = new Vector3(7.583423f, 74.48707f, 298.3278f),
                    localScale = new Vector3(.0015f, .0015f, .0015f)
                }
            });
            rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ShoulderL",
                    localPos = new Vector3(-0.005143169f, 0.1371031f, -0.1040316f),
                    localAngles = new Vector3(22.2781f, 147.3504f, 329.4527f),
                    localScale = new Vector3(.115f, .115f, .115f)
                }
            });
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmR",
                    localPos = new Vector3(-0.1377259f, 0.1770405f, -0.140108f),
                    localAngles = new Vector3(328.466f, 254.5523f, 133.0548f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.07692271f, 0.2731504f, 0.0435502f),
                    localAngles = new Vector3(355.1563f, 290.847f, 8.419727f),
                    localScale = new Vector3(.125f, .125f, .125f)
                }
            });
            rules.Add("mdlExecutioner", new RoR2.ItemDisplayRule[]
{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.0009838672f, 0.002290892f, 0.000706371f),
                    localAngles = new Vector3(31.00255f, 260.6511f, 326.2855f),
                    localScale = new Vector3(0.0015f, 0.0015f, 0.0015f)
                }
});
            rules.Add("mdlNemmando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.0008129599f, 0.002373055f, 0.0006224829f),
                    localAngles = new Vector3(26.62126f, 310.2268f, 8.696174f),
                    localScale = new Vector3(0.00125f, 0.00125f, 0.00125f)
                }
            });
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hat",
                    localPos = new Vector3(0.08793373f, 0.06446167f, 0.08365246f),
                    localAngles = new Vector3(13.14883f, 34.15833f, 351.6671f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HeadBone",
                    localPos = new Vector3(0.08075233f, 0.1990717f, 0.1121317f),
                    localAngles = new Vector3(353.3702f, 28.81539f, .0000001074402f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }
            });
            rules.Add("mdlHANDOverclocked", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-1.129021f, 0.9637638f, 1.086472f),
                    localAngles = new Vector3(357.7567f, 304.5477f, 1.179182f),
                    localScale = new Vector3(.8f, .8f, .8f)
                }
            });
            rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "BlackBox",
                    localPos = new Vector3(-0.1535489f, 0.8817815f, 0.1476587f),
                    localAngles = new Vector3(11.46817f, 303.6623f, 358.8441f),
                    localScale = new Vector3(.15f, .15f, .15f)
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
                    childName = "Head",
                    localPos = new Vector3(-0.43021F, 1.47898F, 0.29508F),
                    localAngles = new Vector3(0F, 301.5747F, 0F),
                    localScale = new Vector3(0.6F, 0.6F, 0.6F)
                }
            });
            //rules.Add("Spearman", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "chest",
            //        localPos = new Vector3(0.01208F, 0.01102F, 0.0059F),
            //        localAngles = new Vector3(348.4865F, 9.06016F, 0F),
            //        localScale = new Vector3(0.00875F, 0.00875F, 0.00875F)
            //    }
            //});
            rules.Add("mdlAssassin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "leg_bone1.R",
                    localPos = new Vector3(0.02424F, 0.29209F, -0.26214F),
                    localAngles = new Vector3(34.46186F, 151.8756F, 328.8455F),
                    localScale = new Vector3(0.2F, 0.2F, 0.2F)
                }
            });
            rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.13397F, 0.25555F, 0.11431F),
                    localAngles = new Vector3(10.41568F, 340.447F, 359.2312F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                }
            });
            rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.46953F, 1.1757F, 0.99885F),
                    localAngles = new Vector3(9.5746F, 33.33859F, 28.16472F),
                    localScale = new Vector3(0.45F, 0.45F, 0.45F)
                }
            });
            rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.2162368f, 0.05608493f, 0.1440472f),
                    localAngles = new Vector3(39.53577f, 314.8438f, 358.4068f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }
            });
            rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "WingR",
                    localPos = new Vector3(0.12348F, 0.90881F, -0.01279F),
                    localAngles = new Vector3(20.11915F, 47.92907F, 338.0743F),
                    localScale = new Vector3(0.25F, 0.25F, 0.25F)
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
                    childName = "Chest",
                    localPos = new Vector3(-0.24938F, 0.2834F, 0.08566F),
                    localAngles = new Vector3(339.9367F, 289.0134F, 357.2876F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.1532F, 0.4091F, 0.33949F),
                    localAngles = new Vector3(352.9707F, 321.627F, 14.20378F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.23273F, 0.16713F, -0.32676F),
                    localAngles = new Vector3(23.24931F, 60.94613F, 339.5645F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            return rules;
        }

        public override void Hooks()
        {
            
            //On.RoR2.HealthComponent.TakeDamage += AdzeDamageBonus;
            //On.RoR2.HoldoutZoneController.UpdateHealingNovas += BarrierLotusNova;
        }

        //public void CreateBuff()
        //{
        //    var buffColor = new Color(0.7568f, 0.1019f, 0.9372f);
        //    lotusSlow = ScriptableObject.CreateInstance<BuffDef>();
        //    lotusSlow.buffColor = buffColor;
        //    lotusSlow.canStack = false;
        //    lotusSlow.isDebuff = true;
        //    //lotusSlow.isHidden = true;
        //    lotusSlow.name = "ZnVV" + "lotusSlow";
        //    lotusSlow.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("lotusSlow");
        //    ContentAddition.AddBuffDef(lotusSlow);
        //}


        //int chargesRemaining;
        //private void BarrierLotusNova(On.RoR2.HoldoutZoneController.orig_UpdateHealingNovas orig, HoldoutZoneController self, bool isCharging)
        //{
        //    if (isCharging)
        //    {
        //        
        //    }
        //    //Debug.Log("Hello, I am the HoldoutZoneController UpdatHealingNovas BarrierLotusNova Hook");
        //    orig(self, isCharging);
        //}
    }
}
