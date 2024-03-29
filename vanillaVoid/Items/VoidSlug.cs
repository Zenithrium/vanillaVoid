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
using RoR2.Skills;

namespace vanillaVoid.Items
{
    public class VoidSlug : ItemBase<VoidSlug>
    {
        public ConfigEntry<float> baseRegen;

        public ConfigEntry<float> baseRegenPerStack;
        public override string ItemName => "Well Stocked Stocki";

        public override string ItemLangTokenName => "SLUG_ITEM";

        public override string ItemPickupDesc => $"Gain health regeneration for every charged ability. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"yeag. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"<style=cMono>//-- AUTO-TRANSCRIPTION FROM CARGO BAY 6 OF UES [Redacted] --//</style>" +
            "\n\n\"slug was born with a special power\"";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlAdzePickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("adzeIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.Damage };

        public BuffDef voidSlugRegen { get; private set; }

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            CreateBuff();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            //VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);



            Hooks();
        }

        //public override string VoidPair()
        //{
        //    return voidPair.Value;
        //}


        public override void CreateConfig(ConfigFile config)
        {
            baseRegen = config.Bind<float>("Item: " + ItemName, "Base Regen per Buff Stack", .75f, "Adjust the amount of regeneration for every stack of the first item");
            baseRegenPerStack = config.Bind<float>("Item: " + ItemName, "Base J per J per J", .75f, "Adjust the amount of J in each stack of J");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "HealWhileSafe", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {

            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlAdzeDisplay.prefab");
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

        public override void Hooks() //classic more complicated than anticipated item
        {
            RecalculateStatsAPI.GetStatCoefficients += CalculateStatsVoidSlugRegen;

            On.RoR2.GenericSkill.DeductStock += VoidSlugDeductStock;
            On.RoR2.Skills.SkillDef.OnExecute += VoidSlugAlsoDeductStock;

            On.RoR2.GenericSkill.RestockSteplike += VoidSlugRestock;
            On.RoR2.GenericSkill.AddOneStock += VoidSlugAddOne;
            On.RoR2.GenericSkill.ApplyAmmoPack += VoidSlugPack;
            On.RoR2.GenericSkill.Reset += VoidSlugReset;
            On.EntityStates.Railgunner.Reload.Reloading.OnExit += GodDamnItRailgunner;
            On.EntityStates.Railgunner.Backpack.Reboot.OnEnter += RebootEnter;
            On.EntityStates.Railgunner.Backpack.UseCryo.OnEnter += CryoEnter;

            On.RoR2.CharacterBody.OnInventoryChanged += VoidSlugInventoryChanged;

            CharacterBody.onBodyStartGlobal += VoidSlugCheckStocks;

            On.EntityStates.Toolbot.ToolbotDualWieldBase.OnEnter += Hate;
            On.EntityStates.Toolbot.ToolbotDualWieldStart.OnEnter += This;
            On.EntityStates.Toolbot.ToolbotDualWieldEnd.OnEnter += stupid;
            On.EntityStates.Toolbot.ToolbotDualWield.OnEnter += character;

            //On.RoR2.GenericSkill.SetSkillInternal += SetSkillInternalSlug;

            On.RoR2.GenericSkill.SetSkillOverride += SetOverrideSlug;
            On.RoR2.GenericSkill.UnsetSkillOverride += UnsetOverrideSlug;
            /// SetSkillOverride

            // !!!!passivvely gains stocks whiel zoomed in with backup mag. why? this game sucks! (probably because you're tecchnically "casting" scope while you're scoped and it's a 1 of. just ban it from giving stoccks probably


            /// !!!!!!aaa fucking spikestrip raillguner scope fuck 
            /// captains special should probably count - therefore bandit shotgun and nemc m2 should also NAH
            /// executioner's m1 going off cooldown gives a stack FIXED by >. also the m2 does not interact at all
            /// chirr's util gives an extra stack - ignore Drop completely FIXED by >
            ///
            /// 
            /// rex m2 should get it (not biased)
            /// llook into retool and rockets for mult 
            //todo: make it do the above on body spawn / stage start ?  whicchiever is less dumb 
            //probably do it on bodystart because if you ever swap characters during a stage (rare but some mods are stupid) it would probably bug a bit. have it update there 


            //TREEBOT_SECONDARY_NAME 
            //SS2_EXECUTIONER2_IONCHARGE_NAME // SS2_EXECUTIONER2_IONBURST_NAME
            //RAILGUNNER_SNIPE_HEAVY_NAME // RAILGUNNER_SNIPE_LIGHT_NAME
        }

        private void UnsetOverrideSlug(On.RoR2.GenericSkill.orig_UnsetSkillOverride orig, GenericSkill self, object source, SkillDef newSkill, GenericSkill.SkillOverridePriority priority)
        {
            if (GetCount(self.characterBody) > 0)
            {
                Debug.Log("UNSET OVERRIDE SLUG:");
                Debug.Log(self.skillName + " | " + self.skillNameToken + " | self.recharge: " + self.baseRechargeInterval + " | self.stock: " + self.stock);
                Debug.Log(newSkill.skillName + " | " + newSkill.skillNameToken + " |  new.recharge: " + newSkill.baseRechargeInterval + " | new.restock: " + newSkill.fullRestockOnAssign);

                if (self.baseRechargeInterval > .5 && self.stock > 0 && newSkill.baseRechargeInterval < .5)
                { //if HAD stock, but new skill isn't appliciable, remove a stack
                    Debug.Log("Removing stock because nwe skill invalid");
                    if (self.characterBody.GetBuffCount(voidSlugRegen) >= 1)
                    {
                        self.characterBody.RemoveBuff(voidSlugRegen);
                    }
                }
                else if (self.baseRechargeInterval < .5 && newSkill.baseRechargeInterval > .5 && newSkill.fullRestockOnAssign)
                { //old skill does not count, new one does and should be insta charged
                    self.characterBody.AddBuff(voidSlugRegen);
                    Debug.Log("Giving stock - old skill invalid");
                }
                else if (self.baseRechargeInterval > .5 && self.stock == 0 && newSkill.baseRechargeInterval > .5 && newSkill.fullRestockOnAssign)
                { //old skill did count, but isn't charged, and new one does
                    self.characterBody.AddBuff(voidSlugRegen);
                    Debug.Log("Giving stock - valid skill on cooldown");
                }
                else
                {
                    Debug.Log("Not Interacting - Conditions not met");
                }

            }

            orig(self, source, newSkill, priority);
        }

        private void SetOverrideSlug(On.RoR2.GenericSkill.orig_SetSkillOverride orig, GenericSkill self, object source, SkillDef newSkill, GenericSkill.SkillOverridePriority priority)
        {
            if (GetCount(self.characterBody) > 0)
            {
                Debug.Log("SET OVERRIDE SLUG:");
                Debug.Log(self.skillName + " | " + self.skillNameToken + " | self.recharge: " + self.baseRechargeInterval + " | self.stock: " + self.stock);
                Debug.Log(newSkill.skillName + " | " + newSkill.skillNameToken + " |  new.recharge: " + newSkill.baseRechargeInterval + " | new.restock: " + newSkill.fullRestockOnAssign);

                if (self.baseRechargeInterval > .5 && self.stock > 0 && newSkill.baseRechargeInterval < .5)
                { //if HAD stock, but new skill isn't appliciable, remove a stack
                    Debug.Log("Removing stock because nwe skill invalid");
                    if (self.characterBody.GetBuffCount(voidSlugRegen) >= 1)
                    {
                        self.characterBody.RemoveBuff(voidSlugRegen);
                    }
                }
                else if (self.baseRechargeInterval < .5 && newSkill.baseRechargeInterval > .5 && newSkill.fullRestockOnAssign)
                { //old skill does not count, new one does and should be insta charged
                    self.characterBody.AddBuff(voidSlugRegen);
                    Debug.Log("Giving stock - old skill invalid");
                }
                else if (self.baseRechargeInterval > .5 && self.stock == 0 && newSkill.baseRechargeInterval > .5 && newSkill.fullRestockOnAssign)
                { //old skill did count, but isn't charged, and new one does
                    self.characterBody.AddBuff(voidSlugRegen);
                    Debug.Log("Giving stock - valid skill on cooldown");
                }
                else
                {
                    Debug.Log("Not Interacting - Conditions not met");
                }

            }

            orig(self, source, newSkill, priority);
        }

        private void SetSkillInternalSlug(On.RoR2.GenericSkill.orig_SetSkillInternal orig, GenericSkill self, SkillDef newSkill)
        {


            if (GetCount(self.characterBody) > 0)
            {
                Debug.Log("-----");
                Debug.Log("self.recharge: " + self.baseRechargeInterval + " | self.stock: " + self.stock + " |  new.recharge" + newSkill.baseRechargeInterval + " | new.restock: " + newSkill.fullRestockOnAssign);

                //if (self.baseRechargeInterval > .5 && self.stock > 0 && newSkill.baseRechargeInterval < .5){ //if HAD stock, but new skill isn't appliciable, remove a stack
                //    Debug.Log("Removing stock because nwe skill invalid");
                //    if (self.characterBody.GetBuffCount(voidSlugRegen) >= 1){
                //        self.characterBody.RemoveBuff(voidSlugRegen);
                //    }
                //}else if (self.baseRechargeInterval < .5 && newSkill.baseRechargeInterval > .5 && newSkill.fullRestockOnAssign){ //old skill does not count, new one does and should be insta charged
                //    self.characterBody.AddBuff(voidSlugRegen);
                //    Debug.Log("Giving stock - old skill invalid");
                //}else if(self.baseRechargeInterval > .5 && self.stock == 0 && newSkill.baseRechargeInterval > .5 && newSkill.fullRestockOnAssign){ //old skill did count, but isn't charged, and new one does
                //    self.characterBody.AddBuff(voidSlugRegen);
                //    Debug.Log("Giving stock - valid skill on cooldown");
                //}
                //else
                //{
                //    Debug.Log("Not Interacting - Conditions not met");
                //}
                Debug.Log("Self: " + self.skillNameToken + " | " + self.skillDef.fullRestockOnAssign + " | " + self.skillDef.baseRechargeInterval + " | " + self.stock + " | " + self.baseStock);
                Debug.Log("newskill: " + newSkill.skillNameToken + " | " + newSkill.fullRestockOnAssign + " | " + newSkill.baseRechargeInterval);
            }

            orig(self, newSkill);
        }

        private void Hate(On.EntityStates.Toolbot.ToolbotDualWieldBase.orig_OnEnter orig, EntityStates.Toolbot.ToolbotDualWieldBase self)
        {
            Debug.Log("yeah BASE: " + self.allowPrimarySkills + " | " + self.shouldAllowPrimarySkills);
            //Debug.Log(self.characterBody.skillLocator.primary.name + "'s primary stock: " + self.characterBody.skillLocator.primary.stock);
            //Debug.Log(self.characterBody.skillLocator.secondary.name + "'s secondary stock: " + self.characterBody.skillLocator.secondary.stock);
            orig(self);

        }

        private void stupid(On.EntityStates.Toolbot.ToolbotDualWieldEnd.orig_OnEnter orig, EntityStates.Toolbot.ToolbotDualWieldEnd self)
        {

            Debug.Log("yeah WIELD END ");


            //Debug.Log(self.skillLocator.secondary.stock + " | " + self.skillLocator.secondary.skillNameToken + " | " + self.skillLocator.primary.stock + " | " + self.skillLocator.primary.skillNameToken);

            orig(self);
        }

        private void character(On.EntityStates.Toolbot.ToolbotDualWield.orig_OnEnter orig, EntityStates.Toolbot.ToolbotDualWield self)
        {
            Debug.Log("yeah WIELD");
            orig(self);
        }

        private void This(On.EntityStates.Toolbot.ToolbotDualWieldStart.orig_OnEnter orig, EntityStates.Toolbot.ToolbotDualWieldStart self)
        {
            //if (self.skillLocator.secondary.stock == 0){
            //    self.characterBody.AddBuff(voidSlugRegen);
            //}
            orig(self);
        }

        public void CreateBuff()
        {
            voidSlugRegen = ScriptableObject.CreateInstance<BuffDef>();
            voidSlugRegen.buffColor = new Color(136, 101, 207);
            voidSlugRegen.canStack = true;
            voidSlugRegen.isDebuff = false;
            voidSlugRegen.name = "DmVV" + "voidSlugRegen"; //cutie !!
            voidSlugRegen.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("whorlRegenBuffIcon");
            ContentAddition.AddBuffDef(voidSlugRegen);
        }

        private void CalculateStatsVoidSlugRegen(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender)
            {
                int buffCount = sender.GetBuffCount(voidSlugRegen);
                if (buffCount > 0)
                {
                    // the actual stat value should be like buffcount * 0.75 or something ?? i think what she 
                    var stackCount = GetCount(sender);
                    if (stackCount == 0)
                    {
                        stackCount = 1;
                    }
                    var regenAmount = (baseRegen.Value + (baseRegenPerStack.Value * (stackCount - 1)));
                    args.baseRegenAdd += (regenAmount + ((regenAmount / 5) * sender.level)) * buffCount;
                }
            }
        }

        private void VoidSlugDeductStock(On.RoR2.GenericSkill.orig_DeductStock orig, GenericSkill self, int count)
        {
            orig(self, count);
            Debug.Log("VoidSlugDeductStock; " + self.skillNameToken);
            if (self.stock == 0 && (self.baseRechargeInterval > .5 || self.skillNameToken == "TREEBOT_SECONDARY_NAME" || self.skillNameToken == "SS2_EXECUTIONER_IONCHARGE_NAME"))
            {
                if (self.characterBody.GetBuffCount(voidSlugRegen) >= 1)
                {
                    self.characterBody.RemoveBuff(voidSlugRegen);
                }
            }


        }
        private void VoidSlugAlsoDeductStock(On.RoR2.Skills.SkillDef.orig_OnExecute orig, RoR2.Skills.SkillDef self, GenericSkill skill)
        {
            orig(self, skill);
            Debug.Log("VoidSlugAlsoDeductStock; " + self.skillNameToken);
            if (GetCount(skill.characterBody) > 0)
            {
                if (self.stockToConsume > 0 && skill.stock == 0 && (self.baseRechargeInterval > .5 || self.skillNameToken == "TREEBOT_SECONDARY_NAME"))
                {
                    if (skill.characterBody.GetBuffCount(voidSlugRegen) >= 1)
                    {
                        skill.characterBody.RemoveBuff(voidSlugRegen);
                    }
                }
            }
        }

        private void VoidSlugRestock(On.RoR2.GenericSkill.orig_RestockSteplike orig, GenericSkill self)
        {
            Debug.Log("VoidSlugRestock; " + self.skillNameToken);
            if (GetCount(self.characterBody) > 0)
            {
                if (self.stock == 0 && (self.baseRechargeInterval > .5 || self.skillNameToken == "TREEBOT_SECONDARY_NAME"))
                {
                    self.characterBody.AddBuff(voidSlugRegen);
                }
            }
            orig(self);
            //Debug.Log("VoidSlugRestock; " + self.skillNameToken);
            //if (GetCount(self.characterBody) > 0) 
            //{
            //    if (self.stock == 1 && (self.baseRechargeInterval > .5 || self.skillNameToken == "TREEBOT_SECONDARY_NAME"))
            //    {
            //        self.characterBody.AddBuff(voidSlugRegen);
            //    }
            //}
        }

        private void VoidSlugAddOne(On.RoR2.GenericSkill.orig_AddOneStock orig, GenericSkill self)
        {
            orig(self);
            Debug.Log("VoidSlugAddOne; " + self.skillNameToken);
            if (GetCount(self.characterBody) > 0)
            {
                if (self.stock == 1 && (self.baseRechargeInterval > .5 || self.skillNameToken == "TREEBOT_SECONDARY_NAME"))
                {
                    self.characterBody.AddBuff(voidSlugRegen);
                }
            }
        }

        private void VoidSlugPack(On.RoR2.GenericSkill.orig_ApplyAmmoPack orig, GenericSkill self)
        {
            if (GetCount(self.characterBody) > 0)
            {
                if (self.stock == 0 && (self.baseRechargeInterval > .5 || self.skillNameToken == "TREEBOT_SECONDARY_NAME"))
                {
                    self.characterBody.AddBuff(voidSlugRegen);
                }
            }
            orig(self);

        }

        private void VoidSlugReset(On.RoR2.GenericSkill.orig_Reset orig, GenericSkill self)
        {
            Debug.Log("VoidSlugReset; " + self.skillNameToken);
            if (GetCount(self.characterBody) > 0)
            {
                if (self.stock == 0 && self.baseRechargeInterval > .5)
                {
                    self.characterBody.AddBuff(voidSlugRegen);
                }
            }
            orig(self);
        }

        private void CryoEnter(On.EntityStates.Railgunner.Backpack.UseCryo.orig_OnEnter orig, EntityStates.Railgunner.Backpack.UseCryo self)
        {
            orig(self);
            if (GetCount(self.characterBody) > 0)
            {
                self.characterBody.AddBuff(voidSlugRegen);
            }
        }

        private void RebootEnter(On.EntityStates.Railgunner.Backpack.Reboot.orig_OnEnter orig, EntityStates.Railgunner.Backpack.Reboot self)
        {
            orig(self);
            if (GetCount(self.characterBody) > 0)
            {
                self.characterBody.AddBuff(voidSlugRegen);
            }
        }

        private void GodDamnItRailgunner(On.EntityStates.Railgunner.Reload.Reloading.orig_OnExit orig, EntityStates.Railgunner.Reload.Reloading self)
        {
            orig(self);
            if (GetCount(self.characterBody) > 0)
            {
                self.characterBody.AddBuff(voidSlugRegen);
            }
        }

        private void VoidSlugInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (GetCount(self) > 0)
            {
                if (!self.GetComponent<VoidSlugToken>())
                {
                    self.gameObject.AddComponent<VoidSlugToken>();
                    //var primary = self.skillLocator.primary;
                    //if (primary.stock > 0 && primary.baseRechargeInterval >= .5)
                    //{
                    //
                    //}
                    //
                    //var secondary = self.skillLocator.secondary;
                    //if (secondary.stock > 0 && secondary.baseRechargeInterval >= .5)
                    //{
                    //
                    //}
                    //
                    //var util = self.skillLocator.utility;
                    //if (secondary.stock > 0 && secondary.baseRechargeInterval >= .5)
                    //{
                    //
                    //}
                    //
                    //var special = self.skillLocator.special;
                    //if (secondary.stock > 0 && secondary.baseRechargeInterval >= .5)
                    //{
                    //
                    //}

                    var amount = ((self.skillLocator.primary.stock > 0 && self.skillLocator.primary.baseRechargeInterval > .5) ? 1 : 0) + ((self.skillLocator.secondary.stock > 0 && (self.skillLocator.secondary.baseRechargeInterval > .5 || self.skillLocator.secondary.skillNameToken == "TREEBOT_SECONDARY_NAME" || self.skillLocator.secondary.skillNameToken == "SS2_EXECUTIONER_IONCHARGE_NAME")) ? 1 : 0) + ((self.skillLocator.utility.stock > 0 && self.skillLocator.utility.baseRechargeInterval > .5) ? 1 : 0) + ((self.skillLocator.special.stock > 0 && self.skillLocator.special.baseRechargeInterval > .5) ? 1 : 0);
                    Debug.Log("yeah : " + amount);
                    for (int i = 0; i < amount; ++i)
                    {
                        self.AddBuff(voidSlugRegen);
                    }
                }
            }
        }

        private void VoidSlugCheckStocks(CharacterBody self)
        {
            if (!self.inventory)
            {
                return;
            }

            if (GetCount(self) > 0)
            {
                if (!self.GetComponent<VoidSlugToken>())
                {
                    self.gameObject.AddComponent<VoidSlugToken>();

                    var amount = ((self.skillLocator.primary.stock > 0 && self.skillLocator.primary.baseRechargeInterval > .5) ? 1 : 0) + ((self.skillLocator.secondary.stock > 0 && (self.skillLocator.secondary.baseRechargeInterval > .5 || self.skillLocator.secondary.skillNameToken == "TREEBOT_SECONDARY_NAME" || self.skillLocator.secondary.skillNameToken == "SS2_EXECUTIONER_IONCHARGE_NAME")) ? 1 : 0) + ((self.skillLocator.utility.stock > 0 && self.skillLocator.utility.baseRechargeInterval > .5) ? 1 : 0) + ((self.skillLocator.special.stock > 0 && self.skillLocator.special.baseRechargeInterval > .5) ? 1 : 0);
                    Debug.Log("yeah : " + amount);
                    for (int i = 0; i < amount; ++i)
                    {
                        self.AddBuff(voidSlugRegen);
                    }
                }
            }
        }



        public class VoidSlugToken : MonoBehaviour
        {

        }

    }

}