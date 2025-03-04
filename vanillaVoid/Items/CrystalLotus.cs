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
using RoR2.Projectile;
using System.Collections;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace vanillaVoid.Items
{
    public class CrystalLotus : ItemBase<CrystalLotus>
    {
        //public ConfigEntry<int> LotusVariant;
        //
        //public ConfigEntry<float> LotusDuration;
        //
        //public ConfigEntry<float> LotusSlowPercent;
        public ConfigEntry<int> LotusVariant;

        public ConfigEntry<float> LotusDuration;
        public ConfigEntry<float> LotusSlowPercent;

        public ConfigEntry<float> barrierAmount;

        public ConfigEntry<float> pulseCountStacking;

        public override string ItemName => "Crystalline Lotus";

        public override string ItemLangTokenName => "BARRIERLOTUS_ITEM";

        public override string ItemPickupDesc => tempItemPickupDesc;

        public override string ItemFullDescription => tempItemFullDescription;

        public override string ItemLore => tempLore;

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlFinalLotusPickupReal.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("lotusIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        //public static BuffDef lotusSlow { get; private set; }

        public override ItemTag[] ItemTags => new ItemTag[5] { ItemTag.Utility, ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.CannotCopy, ItemTag.HoldoutZoneRelated };

        string tempItemPickupDesc;
        string tempItemFullDescription;
        string tempLore;

        public static GameObject lotusEffect;

        Vector3 heightAdjust = new Vector3(0, 2.212f, 0);
        float previousPulseFraction = 0;
        float currentCharge = 0;
        float secondsUntilAttempt = 0;

        public float lotusTimer;
        //public float lotusDuration = 25f;
        AnimationCurve speedCurve;
        public static AnimationCurve speedCurveRise;

        static Vector3 teleporterPos;
        GameObject tempLotusObject;
        bool lotusSpawned = false;
        //public float slowCoeffValue = 1f;
        //bool detonationTime = false;
        public Material lotusMaterial;
        //public static float slowCoeffValue = 1f;

        public static BuffDef lotusSlow { get; private set; }

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
            CreateBuffAndVFX();
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
            lotusObject = MainAssets.LoadAsset<GameObject>("mdlLotusWorldObject2.prefab");
            lotusObject.AddComponent<TeamFilter>();
            lotusObject.AddComponent<NetworkIdentity>();

            lotusCollider = MainAssets.LoadAsset<GameObject>("LotusTeleporterCollider.prefab");
            lotusCollider.AddComponent<TeamFilter>();
            lotusCollider.AddComponent<NetworkIdentity>();
            //lotusCollider.AddComponent<LotusColliderToken>();
            lotusCollider.AddComponent<BuffWard>();
            lotusCollider.AddComponent<SlowDownProjectiles>();
            PrefabAPI.RegisterNetworkPrefab(lotusObject);


            string effect = "RoR2/DLC1/VoidRaidCrab/VoidRaidCrabDeathPending.prefab";
            GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>(effect).WaitForCompletion();
            //effectPrefab.AddComponent<NetworkIdentity>();

            lotusEffect = PrefabAPI.InstantiateClone(effectPrefab, "lotusEffect");
            lotusEffect.AddComponent<NetworkIdentity>();
            var effectcomp = lotusEffect.GetComponent<EffectComponent>();
            if (effectcomp)
            {
                //Debug.Log(effectcomp + " < | > " + effectcomp.soundName);
                effectcomp.soundName = "";
                //Debug.Log(effectcomp + " < | > " + effectcomp.soundName);
            }

            var timer = lotusEffect.AddComponent<DestroyOnTimer>();
            float delay = 1.15f;
            timer.duration = delay;

            ContentAddition.AddEffect(lotusEffect);

            speedCurve = new AnimationCurve();
            speedCurve.keys = new Keyframe[] {
            new Keyframe(0, LotusSlowPercent.Value, 0.33f, 0.33f),
            new Keyframe(0.75f, 0.33f, 0.33f, 0.33f),
            new Keyframe(1, 1, 0.33f, 0.33f)
            };

            speedCurveRise = new AnimationCurve();
            speedCurveRise.keys = new Keyframe[] {
            new Keyframe(0, 0),
            new Keyframe(.25f, .062f),
            new Keyframe(.5f, .25f),
            new Keyframe(.75f, .56f),
            new Keyframe(1, .95f)
            };

            Hooks();
        }

        public override void CreateConfig(ConfigFile config)
        {
            //LotusVariant = config.Bind<int>("Item: " + ItemName, "Variant of Item", 0, "Adjust which version of " + ItemName + " you'd prefer to use. Variant 0 releases slowing novas per pulse, which reduce enemy and projectile speed, while Variant 1 provides 50% barrier per pulse.");
            //LotusDuration = config.Bind<float>("Item: " + ItemName, "Slow Duration", 30f, "Adjust how long the slow should last per pulse. A given slow is replaced by the next slow, so with enough lotuses, the full duration won't get used. However, increasing this also decreases the rate at which the slow fades.");
            //LotusSlowPercent = config.Bind<float>("Item: " + ItemName, "Slow Percent", 0.075f, "Adjust the strongest slow percent (between 0 and 1). Increasing this also makes it so the slow 'feels' shorter, as high values (near 1) feel very minor.");
            string lotusname = "Crystalline Lotus";

            LotusVariant = config.Bind<int>("Item: " + lotusname, "Variant of Item", 0, "Adjust which version of " + lotusname + " you'd prefer to use. Variant 0 releases slowing novas per pulse, which reduce enemy and projectile speed, while Variant 1 provides 50% barrier per pulse.");
            LotusDuration = config.Bind<float>("Item: " + lotusname, "Slow Duration", 25f, "Variant 0: Adjust how long the slow should last per pulse. A given slow is replaced by the next slow, so with enough lotuses, the full duration won't get used. However, increasing this also decreases the rate at which the slow fades.");
            LotusSlowPercent = config.Bind<float>("Item: " + lotusname, "Slow Intensity", 0, "Variant 0: Adjust the strongest slow percent (between 0 and 1). Increasing this also makes it so the slow 'feels' shorter, as high values (near 1) feel very minor. Note that this is inverted, where 0 = 100% slow and 1 = 0% slow.");

            barrierAmount = config.Bind<float>("Item: " + lotusname, "Percent Barrier Provided", .5f, "Variant 1: Adjust percent of health that the barrier pulse provides.");
            pulseCountStacking = config.Bind<float>("Item: " + lotusname, "Activations per Stack", 1f, "Variant 1: Adjust the number of pulses each stack provides.");
            voidPair = config.Bind<string>("Item: " + lotusname, "Item to Corrupt", "TPHealingNova", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlFinalLotusDisplay.prefab");

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            var mpp = ItemModel.AddComponent<ModelPanelParameters>();
            mpp.focusPointTransform = ItemModel.transform.Find("Target");
            mpp.cameraPositionTransform = ItemModel.transform.Find("Source");
            mpp.minDistance = 1f;
            mpp.maxDistance = 3f;
            mpp.modelRotation = Quaternion.Euler(new Vector3(0, 0, 0));

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
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00115F, 0.12506F, 0.10641F),
                    localAngles = new Vector3(0F, 0F, 0F),
                    localScale = new Vector3(0.1125F, 0.1125F, 0.1125F)
                }
            });
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.36181F, 0.0042F, 0.20283F),
                    localAngles = new Vector3(34.76578F, 359.3652F, 37.78717F),
                    localScale = new Vector3(0.225F, 0.225F, 0.225F)
                }
            });
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.17811F, 0.27437F, 0.25839F),
                    localAngles = new Vector3(359.8092F, 3.72978F, 356.6343F),
                    localScale = new Vector3(0.2F, 0.2F, 0.2F)
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

            rules.Add("RA2ChronoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "backpack_base",
                    localPos = new Vector3(-0.32942F, -0.13828F, -0.38581F),
                    localAngles = new Vector3(37.95247F, 210.9446F, 346.3948F),
                    localScale = new Vector3(0.2F, 0.2F, 0.2F)
                }
            });
            rules.Add("RobRavagerBody", new RoR2.ItemDisplayRule[]
            {
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
            rules.Add("mdlMorris", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hat",
                    localPos = new Vector3(0.07035F, 0.16124F, -0.14492F),
                    localAngles = new Vector3(325.7638F, 9.17666F, 336.5881F),
                    localScale = new Vector3(0.125F, 0.125F, 0.125F)
                }
            });
            return rules;
        }

        public void CreateBuffAndVFX()
        {
            var buffColor = new Color(0.5215f, 0.3764f, 0.8549f);
            lotusSlow = ScriptableObject.CreateInstance<BuffDef>();
            lotusSlow.buffColor = buffColor;
            lotusSlow.canStack = false;
            lotusSlow.isDebuff = true;
            //lotusSlow.isHidden = true;
            lotusSlow.name = "ZnVV" + "lotusSlow";
            lotusSlow.iconSprite = MainAssets.LoadAsset<Sprite>("lotusSlow");
            ContentAddition.AddBuffDef(lotusSlow);

            Texture tex = MainAssets.LoadAsset<Texture>("texRampIce4.png");
            var tempmaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matSlow80Debuff.mat").WaitForCompletion();
            lotusMaterial = Material.Instantiate(tempmaterial);
            lotusMaterial.name = "VVLotusSlowMaterial";
            //lotusMaterial.SetFloat("_BrightnessBoost", 1.25f)
            //edit alpha boost
            //lotusMaterial.SetFloat("_")
            //Debug.Log("boost: " + matInstance.GetFloat("_Boost"));
            //matInstance.SetTexture("_RemapTex", tex);
            //Debug.Log("_AlphaBoost: " + lotusMaterial.GetFloat("_AlphaBoost")); //1.17
                                                                                //lotusMaterial.SetFloat("_Boost", 1);
            lotusMaterial.SetFloat("_AlphaBoost", 0);
            //lotusMaterial.SetFloat("_AlphaBoost", matInstance.GetFloat("_AlphaBoost"));
            lotusMaterial.SetTexture("_RemapTex", tex);
            ///////matInstance.SetFloat("_Boost", matInstance.GetFloat("_Boost") - (coeff * 2));////

        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddLotusOnPickup;
            On.RoR2.HoldoutZoneController.UpdateHealingNovas += CrystalLotusNova;
            //On.RoR2.HoldoutZoneController.FixedUpdate += LotusSlowNova;

            //On.RoR2.SceneDirector.PlaceTeleporter += PrimoridalTeleporterCheck;
            //On.RoR2.Projectile.SlowDownProjectiles.OnTriggerEnter += fuck;
            RecalculateStatsAPI.GetStatCoefficients += LotusSlowStatsHook;
            //On.RoR2.CharacterBody.FixedUpdate += LotusSlowVisuals;
            //n.RoR2.CharacterModel.UpdateOverlays += AddLotusMaterial;
            //On.RoR2.CharacterBody.FixedUpdate += LastTry;
            //On.RoR2.HealthComponent.TakeDamage += AdzeDamageBonus;
            //On.RoR2.HoldoutZoneController.UpdateHealingNovas += BarrierLotusNova;
            RoR2.SceneDirector.onPrePopulateSceneServer += ClearLotusTeleporter;

            On.RoR2.Run.OnServerTeleporterPlaced += AddLotus;

            On.RoR2.BuffWard.BuffTeam += LotusTeamBuff;

            On.RoR2.CharacterBody.OnBuffFinalStackLost += RemoveLotusToken;
            //On.RoR2.CharacterBody.AddTimedBuff_BuffIndex_float += LotusAddBuff;

            //On.RoR2.TemporaryOverlayInstance.CleanupEffect += StopDoingThat;

            //IL.RoR2.TemporaryOverlayInstance.CleanupEffect += il =>
            //{
            //    var ilCursor = new ILCursor(il);
            //    ilCursor.Emit(OpCodes.Ldarg_0);
            //    ilCursor.Emit<TemporaryOverlayInstance>(OpCodes.Ldfld, nameof(TemporaryOverlayInstance.assignedCharacterModel));
            //    ilCursor.Emit<CharacterModel>(OpCodes.Ldfld, nameof(CharacterModel.body));
            //    if (ilCursor.TryGotoNext(
            //            x => x.MatchLdarg(0),
            //            x => x.MatchLdfld<TemporaryOverlayInstance>(nameof(TemporaryOverlayInstance.materialInstance)),
            //            x => x.MatchCall<UnityEngine.Object>("op_Implicit"),
            //            x => x.MatchBrfalse(out _)))
            //    {
            //        ILLabel ilLabel = ilCursor.DefineLabel();
            //
            //        ilCursor.EmitDelegate<Func<CharacterBody, bool>>(body => false);
            //        ilCursor.Emit(OpCodes.Brfalse, ilLabel);
            //        if (ilCursor.TryGotoNext(
            //                x => x.MatchLdarg(0),
            //                x => x.MatchLdfld<TemporaryOverlayInstance>(nameof(TemporaryOverlayInstance.destroyObjectOnEnd)),
            //                x => x.MatchBrtrue(out _),
            //                x => x.MatchLdarg(0),
            //                x => x.MatchLdfld<TemporaryOverlayInstance>(nameof(TemporaryOverlayInstance.destroyComponentOnEnd)),
            //                x => x.MatchBrtrue(out _)))
            //        {
            //            ilCursor.MarkLabel(ilLabel);
            //        }
            //    }
            //};
            //IL.RoR2.TemporaryOverlayInstance.CleanupEffect += StopDoingThat;
        }

        //private void StopDoingThat(ILContext il) {
        //    ILCursor c = new ILCursor(il);
        //
        //    bool ILFound2 = c.TryGotoNext(MoveType.After,
        //    x => x.MatchLdarg(0));
        //    if (ILFound2)
        //    {
        //        c.EmitDelegate<Func<RoR2.TemporaryOverlayInstance, CharacterBody>>((self) =>
        //        {
        //            CharacterBody body = null;
        //            if (self.assignedCharacterModel && self.assignedCharacterModel.body) {
        //                body = self.assignedCharacterModel.body;
        //            }
        //            return body;
        //        });
        //        c.Emit(OpCodes.Stfld, typeof(RoR2.CharacterBody));
        //
        //    }
        //
        //    bool ILFound = c.TryGotoNext(MoveType.After,
        //    x => x.MatchLdarg(0),
        //    x => x.MatchLdfld<RoR2.TemporaryOverlayInstance>(nameof(RoR2.TemporaryOverlayInstance.materialInstance)),
        //    x => x.MatchCallOrCallvirt(typeof(UnityEngine.Object).GetMethod("op_Implicit"))
        //    );
        //
        //    if (ILFound){
        //        c.Emit(OpCodes.Ldarg, 0);
        //        c.Emit(OpCodes.Ldloc, typeof(RoR2.CharacterBody));
        //        c.EmitDelegate<Func<bool, RoR2.TemporaryOverlayInstance, CharacterBody, bool>>((boolean, self, body) => {
        //            Debug.Log("hiiiii hello");
        //            if (body){
        //                Debug.Log("yeah: " + body + " | ");
        //                var handler = body.GetComponent<LotusHandler>();
        //                if (handler && self.materialInstance == handler.matInstance){
        //                    Debug.Log("It was the real mateiral ");
        //                    return false;
        //                }
        //                Debug.Log("it wasnt ");
        //            }
        //            return boolean;
        //        });
        //    }else{
        //        Debug.Log("ah fuck,,");
        //    }
        //}

        private void RemoveLotusToken(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef){
            orig(self, buffDef);
            if(buffDef == lotusSlow){

                var token = self.gameObject.GetComponent<LotusToken>();
                if (token){
                    //Debug.Log("Removing token");
                    //if(self.modelLocator.modelTransform.GetComponent<CharacterModel>().temporaryOverlays.Count > 0){
                    //    foreach (var overlay in self.modelLocator.modelTransform.GetComponent<CharacterModel>().temporaryOverlays){
                    //        //Debug.Log("overlay:  " + overlay + " | om: " + overlay.originalMaterial + " | mi: " + overlay.materialInstance);
                    //        if (overlay.materialInstance == token.handler.matInstance){
                    //            //Debug.Log("overlay stopwatch: " + overlay.stopwatch + " | " + overlay.duration + body.name + " | " + body.baseNameToken);
                    //            //overlay.stopwatch = 0;
                    //            overlay.Destroy();
                    //            break;
                    //        }
                    //    }
                    //}
                    //GameObject.Destroy(token);
                }
            }
        }

        private void ClearLotusTeleporter(SceneDirector obj)
        {
            //Debug.Log("clearing tp location ");
            teleporterPos = Vector3.zero;
        }

        private void AddLotus(On.RoR2.Run.orig_OnServerTeleporterPlaced orig, Run self, SceneDirector sceneDirector, GameObject teleporter){
            orig(self, sceneDirector, teleporter);

            int itemCount = 0;
            TeamIndex teamDex = default;
            foreach (var player in PlayerCharacterMasterController.instances){
                itemCount += player.master.inventory.GetItemCount(CrystalLotus.instance.ItemDef);
                teamDex = player.master.teamIndex;
            }
            if (teleporter)
            {
                teleporterPos = teleporter.transform.position;
                if (teleporter.name.Contains("Lunar"))
                {
                    teleporterPos += new Vector3(0, -.675f, 0);
                }
            }

            if (itemCount > 0 && teleporterPos != Vector3.zero)
            {
                Quaternion rot = Quaternion.Euler(1.52666613f, 180, 9.999999f);
                var tempLotus = GameObject.Instantiate(lotusObject, teleporterPos, rot);
                tempLotus.GetComponent<TeamFilter>().teamIndex = teamDex;
                tempLotus.transform.position = teleporterPos + heightAdjust;
                NetworkServer.Spawn(tempLotus);
                tempLotusObject = tempLotus;

                lotusSpawned = true;
                EffectData effectData = new EffectData { origin = tempLotus.transform.position };
                effectData.SetNetworkedObjectReference(tempLotus.gameObject);
                EffectManager.SpawnEffect(HealthComponent.AssetReferences.crowbarImpactEffectPrefab, effectData, transmit: true);
            }
        }

        private void AddLotusOnPickup(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            if (self){
                orig(self);

                if (!lotusSpawned){
                    int itemCount = 0;
                    TeamIndex teamDex = default;
                    foreach (var player in PlayerCharacterMasterController.instances){
                        itemCount += player.master.inventory.GetItemCount(CrystalLotus.instance.ItemDef);
                        teamDex = player.master.teamIndex;
                    }
        
                    if (itemCount > 0 && teleporterPos != Vector3.zero){
                        Quaternion rot = Quaternion.Euler(1.52666613f, 180, 9.999999f);
                        var tempLotus = GameObject.Instantiate(lotusObject, teleporterPos, rot);
                        tempLotus.GetComponent<TeamFilter>().teamIndex = teamDex;
                        tempLotus.transform.position = teleporterPos + heightAdjust;
                        NetworkServer.Spawn(tempLotus);
                        tempLotusObject = tempLotus;
                        
                        lotusSpawned = true;

                        EffectData effectData = new EffectData { origin = tempLotus.transform.position };
                        effectData.SetNetworkedObjectReference(tempLotus.gameObject);
                        EffectManager.SpawnEffect(HealthComponent.AssetReferences.crowbarImpactEffectPrefab, effectData, transmit: true);
                    }
                }
            }
        }

        private void LotusTeamBuff(On.RoR2.BuffWard.orig_BuffTeam orig, BuffWard self, IEnumerable<TeamComponent> recipients, float radiusSqr, Vector3 currentPosition){
            //Debug.Log("handler  and body");
            orig(self, recipients, radiusSqr, currentPosition);
            //Debug.Log("Teambuff"); oops..
            if (self && self.buffDef == lotusSlow)
            {
                var collidertoken = self.gameObject.GetComponent<LotusToken>();
                if (collidertoken)
                {
                    var handler = collidertoken.handler;
                    if (handler)
                    {
                        //Debug.Log("part 2");
                        foreach (TeamComponent teamComponent in recipients)
                        {

                            CharacterBody body = teamComponent.GetComponent<CharacterBody>();
                            //Debug.Log("body in radius:" + body.name + " | " + body.baseNameToken);
                            Vector3 vector = teamComponent.transform.position - currentPosition;
                            bool skip = false;
                            bool real = false;
                            if (body && body.modelLocator && body.modelLocator.modelBaseTransform && body.modelLocator.modelTransform.GetComponent<CharacterModel>())
                            {
                                real = true;
                                if (body.modelLocator.modelTransform.GetComponent<CharacterModel>().temporaryOverlays.Count > 0)
                                {
                                    foreach (var overlay in body.modelLocator.modelTransform.GetComponent<CharacterModel>().temporaryOverlays)
                                    {
                                        //Debug.Log("overlay:  " + overlay + " | om: " + overlay.originalMaterial + " | mi: " + overlay.materialInstance);
                                        if (overlay.materialInstance.name.Contains("VVLotusSlowMaterial"))
                                        {
                                            //Debug.Log("overlay stopwatch: " + overlay.stopwatch + " | " + overlay.duration + body.name + " | " + body.baseNameToken);
                                            //overlay.stopwatch = 0;
                                            skip = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!skip && vector.sqrMagnitude <= radiusSqr && real)
                            {// && !body.GetComponent<LotusBodyToken>()){
                                //charactermodel.temporary overlay
                                var transform = body.modelLocator.modelTransform;

                                //Debug.Log("Adding overlay;");
                                //TemporaryOverlay overlayreal = new TemporaryOverlay();
                                //overlayreal.materialInstance = handler.matInstance;
                                //overlayreal.duration = 5;

                                var overlay = TemporaryOverlayManager.AddOverlay(transform.gameObject);
                                overlay.duration = 5;
                                //overlay.alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, .5f);
                                //overlay.animateShaderAlpha = true;
                                overlay.destroyComponentOnEnd = true;
                                overlay.originalMaterial = handler.matInstance;
                                //overlay.materialInstance = handler.matInstance;
                                //overlay.material
                                //overlay.componentReference 
                                var model = transform.GetComponent<RoR2.CharacterModel>();
                                //Debug.Log("grahh: " + model);
                                //body.gameObject.AddComponent<LotusBodyToken>();
                                overlay.AddToCharacterModel(model);
                                var token = body.gameObject.GetComponent<LotusToken>();
                                if (!token)
                                {
                                    token = body.gameObject.AddComponent<LotusToken>();
                                    token.model = model;
                                    token.overlay = overlay;
                                }
                                token.handler = handler;
                            }
                        }
                    }
                }
            }
        }

        private void CrystalLotusNova(On.RoR2.HoldoutZoneController.orig_UpdateHealingNovas orig, HoldoutZoneController self, bool isCharging)
        {
            int itemCount = 0;
            TeamIndex teamDex = default;
            foreach (var player in PlayerCharacterMasterController.instances){
                itemCount += player.master.inventory.GetItemCount(CrystalLotus.instance.ItemDef);
                teamDex = player.master.teamIndex;
            }
            var handler = self.gameObject.GetComponent<LotusHandler>();
            float coeff = 1;
            //Debug.Log("pre " + coeff);
            lotusTimer += Time.fixedDeltaTime;
            if (handler && handler.slowCoeffValue < 1){
                if (handler.rise){
                    //handler.matInstance.SetFloat("_AlphaBoost", coeff * 3);
                    coeff = speedCurve.Evaluate(lotusTimer / LotusDuration.Value);
                    handler.slowCoeffValue = coeff;
                    handler.risingCoeffValue = speedCurveRise.Evaluate(lotusTimer / 1.15f);
                    //Debug.Log("handler.risingCoeffValue: " + handler.risingCoeffValue);
                    //handler.risingCoeffValue = 1 - (coeff * 20);
                    //Debug.Log("rising " + (1 - (coeff * 20)) + " | " + handler.risingCoeffValue);
                }
                else{
                    //handler.matInstance.SetFloat("_AlphaBoost", 1 - coeff);
                    coeff = speedCurve.Evaluate(lotusTimer / LotusDuration.Value);
                    handler.slowCoeffValue = coeff;
                    //Debug.Log("handler.slowCoeffValue: " + handler.slowCoeffValue);
                    //Debug.Log("falling " + coeff);
                }

                //handler.matInstance.SetFloat("_AlphaBoost", 1 - coeff);
                if(coeff >= 1){
                    coeff = 1;
                    //handler.matInstance.SetFloat("_AlphaBoost", 0);
                    handler.slowCoeffValue = 1;
                    handler.slowComp.enabled = false;
                    handler.buffComp.enabled = false;
                    //Debug.Log("Burst complete");
                }
            }

            if (itemCount > 0 && isCharging){
                if (NetworkServer.active && Time.fixedDeltaTime > 0f){
                    if (!handler){
                        //Debug.Log("makinng handler");
                        handler = self.gameObject.AddComponent<LotusHandler>();

                        var matInstance = Material.Instantiate(lotusMaterial);
                        handler.matInstance = matInstance;
                        handler.slowCoeffValue = 1;
                        Vector3 holdoutpos = self.gameObject.transform.position;
                        var tempLotusCollider = UnityEngine.Object.Instantiate<GameObject>(lotusCollider, holdoutpos, new Quaternion(0, 0, 0, 0));

                        NetworkServer.Spawn(tempLotusCollider);

                        var temp = tempLotusCollider.GetComponent<TeamFilter>();
                        temp.teamIndex = teamDex;
                        tempLotusCollider.layer = 12;

                        TeamFilter filter = tempLotusCollider.GetComponent<TeamFilter>();
                        var tempcomp = tempLotusCollider.GetComponent<SlowDownProjectiles>();
                        tempcomp.teamFilter = filter;

                        var tempward = tempLotusCollider.GetComponent<BuffWard>();
                        tempward.radius = self.currentRadius;
                        tempward.buffDef = lotusSlow;
                        tempward.invertTeamFilter = true;
                        tempward.enabled = false;
                        tempward.buffDuration = 2;
                        tempward.buffTimer = 1;

                        handler.slowComp = tempcomp;
                        handler.buffComp = tempward;

                        handler.lotusColliderInstance = tempLotusCollider;

                        var ctoken = tempLotusCollider.AddComponent<LotusToken>();
                        ctoken.handler = handler;
                        //ctoken.onBody = false;
                    }else{
                        handler.lotusColliderInstance.transform.position = self.gameObject.transform.position;
                    }

                    var tempCollider = handler.lotusColliderInstance;

                    var spcl = tempCollider.GetComponent<SphereCollider>();
                    spcl.radius = self.currentRadius;

                    var ward = tempCollider.GetComponent<BuffWard>();
                    ward.radius = self.currentRadius;

                    var comp = tempCollider.GetComponent<SlowDownProjectiles>();
                    comp.slowDownCoefficient = coeff;


                    if (secondsUntilAttempt > 0f){
                        secondsUntilAttempt -= Time.fixedDeltaTime;
                    }else{
                        //Debug.Log("attemnpt");
                        if (currentCharge > self.charge){
                            previousPulseFraction = 0;
                            currentCharge = self.charge;
                        }
                        if (self.charge >= 1){
                            if (LotusVariant.Value == 0){
                                if (tempCollider && !handler.isEnding){
                                    //var endtoken = tempCollider.AddComponent<LotusSlowEndingToken>();
                                    //endtoken.tempLotusCollider = tempCollider;
                                    //endtoken.handler = handler;
                                    //endtoken.ticking = true;
                                    handler.EndSlowIntermediate();
                                    //Debug.Log("ending");
                                }
                            }
                        }

                        float nextPulseFraction = CalcNextPulseFraction(itemCount * (int)ItemBase<CrystalLotus>.instance.pulseCountStacking.Value, previousPulseFraction);
                        currentCharge = self.charge;

                        if (nextPulseFraction <= currentCharge){
                            if (LotusVariant.Value == 1){
                                string nova = "RoR2/Base/TPHealingNova/TeleporterHealNovaPulse.prefab";
                                GameObject novaPrefab = Addressables.LoadAssetAsync<GameObject>(nova).WaitForCompletion();
                                novaPrefab.GetComponent<TeamFilter>().teamIndex = teamDex;
                                NetworkServer.Spawn(novaPrefab);
                                //StartCoroutine(LotusDelayedBarrier(self, teamDex));
                            }else{
                                //ward.enabled = true;
                                //Debug.Log("ward buff: " + ward.buffDef + " | " + ward.enabled);
                                lotusTimer = 0;
                                
                                handler.slowCoeffValue = speedCurve.Evaluate(lotusTimer / LotusDuration.Value);
                                handler.risingCoeffValue = speedCurveRise.Evaluate(lotusTimer / 1.15f);

                                handler.slowComp.enabled = true;
                                handler.buffComp.enabled = true;
                                for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex += 1){
                                    if (teamIndex != handler.buffComp.teamFilter.teamIndex){
                                        //handler.buffComp.BuffTeam(TeamComponent.GetTeamMembers(teamIndex), radiusSqr, position);
                                        handler.buffComp.BuffTeam(TeamComponent.GetTeamMembers(teamIndex), handler.buffComp.calculatedRadius * handler.buffComp.calculatedRadius, handler.buffComp.transform.position);

                                    }
                                    //else
                                    //{
                                    //    Debug.Log("Teamindex skippped " + teamIndex + " | " + TeamComponent.GetTeamMembers(teamIndex));
                                    //}
                                }
                                //handler.buffComp.BuffTeam(TeamComponent.GetTeamMembers(handler.buffComp.teamFilter.teamIndex), handler.buffComp.calculatedRadius * handler.buffComp.calculatedRadius, handler.buffComp.transform.position);
                                //Debug.Log("ward buff: " + ward.buffDef + " | " + ward.enabled);
                                //StartCoroutine(Lotus2ExplosionThing(self.gameObject));
                                //var vfxtoken = self.gameObject.AddComponent<LotusVFXEnder>();
                                //vfxtoken.PlayEffect(self.gameObject);
                                handler.PlayEffect(self.gameObject);
                                handler.rise = true;
                            }

                            previousPulseFraction = nextPulseFraction;
                            secondsUntilAttempt = 1f;

                            string effect2 = "RoR2/DLC1/VoidSuppressor/SuppressorClapEffect.prefab";
                            GameObject effect2Prefab = Addressables.LoadAssetAsync<GameObject>(effect2).WaitForCompletion();
                            var ef2efc = effect2Prefab.GetComponent<EffectComponent>();
                            ef2efc.applyScale = true;
                            ef2efc.referencedObject = effect2Prefab;
                            effect2Prefab.transform.localScale *= 4;
                            EffectManager.SimpleImpactEffect(effect2Prefab, teleporterPos, new Vector3(0, 0, 0), true);
                        }
                        
                    }
                }
            }

            orig(self, isCharging);
        }
        private static float CalcNextPulseFraction(int itemCount, float prevPulseFraction)
        {
            //if(charge < .02 && prevPulseFraction > 1)
            //{
            //    Debug.Log("fixing dumb jank in calc" + prevPulseFraction);
            //    prevPulseFraction = 0;
            //}
            float healFraction = 1f / (float)(itemCount + 1);
            //Debug.Log(healFraction + " hela fraction");
            for (int i = 1; i <= itemCount; i++)
            {
                float temp = (float)i * healFraction;
                //Debug.Log("temp: " + temp + " | previous: " + prevPulseFraction);
                if (temp > prevPulseFraction)
                {
                    return temp;
                }
            }
            return 1.1f;
        }
        //IEnumerator LotusDelayedBarrier(HoldoutZoneController self, TeamIndex teamDex)
        //{
        //    yield return new WaitForSeconds(.5f);
        //    foreach (var player in PlayerCharacterMasterController.instances)
        //    {
        //        if (self.IsBodyInChargingRadius(player.body) && player.body.teamComponent.teamIndex == teamDex)
        //        {
        //            //var playerHealthComp = player.GetComponent<HealthComponent>();
        //            //player.body.healthComponent;
        //            if (player.body.healthComponent)
        //            {
        //                //Debug.Log("yoo health component!!");
        //                player.body.healthComponent.AddBarrier(player.body.healthComponent.fullCombinedHealth * ItemBase<CrystalLotus>.instance.barrierAmount.Value); //25% 
        //                //string effect2 = "RoR2/DLC1/VoidSuppressor/SuppressorClapEffect.prefab";
        //                //GameObject effect2Prefab = Addressables.LoadAssetAsync<GameObject>(effect2).WaitForCompletion();
        //                //EffectManager.SimpleImpactEffect(effect2Prefab, player.body.transform.position, player.body.aimOrigin, true);
        //            }
        //            else
        //            {
        //                //Debug.Log("no suitable health component.");
        //            }
        //
        //        }
        //    }
        //}

        //IEnumerator SlowLotusDelayedEnd()
        //{
        //    //Debug.Log("Delayed end called");
        //    var comp = tempLotusCollider.GetComponent<SlowDownProjectiles>();
        //    while (slowCoeffValue < 1)
        //    {
        //        yield return .1f;
        //        slowCoeffValue += .0015f;
        //        comp.slowDownCoefficient = slowCoeffValue;
        //    }
        //    slowCoeffValue = 1;
        //    comp.slowDownCoefficient = 1;
        //
        //}

        private void LotusSlowStatsHook(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args){
            if (sender){
                if (sender.HasBuff(lotusSlow)){
                    float slow = .2f;
                    var token = sender.gameObject.GetComponent<LotusToken>();
                    if (token){
                        slow = 1 - token.handler.slowCoeffValue;            
                    }
                    //Debug.Log("slow: " + slow);
                    args.moveSpeedReductionMultAdd += slow;
                    args.attackSpeedMultAdd -= slow / 2;
                }
            }
        }

        public class LotusHandler : MonoBehaviour {
            public GameObject lotusColliderInstance;
            public Material matInstance;
            public float slowCoeffValue;
            public bool isEnding = false;
            public float risingCoeffValue;


            public bool rise = false;

            public SlowDownProjectiles slowComp;
            public BuffWard buffComp;
            Vector3 heightAdjustPulse = new Vector3(0, 2.5f, 0);

            public void PlayEffect(GameObject holdoutZoneCtrlr){
                var soundID = Util.PlaySound("Play_voidRaid_death", holdoutZoneCtrlr.gameObject);

                Vector3 pulsepos = this.transform.position + heightAdjustPulse;

                EffectManager.SimpleEffect(lotusEffect, pulsepos, new Quaternion(0, 0, 0, 0), true);
                rise = true;
                StartCoroutine(DelayedSoundEnd(soundID, pulsepos));
            }

            IEnumerator DelayedSoundEnd(uint soundID, Vector3 pulsepos){
                yield return new WaitForSeconds(1.15f);
                AkSoundEngine.StopPlayingID(soundID);
                rise = false;
                //detonationTime = false;
                //gameobject.gameObject.transform.position
                string effect1 = "RoR2/DLC1/VoidSuppressor/SuppressorRetreatToShellEffect.prefab"; //"RoR2/DLC1/VoidRaidCrab/LaserImpactEffect.prefab";
                GameObject effect1Prefab = Addressables.LoadAssetAsync<GameObject>(effect1).WaitForCompletion();
                EffectManager.SimpleImpactEffect(effect1Prefab, pulsepos, new Vector3(0, 0, 0), true);
                //Destroy(this);
            }
            

            public void EndSlowIntermediate(){
                isEnding = true;
                StartCoroutine(DelayedSlowEnd());
            }

            IEnumerator DelayedSlowEnd(){
                while(slowCoeffValue < 1){
                    slowCoeffValue += .01f;
                    slowComp.slowDownCoefficient = slowCoeffValue;
                    yield return .1f;
                }
                //Debug.Log("offies!");
                slowComp.enabled = false;
                buffComp.enabled = false;
                //if (handler.slowCoeffValue >= 1)
                //{
                //    handler.slowCoeffValue = 1;
                //    comp.slowDownCoefficient = 1;
                //    Destroy(this);
                //}
            }

        }
        public class LotusToken : MonoBehaviour
        {
            public LotusHandler handler;
            public CharacterModel model;
            public TemporaryOverlayInstance overlay;
            //public bool lateAdded = true;
            //public float lateRiseCoeff;
            public float duration = 0;
            //public bool onBody = true;
            public void FixedUpdate(){
                if (overlay != null && handler){
                    //Debug.Log("overlay: " + overlay + " | " + handler)
                    if (overlay.materialInstance){
                        if (handler.rise){
                            //lateAdded = false;
                            overlay.materialInstance.SetFloat("_AlphaBoost", handler.risingCoeffValue);
                        }else{
                            //if (!lateAdded && !handler.rise){
                            //    duration += Time.fixedDeltaTime;
                            //    lateRiseCoeff = (speedCurveRise.Evaluate(duration / .65f)) * handler.slowCoeffValue;
                            //
                            //    overlay.materialInstance.SetFloat("_AlphaBoost", lateRiseCoeff);
                            //    //if(lotusTimer+)
                            //    if(duration >= .65f){
                            //        lateAdded = false;
                            //    }
                            //}else{
                            overlay.materialInstance.SetFloat("_AlphaBoost", 1 - handler.slowCoeffValue);
                            //}
                        }
                        //Debug.Log("material isntancce is updated");
                    }
                }

            }
        }

        //public class LotusBodyToken : MonoBehaviour
        //{
        //    public LotusHandler handler;
        //}

        //public class LotusColliderToken : MonoBehaviour
        //{
        //    public LotusHandler handler;
        //}

        //public class LotusVFXEnder : MonoBehaviour
        //{
        //    Vector3 heightAdjustPulse = new Vector3(0, 2.5f, 0);
        //
        //    public void PlayEffect(GameObject holdoutZoneCtrlr)
        //    {
        //        var soundID = Util.PlaySound("Play_voidRaid_death", holdoutZoneCtrlr.gameObject);
        //        Vector3 pulsepos;
        //    //if (voidfields)
        //    //{
        //    //    pulsepos = holdoutZoneCtrlr.gameObject.transform.position + (heightAdjustPulse / 2);
        //    //}
        //    //else
        //    //{
        //        pulsepos = this.transform.position + heightAdjustPulse;
        //    //}
        //
        //        EffectManager.SimpleEffect(lotusEffect, pulsepos, new Quaternion(0, 0, 0, 0), true);
        //
        //        StartCoroutine(DelayedSoundEnd(soundID, pulsepos));
        //    }
        //
        //    IEnumerator DelayedSoundEnd(uint soundID, Vector3 pulsepos)
        //    {
        //        yield return new WaitForSeconds(1.15f);
        //        AkSoundEngine.StopPlayingID(soundID);
        //
        //        //detonationTime = false;
        //        //gameobject.gameObject.transform.position
        //        string effect1 = "RoR2/DLC1/VoidSuppressor/SuppressorRetreatToShellEffect.prefab"; //"RoR2/DLC1/VoidRaidCrab/LaserImpactEffect.prefab";
        //        GameObject effect1Prefab = Addressables.LoadAssetAsync<GameObject>(effect1).WaitForCompletion();
        //        EffectManager.SimpleImpactEffect(effect1Prefab, pulsepos, new Vector3(0, 0, 0), true);
        //        Destroy(this);
        //    }
        //}

        //public class LotusSlowEndingToken : MonoBehaviour {
        //    public GameObject tempLotusCollider;
        //    float timer = 0;
        //    public bool ticking = false;
        //    public LotusHandler handler;
        //
        //    void FixedUpdate(){
        //        if (ticking){
        //            //Debug.Log("Delayed end called");
        //            var comp = tempLotusCollider.GetComponent<SlowDownProjectiles>();
        //            timer += Time.fixedDeltaTime;
        //
        //            if (timer > .1f){
        //                handler.slowCoeffValue += .0015f;
        //                comp.slowDownCoefficient = handler.slowCoeffValue;
        //            }
        //
        //            if (handler.slowCoeffValue >= 1){
        //                handler.slowCoeffValue = 1;
        //                comp.slowDownCoefficient = 1;
        //                Destroy(this);
        //            }
        //        }
        //    }
        //}

        //public class LotusBodyToken : MonoBehaviour {
        //    public CharacterBody body;
        //    public TemporaryOverlay overlay;
        //    public float coeff;
        //    public float duration;
        //    public Material matInstance;
        //    //public float oldCoeff;
        //
        //    public void Begin()
        //    {
        //
        //        var transform = body.modelLocator.modelTransform;
        //        var overlay = TemporaryOverlayManager.AddOverlay(transform.gameObject);
        //        overlay.duration = duration;
        //        overlay.alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, .5f);
        //        //
        //        overlay.animateShaderAlpha = true;
        //        overlay.destroyComponentOnEnd = true;
        //        overlay.originalMaterial = matInstance;
        //        overlay.AddToCharacterModel(transform.GetComponent<RoR2.CharacterModel>());
        //    }
        //
        //    public void End()
        //    {
        //        overlay.RemoveFromCharacterModel();
        //    }
        //
        //    void FixedUpdate()
        //    {
        //        matInstance.SetFloat("_Boost", 1 - coeff);
        //        //Debug.Log("boost: " + matInstance.GetFloat("_Boost"));
        //    }
        //}
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
