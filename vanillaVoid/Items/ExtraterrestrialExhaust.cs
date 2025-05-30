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
//using On.RoR2.Items;
using RoR2.Projectile;
using System.Collections;
using RoR2.Items;

#nullable enable 

namespace vanillaVoid.Items
{
    public class ExtraterrestrialExhaust : ItemBase<ExtraterrestrialExhaust>
    {
        //public ConfigEntry<float> rocketsPerSecond;

        public ConfigEntry<float> rocketDamage;

        public ConfigEntry<float> rocketDamageStacking;

        public ConfigEntry<float> secondsPerRocket;

        public ConfigEntry<float> exhaustCoefficient;

        public ConfigEntry<bool> accurateRockets;

        public ConfigEntry<bool> visionsNerf;

        public override string ItemName => "Extraterrestrial Exhaust";

        public override string ItemLangTokenName => "EXT_EXHAUST_ITEM";

        public override string ItemPickupDesc => $"Upon activating a skill, fire a number of rockets depending on the skill's cooldown. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"Upon <style=cIsUtility>activating a skill</style>, <style=cIsDamage>fire a rocket</style> for <style=cIsUtility>every {secondsPerRocket.Value} seconds</style> of the skill's <style=cIsUtility>cooldown</style>, dealing <style=cIsDamage>{rocketDamage.Value}%</style>" +
            (rocketDamageStacking.Value != 0 ? $" <style=cStack>(+{rocketDamageStacking.Value}% per stack)</style>" : "") + $" base damage. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"<style=cMono>//-- AUTO-TRANSCRIPTION FROM RALLYPOINT EPSILON RECORDER 7 --//</style>" +
            "\n\n\"I... I had a dream...and..and, I know it was a dream. There's no way it c-could've been anything else. I..it felt so real, but....it..It Could Not Have Been. It..it ccould not have been anything else, I-I refused to acceptt it. I have.. and will.. continue to remain safe in this shelter while the others are out, and and they'll, they'll be back soon. Any time now rreally. I will tend to my p-plants, and not worry myself ssick over this nonsense..it was a dream. It was all a ddream. I must've just..p-planted this one while I was tired..I...maybe I should get more sleep... \"";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlVentPickup.prefab");
       
        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("ventIcon512.png"); //amgous!!! AAAAAAA

        public static GameObject RocketProjectile;
        //public static GameObject RocketExplosion;

        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Damage, ItemTag.AIBlacklist };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            //VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            CreateProjectile();
            Hooks();
        }

        public override void CreateConfig(ConfigFile config)
        {
            //rocketsPerSecond = config.Bind<float>("Item: " + ItemName, "Rockets per Second", 2f, "Adjust the number of rockets fired for each second of skill cooldown.");
            secondsPerRocket = config.Bind<float>("Item: " + ItemName, "Seconds per Rocket", 2f, "Adjust the number of seconds of skill cooldown needed to fire a rocket. (1 = 1 rocket per second of cooldown)");
            rocketDamage = config.Bind<float>("Item: " + ItemName, "Projectile Damage Percent", 80f, "Adjust the percent damage dealt on the first stack. (100 = 100% base damage)");
            rocketDamageStacking = config.Bind<float>("Item: " + ItemName, "Stacking Projectile Damage Percent", 40f, "Adjust the percent damage gained per stack. (100 = 100% base damage)");
            exhaustCoefficient = config.Bind<float>("Item: " + ItemName, "Proc Coefficient", .2f, "Adjust the proc coefficient for the item's fired projectiles. For reference, Fireworks' is .2 per missile. (0 is no procs, 1 is normal proc rate)");
            accurateRockets = config.Bind<bool>("Item: " + ItemName, "More Accurate Rockets", false, "Adjust if the rockets should more accurately seek out enemies. Advised if another mod adjusts fireworks to be more accurate.");
            visionsNerf = config.Bind<bool>("Item: " + ItemName, "Visions of Heresy Nerf", false, "Adjust if Visions of Heresy should fire half as many rockets as it normally would with Exhaust.");

            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "Firework", "Adjust which item this is the void pair of.");
        }

        private void CreateProjectile() 
        {
            RocketProjectile = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Firework/FireworkProjectile.prefab").WaitForCompletion(), "RocketProjectile", true);
            var impactExplosion = RocketProjectile.GetComponent<ProjectileImpactExplosion>();
            impactExplosion.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MissileVoid/VoidImpactEffect.prefab").WaitForCompletion();
            

            var model = MainAssets.LoadAsset<GameObject>("ventOrb.prefab");
            
            model.AddComponent<NetworkIdentity>();
            model.AddComponent<ProjectileGhostController>();

            //var netID = model.GetComponent<NetworkIdentity>();
            if (accurateRockets.Value)
            {
                var fireworkController = RocketProjectile.GetComponent<MissileController>();
                fireworkController.acceleration = 3f;
                fireworkController.giveupTimer = 30f;
                fireworkController.deathTimer = 30f;
                fireworkController.turbulence = 0f;
                fireworkController.maxSeekDistance = 10000f;
            }

            var projectileController = RocketProjectile.GetComponent<ProjectileController>();
            projectileController.ghostPrefab = model;
            projectileController.startSound = "Play_item_void_critGlasses";
            projectileController.allowPrediction = false;
            projectileController.procCoefficient = exhaustCoefficient.Value;
            //RocketProjectile.GetComponent<EffectComponent>().soundName = "Play_item_void_critGlasses";
            //RocketProjectile.GetComponent<ProjectileExplosion>();

            projectileController.ghostPrefab.AddComponent<VFXAttributes>().DoNotPool = true;

            PrefabAPI.RegisterNetworkPrefab(RocketProjectile);
            //ProjectileAPI.Add(Projectile);
            ContentAddition.AddProjectile(RocketProjectile);

        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = MainAssets.LoadAsset<GameObject>("mdlVentDisplay.prefab");
            //var projectileModel = MainAssets.LoadAsset<GameObject>("ventOrb.prefab");

            //string guardTexture = "RoR2/DLC1/gauntlets/mVoidRaidPortalEdge.mat";
            //string ballTexture = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorBlasterSphereAreaIndicator.mat";
            ////string outerBall = "RoR2/DLC1/VoidRaidCrab/matVoidRaidCrabEye.mat";
            //
            //

            //
            //var guardPickup = ItemModel.transform.Find("Guard").GetComponent<MeshRenderer>();
            //guardPickup.material = Addressables.LoadAssetAsync<Material>(guardTexture).WaitForCompletion();

            //var testShell = ItemModel.transform.Find("Orb").GetComponent<MeshRenderer>();
            //testShell.material = Addressables.LoadAssetAsync<Material>(ballTexture).WaitForCompletion();


            //var guardDisplay = ItemBodyModelPrefab.transform.Find("Guard").GetComponent<MeshRenderer>();
            //guardDisplay.material = Addressables.LoadAssetAsync<Material>(guardTexture).WaitForCompletion();
            //
            //var projCenter = projectileModel.transform.Find("Center").GetComponent<MeshRenderer>();
            //projCenter.material = Addressables.LoadAssetAsync<Material>(ballTexture).WaitForCompletion();
            //
            //var projShell = projectileModel.transform.Find("Orb").GetComponent<MeshRenderer>();
            //projShell.material = Addressables.LoadAssetAsync<Material>(ballTexture).WaitForCompletion();

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            var mpp = ItemModel.AddComponent<ModelPanelParameters>();
            mpp.focusPointTransform = ItemModel.transform.Find("Target");
            mpp.cameraPositionTransform = ItemModel.transform.Find("Source");
            mpp.minDistance = 6f;
            mpp.maxDistance = 12f;
            mpp.modelRotation = Quaternion.Euler(new Vector3(0, 0, 0));

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.0110546f, 0.4341825f, -0.2253848f),
                    localAngles = new Vector3(359.2442f, 167.7944f, 354.3296f),
                    localScale = new Vector3(0.03f, 0.03f, 0.03f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1399796f, 0.2696737f, -0.1383592f),
                    localAngles = new Vector3(29.00727f, 121.5664f, 2.413216f),
                    localScale = new Vector3(0.0225f, 0.0225f, 0.0225f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.008324906f, 0.2495107f, -0.2114665f),
                    localAngles = new Vector3(4.861009f, 157.5361f, 355.5444f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.4376241f, 2.32833f, -2.461288f),
                    localAngles = new Vector3(1.790418f, 91.38383f, 351.9803f),
                    localScale = new Vector3(0.1f, 0.1f, 0.1f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.01026348f, 0.4762573f, -0.2577912f),
                    localAngles = new Vector3(0.757121f, 84.66977f, 4.730944f),
                    localScale = new Vector3(0.035f, 0.035f, 0.035f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.4434818f, 1.443157f, -1.160039f),
                    localAngles = new Vector3(0F, 270F, 0f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)

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
                    localPos = new Vector3(0.1097722f, 0.378505f, -0.1722545f),
                    localAngles = new Vector3(355.193f, 120.7892f, 7.731801f),
                    localScale = new Vector3(0.0175f, 0.0175f, 0.0175f)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.1149599f, 0.3788371f, -0.1733119f),
                    localAngles = new Vector3(355.193f, 120.7892f, 7.731801f),
                    localScale = new Vector3(0.0175f, 0.0175f, 0.0175f)
                }

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.006229824f, 0.1942842f, -0.3125757f),
                    localAngles = new Vector3(9.990238f, 94.72073f, 283.2024f),
                    localScale = new Vector3(0.015f, 0.0125f, 0.015f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "FootFrontR",
                    localPos = new Vector3(-0.1326539f, 0.3482078f, -0.02275122f),
                    localAngles = new Vector3(349.3916f, 271.9016f, 183.2796f),
                    localScale = new Vector3(0.02f, 0.02f, 0.02f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1311413f, 0.5595166f, -0.2539013f),
                    localAngles = new Vector3(0f, 91.21104f, 4.466461f),
                    localScale = new Vector3(0.02f, 0.02f, 0.02f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(2.481646f, 0.7097853f, 3.123438f),
                    localAngles = new Vector3(322.2655f, 121.3876f, 105.6704f),
                    localScale = new Vector3(0.125f, 0.125f, 0.125f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.02279982f, 0.3192666f, -0.2125499f),
                    localAngles = new Vector3(357.248f, 212.8494f, 12.82024f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "BottomRail",
                    localPos = new Vector3(-0.001957409f, 0.7204793f, -0.06810854f),
                    localAngles = new Vector3(5.573401f, 188.9093f, 0f),
                    localScale = new Vector3(0.02f, 0.02f, 0.02f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmL",
                    localPos = new Vector3(0.08418835f, 0.1827631f, 0.024172f),
                    localAngles = new Vector3(349.521f, 175.745f, 74.09998f),
                    localScale = new Vector3(0.01f, 0.01f, 0.01f)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmL",
                    localPos = new Vector3(0.09908976f, 0.1211472f, 0.003674036f),
                    localAngles = new Vector3(356.6176f, 198.4006f, 71.32525f),
                    localScale = new Vector3(0.0075f, 0.0075f, 0.0075f)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmL",
                    localPos = new Vector3(0.05727483f, 0.239799f, 0.04521416f),
                    localAngles = new Vector3(326.0425f, 334.65f, 283.8331f),
                    localScale = new Vector3(0.0045f, 0.0045f, 0.0045f)
                }
            });
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.14285F, 0.17762F, -0.21428F),
                    localAngles = new Vector3(29.44553F, 102.6059F, 4.5236F),
                    localScale = new Vector3(0.04F, 0.04F, 0.04F)
                }
            });
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.36731F, -0.14817F, 0.21139F),
                    localAngles = new Vector3(0F, 0F, 92.25253F),
                    localScale = new Vector3(0.03F, 0.03F, 0.03F)
                }
            });
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.03705F, 0.03254F, -0.30368F),
                    localAngles = new Vector3(23.29488F, 115.029F, 296.4156F),
                    localScale = new Vector3(0.045F, 0.045F, 0.045F)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(6.042785f, 3.98034f, 1.600928f),
                    localAngles = new Vector3(16.82158f, 136.9528f, 52.64933f),
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
                    childName =  "Pelvis",
                    localPos =   new Vector3(0.1125845f, 0.007865861f, 0.2631455f),
                    localAngles = new Vector3(343.1581f, 318.323f, 260.9673f),
                    localScale = new Vector3(.025f, .035f, .025f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos =   new Vector3(0.0001687468f, -0.0008735295f, 0.008880665f),
                    localAngles = new Vector3(53.58856f, 299.2462f, 291.1205f),
                    localScale = new Vector3(.0004f, .0004f, .0004f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] //these ones don't work for some reason!
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos =   new Vector3(-0.2453761f, 0.582325f, 0.009619509f),
                    localAngles = new Vector3(0.6984521f, 82.43644f, 0.5239916f),
                    localScale = new Vector3(.025f, .025f, .025f)
                }
            });
            //rules.Add("mdlCHEF", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Head",
            //        localPos = new Vector3(1.312011f, 0.5727088f, -0.789186f),
            //        localAngles = new Vector3(357.4176f, 288.5043f, 1.859076f),
            //        localScale = new Vector3(.12f, .12f, .12f)
            //    }
            //});
            rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "PickL",
                    localPos =   new Vector3(-0.001340121f, 0.0004611782f, 0.001501753f),
                    localAngles = new Vector3(20.54011f, 359.8128f, 8.332644f),
                    localScale = new Vector3(.00025f, .00025f, .00025f)
                }
            });
            rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.05744834f, 0.1742157f, -0.1112903f),
                    localAngles = new Vector3(10.45809f, 266.0404f, 160.3855f),
                    localScale = new Vector3(.025f, .025f, .025f)
                }
            });
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandR",
                    localPos =   new Vector3(0.03721922f, 0.10253f, 0.07642128f),
                    localAngles = new Vector3(85.65891f, 155.6693f, 142.5529f),
                    localScale = new Vector3(.01f, .01f, .01f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerTorso",
                    localPos =   new Vector3(0.1349856f, 0.2377253f, 0.03491673f),
                    localAngles = new Vector3(12.96118f, 99.2438f, 6.065573f),
                    localScale = new Vector3(.01f, .01f, .01f)
                }
            });
            rules.Add("mdlExecutioner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.001051476f, -0.0006094702f, 0.001399369f),
                    localAngles = new Vector3(355.4796f, 337.4572f, 181.8065f),
                    localScale = new Vector3(0.00015f, 0.00015f, 0.00015f)
                }
            });
            rules.Add("mdlNemmando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.001057187f, -0.0001041672f, -0.001190624f),
                    localAngles = new Vector3(5.730634f, 25.73074f, 1.60499f),
                    localScale = new Vector3(0.0001f, 0.0001f, 0.0001f)
                }
            });
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.05601266f, 0.09349241f, 0.06325182f),
                    localAngles = new Vector3(357.7929f, 171.6497f, 170.6236f),
                    localScale = new Vector3(.0125f, .0125f, .0125f)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.07003837f, 0.1822689f, 0.04212472f),
                    localAngles = new Vector3(14.81966f, 107.9849f, 169.126f),
                    localScale = new Vector3(.0125f, .0125f, .0125f)
                }
            });
            rules.Add("mdlHANDOverclocked", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(1.312011f, 0.5727088f, -0.789186f),
                    localAngles = new Vector3(357.4176f, 288.5043f, 1.859076f),
                    localScale = new Vector3(.12f, .12f, .12f)
                }
            });
            rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "BlackBox",
                    localPos = new Vector3(-0.04008894f, 0.4325569f, -0.1809917f),
                    localAngles = new Vector3(15.42795f, 0.09629212f, 89.88361f),
                    localScale = new Vector3(.035f, .035f, .035f)
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
                    localPos = new Vector3(1.26923F, 1.05318F, -0.09499F),
                    localAngles = new Vector3(0F, 355.7435F, 0.00002F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
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
                    childName = "leg_bone2.L",
                    localPos = new Vector3(-0.32548F, 0.22482F, 0.09749F),
                    localAngles = new Vector3(3.90754F, 179.2157F, 192.2801F),
                    localScale = new Vector3(0.035F, 0.035F, 0.035F)
                }
            });
            rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ShoulderR",
                    localPos = new Vector3(-0.02813F, -0.10344F, -0.05165F),
                    localAngles = new Vector3(24.14783F, 357.26F, 157.5212F),
                    localScale = new Vector3(0.015F, 0.015F, 0.015F)
                }
            });
            rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0.80923F, -0.03724F, 0.63302F),
                    localAngles = new Vector3(4.29694F, 144.3949F, 176.929F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Shotgun",
                    localPos = new Vector3(-0.03222318f, -0.3880526f, -0.6756665f),
                    localAngles = new Vector3(295.934f, 174.899f, 183.5748f),
                    localScale = new Vector3(.01f, .01f, .01f)
                }
            });
            rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.20181F, -0.13054F, 0.20042F),
                    localAngles = new Vector3(18.90175F, 201.0376F, 141.2642F),
                    localScale = new Vector3(0.045F, 0.045F, 0.045F)
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
                    childName = "CalfR",
                    localPos = new Vector3(-0.12546F, 0.07493F, -0.00753F),
                    localAngles = new Vector3(359.5648F, 101.6721F, 177.7695F),
                    localScale = new Vector3(0.02F, 0.02F, 0.02F)
                }
            });
            rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfR",
                    localPos = new Vector3(0.16078F, 0.05913F, -0.16904F),
                    localAngles = new Vector3(3.01908F, 274.7029F, 183.3804F),
                    localScale = new Vector3(0.02F, 0.02F, 0.02F)
                }
            });
            rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.12312F, 0.60723F, -0.31391F),
                    localAngles = new Vector3(4.18845F, 0F, 0F),
                    localScale = new Vector3(0.0125F, 0.0125F, 0.0125F)
                }
            });

            rules.Add("RA2ChronoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(-0.12354F, 0.09663F, -0.16216F),
                    localAngles = new Vector3(18.85852F, 355.1425F, 196.0733F),
                    localScale = new Vector3(0.025F, 0.025F, 0.025F)
                }
            });
            rules.Add("RobRavagerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.01105F, 0.43418F, -0.22538F),
                    localAngles = new Vector3(359.8066F, 357.293F, 5.71713F),
                    localScale = new Vector3(0.03F, 0.03F, 0.03F)
                }
            });
            rules.Add("mdlMorris", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(0.04001F, 0.37044F, -0.19773F),
                    localAngles = new Vector3(350.7635F, 156.4062F, 184.0101F),
                    localScale = new Vector3(0.025F, 0.025F, 0.025F)
                }
            });
            return rules;

        }

        public override void Hooks(){
            On.RoR2.GenericSkill.DeductStock += ExtExhaustStock;
            On.RoR2.Skills.SkillDef.OnExecute += ExtExecute;

            On.EntityStates.Mage.Weapon.PrepWall.OnExit += ExtExhaustIceWall; //even with the new method of doing this, ice wall is an exception for some reason
            On.EntityStates.Engi.EngiMissilePainter.Fire.FireMissile += ExtExhaustHarpoonFire;

            On.RoR2.CharacterBody.OnInventoryChanged += AddExhaustToken;
        }

        private void AddExhaustToken(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self) {
            orig(self);
            if (self.inventory) {
                int itemCount = self.inventory.GetItemCount(ItemDef);
                var token = self.gameObject.GetComponent<ExhaustHandler>();
                if (itemCount > 0){
                    //var token = self.gameObject.GetComponent<AirdashToken>();
                    if (!token){
                        token = self.gameObject.AddComponent<ExhaustHandler>();
                    }
                } else if (token) {
                    GameObject.Destroy(token);
                }
            }
        }

        private void ExtExecute(On.RoR2.Skills.SkillDef.orig_OnExecute orig, RoR2.Skills.SkillDef self, GenericSkill skillSlot){
            orig(self, skillSlot);
            if (self.stockToConsume >= 1)
            { //if the stock is consumed here, then it won't be consumed later - active rockets now 
                //var body = skillSlot.characterBody;
                //if (skillSlot.characterBody != body) { return; }

                if (skillSlot.characterBody) { //to be safe i guess
                    var token = skillSlot.characterBody.gameObject.GetComponent<ExhaustHandler>();
                    if (token) { token.TryExhaust(skillSlot.characterBody, skillSlot); }
                }
            }
        }

        private void ExtExhaustStock(On.RoR2.GenericSkill.orig_DeductStock orig, GenericSkill self, int count){
            orig(self, count);

            if (self.characterBody){
                var token = self.characterBody.gameObject.GetComponent<ExhaustHandler>();
                if (token) { token.TryExhaust(self.characterBody, self); } //skill is special and calls deduct stock itself - fire now
            }
        }

        private void ExtExhaustIceWall(On.EntityStates.Mage.Weapon.PrepWall.orig_OnExit orig, EntityStates.Mage.Weapon.PrepWall self){
            if (self.goodPlacement)
            {
                if (self.characterBody)
                {
                    //var inventory = self.characterBody.inventory;
                    int count = GetCount(self.characterBody);
                    if (count > 0)
                    {
                        //var inventoryCount = inventory.GetItemCount(ItemBase<ExtraterrestrialExhaust>.instance.ItemDef);
                        var skill = self.skillLocator.utilityBonusStockSkill;
                        if (skill.cooldownRemaining > 0)
                        { //maybe make this higher
                            float skillCD = skill.baseRechargeInterval;

                            int missleCount = (int)Math.Ceiling(skillCD / secondsPerRocket.Value);

                            var token = self.characterBody.gameObject.GetComponent<ExhaustHandler>();
                            if (token) { token.FireIntermediate(self.characterBody, missleCount, count); }
                            //StartCoroutine(delayedRockets(self.characterBody, missleCount, count)); //this can probably be done better
                        }
                    }
                }
            }
            orig(self);
        }

        private void ExtExhaustHarpoonFire(On.EntityStates.Engi.EngiMissilePainter.Fire.orig_FireMissile orig, EntityStates.Engi.EngiMissilePainter.Fire self, HurtBox target, Vector3 position)
        {
            orig(self, target, position);
            //if (self.characterBody != body) { return; }
            int count = GetCount(self.characterBody);
            if (count > 0)
            {
                var skill = self.skillLocator.utilityBonusStockSkill;
                float skillCD = skill.baseRechargeInterval;

                int missleCount = (int)Math.Ceiling(skillCD / secondsPerRocket.Value);

                var token = self.characterBody.gameObject.GetComponent<ExhaustHandler>();
                if (token) { token.FireIntermediate(self.characterBody, missleCount, count, target.gameObject); }
            }
        }
    }

    public class ExhaustHandler : MonoBehaviour {
        public void FireIntermediate(RoR2.CharacterBody player, int missileCount, int inventoryCount, GameObject? victim = null)
        {
            StartCoroutine(delayedRockets(player, missileCount, inventoryCount, victim));
        }

        public void TryExhaust(CharacterBody body, GenericSkill skill){
            //int inventoryCount = body.inventory.GetItemCount(ItemBase<ExtraterrestrialExhaust>.instance.ItemDef);
            int count = body.inventory.GetItemCount(ExtraterrestrialExhaust.instance.ItemDef);
            if (count > 0 && skill.cooldownRemaining > 0 && skill.skillDef.skillNameToken != "MAGE_UTILITY_ICE_NAME" && skill.skillDef.skillNameToken != "ENGI_SKILL_HARPOON_NAME") { //ice wall is stupid
                float skillCD = skill.baseRechargeInterval;

                int missleCount = (int)Math.Ceiling(skillCD / ExtraterrestrialExhaust.instance.secondsPerRocket.Value);

                if (skill.skillDef.skillNameToken != "SKILL_LUNAR_PRIMARY_REPLACEMENT_NAME" && ExtraterrestrialExhaust.instance.visionsNerf.Value)
                {
                    missleCount = skill.stock % 2 != 0 ? 1 : 0;
                }
                StartCoroutine(delayedRockets(body, missleCount, count)); //this can probably be done better
            }
        }

        IEnumerator delayedRockets(RoR2.CharacterBody player, int missileCount, int inventoryCount, GameObject? victim = null) {
            for (int i = 0; i < missileCount; i++) {
                yield return new WaitForSeconds(.1f);
                Vector3 Upwards = new Vector3(UnityEngine.Random.Range(-10, 10), 180 - UnityEngine.Random.Range(-30, 30), UnityEngine.Random.Range(-10, 10));
                Vector3 upTransform = new Vector3(0, 1, 0);

                float rocketDamage = player.damage * ((ExtraterrestrialExhaust.instance.rocketDamage.Value + (ExtraterrestrialExhaust.instance.rocketDamageStacking.Value * (inventoryCount - 1))) / 100);

                ProcChainMask procChainMask = default(ProcChainMask);

                MissileUtils.FireMissile(player.corePosition + upTransform, player, procChainMask, victim, rocketDamage, player.RollCrit(), ExtraterrestrialExhaust.RocketProjectile, DamageColorIndex.Item, Upwards, 10f, false);
            }
        }
    }
}


