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
using VoidItemAPI;
using EntityStates;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace vanillaVoid.Items
{
    public class BivalveShell : ItemBase<BivalveShell>
    {
        public ConfigEntry<float> bivalveAttackSpeed;

        //public ConfigEntry<bool> doRailer;

        public ConfigEntry<string> voidPair;

        public override string ItemName => "Bivalve Shell";

        public override string ItemLangTokenName => "BIVALVESHELL_ITEM";

        public override string ItemPickupDesc => "Increases attack speed while your Secondary skill is on cooldown <style=cIsVoid>Corrupts all Backup Magazines</style>.";

        public override string ItemFullDescription => $"Increase attack speed by {bivalveAttackSpeed.Value}% <style=cStack>(+{bivalveAttackSpeed.Value}% per stack)</style> while your Secondary Skill is on cooldown. <style=cIsVoid>Corrupts all Backup Magazines</style>.";

        public override string ItemLore => "working on it";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlQuillPickupReal.prefab"); //quill for now

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("quillIcon512.png"); //quill for now


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.Damage };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);

            Hooks(); 
        }

        public override void CreateConfig(ConfigFile config)
        {
            bivalveAttackSpeed = config.Bind<float>("Item: " + ItemName, "Bonus attack speed", 0.2f, "Attack speed increase per stack while secondary is on cooldown.");
            //doRailer = config.Bind<bool>("Item: " + ItemName, "Railgunner Hooks", true, "Allows Bivalve Shell to affect reload window and special cooldown. Disable for compatability.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "SecondarySkillMagazine", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlQuillSoloDisplay.prefab");
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
                    childName = "UpperArmL",
                    localPos = new Vector3(0.1353241f, 0.08783203f, 0.03012169f),
                    localAngles = new Vector3(307.3915f, 23.86982f, 217.4884f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.01994211f, 0.0001275018f, -0.1070882f),
                    localAngles = new Vector3(24.3717f, 268.774f, 111.6553f),
                    localScale = new Vector3(0.045f, 0.045f, 0.045f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hat",
                    localPos = new Vector3(-0.1191038f, 0.02249365f, 0.01268844f),
                    localAngles = new Vector3(3.652771f, 93.2954f, 250.8319f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.3592396f, 0.9387559f, 0.082003f),
                    localAngles = new Vector3(26.05843f, 3.707305f, 93.7645f),
                    localScale = new Vector3(0.5f, 0.5f, 0.5f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.04101966f, 0.1495899f, 0.03951247f),
                    localAngles = new Vector3(19.68638f, 195.4905f, 116.7981f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.01408422f, 0.8379388f, -1.267355f),
                    localAngles = new Vector3(13.6687f, 108.9054f, 322.5609f),
                    localScale = new Vector3(0.25f, 0.25f, 0.25f)

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
                    childName = "UpperArmL",
                    localPos = new Vector3(0.03000752f, 0.1098852f, -0.007914024f),
                    localAngles = new Vector3(345.2831f, 15.06405f, 259.0744f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.1353378f, 0.1148387f, 0.02999666f),
                    localAngles = new Vector3(18.60601f, 201.3888f, 107.481f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "FootFrontR",
                    localPos = new Vector3(0.1380639f, -0.04221976f, 0.00659878f),
                    localAngles = new Vector3(31.21218f, 185.5531f, 85.90443f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.02692784f, 0.1715988f, -0.07528451f),
                    localAngles = new Vector3(27.92293f, 233.8488f, 42.9845f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-1.474291f, 0.4146849f, 1.184042f),
                    localAngles = new Vector3(9.905084f, 49.179f, 92.92774f),
                    localScale = new Vector3(0.55f, 0.55f, 0.55f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.06930835f, 0.1518766f, -0.047157f),
                    localAngles = new Vector3(37.86217f, 251.961f, 115.654f),
                    localScale = new Vector3(0.0756f, 0.0756f, 0.0756f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(-0.188871f, 0.3919613f, -0.07400557f),
                    localAngles = new Vector3(7.768537f, 292.3127f, 358.3477f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(-0.02819476f, 0.2904983f, -0.06415462f),
                    localAngles = new Vector3(10.21205f, 67.92503f, 195.5395f),
                    localScale = new Vector3(0.055f, 0.055f, 0.055f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.4837f, -0.8265978f, 2.033911f),
                    localAngles = new Vector3(17.15146f, 100.2759f, 119.889f),
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
                    childName =  "PauldronR",
                    localPos = new Vector3(0.5386145f, -0.2152944f, 0.5276036f),
                    localAngles = new Vector3(304.1717f, 100.8858f, 106.0395f),
                    localScale = new Vector3(0.12f, 0.12f, 0.12f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ElbowL",
                    localPos = new Vector3(0.002142102f, 0.005764513f, 0.00174125f),
                    localAngles = new Vector3(331.2649f, 1.659026f, 256.17f),
                    localScale = new Vector3(0.002f, 0.002f, 0.002f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.04007983f, 0.2696055f, -0.1396203f),
                    localAngles = new Vector3(41.84266f, 32.5432f, 243.7065f),
                    localScale = new Vector3(0.09f, 0.09f, 0.09f)
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
                    childName = "ElbowL",
                    localPos = new Vector3(0.0003440298f, 0.001506613f, 0.00005643925f),
                    localAngles = new Vector3(53.94598f, 327.5067f, 235.5436f),
                    localScale = new Vector3(0.001f, 0.001f, 0.001f)
                }
            });
            //rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Body",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00003724613f, 0.1874127f, -0.02244581f),
                    localAngles = new Vector3(46.24128f, 144.1943f, 328.1375f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.01266001f, 0.15735f, -0.1306189f),
                    localAngles = new Vector3(27.29047f, 60.5005f, 203.4501f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }
            });
            rules.Add("ExecutionerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.000860632f, 0.002425583f, -0.0005776987f),
                    localAngles = new Vector3(44.26307f, 350.5325f, 99.25278f),
                    localScale = new Vector3(0.00035f, 0.00035f, 0.00035f)
                }
            });
            rules.Add("NemmandoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.001144606f, 0.002062514f, -0.0005542804f),
                    localAngles = new Vector3(354.0225f, 169.6777f, 242.9034f),
                    localScale = new Vector3(0.00035f, 0.00035f, 0.00035f)
                }
            });
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hat",
                    localPos = new Vector3(0.1047899f, -0.002434582f, 0.090097f),
                    localAngles = new Vector3(313.9162f, 343.7477f, 309.0277f),
                    localScale = new Vector3(.05f, .05f, .05f)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HeadBone",
                    localPos = new Vector3(-0.06921848f, 0.2019767f, 0.0779769f),
                    localAngles = new Vector3(6.506618f, 124.3292f, 304.0726f),
                    localScale = new Vector3(.04f, .04f, .04f)
                }
            });
            return rules;
        }

        public override void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += BivalveAttackReward;
            On.RoR2.CharacterBody.OnInventoryChanged += AddBivalveTracker;
            /*
            if (doRailer.Value)
            {
                IL.EntityStates.Railgunner.Backpack.Offline.OnEnter += StupidRailgunnerHook;
            }
            */
        }

        private void AddBivalveTracker(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (self.inventory)
            {
                int itemCount = self.inventory.GetItemCount(ItemBase<BivalveShell>.instance.ItemDef);
                var token = self.gameObject.GetComponent<BivalveTracker>();
                if (itemCount > 0)
                {
                    if (!token)
                    {
                        token = self.gameObject.AddComponent<BivalveTracker>();
                        token.body = self;
                    }
                }
                else if (token)
                {
                    //Destroy(token.gameObject); it wont let me do this so uhhh
                    token.timeToDie = true;
                }

            }
        }


        private void BivalveAttackReward(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            var token = sender.gameObject.GetComponent<BivalveTracker>();
            if (token)
            {
                if (!token.secondaryIsReady && !token.huntressMoment || token.unusableSkill)
                {
                    args.attackSpeedMultAdd += bivalveAttackSpeed.Value * GetCount(sender);
                }
            }
        }
        /*   I copied this directly from Riskymod and their one works and mine doesn't and I don't know why. Fuck IL.

        private void StupidRailgunnerHook(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld(typeof(EntityStates.Railgunner.Backpack.Offline), "attackSpeedStat")
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, EntityStates.Railgunner.Backpack.Offline, float>>((atkspd, self) =>
                {
                    if (self.characterBody.inventory)
                    {
                        Debug.Log("Inventory real, no diea why this didn't work");
                        return atkspd += bivalveAttackSpeed.Value * self.characterBody.inventory.GetItemCount(ItemBase<BivalveShell>.instance.ItemDef);
                    }
                    else { Debug.Log("no Inventory?"); }
                    return atkspd;
                });
            }
            else
            {
                Debug.LogError("VV: Railgunner Backpack AttackSpeed IL Hook failed");
            }
        }
        */        

        public class BivalveTracker : MonoBehaviour
        {
            public bool huntressMoment;
            public bool secondaryIsReady;
            public bool unusableSkill;
            private bool secondaryWasReady;
            public bool timeToDie;
            public EntityStateMachine railState;
            public CharacterBody body; //the player it's attached to
            

            void Awake()
            {
                unusableSkill = false;
                timeToDie = false;
            }

            private void FixedUpdate()
            {
                if (timeToDie)
                {
                    Destroy(this);
                }
                switch (body.skillLocator.secondaryBonusStockSkill.skillNameToken)
                {
                    case "HERETIC_DEFAULT_SKILL_NAME":
                        if (!unusableSkill)
                        {
                            body.RecalculateStats();
                            unusableSkill = true;
                        }
                        break;
                    case "CAPTAIN_SKILL_DISCONNECT_NAME":
                        if (!unusableSkill)
                        {
                            body.RecalculateStats();
                            unusableSkill = true;
                        }
                        break;
                    case "HUNTRESS_SECONDARY_NAME":
                        unusableSkill = false;
                        if (body.GetComponent<HuntressTracker>().GetTrackingTarget() is null && body.skillLocator.secondaryBonusStockSkill.cooldownRemaining == 0) 
                             { huntressMoment = true; }
                        else { huntressMoment = false; }
                        secondaryWasReady = secondaryIsReady;
                        secondaryIsReady = body.skillLocator.secondaryBonusStockSkill.IsReady();
                        if (secondaryIsReady == secondaryWasReady)
                        {
                            body.RecalculateStats();
                        }
                        break;
                    default:
                        unusableSkill = false;
                        secondaryWasReady = secondaryIsReady;
                        secondaryIsReady = body.skillLocator.secondaryBonusStockSkill.IsReady();
                        if (secondaryIsReady == secondaryWasReady)
                        {
                            body.RecalculateStats();
                        }
                        break;
                }
            }
        }
    }
}