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

        public override string ItemPickupDesc => $"Gain health increased regeneration for every charged ability. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"yeag. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"<style=cMono>//-- AUTO-TRANSCRIPTION FROM CARGO BAY 6 OF UES [Redacted] --//</style>" +
            "\n\n\"slug was born with a special power\"";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("Microbe.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("slugIcon512.png");


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

            On.RoR2.GenericSkill.SetSkillInternal += SetSkillInternalSlug;

            On.RoR2.CharacterBody.OnInventoryChanged += VoidSlugInventoryChanged;
            CharacterBody.onBodyStartGlobal += VoidSlugCheckStocks;


            On.EntityStates.Railgunner.Backpack.Offline.OnEnter += OfflineEnter;
            On.EntityStates.Railgunner.Backpack.Reboot.OnEnter += RebootEnter;
            On.EntityStates.Merc.Assaulter.OnExit += AssualterExit;
            On.EntityStates.Merc.Assaulter2.OnExit += AssualterExit2;

            //On.RoR2.GenericSkill.SetSkillOverride += SetOverrideSlug;
            //On.RoR2.GenericSkill.UnsetSkillOverride += UnsetOverrideSlug;

            //On.EntityStates.Railgunner.Reload.Reloading.OnExit += GodDamnItRailgunner;
            //On.EntityStates.Railgunner.Backpack.UseCryo.OnEnter += CryoEnter;
            //
            //CharacterBody.onBodyStartGlobal += VoidSlugCheckStocks;
            //
            //On.EntityStates.Toolbot.ToolbotDualWieldBase.OnEnter += Hate;
            //On.EntityStates.Toolbot.ToolbotDualWieldStart.OnEnter += This;
            //On.EntityStates.Toolbot.ToolbotDualWieldEnd.OnEnter += stupid;
            //On.EntityStates.Toolbot.ToolbotDualWield.OnEnter += character;

            //On.RoR2.GenericSkill.SetSkillInternal += SetSkillInternalSlug;

            /// SetSkillOverride

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


            //[Info   : Unity Log] SS2_NEMESIS_MERCENARY_UTILITY_BLINK_NAME
            //[Info   : Unity Log] SS2_EXECUTIONER2_IONCHARGE_NAMETrue0
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

        private void CheckStockCounts(CharacterBody body)
        {
            int intended = 0; //read skills and decide what the final buff count should be
            var locator = body.skillLocator;

            Debug.Log(locator.allSkills.Length + " , " + locator.allSkills + " | ");

            if (locator){
                if (locator.primary){
                    Debug.Log(locator.primary.skillNameToken);//locator.primary.IsReady() &&
                    if(locator.primary.skillNameToken == "LOADER_PRIMARY_NAME"){
                        //loader,,
                    }else if (locator.primary.IsReady() && locator.primary.baseRechargeInterval > .5){
                        ++intended;
                    }
                }

                if (locator.secondary){
                    Debug.Log(locator.secondary.skillNameToken + locator.secondary.IsReady() + locator.secondary.baseRechargeInterval + " | " + locator.secondary.stock); //locator.secondary.IsReady() &&
                    if (locator.secondary.IsReady() && (locator.secondary.baseRechargeInterval > .5 || locator.secondary.skillNameToken == "TREEBOT_SECONDARY_NAME" || locator.secondary.skillNameToken == "SS2_EXECUTIONER_IONBURST_NAME")){
                        ++intended;
                    }else if(locator.secondary.stock > 0 && (locator.secondary.skillNameToken == "SS2_EXECUTIONER2_IONCHARGE_NAME" || locator.secondary.skillNameToken == "HUNTRESS_SECONDARY_NAME")){ 
                        ++intended;
                    }
                }

                if (locator.utility){
                    Debug.Log(locator.utility.skillNameToken + locator.utility.IsReady() + locator.utility.baseRechargeInterval + " | " + locator.utility.stock); //locator.secondary.IsReady() &&
                    if (locator.utility.IsReady() && (locator.utility.baseRechargeInterval > .5 || locator.utility.skillNameToken == "SS2_NEMESIS_MERCENARY_UTILITY_BLINK_NAME")){
                        ++intended;
                    }
                }

                if (locator.special){
                    Debug.Log(locator.special.skillNameToken);
                    if (locator.special.IsReady() && locator.special.baseRechargeInterval > .5){
                        ++intended;
                    }else if (locator.special.skillNameToken == "SS2_CHIRR_BEFRIEND_NAME" && locator.special.stock > 0){
                        ++intended;
                    }
                }


            }

            int current = body.GetBuffCount(voidSlugRegen);
            while (current < intended){
                body.AddBuff(voidSlugRegen);
                ++current;
            }

            while (current > intended){
                body.RemoveBuff(voidSlugRegen);
                --current;
            }
        }


        private void SetSkillInternalSlug(On.RoR2.GenericSkill.orig_SetSkillInternal orig, GenericSkill self, SkillDef newSkill)
        {
            orig(self, newSkill);
            if (GetCount(self.characterBody) > 0){
                CheckStockCounts(self.characterBody);
            }
        }

        private void VoidSlugDeductStock(On.RoR2.GenericSkill.orig_DeductStock orig, GenericSkill self, int count)
        {
            orig(self, count);
            Debug.Log("bean: " + self.skillNameToken + " | " + self.name + " | " + self.GetScriptClassName());
            if (GetCount(self.characterBody) > 0){
                CheckStockCounts(self.characterBody);
            }
        }

        private void VoidSlugAlsoDeductStock(On.RoR2.Skills.SkillDef.orig_OnExecute orig, RoR2.Skills.SkillDef self, GenericSkill skill)
        {
            orig(self, skill);
            Debug.Log("bean2: " + self.skillNameToken + " | " + self.name + " | " + skill.GetScriptClassName() + " | " + skill.name);
            if (GetCount(skill.characterBody) > 0){
                CheckStockCounts(skill.characterBody);
            }
        }

        private void VoidSlugRestock(On.RoR2.GenericSkill.orig_RestockSteplike orig, GenericSkill self)
        {
            orig(self);
            if (GetCount(self.characterBody) > 0){
                CheckStockCounts(self.characterBody);
            }
        }

        private void VoidSlugAddOne(On.RoR2.GenericSkill.orig_AddOneStock orig, GenericSkill self)
        {
            orig(self);
            if (GetCount(self.characterBody) > 0){
                CheckStockCounts(self.characterBody);
            }
        }

        private void VoidSlugPack(On.RoR2.GenericSkill.orig_ApplyAmmoPack orig, GenericSkill self)
        {
            orig(self);
            if (GetCount(self.characterBody) > 0){
                CheckStockCounts(self.characterBody);
            }

        }

        private void VoidSlugReset(On.RoR2.GenericSkill.orig_Reset orig, GenericSkill self)
        {
            orig(self);
            if (GetCount(self.characterBody) > 0){
                CheckStockCounts(self.characterBody);
            }
        }

        private void VoidSlugInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (GetCount(self) > 0){
                CheckStockCounts(self);
            }
        }

        private void VoidSlugCheckStocks(CharacterBody self)
        {
            if (!self.inventory){
                return;
            }

            if (GetCount(self) > 0){
                CheckStockCounts(self);
            }
        }

        private void RebootEnter(On.EntityStates.Railgunner.Backpack.Reboot.orig_OnEnter orig, EntityStates.Railgunner.Backpack.Reboot self)
        {
            Debug.Log("reboob extra check");
            orig(self);
            if (GetCount(self.characterBody) > 0){
                CheckStockCounts(self.characterBody);
            }
        }

        private void OfflineEnter(On.EntityStates.Railgunner.Backpack.Offline.orig_OnEnter orig, EntityStates.Railgunner.Backpack.Offline self)
        {
            Debug.Log("Offlibe extra check");
            orig(self);
            if (GetCount(self.characterBody) > 0){
                CheckStockCounts(self.characterBody);
            }
        }

        private void AssualterExit2(On.EntityStates.Merc.Assaulter2.orig_OnExit orig, EntityStates.Merc.Assaulter2 self)
        {
            Debug.Log("aa2");
            orig(self);
            if (GetCount(self.characterBody) > 0)
            {
                CheckStockCounts(self.characterBody);
            }
        }

        private void AssualterExit(On.EntityStates.Merc.Assaulter.orig_OnExit orig, EntityStates.Merc.Assaulter self)
        {
            Debug.Log("aaa");
            orig(self);
            if (GetCount(self.characterBody) > 0)
            {
                CheckStockCounts(self.characterBody);
            }
        }

        public class VoidSlugToken : MonoBehaviour { }

    }

}