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

namespace vanillaVoid.Items
{
    public class CryoCanister : ItemBase<CryoCanister>
    {
        public ConfigEntry<float> baseDamageAOE;

        public ConfigEntry<float> stackingDamageAOE;

        public ConfigEntry<float> aoeRangeBase;

        public ConfigEntry<float> aoeRangeStacking;

        public ConfigEntry<int> requiredStacksForFreeze;

        public ConfigEntry<int> requiredStacksForBossFreeze;

        public ConfigEntry<float> slowDuration;

        public ConfigEntry<float> slowDurationStacking;

        public ConfigEntry<float> slowPercentage;

        public ConfigEntry<float> cryoCoefficient;

        public ConfigEntry<bool> displaySlowAmount;

        public override string ItemName => "Supercritical Coolant";

        public override string ItemLangTokenName => "CRYOCANISTER_ITEM";

        public override string ItemPickupDesc => $"Killing an enemy slows and eventually freezes other nearby enemies. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"Killing an enemy <style=cIsUtility>slows</style> all enemies" +
            (displaySlowAmount.Value ? $" by up to <style=cIsUtility>{slowPercentage.Value * 100}%</style>" : "") + $" within <style=cIsDamage>{aoeRangeBase.Value}m</style>" +
            (aoeRangeStacking.Value != 0 ? $" <style=cStack>(+{aoeRangeStacking.Value}m per stack)</style>" : "") + $" for <style=cIsDamage>{baseDamageAOE.Value * 100}%</style>" +
            (stackingDamageAOE.Value != 0 ? $" <style=cStack>(+{stackingDamageAOE.Value * 100}% per stack)</style>" : "") + $" base damage, which lasts for <style=cIsUtility>{slowDuration.Value}</style>" +
            (slowDurationStacking.Value != 0 ? $" <style=cStack>(+{slowDurationStacking.Value} per stack)</style>" : "") + $" seconds. Upon applying <style=cIsUtility>{requiredStacksForFreeze.Value} stacks</style> of <style=cIsUtility>slow</style> to an enemy, they are <style=cIsDamage>frozen</style>. " +
            (requiredStacksForBossFreeze.Value > 0 ? $"Freezing is less effective on bosses." : "Cannot freeze bosses.") + $" <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"<style=cSub>Order: Supercritical Coolant \nTracking Number: 03691215 \nEstimated Delivery: 25/10/2112 \nShipping Method: High Priority/Fragile \nShipping Address: [REDACTED] \nShipping Details: \n\n</style>" +
            "Originally we studied Void occurrences from afar, observing and cataloguing the distribution of galaxies and refining cosmological evolution models. We are in a new age of cosmic exploration. Advancements in space travel partnered with determined curiosity have brought us closer to our object of study, and with it, revelation.";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCryoPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("cryoIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Damage, ItemTag.OnKillEffect };

        public BuffDef preFreezeSlow { get; private set; }

        //public GameObject iceDeathAOEObject;// = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteIce/AffixWhiteExplosion.prefab").WaitForCompletion();
        //GameObject iceDeathObject;

        public static GameObject iceDeathAOEObject { get; set; } = MainAssets.LoadAsset<GameObject>("IceExplosionAoe");

        public static GameObject iceDeathAOE2 { get; set; } = MainAssets.LoadAsset<GameObject>("CryoExplosionAOE");

        //GameObject gameObject = UnityEngine.Object.Instantiate(iceDeathAOEObject, position, Quaternion.identity);


        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            //VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            CreateBuff();

            var vfxattr = iceDeathAOEObject.AddComponent<VFXAttributes>();
            vfxattr.vfxPriority = VFXAttributes.VFXPriority.Low;
            vfxattr.vfxIntensity = VFXAttributes.VFXIntensity.Low;

            var effect = iceDeathAOEObject.AddComponent<EffectComponent>();
            effect.applyScale = true;
            //effectcomp.positionAtReferencedTransform = false;

            iceDeathAOEObject.AddComponent<DestroyOnParticleEnd>();

            ContentAddition.AddEffect(iceDeathAOEObject);


            var efc = iceDeathAOE2.AddComponent<EffectComponent>();
            efc.soundName = "Play_item_proc_iceRingSpear";
            efc.applyScale = true;

            var light = iceDeathAOE2.transform.Find("Point Light");
            var lic = light.gameObject.AddComponent<LightIntensityCurve>();

            AnimationCurve cryocurve = new AnimationCurve {
                keys = new Keyframe[] { new Keyframe(0, 1), new Keyframe(.5f, .25f), new Keyframe(1, 0) },
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };

            lic.curve = cryocurve;
            lic.timeMax = .3f;

            light.gameObject.AddComponent<LightScaleFromParent>();

            var vfx = iceDeathAOE2.AddComponent<VFXAttributes>();
            vfx.vfxPriority = VFXAttributes.VFXPriority.Medium;
            vfx.vfxIntensity = VFXAttributes.VFXIntensity.Medium;

            vfx.optionalLights = new Light[1];
            vfx.optionalLights[0] = light.gameObject.GetComponent<Light>();

            iceDeathAOE2.AddComponent<DestroyOnParticleEnd>();

            ContentAddition.AddEffect(iceDeathAOE2);

            //RoR2/Base/EliteIce/AffixWhiteExplosion.prefab
            //string aoePath = "RoR2/Base/Common/VFX/OmniImpactVFXFrozen.prefab"; // "RoR2 /Base/Common/VFX/OmniImpactVFXFrozen.prefab";
            //iceDeathAOEObjectLazy = Addressables.LoadAssetAsync<GameObject>(aoePath).WaitForCompletion();
            //
            //Debug.Log(" cryo canister vfx fake: " + iceDeathAOEObjectLazy);
            //
            //string aoePath2 = "RoR2/Base/EliteIce/AffixWhiteExplosion.prefab";
            //iceDeathAOEObjectFucked = Addressables.LoadAssetAsync<GameObject>(aoePath).WaitForCompletion();
            //
            //GameObject iceDeathObjectFucked = UnityEngine.Object.Instantiate<GameObject>(iceDeathAOEObjectFucked);
            //iceDeathObjectFucked.AddComponent<NetworkIdentity>();
            //
            ////GameObject iceDeathObject = UnityEngine.Object.Instantiate<GameObject>(iceDeathAOEObject);

            //iceDeathAOEObject = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("CryoAOEExplosionEffect");
            ////iceDeathAOEObject = GlobalEventManager.CommonAssets.bleedOnHitAndExplodeImpactEffect;
            ////iceDeathAOEObject.
            //var effComp = iceDeathAOEObject.AddComponent<EffectComponent>();
            //effComp.positionAtReferencedTransform = true;
            //var vfxAtrb = iceDeathAOEObject.AddComponent<VFXAttributes>();
            //vfxAtrb.vfxIntensity = VFXAttributes.VFXIntensity.Medium;
            //vfxAtrb.vfxPriority = VFXAttributes.VFXPriority.Always;
            //ContentAddition.AddEffect(iceDeathAOEObject);
            //Debug.Log(" cryo canister vfx: " + iceDeathAOEObject);
            //iceDeathAOEObject.AddComponent<NetworkIdentity>();
            //PrefabAPI.RegisterNetworkPrefab(iceDeathAOEObject);

            //EffectDef effectDef = new EffectDef(iceDeathAOEObject);
            //ContentAddition.AddEffect(iceDeathAOEObject);
            //AssetsMainAssets.Add(effectDef);

            //i hate modding

            Hooks();
        }

        public void CreateBuff()
        {
            preFreezeSlow = ScriptableObject.CreateInstance<BuffDef>();
            preFreezeSlow.buffColor = Color.cyan;
            preFreezeSlow.canStack = true;
            preFreezeSlow.isDebuff = true;
            preFreezeSlow.name = "ZnVV" + "preFreezeSlow";
            preFreezeSlow.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("preFreezeSlow");
            ContentAddition.AddBuffDef(preFreezeSlow);
        }

        public override void CreateConfig(ConfigFile config)
        {
            baseDamageAOE = config.Bind<float>("Item: " + ItemName, "Base Damage", .2f, "Adjust the percent base damage the AOE does. (1 = 100% base damage)");
            stackingDamageAOE = config.Bind<float>("Item: " + ItemName, "Base Damage Increase per Stack", .2f, "Adjust the percent base damage the AOE gains per stack. (1 = 100% base damage)");
            aoeRangeBase = config.Bind<float>("Item: " + ItemName, "AOE Range", 12f, "Adjust the range of the slow AOE on the first stack. (1 = 1m range diameter)");
            aoeRangeStacking = config.Bind<float>("Item: " + ItemName, "AOE Range Increase per Stack", 4f, "Adjust the range the slow AOE gains per stack. (1 = 1m range diameter)");
            requiredStacksForFreeze = config.Bind<int>("Item: " + ItemName, "Debuff Stacks Required for Freeze", 3, "Adjust the number of stacks needed to freeze an enemy.");
            requiredStacksForBossFreeze = config.Bind<int>("Item: " + ItemName, "Buff Stacks Required for Boss Freeze", 10, "Adjust the number of stacks needed to freeze a boss. Set to 0 or less to remove this.");
            slowPercentage = config.Bind<float>("Item: " + ItemName, "Max Percent Slow", .5f, "Adjust the percentage slow the buff causes. (1 = 100% slow)");
            slowDuration = config.Bind<float>("Item: " + ItemName, "Slow Debuff Duration", 6, "Adjust the duration the slow lasts, in seconds.");
            slowDurationStacking = config.Bind<float>("Item: " + ItemName, "Slow Debuff Duration per Stack", 3, "Adjust the duration the slow gains per stack.");
            cryoCoefficient = config.Bind<float>("Item: " + ItemName, "Proc Coefficient", 0, "Adjust the proc coefficient for the item's damage AOE. For reference, Gasoline's is 0. (0 is no procs, 1 is normal proc rate)");
            displaySlowAmount = config.Bind<bool>("Item: " + ItemName, "Display Slow Percent in Item Description", false, "Adjust whether the the slow percentage is displayed in the logbook description.");

            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "IgniteOnKill", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCryoDisplay.prefab");

            string fluidMat = "RoR2/Base/Huntress/matHuntressGlaive.mat"; //this one doesn't seem to work if loaded from assetbundle? so i guess this way it is

            var cryoFluidModel = ItemModel.transform.Find("Intermediate").Find("Glowybits").GetComponent<MeshRenderer>();
            cryoFluidModel.material = Addressables.LoadAssetAsync<Material>(fluidMat).WaitForCompletion();
            
            var cryoFluidDisplay = ItemBodyModelPrefab.transform.Find("Glowybits").GetComponent<MeshRenderer>();
            cryoFluidDisplay.material = Addressables.LoadAssetAsync<Material>(fluidMat).WaitForCompletion();


            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            var mpp = ItemModel.AddComponent<ModelPanelParameters>();
            mpp.focusPointTransform = ItemModel.transform.Find("Target");
            mpp.cameraPositionTransform = ItemModel.transform.Find("Source");
            mpp.minDistance = 3.5f;
            mpp.maxDistance = 7.5f;
            mpp.modelRotation = Quaternion.Euler(new Vector3(0, 0, 0));

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1515263f, 0.06217923f, -0.01854283f),
                    localAngles = new Vector3(294.0765f, 24.31453f, 41.70001f),
                    localScale = new Vector3(0.08f, 0.08f, 0.08f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1019495f, -0.004779898f, 0.08037122f),
                    localAngles = new Vector3(307.2001f, 179.7792f, 96.47073f),
                    localScale = new Vector3(0.065f, 0.065f, 0.065f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.10892F, 0.15548F, -0.01474F),
                    localAngles = new Vector3(315.6412F, 193.1838F, 51.33574F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-1.213294f, 0.9907005f, -5.987041f),
                    localAngles = new Vector3(287.2282f, 278.8165f, 82.6935f),
                    localScale = new Vector3(.5f, .5f, .5f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1874898f, 0.1037676f, 0.04180111f),
                    localAngles = new Vector3(283.4243f, 178.2074f, 65.93817f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.7404389f, 0.07693511f, 0.002353907f),
                    localAngles = new Vector3(61.9685f, 160.8914f, 234.2564f),
                    localScale = new Vector3(.2f, .2f, .2f)

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
                    localPos = new Vector3(0.128604f, 0.04170567f, 0.0362651f),
                    localAngles = new Vector3(294.3157f, 50.21239f, 8.014451f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1502278f, 0.02505913f, 0.005195001f),
                    localAngles = new Vector3(312.9884f, 185.7034f, 77.97595f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HeadBase",
                    localPos = new Vector3(0.6552188f, -0.1591174f, -0.1081835f),
                    localAngles = new Vector3(28.51935f, 16.06732f, 68.01345f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1418974f, 0.1018404f, 0.1193604f),
                    localAngles = new Vector3(305.2793f, 13.87357f, 33.9735f),
                    localScale = new Vector3(0.09f, 0.09f, 0.09f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(1.189638f, -0.05416813f, -0.431556f),
                    localAngles = new Vector3(327.4012f, 170.4274f, 285.5928f),
                    localScale = new Vector3(0.5f, 0.5f, 0.5f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1160537f, -0.05998493f, 0.09456927f),
                    localAngles = new Vector3(352.1081f, 175.0939f, 243.0748f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(-0.09908636f, -0.4335292f, 0.0002002567f),
                    localAngles = new Vector3(3.861964f, 59.13883f, 332.0835f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(0.01783761f, 0.1200769f, -0.1452967f),
                    localAngles = new Vector3(315.8733f, 81.28423f, 78.75524f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.18375F, 0.2006F, -0.01614F),
                    localAngles = new Vector3(297.7247F, 59.245F, 30.80943F),
                    localScale = new Vector3(0.065F, 0.065F, 0.065F)
                }
            });
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.07254F, 0.01762F, 0.02657F),
                    localAngles = new Vector3(6.48965F, 152.4758F, 337.7243F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                }
            });
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.13133F, 0.46684F, -0.15223F),
                    localAngles = new Vector3(296.9298F, 263.6136F, 37.09455F),
                    localScale = new Vector3(0.0825F, 0.0825F, 0.0825F)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(4.056051f, -0.3732671f, 0.59979f),
                    localAngles = new Vector3(298.3842f, 184.4189f, 56.38975f),
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
                    childName =  "CalfL",
                    localPos =   new Vector3(0.03588771f, 0.09803209f, 0.1293422f),
                    localAngles = new Vector3(307.0168f, 268.4248f, 63.07141f),
                    localScale = new Vector3(0.085f, 0.085f, 0.085f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.0006026067f, 0.002815368f, 0.009515685f),
                    localAngles = new Vector3(308.2012f, 0.3406859f, 60.02274f),
                    localScale = new Vector3(0.003f, 0.003f, 0.003f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] //these ones don't work for some reason!
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(0.1704589f, 0.2944336f, 0.09629773f),
                    localAngles = new Vector3(302.1373f, 171.8758f, 50.29458f),
                    localScale = new Vector3(0.09f, 0.09f, 0.09f)
                }
            });
            //rules.Add("mdlCHEF", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Door",
            //        localPos = new Vector3(0F, 0.00347F, -0.00126F),
            //        localAngles = new Vector3(0F, 90F, 0F),
            //        localScale = new Vector3(0.01241F, 0.01241F, 0.01241F)
            //    }
            //});
            rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LegL",
                    localPos = new Vector3(0.0001376976f, 0.000773763f, 0.001397073f),
                    localAngles = new Vector3(296.4636f, 275.2156f, 65.79694f),
                    localScale = new Vector3(0.001f, 0.001f, 0.001f)
                }
            });
            rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.09503975f, 0.1687267f, 0.05470692f),
                    localAngles = new Vector3(298.8525f, 357.139f, 61.08296f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)
                }
            });
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(-0.2642168f, 0.08241536f, -0.03230814f),
                    localAngles = new Vector3(80.15402f, 192.351f, 264.9937f),
                    localScale = new Vector3(0.085f, 0.085f, 0.085f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Sheath",
                    localPos = new Vector3(0.04240592f, 0.2797743f, -0.004625648f),
                    localAngles = new Vector3(61.60046f, 171.9134f, 239.4733f),
                    localScale = new Vector3(0.061f, 0.061f, 0.061f)
                }
            });
            rules.Add("mdlExecutioner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.001009098f, 0.002151658f, -0.0001070039f),
                    localAngles = new Vector3(306.6896f, 353.9325f, 64.98692f),
                    localScale = new Vector3(0.0006f, 0.0006f, 0.0006f)
                }
            });
            rules.Add("mdlNemmando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.001197227f, 0.001904767f, 0.00006217563f),
                    localAngles = new Vector3(299.9001f, 176.2292f, 45.39405f),
                    localScale = new Vector3(0.0007f, 0.0007f, 0.0007f)
                }
            });
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0.06943482f, 0.02165982f, 0.1523041f),
                    localAngles = new Vector3(324.1993f, 338.8151f, 22.61926f),
                    localScale = new Vector3(.037f, .037f, .037f)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "StomachBone",
                    localPos = new Vector3(0.06085669f, 0.01383843f, -0.1985489f),
                    localAngles = new Vector3(56.67355f, 225.2023f, 222.7157f),
                    localScale = new Vector3(.06f, .06f, .06f)
                }
            });
            rules.Add("mdlHANDOverclocked", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.02344499f, -0.736992f, -0.7769091f),
                    localAngles = new Vector3(315.0295f, 220.1065f, 134.9871f),
                    localScale = new Vector3(.17f, .17f, .17f)
                }
            });
            rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "thigh.R",
                    localPos = new Vector3(-0.01114797f, 0.2202085f, 0.1858894f),
                    localAngles = new Vector3(299.1791f, 264.4482f, 47.60094f),
                    localScale = new Vector3(.06f, .06f, .06f)
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
                    childName = "ThighR",
                    localPos = new Vector3(0F, -0.66441F, -0.86397F),
                    localAngles = new Vector3(308.728F, 218.1655F, 149.5044F),
                    localScale = new Vector3(0.3F, 0.3F, 0.3F)
                }
            });
            //rules.Add("Spearman", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "thigh.l",
            //        localPos = new Vector3(0.00921F, -0.00021F, -0.00079F),
            //        localAngles = new Vector3(312.1787F, 335.8511F, 81.30679F),
            //        localScale = new Vector3(0.0025F, 0.0025F, 0.0025F)
            //    }
            //});
            rules.Add("mdlAssassin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "leg_bone1.L",
                    localPos = new Vector3(0.64538F, -0.20998F, 0.44171F),
                    localAngles = new Vector3(25.23727F, 359.1689F, 42.15331F),
                    localScale = new Vector3(0.125F, 0.125F, 0.125F)
                }
            });
            rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Gun",
                    localPos = new Vector3(0.21453F, 0.25686F, -0.01855F),
                    localAngles = new Vector3(24.05557F, 256.9858F, 244.0798F),
                    localScale = new Vector3(0.035F, 0.035F, 0.035F)
                }
            });
            rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.6435F, 0.08005F, -0.27187F),
                    localAngles = new Vector3(289.8074F, 138.0352F, 303.3961F),
                    localScale = new Vector3(0.225F, 0.225F, 0.225F)
                }
            });
            rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperLegL",
                    localPos = new Vector3(0.1238025f, 0.03419906f, -0.03510308f),
                    localAngles = new Vector3(298.8151f, 34.13566f, 56.94458f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
            });
            rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Stomach",
                    localPos = new Vector3(0.32346F, 0.03089F, 0.00371F),
                    localAngles = new Vector3(356.1124F, 171.2522F, 259.5945F),
                    localScale = new Vector3(0.1125F, 0.1125F, 0.1125F)
                }
            });
            //rules.Add("RobDriverBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "ThighL",
            //        localPos = new Vector3(0.1236295f, 0.09647375f, -0.02019574f),
            //        localAngles = new Vector3(300.3451f, 3.208119f, 59.22709f),
            //        localScale = new Vector3(.09f, .09f, .09f)
            //    }
            //});
            rules.Add("mdlTeslaTrooper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(-0.06972F, 0.1303F, 0.08915F),
                    localAngles = new Vector3(297.6132F, 27.87663F, 54.98442F),
                    localScale = new Vector3(0.07F, 0.07F, 0.07F)
                }
            });
            rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(-0.17152F, 0.10871F, 0.07852F),
                    localAngles = new Vector3(304.7397F, 44.80901F, 39.34168F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                }
            });
            rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "GunMuzzle",
                    localPos = new Vector3(-0.0296F, -0.20414F, 0.23172F),
                    localAngles = new Vector3(295.8711F, 11.80507F, 64.46541F),
                    localScale = new Vector3(0.0425F, 0.0425F, 0.0425F)
                }
            });


            rules.Add("RA2ChronoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "weapon_base",
                    localPos = new Vector3(-0.2016F, -0.5702F, -0.41532F),
                    localAngles = new Vector3(310.171F, 165.9906F, 261.7139F),
                    localScale = new Vector3(0.09F, 0.09F, 0.09F)
                }
            });
            rules.Add("RobRavagerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1515263f, 0.06217923f, -0.01854283f),
                    localAngles = new Vector3(294.0765f, 24.31453f, 41.70001f),
                    localScale = new Vector3(0.08f, 0.08f, 0.08f)
                }
            });
            rules.Add("mdlMorris", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.24497F, 0.05832F, 0.24744F),
                    localAngles = new Vector3(301.067F, 262.8177F, 30.36138F),
                    localScale = new Vector3(0.08F, 0.08F, 0.08F)
                }
            });

            return rules;
        }

        public override void Hooks()
        {
            //On.RoR2.HealthComponent.TakeDamage += AdzeDamageBonus;
            //GlobalEventManager.onCharacterDeathGlobal += CryoCanisterAOE;
            RecalculateStatsAPI.GetStatCoefficients += CalculateStatsCryoHook;
            GlobalEventManager.onCharacterDeathGlobal += CryoAoe2;
            //On.RoR2.CharacterBody.RecalculateStats += CryoStatsHook;

        }

        private void CryoAoe2(DamageReport obj)
        {

            if (!obj.attacker || !obj.attackerBody || !obj.victim || !obj.victimBody)
            {
                return; //end func if death wasn't killed by something real enough
            }

            CharacterBody victimBody = obj.victimBody;
            CharacterBody attackerBody = obj.attackerBody;
            
            if (attackerBody.inventory)
            {
                var cryoCount = attackerBody.inventory.GetItemCount(this.ItemDef);
                if (cryoCount > 0)
                {
                    float stackRadius = aoeRangeBase.Value + (aoeRangeStacking.Value * (float)(cryoCount - 1));
                    float victimRadius = victimBody.radius;
                    float effectiveRadius = stackRadius + victimRadius;
                    float AOEDamageMult = baseDamageAOE.Value + (stackingDamageAOE.Value * (float)(cryoCount - 1));
                    float AOEDamage = obj.attackerBody.damage * AOEDamageMult;

                    float duration = slowDuration.Value + (slowDurationStacking.Value * (cryoCount - 1));

                    var attackerTeamIndex = attackerBody.teamComponent.teamIndex;

                    Vector3 corePosition = victimBody.corePosition;

                    SphereSearch cryoAOESphereSearch = new SphereSearch();
                    List<HurtBox> cryoAOEHurtBoxBuffer = new List<HurtBox>();

                    cryoAOESphereSearch.origin = corePosition;
                    cryoAOESphereSearch.mask = LayerIndex.entityPrecise.mask;
                    cryoAOESphereSearch.radius = effectiveRadius;
                    cryoAOESphereSearch.RefreshCandidates();
                    cryoAOESphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(attackerTeamIndex));
                    cryoAOESphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                    cryoAOESphereSearch.OrderCandidatesByDistance();
                    cryoAOESphereSearch.GetHurtBoxes(cryoAOEHurtBoxBuffer);
                    cryoAOESphereSearch.ClearCandidates();

                    for (int i = 0; i < cryoAOEHurtBoxBuffer.Count; i++)
                    {
                        HurtBox hurtBox = cryoAOEHurtBoxBuffer[i];
                        //Debug.Log("hurtbox " + hurtBox);
                        if (hurtBox.healthComponent)
                        {
                            hurtBox.healthComponent.body.AddTimedBuffAuthority(preFreezeSlow.buffIndex, duration);
                            

                            if(hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow) >= requiredStacksForFreeze.Value && !hurtBox.healthComponent.body.isBoss)
                            {
                                SetStateOnHurt setState = hurtBox.healthComponent.body.gameObject.GetComponent<SetStateOnHurt>();
                                if (setState && setState.canBeFrozen)
                                {
                                    int buffCount = hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow);
                                    for (int j = 0; j < buffCount; j++)
                                    {
                                        hurtBox.healthComponent.body.RemoveOldestTimedBuff(preFreezeSlow);
                                    }
                            
                                    setState.SetFrozen(duration);

                                }
                            }
                            else if(hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow) >= requiredStacksForBossFreeze.Value && hurtBox.healthComponent.body.isBoss && requiredStacksForBossFreeze.Value > 0)
                            {
                                int buffCount = hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow);
                                for (int j = 0; j < buffCount; j++)
                                {
                                    hurtBox.healthComponent.body.RemoveOldestTimedBuff(preFreezeSlow);
                                }
                            
                                //setState.SetFrozen(duartion);
                                hurtBox.healthComponent.isInFrozenState = true;
                            }
                        }
                    }
                    cryoAOEHurtBoxBuffer.Clear();
                    
                    new BlastAttack
                    {
                        radius = effectiveRadius,
                        baseDamage = AOEDamage,
                        procCoefficient = cryoCoefficient.Value,
                        crit = Util.CheckRoll(obj.attackerBody.crit, obj.attackerMaster),
                        damageColorIndex = DamageColorIndex.Item,
                        attackerFiltering = AttackerFiltering.Default,
                        falloffModel = BlastAttack.FalloffModel.None,
                        attacker = obj.attacker,
                        teamIndex = attackerTeamIndex,
                        position = corePosition,
                        //baseForce = 0,
                        //damageType = DamageType.AOE
                    }.Fire();

                    //EntityStates.Mage.Weapon.IceNova.impactEffectPrefab
                    EffectManager.SpawnEffect(iceDeathAOEObject, new EffectData
                    {
                        origin = corePosition,
                        scale = effectiveRadius/9,
                        rotation = Util.QuaternionSafeLookRotation(obj.damageInfo.force)
                    }, true);

                    EffectManager.SpawnEffect(iceDeathAOE2, new EffectData
                    {
                        origin = corePosition,
                        scale = effectiveRadius,
                        rotation = Util.QuaternionSafeLookRotation(obj.damageInfo.force)
                    }, true);
                }
            }
        }

        private void CalculateStatsCryoHook(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            //Debug.Log("buh");
            if (sender)
            {
                int buffCount = sender.GetBuffCount(preFreezeSlow);
                if (buffCount > 0)
                {
                    //float a = slowPercentage.Value * 2;
                    //int b = requiredStacksForFreeze.Value - buffCount;
                    //if(b < 1)
                    //{
                    //    b = 1;
                    //}
                    float ratio;

                    //Debug.Log("aahh: " + ((float)buffCount) / ((float)requiredStacksForFreeze.Value - 1f));
                    if((requiredStacksForFreeze.Value - 1) != 0) //this is slightly better
                    {
                        if (buffCount >= requiredStacksForFreeze.Value - 1)
                        {
                            ratio = 1;
                        }
                        else
                        {
                            ratio = ((float)buffCount) / ((float)requiredStacksForFreeze.Value - 1f);
                        }
                    }
                    else
                    {
                        ratio = 1;
                    }
                    float slowProportion = -(slowPercentage.Value / (slowPercentage.Value - 1f)); //converts an input of .5 -> 50% to 1 which if added as a reductionmultadd you get 50% slow
                    //float stacks = requiredStacksForFreeze.Value - buffCount - 1; //4 - 3 - 1 = 0
                    //float distFromMax1 = slowProportion / (requiredStacksForFreeze.Value - buffCount - 1); //converts the max slow into a proportion based on missing stacks 

                    float distFromMax = slowProportion * ratio; //this no longer accurately splits the slow up between each stack but who fucking cares

                    //Debug.Log("ratio: " + ratio + " | " + distFromMax + " | " + buffCount + " | " + ((float)buffCount + 1f) / (float)requiredStacksForFreeze.Value);

                    args.moveSpeedReductionMultAdd += distFromMax;
                }
            }
        }
        
        public class CryoToken : MonoBehaviour
        {
        }
        public class preFreezeToken : MonoBehaviour
        {

        }
    }

}