using BepInEx.Configuration;
using R2API;
using RoR2;
using vanillaVoid.Utils;
using vanillaVoid.Misc;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using HarmonyLib;
using static vanillaVoid.vanillaVoidPlugin;
using RoR2.Items;
using EntityStates;
using System.Collections;
using System.Runtime.InteropServices;

namespace vanillaVoid.Items
{
    public class VoidFin : ItemBase<VoidFin>
    {
        public static ConfigEntry<float> baseDamage;
        public static ConfigEntry<float> stackingDamage;

        public static ConfigEntry<float> baseAOEDamage;
        public static ConfigEntry<float> stackingAOEDamage;

        public static ConfigEntry<int> baseKicks;
        public static ConfigEntry<int> stackingKicks;

        public static ConfigEntry<bool> invertForce;
        public static ConfigEntry<bool> allowJumpOverrides;

        public static ConfigEntry<float> kickRange;
        //public ConfigEntry<float> stackingBuff;

        public override string ItemName => "Warden's Clutch"; // Marauder's Clutch, Brachial, Sessile , Plunderer's Pincer, Ichthyic Invertebrate, Briny Bivalve, Anchor, Clutch, Loch, Thalassic, Incisor, Chisel

        public override string ItemLangTokenName => "FIN_ITEM";

        public override string ItemPickupDesc => $"Jump off of enemies to damage and launch them. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"Gain <style=cIsUtility>{baseKicks.Value}</style>" + (stackingKicks.Value != 0 ? $" <style=cStack>(+{stackingKicks.Value} per stack)</style>" : "") + $" enemy <style=cIsUtility>kickoffs</style>, dealing <style=cIsDamage>{baseDamage.Value * 100}%</style>" + (stackingDamage.Value != 0 ? $" <style=cStack>(+{stackingDamage.Value * 100} per stack)</style>" : "") + $" base damage, <style=cIsUtility>launching</style> them away and <style=cIsUtility>dispersing</style> nearby enemies" + (baseAOEDamage.Value != 0 ? $" for <style=cIsDamage>{baseAOEDamage.Value * 100}%</style>" + (stackingAOEDamage.Value != 0 ? $" <style=cStack>(+{stackingAOEDamage.Value * 100}</style>" : "") : "") + (baseAOEDamage.Value != 0 ? " base damage" : "") + $". <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"Out of all of what our research has attained, this, single, specimen has invited some of the most questions. \nHow, pray tell, was this discarded? Was it, perhaps, a process of augmentation - of casting off one's carapace and becoming more? Was it a punishment, a consequence of a task failed, or a lost battle, a grievous mark left by the victor? Did the creature survive, and if not, where was the rest of its body taken, or did it the wound simply lead to it bleeding out somewhere else? Perhaps, even, it was the victor; only gaining a pyrrhic victory of an invader lost in the light of our home?\n\nEither way; this specimen has given us much to ponder. Is there something greater than this creature? Are they easier to defeat than we thought? Or are they intentionally modifing themselves, a species collectively grafting themselves into a creature greater than we can comprehend - an act of pride, or necessary protection from something beyond? We pray we never need know the answer. \n- Lost Journal, stored safely at the end of time."; //Worry not. You will never need to know.

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCrabClawReal.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("clutchIcon512.png");

        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Utility, ItemTag.Damage };

        public static GameObject jumpVFX = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("FinVfx.prefab");
        
        public static GameObject clutchAttachment;

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;

            Hooks();

            Debug.Log("esawawawawa: " + jumpVFX);

            var efc = jumpVFX.AddComponent<EffectComponent>();
            efc.positionAtReferencedTransform = true;
            efc.applyScale = true;

            var vfxattr = jumpVFX.AddComponent<VFXAttributes>();
            vfxattr.vfxPriority = VFXAttributes.VFXPriority.Low;
            vfxattr.vfxIntensity = VFXAttributes.VFXIntensity.Low;

            var destroy = jumpVFX.AddComponent<DestroyOnTimer>();
            destroy.duration = 2;

            ContentAddition.AddEffect(jumpVFX);

            clutchAttachment = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("ClutchAttachment.prefab");
            var netid = clutchAttachment.AddComponent<NetworkIdentity>();
            netid.localPlayerAuthority = true;
            

            clutchAttachment.AddComponent<NetworkedBodyAttachment>().shouldParentToAttachedBody = true;

            clutchAttachment.AddComponent<ClutchNetBehavior>();

            PrefabAPI.RegisterNetworkPrefab(clutchAttachment);
        }

        public override void CreateConfig(ConfigFile config)
        {
            string name = ItemName.Replace("'", "");

            baseDamage = config.Bind<float>("Item: " + name, "Base Damage", 3f, "Adjust the base damage of the enemy kickoff.");
            stackingDamage = config.Bind<float>("Item: " + name, "Stacking Damage", 0f, "Adjust the stacking damage of the enemy kickoff.");

            baseAOEDamage = config.Bind<float>("Item: " + name, "Base AOE Damage", 1.25f, "Adjust the base damage of the enemy kickoff's AOE.");
            stackingAOEDamage = config.Bind<float>("Item: " + name, "Stacking AOE Damage", 0f, "Adjust the stacking damage of the enemy kickoff's AOE.");

            baseKicks = config.Bind<int>("Item: " + name, "Base Enemy Kicks", 1, "Adjust how many enemy kickoffs the first stack grants.");
            stackingKicks = config.Bind<int>("Item: " + name, "Enemy Kicks Per Stack", 1, "Adjust how many enemy kickoffs gained per stack.");

            kickRange = config.Bind<float>("Item: " + name, "Kick Range", 3f, "Adjust the radius of valid kicking range. This is added ontop of the radius of the body trying to use it.");
            
            invertForce = config.Bind<bool>("Item: " + name, "Invert Kick Force", false, "If false, kickoffs launch enemies towards the player's movement direction - if true, it pushes opposite of your inputs. More logical, but less fun to combo with.");
            allowJumpOverrides = config.Bind<bool>("Item: " + name, "Allow Kickoffs With Extra Jumps", true, "If true, jumps near enemies will activate kickoffs instead of using your extra jumps. Particularly useful for characters like Merc and Heretic. If false, you can only preform an enemy kickoff after expending all bonus jumps, similar to how Quill works.");

            voidPair = config.Bind<string>("Item: " + name, "Item to Corrupt", "KnockBackHitEnemies", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {

            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCrabClawDisplay.prefab");
            Debug.Log("ItemBodyModelPrefab: " + ItemBodyModelPrefab);

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
                    localPos = new Vector3(0.00465F, 0.20572F, -0.22672F),
                    localAngles = new Vector3(12.00147F, 357.0918F, 359.3205F),
                    localScale = new Vector3(0.125F, 0.125F, 0.125F)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.08842F, 0.07391F, -0.14395F),
                    localAngles = new Vector3(353.7863F, 313.8066F, 332.5388F),
                    localScale = new Vector3(0.11F, 0.11F, 0.11F)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.00001F, 0.11897F, -0.20476F),
                    localAngles = new Vector3(7.68725F, 0F, 0F),
                    localScale = new Vector3(0.1125F, 0.1125F, 0.1125F)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.00002F, 0.94018F, -2.07092F),
                    localAngles = new Vector3(0F, 0F, 1.10479F),
                    localScale = new Vector3(0.85F, 0.85F, 0.85F)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.00258F, 0.23876F, -0.30678F),
                    localAngles = new Vector3(17.73203F, 0F, 0F),
                    localScale = new Vector3(0.13F, 0.13F, 0.13F)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Neck",
                    localPos = new Vector3(0F, 0.36232F, -0.2266F),
                    localAngles = new Vector3(8.38175F, 0F, 0F),
                    localScale = new Vector3(0.225F, 0.225F, 0.225F)

                    //localPos = new Vector3(0.3982559f, 0.5157748f, 1.197929f), //std turret
                    //localAngles = new Vector3(2.650187f, 268.003f, 247.601f),
                    //localScale = new Vector3(.25f, .25f, .25f)
                }
            });
            rules.Add("mdlEngiWalkerTurret", new RoR2.ItemDisplayRule[]
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
                    childName = "Chest",
                    localPos = new Vector3(0F, 0.09148F, -0.22617F),
                    localAngles = new Vector3(17.96363F, 0F, 0F),
                    localScale = new Vector3(0.11F, 0.11F, 0.11F)
                }

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.00001F, -0.05583F, -0.18961F),
                    localAngles = new Vector3(2.13914F, 0F, 0F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "PlatformBase",
                    localPos = new Vector3(0.33212F, 0.33479F, -0.66757F),
                    localAngles = new Vector3(5.23079F, 331.4161F, 0.10743F),
                    localScale = new Vector3(0.225F, 0.225F, 0.225F)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0F, 0.21241F, -0.39337F),
                    localAngles = new Vector3(8.35009F, 0F, 0F),
                    localScale = new Vector3(0.1275F, 0.1275F, 0.1275F)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "SpineChest3",
                    localPos = new Vector3(0.00793F, 0.43607F, -0.14117F),
                    localAngles = new Vector3(71.48431F, 352.6058F, 173.3602F),
                    localScale = new Vector3(1.05F, 1.05F, 1.05F)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0F, 0.1763F, -0.25174F),
                    localAngles = new Vector3(5.78069F, 0F, 0F),
                    localScale = new Vector3(0.125F, 0.125F, 0.125F)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(0.02401F, -0.48462F, 0.00096F),
                    localAngles = new Vector3(275.0708F, 266.1274F, 1.32135F),
                    localScale = new Vector3(0.095F, 0.095F, 0.095F)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.00001F, 0.0007F, -0.20147F),
                    localAngles = new Vector3(346.267F, 0F, 0F),
                    localScale = new Vector3(0.11F, 0.11F, 0.11F)
                }
            });
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.10576F, 0.10389F, -0.2259F),
                    localAngles = new Vector3(4.87656F, 354.1656F, 37.63293F),
                    localScale = new Vector3(0.1125F, 0.1125F, 0.1125F)
                }
            });
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00631F, -0.29485F, -0.09073F),
                    localAngles = new Vector3(270F, 104.284F, 0F),
                    localScale = new Vector3(0.1075F, 0.1075F, 0.1075F)
                }
            });
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.08101F, -0.01713F, -0.3407F),
                    localAngles = new Vector3(351.5275F, 12.11302F, 1.56139F),
                    localScale = new Vector3(0.15F, 0.15F, 0.15F)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.16016F, 2.33516F, 0.74587F),
                    localAngles = new Vector3(340.7508F, 179.5365F, 188.4355F),
                    localScale = new Vector3(0.95F, 0.95F, 0.95F)
                }
            });

            //Modded Chars 
            //rules.Add("EnforcerBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName =  "Shield",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            //rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            //rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            ////rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            ////{
            ////    new RoR2.ItemDisplayRule
            ////    {
            ////        ruleType = ItemDisplayRuleType.ParentedPrefab,
            ////        followerPrefab = ItemBodyModelPrefab,
            ////        childName = "Door",
            ////        localPos = new Vector3(0f, 0f, 0f),
            ////        localAngles = new Vector3(0f, 0f, 0f),
            ////        localScale = new Vector3(1f, 1f, 1f)
            ////    }
            ////});
            //rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "PickL",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            //rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            //rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "LowerArmL",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            //rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlMorris", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlHANDOverclocked", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlAssassin", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "arm_bone2.L",
            //        localPos = new Vector3(0, 0, 0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
            //rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(0, 0, 0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
            rules.Add("RobDriverBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.02703F, 0.18342F, -0.24183F),
                    localAngles = new Vector3(12.00147F, 357.0918F, 1.80554F),
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
                    localPos = new Vector3(0.02703F, 0.18342F, -0.24183F),
                    localAngles = new Vector3(12.00147F, 357.0918F, 359.3205F),
                    localScale = new Vector3(0.125F, 0.125F, 0.125F)
                }
            });
            //rules.Add("mdlTeslaTrooper", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlChrono", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Chest",
            //        localPos = new Vector3(0, 0, 0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
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
            //On.RoR2.CharacterBody.OnInventoryChanged += AddJumpToken;
            On.EntityStates.GenericCharacterMain.ProcessJump += TryClutch;
        }

        private void TryClutch(On.EntityStates.GenericCharacterMain.orig_ProcessJump orig, GenericCharacterMain self){
            bool eatInput = false;
            if (self.hasCharacterMotor && allowJumpOverrides.Value){
                bool flag3 = self.characterMotor.jumpCount < self.characterBody.maxJumpCount; //could this be a normal jump

                if (self.jumpInputReceived && self.characterBody && flag3){
                    Debug.Log("awawawawawa " + flag3);
                    int clutchCount = self.characterBody.inventory.GetItemCount(ItemDef);
                    if(clutchCount > 0){
                        var behv = self.characterBody.GetComponent<ClutchBehavior>();
                        if (behv && behv.token && self.characterMotor.jumpCount != 0){
                            bool other = self.inputBank.jump.justPressed && behv.token.jumpCurrent > 0 && self.characterBody.moveSpeed != 0 && behv.token.canEnemyJump;
                            Debug.Log("other : " + other + " just pressed: " + self.inputBank.jump.justPressed + " | (> 0?) jumpcurrent: " + behv.token.jumpCurrent + " | moveSpeed: " + self.characterBody.moveSpeed + " | canEnemyJump: " + behv.token.canEnemyJump);

                            //if (self.isAuthority && self.localPlayerAuthority){
                            //    //behv.token.CmdSetClutchOverrideCalc();
                            //    other = self.inputBank.jump.justPressed && behv.token.jumpCurrent > 0 && self.characterBody.moveSpeed != 0 && behv.token.canEnemyJump;
                            //    Debug.Log("other : " + other + " just pressed: " + self.inputBank.jump.justPressed + " | (> 0?) jumpcurrent: " + behv.token.jumpCurrent + " | moveSpeed: " + self.characterBody.moveSpeed + " | canEnemyJump: " + behv.token.canEnemyJump);
                            //} 
                            //
                            if (behv.token.jumpOverride || other){
                                eatInput = true;                          //false                              //true                        //true                              //false, for client intending to use this item
                                Debug.Log("eat : " + eatInput + " | " + behv.token.hasAuthority + " | " + self.isAuthority + " | " + self.localPlayerAuthority + " | " + NetworkServer.active);

                                Debug.Log("calling stupid intermediate");
                                behv.token.StupidIntermediate();


                                //if (!NetworkServer.active){
                                //    behv.token.ActivateClutch();
                                //}
                            }
                            Debug.Log("eatInput " + eatInput + " | behv.token.jumpOverride: " + behv.token.jumpOverride + " | " + self.isAuthority + " | " + self.localPlayerAuthority + " | ");
                        }
                    }
                }
            }
            if (!eatInput){
                orig(self);
            }
        }

        private void AddJumpToken(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self) {
            orig(self);
            if (self.inventory) {
                int itemCount = self.inventory.GetItemCount(ItemBase<VoidFin>.instance.ItemDef);
                var token = self.gameObject.GetComponent<ClutchNetBehavior>();
                if (itemCount > 0) {
                    //var token = self.gameObject.GetComponent<AirdashToken>();
                    if (!token) {
                        token = self.gameObject.AddComponent<ClutchNetBehavior>();
                        //token.body = self;
                    }
                    Debug.Log("add token");
                    token.jumpMax = itemCount * 1; // dashesPerStack.Value;
                } else if (token) {
                    GameObject.Destroy(token);
                }
            }
        }
    }

    public sealed class ClutchBehavior : BaseItemBodyBehavior {

        [ItemDefAssociation(useOnServer = true, useOnClient = true)]
        private static ItemDef GetItemDef() { return ItemBase<VoidFin>.instance?.ItemDef; }

        public NetworkedBodyAttachment attachment;
        public ClutchNetBehavior token;
      
        private void OnEnable()
        {
            attachment = UnityEngine.Object.Instantiate<GameObject>(VoidFin.clutchAttachment, body.transform).GetComponent<NetworkedBodyAttachment>();
            attachment.AttachToGameObjectAndSpawn(body.gameObject, null);
            token = this.attachment.GetComponent<ClutchNetBehavior>();
            Debug.Log("attach: " + attachment + " | " + token);
        }

        private void OnDisable()
        {
            Debug.Log("attach: " + attachment + " | " + token);
            if (attachment)
            {
                Destroy(attachment);
            }
            attachment = null;
            token = null;
        }
    }

    public class SquashedComponent : MonoBehaviour {
        public float speed = 5f;
        public Vector3 originalScale;

        private void Awake(){
            originalScale = transform.localScale;
            var cmodel = gameObject.GetComponent<CharacterModel>();
            HullClassification? hull = null;
            if (cmodel){
                hull = cmodel.body.hullClassification;
            }

            switch (hull){
                case HullClassification.Human:
                    transform.localScale = new Vector3(1.3f * transform.localScale.x, 0.0275f * transform.localScale.y, 1.3f * transform.localScale.z);
                    break;

                case HullClassification.Golem:
                    transform.localScale = new Vector3(1.175f * transform.localScale.x, 0.6f * transform.localScale.y, 1.175f * transform.localScale.z);
                    break;

                case HullClassification.BeetleQueen:
                    transform.localScale = new Vector3(1.075f * transform.localScale.x, 0.875f * transform.localScale.y, 1.075f * transform.localScale.z);
                    break;
                case HullClassification.Count:
                default:
                    transform.localScale = new Vector3(1.25f * transform.localScale.x, 0.1f * transform.localScale.y, 1.25f * transform.localScale.z);
                    break;
            }
            StartCoroutine(EndSquash());
        }

        IEnumerator EndSquash(){
            yield return new WaitForSeconds(.275f);

            float t = 0f;
            while (t < 1f){
                t += speed * Time.deltaTime;
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t);

                yield return 0;
            }

            transform.localScale = originalScale;
            Destroy(this);

            yield return null;
        }
    }
}

