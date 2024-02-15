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
    public class ClockworkMechanism : ItemBase<ClockworkMechanism>
    {
        public ConfigEntry<int> itemVariant;

        public ConfigEntry<int> itemsPerStage;

        public ConfigEntry<int> itemsPerStageStacking;

        //public ConfigEntry<int> breaksPerStageCap;

        public ConfigEntry<float> directorBuff;

        public ConfigEntry<float> stackingBuff;

        public ConfigEntry<float> breakCooldown;

        public ConfigEntry<bool> scrapInstead;

        public ConfigEntry<bool> destroySelf;

        public ConfigEntry<bool> proritizeLowTier;

        public ConfigEntry<bool> scaleDestruction;

        public ConfigEntry<bool> alwaysHappen;
        public ConfigEntry<bool> bazaarHappen;

        public ConfigEntry<bool> var2Mult;
        public ConfigEntry<bool> isPerPlayer;

        public ConfigEntry<float> directorMultiplier;

        public ConfigEntry<float> directorMultiplierStacking;

        public ConfigEntry<int> variantBreakAmount;

        public Xoroshiro128Plus watchVoidRng;
        public override string ItemName => "Clockwork Mechanism";

        public override string ItemLangTokenName => "CLOCKWORK_ITEM";

        public override string ItemPickupDesc => tempItemPickupDesc;

        public override string ItemFullDescription => tempItemFullDescription;

        public override string ItemLore => tempLore;

        public override ItemTier Tier => ItemTier.VoidTier1;
        
        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlClockworkPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("watchIcon512.png");

        public override ItemTag[] ItemTags => new ItemTag[3] { ItemTag.Utility, ItemTag.LowHealth, ItemTag.AIBlacklist };

        public static GameObject ItemBodyModelPrefab;

        public BuffDef recentBreak { get; private set; }

        string tempItemPickupDesc;
        string tempItemFullDescription;
        string tempLore;

        bool isBazaarStage;
        bool isValid;

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);

            switch (itemVariant.Value)
            {
                case 0:
                    if (destroySelf.Value)
                    {
                        tempItemPickupDesc = $"Gain items at the start of the next stage. Breaks half of the current stack at low health. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                        tempItemFullDescription = $"Gain <style=cIsUtility>{itemsPerStage.Value}</style>" +
                            (itemsPerStageStacking.Value != 0 ? $" <style=cStack>(+{itemsPerStageStacking.Value} per stack)</style>" : "") + $" items at the start of the next stage. Taking damage to below <style=cIsHealth>25% health</style> breaks half of the current stack, with a cooldown of <style=cIsUtility>{breakCooldown.Value} seconds</style>. <style=cIsVoid>Corrupts all Delicate Watches</style>.";
                    }
                    else
                    {
                        tempItemPickupDesc = $"Gain items at the start of the next stage. Breaks a random item at low health. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                        tempItemFullDescription = $"Gain <style=cIsUtility>{itemsPerStage.Value}</style>" +
                            (itemsPerStageStacking.Value != 0 ? $" <style=cStack>(+{itemsPerStageStacking.Value} per stack)</style>" : "") + $" items at the start of the next stage. Taking damage to below <style=cIsHealth>25% health</style> breaks " +
                            (scaleDestruction.Value ? $"<style=cDeath>1</style> <style=cStack>(+1 per stack)</style> <style=cDeath>random items</style>" : "<style=cDeath>a random item</style>") + $", with a cooldown of <style=cIsUtility>{breakCooldown.Value} seconds</style>. <style=cIsVoid>Corrupts all Delicate Watches</style>.";

                    }
                    tempLore = $"\"The clock is always ticking. The hands of time move independently of your desire for them to still - the sands flow eternally and will never pause. Use what little time you have efficiently - once you've lost that time, it's quite hard to find more.\"" +
            "\n\n- Lost Journal, recovered from Petrichor V";
                    CreateBuff();

                    break;

                case 1:
                    if (destroySelf.Value) //this needs to be COMPLETELY redone but i'm not fucking doing that right now
                    {
                        tempItemPickupDesc = $"Increase the number of interactables per stage. Breaks half of the current stack at low health. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                        tempItemFullDescription = $"Increase the number of <style=cIsUtility>interactable credits</style> per stage by an amount equal to <style=cIsUtility>{directorBuff.Value}</style>" +
                            (stackingBuff.Value != 0 ? $" <style=cStack>(+{stackingBuff.Value} per stack)</style>" : "") + $". Taking damage to below <style=cIsHealth>25% health</style> breaks half of the current stack, with a cooldown of <style=cIsUtility>{breakCooldown.Value} seconds</style>. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    }
                    else
                    {
                        tempItemPickupDesc = $"Increase the number of interactables per stage. Breaks a random item at low health. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                        tempItemFullDescription = $"Increase the number of <style=cIsUtility>interactable credits</style> per stage by <style=cIsUtility>{directorBuff.Value}</style>" +
                            (stackingBuff.Value != 0 ? $" <style=cStack>(+{stackingBuff.Value} per stack)</style>" : "") + $". Taking damage to below <style=cIsHealth>25% health</style> breaks " +
                            (scaleDestruction.Value ? $"<style=cDeath>1</style> <style=cStack>(+1 per stack)</style> <style=cDeath>random items</style>" : "<style=cDeath>a random item</style>") + $", with a cooldown of <style=cIsUtility>{breakCooldown.Value} seconds</style>. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    
                    }
                    //if (scrapInstead.Value)
                    //{
                    //    tempItemPickupDesc = "Increase the number of interactables per stage. Scraps a random item at low health. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    //    tempItemFullDescription = $"Increase the number of <style=cIsUtility>interactables</style> per stage by an amount equal to <style=cIsUtility>{Math.Round(directorBuff.Value / 15, 1)}</style> <style=cStack>(+{Math.Round(stackingBuff.Value / 15, 1)} per stack)</style> chests. Taking damage to below <style=cIsHealth>25% health</style> scraps <style=cDeath>a random item</style>, with a cooldown of <style=cIsUtility>{breakCooldown.Value} seconds</style>. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    //}

                    //tempItemPickupDesc = $"Increase the number of interactables per stage. Breaks a random item at low health. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    //tempItemFullDescription = $"Increase the number of <style=cIsUtility>interactables</style> per stage by an amount equal to <style=cIsUtility>{Math.Round(directorBuff.Value / 15, 1)}</style> <style=cStack>(+{Math.Round(stackingBuff.Value / 15, 1)} per stack)</style> chests. Taking damage to below <style=cIsHealth>25% health</style> breaks <style=cDeath>a random item</style>, with a cooldown of <style=cIsUtility>{breakCooldown.Value} seconds</style>. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    tempLore = $"\"The clock is always ticking. The hands of time move independently of your desire for them to still - the sands flow eternally and will never pause. Use what little time you have efficiently - once you've lost that time, it's quite hard to find more.\"" +
            "\n\n- Lost Journal, recovered from Petrichor V";
                    CreateBuff();
                    break;

                case 2:
                    tempItemPickupDesc = $"Greatly increase the number of interactables in the next stage. Breaks after use. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    tempItemFullDescription = "";
                    tempLore = $"\"May your greed know no bounds. Take what you have, and destroy it, for something better. It will have been worth it. \nI guarantee it.\"\n\n- Lost Journal, recovered from Petrichor V";
                    if (variantBreakAmount.Value < 0)
                    {
                        tempItemFullDescription = (var2Mult.Value ? "Multiply " : "Increase ") + $"the number of <style=cIsUtility>" + (var2Mult.Value ? "interactables" : "interactable credits") + $"</style> in the next stage by <style=cIsUtility>{directorMultiplier.Value}</style>" +
                            (directorMultiplierStacking.Value != 0 ? $" <style=cStack>(+{directorMultiplierStacking.Value} per stack)</style>" : "") + $". Breaks <style=cDeath>all</style> stacks after use. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    }
                    else if (variantBreakAmount.Value == 1)
                    {
                        tempItemFullDescription = (var2Mult.Value ? "Multiply " : "Increase ") + $"the number of <style=cIsUtility>" + (var2Mult.Value ? "interactables" : "interactable credits") + $"</style> in the next stage by <style=cIsUtility>{directorMultiplier.Value}</style>" +
                            (directorMultiplierStacking.Value != 0 ? $" <style=cStack>(+{directorMultiplierStacking.Value} per stack)</style>" : "") + $". Breaks <style=cDeath>{variantBreakAmount.Value}</style> stack after use. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    }
                    else
                    {
                        tempItemFullDescription = (var2Mult.Value ? "Multiply " : "Increase ") + $"the number of <style=cIsUtility>" + (var2Mult.Value ? "interactables" : "interactable credits") + $"</style> in the next stage by <style=cIsUtility>{directorMultiplier.Value}</style>" +
                            (directorMultiplierStacking.Value != 0 ? $" <style=cStack>(+{directorMultiplierStacking.Value} per stack)</style>" : "") + $". Breaks <style=cDeath>{variantBreakAmount.Value}</style> stacks after use. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                    }

                    if (scrapInstead.Value)
                    {
                        tempItemPickupDesc.Replace("Breaks", "Scraps");
                        tempItemFullDescription.Replace("Breaks", "Scraps");
                    }
                    //tempItemPickupDesc.Replace("Breaks", "Scraps");
                    //tempItemFullDescription.Replace("Breaks", "Scraps");
                    break;
                    
                default:
                    tempItemPickupDesc = "Invalid item Variant in config. Please enter a 0, 1, or 2.";
                    tempItemFullDescription = $"Invalid item Variant in config. Please enter a 0, 1, or 2.";
                    break;
            }
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            //VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            //CreateBuff();
            Hooks();
        }

        public override void CreateConfig(ConfigFile config)
        {
            itemVariant = config.Bind<int>("Item: " + ItemName, "Variant of Item", 0, "Adjust which version of " + ItemName + " you'd prefer to use. Variant 0 gives you items at the start of each stage, and breaks a random item at low health. Variant 1 slightly increases interactables per stage, and breaks a random item at low health, while Variant 2 breaks itself at the start of the next stage, but greatly increases the number of interactables.");

            itemsPerStage = config.Bind<int>("Item: " + ItemName, "Items per Stage", 3, "Variant 0: Adjust the number of items you get upon entering a new stage with this item.");
            itemsPerStageStacking = config.Bind<int>("Item: " + ItemName, "Extra Items per Stack", 1, "Variant 0: Adjust the additional number of items you get for each subsequent stack.");
            bazaarHappen = config.Bind<bool>("Item: " + ItemName, "Function in Bazaar", false, "Variant 0: Adjust whether or not should function in the bazaar. This additionally causes the item to no longer break items in the bazaar.");

            breakCooldown = config.Bind<float>("Item: " + ItemName, "Breaking Cooldown", 3.0f, "Variant 0 and 1: Adjust how long the cooldown is between the item breaking other items.");
            scrapInstead = config.Bind<bool>("Item: " + ItemName, "Scrap Instead", false, "Variant 0 and 1: Adjust whether the items are scrapped or destroyed.");
            destroySelf = config.Bind<bool>("Item: " + ItemName, "Destroy Self Instead", false, "Variant 0 and 1: Adjust if the item should destroy itself, rather than other items. Destroys half of the current stack. Overrides the config option below (tier priority).");
            proritizeLowTier = config.Bind<bool>("Item: " + ItemName, "Prioritize Lower Tier", true, "Variant 0 and 1: Adjust the item's preference for lower tier items. False means no prefrence, true means a general preference (unlikely, but possible to destroy higher tiers).");
            scaleDestruction = config.Bind<bool>("Item: " + ItemName, "Scale Number of Items Broken per Stack", false, "Variant 0 and 1: Adjust whether or not the item should break more items the more stacks of it you have. One break per stack.");

            //breaksPerStageCap = config.Bind<int>("Item: " + ItemName, "Breaks per Stage", -1, "Cap the number of items this item can break per stage at this number. -1 means there is no cap.");
            alwaysHappen = config.Bind<bool>("Item: " + ItemName, "Function in Special Stages", false, "Variant 1: Adjust whether or not should function in stages where the director doesn't get any credits (ex Gilded Coast, Commencement, Bazaar).");
            directorBuff = config.Bind<float>("Item: " + ItemName, "Credit Bonus", 22.5f, "Variant 1: Adjust how many credits the first stack gives the director. 15 credits is one chest.");
            stackingBuff = config.Bind<float>("Item: " + ItemName, "Credit Bonus per Stack", 22.5f, "Variant 1: Adjust the increase gained per stack."); //22.5f is 1.5 chests

            isPerPlayer = config.Bind<bool>("Item: " + ItemName, "Multiply Adjustment per Player", false, "Variant 1 and 2: Adjust whether these variants should multiply the number of credits being added/multiplied to the director by the player count. Makes the item significantly stronger with many players.");
            
            var2Mult = config.Bind<bool>("Item: " + ItemName, "Multiply Credits", true, "Variant 2: Adjust whether the variant should multiply credits or add credits to the director. Multiplying is true, adding is false.");
            directorMultiplier = config.Bind<float>("Item: " + ItemName, "Director Multiplier", 1.75f, "Variant 2: Adjust the multiplier to the number of credits the director gets.");
            directorMultiplierStacking = config.Bind<float>("Item: " + ItemName, "Director Multiplier Stacking", 1f, "Variant 2: Adjust the multiplier bonus provided by every stack except the first (This means that in multiplayer, if two players have the item, the base multiplier will still only be applied once, and this one applied for every other stack).");
            variantBreakAmount = config.Bind<int>("Item: " + ItemName, "Variant 2 Breaks per Stage", -1, "Variant 2: Adjust how many items in the stack Variant 2 breaks when stage-transitioning. The number of items broken times the multiplier is how much the director credits will be increased by (thus breaking only one means the muliplier will only apply once, per player). A negative number means it will break the entire stack.");
            
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "FragileDamageBonus", "Adjust which item this is the void pair of.");
        }

        public void CreateBuff()
        {
            recentBreak = ScriptableObject.CreateInstance<BuffDef>();
            recentBreak.buffColor = Color.white;
            recentBreak.canStack = true;
            recentBreak.isDebuff = false;
            recentBreak.name = "ZnVV" + "shatterStatus";
            recentBreak.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("shatterStatus");
            ContentAddition.AddBuffDef(recentBreak);
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlClockworkDisplay.prefab");

            //string transpMat = "RoR2/DLC1/voidraid/matVoidRaidPlanetAcidRing.mat";
            //
            //var transpBit = ItemBodyModelPrefab.transform.Find("CaseTopTransp").GetComponent<MeshRenderer>(); //CaseTopTransp 
            //transpBit.material = Addressables.LoadAssetAsync<Material>(transpMat).WaitForCompletion();

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(-0.01480415f, 0.02261418f, 0.05049226f),
                    localAngles = new Vector3(76.72475f, 298.479f, 269.3308f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(0.02170372f, 0.02288273f, 0.03459106f),
                    localAngles = new Vector3(40.77299f, 59.15304f, 45.32623f),
                    localScale = new Vector3(0.055f, 0.055f, 0.055f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MainWeapon",
                    localPos = new Vector3(-0.05870793f, 0.4463752f, -0.03718469f),
                    localAngles = new Vector3(272.7316f, 270.5243f, 60.82039f),
                    localScale = new Vector3(0.055f, 0.055f, 0.055f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(-0.009995257f, 3.125906f, 0.3110375f),
                    localAngles = new Vector3(321.4942f, 87.76231f, 68.65869f),
                    localScale = new Vector3(.5f, .5f, .5f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.008402169f, 0.317406f, -0.02993058f),
                    localAngles = new Vector3(16.07273f, 266.9244f, 56.10647f),
                    localScale = new Vector3(.05f, .05f, .05f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.7556458f, 0.5156651f, -0.7279017f),
                    localAngles = new Vector3(359.746f, 4.18056f, 245.4453f),
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
                    childName = "UpperArmL",
                    localPos = new Vector3(0.02184698f, 0.2957828f, 0.01117276f),
                    localAngles = new Vector3(40.07924f, 169.024f, 58.42667f),
                    localScale = new Vector3(0.04f, 0.04f, 0.04f)
                }
                
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandR",
                    localPos = new Vector3(-0.005859678f, 0.04901259f, 0.08395796f),
                    localAngles = new Vector3(312.638f, 68.74719f, 76.17706f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(-0.001769217f, 0.8750249f, 0.07755471f),
                    localAngles = new Vector3(86.13224f, 306.094f, 282.6791f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.0394742f, 0.2271337f, -0.01859298f),
                    localAngles = new Vector3(284.3109f, 327.4875f, 323.4077f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(0.5622984f, 1.865077f, -0.6787693f),
                    localAngles = new Vector3(66.2142f, 74.12748f, 274.7035f),
                    localScale = new Vector3(0.7f, 0.7f, 0.7f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.01683217f, 0.1543664f, -0.03668313f),
                    localAngles = new Vector3(64.64587f, 102.4221f, 240.8662f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(-0.007638952f, 0.2209761f, -0.04840721f),
                    localAngles = new Vector3(278.3271f, 191.7697f, 152.4978f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmL",
                    localPos = new Vector3(0.04742341f, 0.3199588f, 0.01808804f),
                    localAngles = new Vector3(73.18906f, 38.83948f, 291.2487f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });

            //Modded Chars 
            rules.Add("EnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName =  "LowerArmR",
                    localPos =   new Vector3(-0.01429838f, 0.1628605f, 0.09412687f),
                    localAngles = new Vector3(311.9879f, 76.6885f, 66.30946f),
                    localScale = new Vector3(0.08f, 0.08f, 0.08f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ElbowR",
                    localPos = new Vector3(-0.00110624f, 0.006843123f, 0.002782739f),
                    localAngles = new Vector3(282.4773f, 143.4393f, 359.4552f),
                    localScale = new Vector3(0.0015f, 0.0015f, 0.0015f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] //these ones don't work for some reason!
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.1011845f, 0.2749843f, 0.007670165f),
                    localAngles = new Vector3(273.0486f, 82.48837f, 352.7972f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
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
                    childName = "ElbowR",
                    localPos = new Vector3(0.0004766831f, 0.00165303f, 0.0005244045f),
                    localAngles = new Vector3(80.75832f, 74.6026f, 14.01888f),
                    localScale = new Vector3(0.0005f, 0.0005f, 0.0005f)
                }
            });
            rules.Add("mdlSniper", new RoR2.ItemDisplayRule[] //delicate watch doesn't have a display?
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(-0.04290858f, 0.0918955f, -0.03801716f),
                    localAngles = new Vector3(274.6219f, 252.0854f, 131.9637f),
                    localScale = new Vector3(.03f, .03f, .03f)
                }
            });
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[] //dancer doesn't have a watch display so hopefully this is okay
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.07516018f, 0.08470076f, 0.03731194f),
                    localAngles = new Vector3(80.93408f, 46.17232f, 319.4481f),
                    localScale = new Vector3(0.045f, 0.045f, 0.045f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LeftForearm",
                    localPos = new Vector3(-0.0002630837f, 0.1237077f, -0.04502212f),
                    localAngles = new Vector3(73.09189f, 236.2813f, 31.97718f),
                    localScale = new Vector3(0.0375f, 0.0375f, 0.0375f)
                }
            });
            rules.Add("mdlExecutioner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(0.0007848355f, -0.00006233127f, -0.000125948f),
                    localAngles = new Vector3(41.89046f, 95.15518f, 350.9249f),
                    localScale = new Vector3(0.0005f, 0.0005f, 0.0005f)
                }
            });
            rules.Add("mdlNemmando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ArmL",
                    localPos = new Vector3(0.00009739055f, 0.001602058f, -0.0005851662f),
                    localAngles = new Vector3(4.887274f, 261.3586f, 57.98376f),
                    localScale = new Vector3(0.0005f, 0.0005f, 0.0005f)
                }
            });
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmL",
                    localPos = new Vector3(-0.01949072f, 0.207683f, 0.0484807f),
                    localAngles = new Vector3(74.97482f, 16.09941f, 0.4198857f),
                    localScale = new Vector3(.05f, .05f, .05f)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.008442232f, 0.2362802f, -0.04167819f),
                    localAngles = new Vector3(272.091f, 270.4824f, 61.01729f),
                    localScale = new Vector3(.05f, .05f, .05f)
                }
            });
            rules.Add("mdlHANDOverclocked", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(-0.2449125f, 0.4218235f, 0.06620573f),
                    localAngles = new Vector3(13.52424f, 9.952581f, 60.31852f),
                    localScale = new Vector3(.35f, .35f, .35f)
                }
            });
            rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "forearm.L",
                    localPos = new Vector3(0.005877196f, 0.1465954f, 0.09405536f),
                    localAngles = new Vector3(72.98094f, 322.9417f, 297.9598f),
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
                    childName = "HandL",
                    localPos = new Vector3(-0.24002F, 0.44941F, -0.0827F),
                    localAngles = new Vector3(0.57549F, 1.82044F, 66.94895F),
                    localScale = new Vector3(0.3F, 0.3F, 0.3F)
                }
            });
            //rules.Add("Spearman", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "arm.l",
            //        localPos = new Vector3(-0.00018F, 0.01277F, -0.00243F),
            //        localAngles = new Vector3(6.04041F, 101.8368F, 256.191F),
            //        localScale = new Vector3(0.002F, 0.002F, 0.002F)
            //    }
            //});
            rules.Add("mdlAssassin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "arm_bone2.R",
                    localPos = new Vector3(-0.08237F, 0.5071F, -0.03797F),
                    localAngles = new Vector3(272.3181F, 23.97936F, 14.97954F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(-0.00601F, 0.00042F, 0.05118F),
                    localAngles = new Vector3(81.09294F, 71.11505F, 59.86337F),
                    localScale = new Vector3(0.04F, 0.04F, 0.04F)
                }
            });
            rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ArmL",
                    localPos = new Vector3(0.27042F, 1.05886F, 0.00165F),
                    localAngles = new Vector3(354.8711F, 171.603F, 59.65002F),
                    localScale = new Vector3(0.25F, 0.25F, 0.25F)
                }
            });
            rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(-0.05748191f, 0.2905322f, 0.01414572f),
                    localAngles = new Vector3(290.123f, 15.52077f, 57.30558f),
                    localScale = new Vector3(.05f, .05f, .05f)
                }
            });
            rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ShoulderR",
                    localPos = new Vector3(0.02588F, 0.62864F, -0.16148F),
                    localAngles = new Vector3(296.3986F, 266.609F, 70.40233F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
            });
            //rules.Add("RobDriverBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "HandL",
            //        localPos = new Vector3(0.03259495f, -0.01915513f, 0.001383516f),
            //        localAngles = new Vector3(88.92389f, 347.7476f, 240.2745f),
            //        localScale = new Vector3(0.05f, 0.05f, 0.05f)
            //    }
            //});
            rules.Add("mdlTeslaTrooper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MuzzleGauntlet",
                    localPos = new Vector3(-0.00023F, 0.06107F, 0.01692F),
                    localAngles = new Vector3(70.3254F, 58.49537F, 37.27173F),
                    localScale = new Vector3(0.065F, 0.065F, 0.065F)
                }
            });
            rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MuzzleGauntlet",
                    localPos = new Vector3(-0.13232F, 0.00126F, -0.25011F),
                    localAngles = new Vector3(357.2711F, 182.882F, 246.5683F),
                    localScale = new Vector3(0.0625F, 0.0625F, 0.0625F)
                }
            });
            rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(-0.02995F, -0.06883F, 0.08595F),
                    localAngles = new Vector3(59.26641F, 1.97252F, 342.1661F),
                    localScale = new Vector3(0.06F, 0.06F, 0.06F)
                }
            });
            return rules;

        }

        public override void Hooks()
        {
            
            if (itemVariant.Value == 0 || itemVariant.Value == 1)
            {
                On.RoR2.HealthComponent.UpdateLastHitTime += BreakItem;
                if(itemVariant.Value == 0)
                {
                    Stage.onServerStageBegin += DetermineStage;
                }
            }
            RoR2.SceneDirector.onPrePopulateSceneServer += HelpDirector;
            On.RoR2.InfiniteTowerRun.OnPrePopulateSceneServer += HelpSimulacrum;
            RoR2.SceneDirector.onPostPopulateSceneServer += Variant2Clear;
            //On.RoR2.Stage.RespawnCharacter += StageRewards;
        }

        private void Variant2Clear(SceneDirector obj)
        {
            //Debug.Log("Hej jag är jen");
            if (isValid && itemVariant.Value == 2) //var 2
            {
                //Debug.Log("Jag heter j");
                int itemCount = 0;
                int tempItemCount = 0;
                int playerCount = PlayerCharacterMasterController.instances.Count;
                //Debug.Log("Jag heter j " + playerCount);
                //if (playerCount == 0)
                //{
                //    playerCount = 1; //don't think this should ever happen but i wnana be sure it doesnt!
                //}
                foreach (var player in PlayerCharacterMasterController.instances)
                {
                    //itemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                    //Debug.Log("player ");
                    tempItemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                    if (tempItemCount > 0)
                    {
                        //Debug.Log("the j is r eal");
                        if (variantBreakAmount.Value < 0)
                        {
                            player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                            player.master.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, tempItemCount);
                        }
                        else
                        {
                            if (variantBreakAmount.Value > tempItemCount)
                            {
                                player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                                player.master.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, tempItemCount);
                            }
                            else
                            {
                                player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, variantBreakAmount.Value);
                                player.master.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, variantBreakAmount.Value);
                            }
                        }
                        //player.body.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                        //player.body.inventory.GiveItem(ItemBase<BrokenClockworkMechanism>.instance.ItemDef, tempItemCount);
                        CharacterMasterNotificationQueue.SendTransformNotification(player.master, ItemBase<ClockworkMechanism>.instance.ItemDef.itemIndex, ItemBase<ConsumedClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                    }
                    itemCount += tempItemCount;
                    tempItemCount = 0;

                }
                //obj.interactableCredit *= (int)(directorMultiplier.Value * (float)itemCount);
            }
        }

        private void HelpSimulacrum(On.RoR2.InfiniteTowerRun.orig_OnPrePopulateSceneServer orig, InfiniteTowerRun self, SceneDirector obj)
        {
            orig(self, obj);
            //Debug.Log("SIMU function starting, interactable credits: " + obj.interactableCredit);
            if ((alwaysHappen.Value || obj.interactableCredit != 0) && itemVariant.Value == 1){ //var 1
                //Debug.Log("Variant 1 rawring");
                int itemCount = 0;
                //int playerCount = 0; // icould probably do this in a better way
                int playerCount = PlayerCharacterMasterController.instances.Count;
                if (playerCount == 0)
                {
                    playerCount = 1; //don't think this should ever happen but i wnana be sure it doesnt!
                }
                foreach (var player in PlayerCharacterMasterController.instances)
                {
                    //++playerCount;
                    itemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                }
                //Debug.Log("itemCount: " + itemCount);

                if (itemCount > 0)
                {
                    float creditBoost = ((directorBuff.Value + (stackingBuff.Value * (itemCount - 1f))));
                    if (isPerPlayer.Value)
                    {
                        creditBoost *= playerCount;
                    }
                    obj.interactableCredit += (int)creditBoost;
                    //Debug.Log("creditBoost: " + creditBoost);
                }
            }
            else if (obj.interactableCredit != 0 && itemVariant.Value == 2) //var 2
            {
                //Debug.Log("Variant 2 rawring");

                int itemCount = 0;
                int tempItemCount = 0;
                int playerCount = PlayerCharacterMasterController.instances.Count;
                if (playerCount == 0)
                {
                    playerCount = 1; //don't think this should ever happen but i wnana be sure it doesnt!
                }

                foreach (var player in PlayerCharacterMasterController.instances)
                {
                    //itemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);

                    tempItemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                    if (tempItemCount > 0)
                    {
                        //Debug.Log("enough ");
                        if (variantBreakAmount.Value < 0)
                        {
                            //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                            //player.master.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, tempItemCount);
                            float creditMult = directorMultiplier.Value + (directorMultiplierStacking.Value * (float)tempItemCount);

                            if (isPerPlayer.Value)
                            {
                                creditMult *= playerCount;
                            }

                            if (var2Mult.Value)
                            {
                                //obj.interactableCredit = (int)(obj.interactableCredit * creditMult);
                                obj.interactableCredit *= (int)creditMult;
                            }
                            else
                            {
                                obj.interactableCredit += (int)creditMult;
                            }
                        }
                        else
                        {
                            if (variantBreakAmount.Value > tempItemCount)
                            {
                                //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                                //player.master.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, tempItemCount);
                                float creditMult = directorMultiplier.Value + (directorMultiplierStacking.Value * (float)tempItemCount);
                                if (var2Mult.Value)
                                {
                                    obj.interactableCredit = (int)(obj.interactableCredit * creditMult);
                                }
                                else
                                {
                                    obj.interactableCredit = (int)(obj.interactableCredit + creditMult);
                                }
                            }
                            else
                            {
                                //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, variantBreakAmount.Value);
                                //player.master.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, variantBreakAmount.Value);
                                float creditMult = directorMultiplier.Value + (directorMultiplierStacking.Value * (float)variantBreakAmount.Value);
                                if (var2Mult.Value)
                                {
                                    obj.interactableCredit = (int)(obj.interactableCredit * creditMult);
                                }
                                else
                                {
                                    obj.interactableCredit = (int)(obj.interactableCredit + creditMult);
                                }

                            }
                        }
                        //player.body.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                        //player.body.inventory.GiveItem(ItemBase<BrokenClockworkMechanism>.instance.ItemDef, tempItemCount);
                        //CharacterMasterNotificationQueue.SendTransformNotification(player.master, ItemBase<ClockworkMechanism>.instance.ItemDef.itemIndex, ItemBase<ConsumedClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                    }
                    itemCount += tempItemCount;
                    tempItemCount = 0;

                }
                //obj.interactableCredit *= (int)(directorMultiplier.Value * (float)itemCount);
            }
            //Debug.Log("SIMU function ending, interactable credits after: " + obj.interactableCredit);
        }

        private void DetermineStage(Stage obj)
        {
            isBazaarStage = false;
            if (obj.sceneDef == SceneCatalog.GetSceneDefFromSceneName("bazaar"))
            {
                //Debug.Log("it's the bazaar");
                isBazaarStage = true;
            }
            if ((bazaarHappen.Value || !isBazaarStage) && itemVariant.Value == 0) //var 0
            {
                //int itemCount = 0;
                foreach (var player in PlayerCharacterMasterController.instances)
                {
                    int itemCount = player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                    if (itemCount > 0)
                    {
                        int rewardCount = itemsPerStage.Value + (itemsPerStageStacking.Value * (itemCount - 1));
                        for (int i = 0; i < rewardCount; i++)
                        {
                            if (watchVoidRng == null)
                            {
                                watchVoidRng = new Xoroshiro128Plus(Run.instance.seed);
                            }

                            PickupIndex pickupResult;// = PickupIndex.none;
                            int randInt = watchVoidRng.RangeInt(1, 100); // 1-79 white // 80-99 green // 100 red
                            if (randInt < 80)
                            {
                                List<PickupIndex> whiteList = new List<PickupIndex>(Run.instance.availableTier1DropList);
                                Util.ShuffleList(whiteList, watchVoidRng);
                                //itemResult = whiteList[0].itemIndex;
                                pickupResult = whiteList[0];
                            }
                            else if (randInt < 99)
                            {
                                List<PickupIndex> greenList = new List<PickupIndex>(Run.instance.availableTier2DropList);
                                Util.ShuffleList(greenList, watchVoidRng);
                                //itemResult = greenList[0].itemIndex;
                                pickupResult = greenList[0];
                            }
                            else
                            {
                                List<PickupIndex> redList = new List<PickupIndex>(Run.instance.availableTier3DropList);
                                Util.ShuffleList(redList, watchVoidRng);
                                //itemResult = redList[0].itemIndex;
                                pickupResult = redList[0];
                            }

                            //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                            float num = 360f / (float)rewardCount;
                            Vector3 a = Quaternion.AngleAxis(num * (float)i, Vector3.up) * Vector3.forward;
                            Vector3 position = player.gameObject.transform.position + a * 8f + Vector3.up * 8f;

                            //PickupDropletController.CreatePickupDroplet(pickupResult, position, Vector3.zero); // <- this sort of worked? work on it later
                            //EffectManager.SpawnEffect(Singularity.effectPrefab, new EffectData
                            //{
                            //    origin = position,
                            //    scale = 2f
                            //}, true);
                            //this.itemDropCount++;

                            player.master.inventory.GiveItem(pickupResult.itemIndex, 1);
                            GenericPickupController.SendPickupMessage(player.master, pickupResult);
                            //CharacterMasterNotificationQueue.SendTransformNotification(player.master, ItemBase<ClockworkMechanism>.instance.ItemDef.itemIndex, itemResult, CharacterMasterNotificationQueue.TransformationType.Default);

                        }
                    }
                }

            }
        }

        private void HelpDirector(SceneDirector obj)
        {
            isValid = false;
            //Debug.Log("function starting, interactable credits: " + obj.interactableCredit);
            if ((bazaarHappen.Value || !isBazaarStage) && itemVariant.Value == 0 && false) //var 0
            {
                //int itemCount = 0;
                foreach (var player in PlayerCharacterMasterController.instances)
                {  
                    int itemCount = player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                    if (itemCount > 0)
                    {
                        int rewardCount = itemsPerStage.Value + (itemsPerStageStacking.Value * (itemCount - 1));
                        for(int i = 0; i< rewardCount; i++)
                        {
                            if (watchVoidRng == null)
                            {
                                watchVoidRng = new Xoroshiro128Plus(Run.instance.seed);
                            }

                            PickupIndex pickupResult;// = PickupIndex.none;
                            int randInt = watchVoidRng.RangeInt(1, 100); // 1-79 white // 80-99 green // 100 red
                            if (randInt < 80)
                            {
                                List<PickupIndex> whiteList = new List<PickupIndex>(Run.instance.availableTier1DropList);
                                Util.ShuffleList(whiteList, watchVoidRng);
                                //itemResult = whiteList[0].itemIndex;
                                pickupResult = whiteList[0];
                            }
                            else if(randInt < 99)
                            {
                                List<PickupIndex> greenList = new List<PickupIndex>(Run.instance.availableTier2DropList);
                                Util.ShuffleList(greenList, watchVoidRng);
                                //itemResult = greenList[0].itemIndex;
                                pickupResult = greenList[0];
                            }
                            else
                            {
                                List<PickupIndex> redList = new List<PickupIndex>(Run.instance.availableTier3DropList);
                                Util.ShuffleList(redList, watchVoidRng);
                                //itemResult = redList[0].itemIndex;
                                pickupResult = redList[0];
                            }

                            //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                            float num = 360f / (float)rewardCount;
                            Vector3 a = Quaternion.AngleAxis(num * (float)i, Vector3.up) * Vector3.forward;
                            Vector3 position = player.gameObject.transform.position + a * 8f + Vector3.up * 8f;

                            player.master.inventory.GiveItem(pickupResult.itemIndex, 1);
                            GenericPickupController.SendPickupMessage(player.master, pickupResult);
                            //CharacterMasterNotificationQueue.SendTransformNotification(player.master, ItemBase<ClockworkMechanism>.instance.ItemDef.itemIndex, itemResult, CharacterMasterNotificationQueue.TransformationType.Default);

                        }
                    }
                }

            }
            else if((alwaysHappen.Value || obj.interactableCredit != 0) && itemVariant.Value == 1) { //var 1
                int itemCount = 0;
                //int playerCount = 0; // icould probably do this in a better way
                int playerCount = PlayerCharacterMasterController.instances.Count;
                if (playerCount == 0)
                {
                    playerCount = 1; //don't think this should ever happen but i wnana be sure it doesnt!
                }
                foreach (var player in PlayerCharacterMasterController.instances)
                {
                    //++playerCount;
                    itemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                }

                float creditBoost = ((directorBuff.Value + (stackingBuff.Value * (itemCount - 1f))));
                if (isPerPlayer.Value)
                {
                    creditBoost *= playerCount;
                }
                obj.interactableCredit += (int)creditBoost;
            }
            else if(obj.interactableCredit != 0 && itemVariant.Value == 2) //var 2
            {
                int itemCount = 0;
                int tempItemCount = 0;
                int playerCount = PlayerCharacterMasterController.instances.Count;
                if(playerCount == 0)
                {
                    playerCount = 1; //don't think this should ever happen but i wnana be sure it doesnt!
                }
                foreach (var player in PlayerCharacterMasterController.instances)
                {
                    //itemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);

                    tempItemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                    if (tempItemCount > 0)
                    {
                        if (variantBreakAmount.Value < 0)
                        {
                            //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                            //player.master.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, tempItemCount);
                            float creditMult = directorMultiplier.Value + (directorMultiplierStacking.Value * (float)tempItemCount);

                            if (isPerPlayer.Value)
                            {
                                creditMult *= playerCount;
                            }

                            if (var2Mult.Value)
                            {
                                //obj.interactableCredit = (int)(obj.interactableCredit * creditMult);
                                obj.interactableCredit *= (int)creditMult;
                            }
                            else
                            {
                                obj.interactableCredit += (int)creditMult;
                            }
                        }
                        else
                        {
                            if (variantBreakAmount.Value > tempItemCount)
                            {
                                //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                                //player.master.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, tempItemCount);
                                float creditMult = directorMultiplier.Value + (directorMultiplierStacking.Value * (float)tempItemCount);
                                if (var2Mult.Value)
                                {
                                    obj.interactableCredit = (int)(obj.interactableCredit * creditMult);
                                }
                                else
                                {
                                    obj.interactableCredit = (int)(obj.interactableCredit + creditMult);
                                }
                            }
                            else
                            {
                                //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, variantBreakAmount.Value);
                                //player.master.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, variantBreakAmount.Value);
                                float creditMult = directorMultiplier.Value + (directorMultiplierStacking.Value * (float)variantBreakAmount.Value);
                                if (var2Mult.Value)
                                {
                                    obj.interactableCredit = (int)(obj.interactableCredit * creditMult);
                                }
                                else
                                {
                                    obj.interactableCredit = (int)(obj.interactableCredit + creditMult);
                                }
                               
                            }
                        }
                        //player.body.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
                        //player.body.inventory.GiveItem(ItemBase<BrokenClockworkMechanism>.instance.ItemDef, tempItemCount);
                        //CharacterMasterNotificationQueue.SendTransformNotification(player.master, ItemBase<ClockworkMechanism>.instance.ItemDef.itemIndex, ItemBase<ConsumedClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                        isValid = true;
                    }
                    itemCount += tempItemCount;
                    tempItemCount = 0;
                    
                }
                //obj.interactableCredit *= (int)(directorMultiplier.Value * (float)itemCount);
            }
            //Debug.Log("function ending, interactable credits after: " + obj.interactableCredit);
        }

        private void BreakItem(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, HealthComponent self, float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker)
        {
            orig.Invoke(self, damageValue, damagePosition, damageIsSilent, attacker);
            //Debug.Log("attacker: " + attacker);
            if (NetworkServer.active && (bool)self && (bool)self.body && ItemBase<ClockworkMechanism>.instance.GetCount(self.body) > 0 && self.isHealthLow && !(self.GetComponent<CharacterBody>().GetBuffCount(recentBreak) > 0) && attacker && (bazaarHappen.Value || !isBazaarStage))
            {
                var cb = self.GetComponent<CharacterBody>();
                for(int i = 1; i <= Mathf.Ceil(breakCooldown.Value); i++)
                {
                    cb.AddTimedBuffAuthority(recentBreak.buffIndex, i);
                }

                if (watchVoidRng == null)
                {
                    watchVoidRng = new Xoroshiro128Plus(Run.instance.seed);
                }

                int itemTierInt = 0;
                if (destroySelf.Value)
                {
                    float count = (float)ItemBase<ClockworkMechanism>.instance.GetCount(self.body);
                    int toLose = (int)Math.Ceiling(count / 2f);

                    self.body.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, toLose);
                    self.body.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef, toLose);
                    CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, ItemBase<ClockworkMechanism>.instance.ItemDef.itemIndex, ItemBase<ConsumedClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);

                }
                else
                {
                    List<ItemIndex> list = new List<ItemIndex>(self.body.inventory.itemAcquisitionOrder);
                    ItemIndex itemIndex = ItemIndex.None;
                    Util.ShuffleList(list, watchVoidRng);

                    int tempCount = 0;
                    foreach (ItemIndex item in list)
                    {

                        ItemDef itemDef = ItemCatalog.GetItemDef(item);
                        if ((bool)itemDef && itemDef.tier != ItemTier.NoTier)
                        {
                            bool allowHigherTier = Util.CheckRoll(.5f);
                            //bool isScrap = false;
                            itemIndex = item;
                            //Debug.Log("index of current item: "+ itemDef.name);
                            if(itemDef.name.Equals("ScrapWhite") || itemDef.name.Equals("ScrapGreen") || itemDef.name.Equals("ScrapRed") || itemDef.name.Equals("ScrapYellow"))
                            {
                                //Debug.Log("attempted to scrap scrap! continuing");
                                continue;
                            }
                            if (proritizeLowTier.Value) //what the fuck is this i don't remember 
                            {
                                //itemIndex = item;
                                //Debug.Log("iten chosen: " + item + " and: " + allowHigherTier);
                                if (itemDef.tier == ItemTier.Lunar)
                                {
                                    if (Util.CheckRoll(.33f)) //allow it to still destroy lunars, but have it be unlikely
                                    {
                                        //itemIndex = item;
                                        //Debug.Log("lunar iten chosen: " + itemIndex);
                                        itemTierInt = 5;
                                        if (!scaleDestruction.Value)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else if (itemDef.tier == ItemTier.Boss || itemDef.tier == ItemTier.VoidBoss)
                                {
                                    if (allowHigherTier && Util.CheckRoll(.80f)) // extra check for boss and red so it's just a little less likely 
                                    {
                                        //itemIndex = item;
                                        //Debug.Log("boss iten chosen: " + itemIndex);
                                        itemTierInt = 4;
                                        if (!scaleDestruction.Value)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else if (itemDef.tier == ItemTier.Tier3 || itemDef.tier == ItemTier.VoidTier3)
                                {
                                    if (allowHigherTier && Util.CheckRoll(.80f))
                                    {
                                        //itemIndex = item;
                                        //Debug.Log("RED iten chosen: " + itemIndex);
                                        itemTierInt = 3;
                                        if (!scaleDestruction.Value)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else if (itemDef.tier == ItemTier.Tier2 || itemDef.tier == ItemTier.VoidTier2)
                                {
                                    if (allowHigherTier)
                                    {
                                        //itemIndex = item;
                                        //Debug.Log("GREN iten chosen: " + itemIndex);
                                        itemTierInt = 2;
                                        if (!scaleDestruction.Value)
                                        {
                                            break;
                                        }
                                    }
                                }
                                else if (itemDef.tier == ItemTier.Tier1 || itemDef.tier == ItemTier.VoidTier1)
                                {
                                    //itemIndex = item;
                                    //Debug.Log("WHITE iten chosen: " + itemIndex);
                                    itemTierInt = 1;
                                    if (!scaleDestruction.Value)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (!scaleDestruction.Value)
                                {
                                    break;
                                }
                            }

                            if (scaleDestruction.Value)
                            {
                                self.body.inventory.RemoveItem(itemIndex);

                                if (scrapInstead.Value)
                                {
                                    switch (itemTierInt)
                                    {
                                        case 1:
                                            self.body.inventory.GiveItem(RoR2Content.Items.ScrapWhite);
                                            CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, RoR2Content.Items.ScrapWhite.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                            break;
                                        case 2:
                                            self.body.inventory.GiveItem(RoR2Content.Items.ScrapGreen);
                                            CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, RoR2Content.Items.ScrapGreen.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                            break;
                                        case 3:
                                            self.body.inventory.GiveItem(RoR2Content.Items.ScrapRed);
                                            CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, RoR2Content.Items.ScrapRed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                            break;
                                        case 4:
                                            self.body.inventory.GiveItem(RoR2Content.Items.ScrapYellow);
                                            CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, RoR2Content.Items.ScrapYellow.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                            break;
                                        case 5:
                                            self.body.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef);
                                            CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, ItemBase<ConsumedClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                            break;
                                        default:
                                            Debug.LogError("Clockwork Mechanism didn't properly select an item to destroy, unable to give correct scrap.");
                                            break;
                                    }
                                }
                                else
                                {
                                    self.body.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef);
                                    CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, ItemBase<ConsumedClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                }
                                ++tempCount;
                                if(tempCount >= ItemBase<ClockworkMechanism>.instance.GetCount(self.body))
                                {
                                    break;
                                }
                            }
                            //itemIndex = item;
                            //break;
                        }
                        

                    }
                    if (itemIndex != ItemIndex.None && !scaleDestruction.Value)
                    {
                        self.body.inventory.RemoveItem(itemIndex);

                        if (scrapInstead.Value)
                        {
                            switch (itemTierInt)
                            {
                                case 1:
                                    self.body.inventory.GiveItem(RoR2Content.Items.ScrapWhite);
                                    CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, RoR2Content.Items.ScrapWhite.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                    break;
                                case 2:
                                    self.body.inventory.GiveItem(RoR2Content.Items.ScrapGreen);
                                    CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, RoR2Content.Items.ScrapGreen.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                    break;
                                case 3:
                                    self.body.inventory.GiveItem(RoR2Content.Items.ScrapRed);
                                    CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, RoR2Content.Items.ScrapRed.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                    break;
                                case 4:
                                    self.body.inventory.GiveItem(RoR2Content.Items.ScrapYellow);
                                    CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, RoR2Content.Items.ScrapYellow.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                    break;
                                case 5:
                                    self.body.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef);
                                    CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, ItemBase<ConsumedClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                                    break;
                                default:
                                    Debug.LogError("Clockwork Mechanism didn't properly select an item to destroy, unable to give correct scrap.");
                                    break;
                            }
                        }
                        else
                        {
                            self.body.inventory.GiveItem(ItemBase<ConsumedClockworkMechanism>.instance.ItemDef);
                            CharacterMasterNotificationQueue.SendTransformNotification(self.body.master, itemIndex, ItemBase<ConsumedClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                        }
                    }
                }

                EffectData effectData = new EffectData
                {
                    origin = self.transform.position
                };
                effectData.SetNetworkedObjectReference(self.gameObject);
                EffectManager.SpawnEffect(HealthComponent.AssetReferences.fragileDamageBonusBreakEffectPrefab, effectData, transmit: true);
            }
            //orig(self, damageValue, damagePosition, damageIsSilent, attacker);
        }
    }
}
