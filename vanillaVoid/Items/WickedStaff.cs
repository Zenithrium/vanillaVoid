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
using VoidItemAPI;
using vanillaVoid.Misc;
using System.Linq;

namespace vanillaVoid.Items
{
    public class WickedStaff : ItemBase<WickedStaff>
    {
        public ConfigEntry<float> strongerBleedDamage;

        public ConfigEntry<float> strongerBleedDamageStacking;

        public ConfigEntry<float> durationBase;

        public ConfigEntry<float> durationStacking;

        public ConfigEntry<float> regenBoostPercent;

        public ConfigEntry<float> regenBoostPercentStacking;

        public ConfigEntry<float> healthBoostPercent;

        public ConfigEntry<float> healthBoostPercentStacking;

        public ConfigEntry<float> maxBonusPerStack;

        public ConfigEntry<string> voidPair;

        //private Xoroshiro128Plus watchVoidRng;
        public override string ItemName => "Wicked Staff";

        public override string ItemLangTokenName => "WICKEDSTAFF_ITEM";

        public override string ItemPickupDesc => "Your bleed effects last longer and deal more damage. Gain health regen for each stack of bleed inflicted. <style=cIsVoid>Corrupts all Ignition Tanks</style>.";

        public override string ItemFullDescription => $"Bleed effects deal 444% damage over time, and last {durationBase.Value} (+{durationStacking.Value} per stack) seconds. Each applied stack of bleed increases health regen by {regenBoostPercent.Value} (+{regenBoostPercentStacking.Value} per stack) up to a max of {maxBonusPerStack.Value}. <style=cIsVoid>Corrupts all Ignition Tanks</style>.";

        public override string ItemLore => $"<style=cSub>Order: Wicked Staff \nTracking Number: 907***** \nEstimated Delivery: 1/12/2057 \nShipping Method: High Priority/Fragile \nShipping Address: 1414 Place, Fillmore, Venus \nShipping Details: \n\n</style>" +
            "Apparently the legends pale to the truth of this relic. The wicked king; as he was called, was able to wield the power gained from the ritual sacrifices through a catalyst; this staff in particular. He used it to expand his life and kingdom, but in the final days of his exceptionally long life he rapidly aged then apparently turned to dust, like a mummy finally being exposed to air." +
            "\n\nI know I'm not meant to ask questions in my...line of work..but you aren't planning to do anything with these items, right? These make for beautiful display pieces, and I imagine it'd be quite...unfortunate if they were used for anything else.";

        public override ItemTier Tier => ItemTier.VoidTier2;
        
        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlStaffPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("watchIcon512.png");

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Damage, ItemTag.Healing };

        public static GameObject ItemBodyModelPrefab;

        public BuffDef hyperBleed { get; private set; }

        public static DotController.DotDef hyperBleedDotDef;
        public static DotController.DotIndex hyperBleedDotIndex;

        public static DamageColorIndex indexHyperBleed;
        //public 

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);

            CreateBuffs();
            Hooks();
            
        }

        public override void CreateConfig(ConfigFile config)
        {
            strongerBleedDamage = config.Bind<float>("Item: " + ItemName, "Sanguine Weep Damage", 444f, "Adjust how much damage the stronger bleed does.");
            strongerBleedDamageStacking = config.Bind<float>("Item: " + ItemName, "Stacking Damage Bonus", 0f, "Adjust how much damage the bleed gains per stack. For reference, bleed does 240%, and Sanguine Weep does 444%.");
            durationBase = config.Bind<float>("Item: " + ItemName, "Sanguine Weep Duration", 3f, "Adjust the duration of Sanguine Weep in seconds. Duration of bleed, for reference, is 3 seconds.");
            durationStacking = config.Bind<float>("Item: " + ItemName, "Stacking Duration Bonus", 1f, "Adjust the duration gained per stack in seconds.");
            
            regenBoostPercent = config.Bind<float>("Item: " + ItemName, "Regen Boost", 0.05f, "Adjust the percentage boost to health regen each applied stack of Sanguine Weep provides.");
            regenBoostPercentStacking = config.Bind<float>("Item: " + ItemName, "Regen Boost Stacking", 0.025f, "Adjust the additional boost to health regen each stack of this item provides.");

            healthBoostPercent = config.Bind<float>("Item: " + ItemName, "Health Boost", 0, "Adjust the percentage boost to max health each applied stack of Sanguine Weep provides. If you want this mechanic, the recommended percent would be 2.5% to 5% (0.025 or 0.05).");
            healthBoostPercentStacking = config.Bind<float>("Item: " + ItemName, "Health Boost Stacking", 0, "Adjust the percentage boost to max health each applied stack of Sanguine Weep provides. If you want this mechanic, the recommended percent would be half whatever you put for the health boost.");

            maxBonusPerStack = config.Bind<float>("Item: " + ItemName, "Max Bonus per Stack", 20, "Adjust the number of simulataneous applications of Sanguine Weep that will provide the above buffs per stack of this item.");

            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "StrengthenBurn", "Adjust which item this is the void pair of.");
        }

        public class StaffToken : MonoBehaviour
        {
            public int itemCount;
            public int enemiesEffected;
        }

        public void CreateBuffs()
        {
            hyperBleed = ScriptableObject.CreateInstance<BuffDef>();
            hyperBleed.buffColor = Color.white;
            hyperBleed.canStack = true;
            hyperBleed.isDebuff = false;
            hyperBleed.name = "ZnVV" + "hyperBleed";
            hyperBleed.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("shatterStatus");
            ContentAddition.AddBuffDef(hyperBleed);

            ModdedDamageColors.ReserveColor(new Color(.885f, 0, .15f), out indexHyperBleed);
            hyperBleedDotDef = new DotController.DotDef
            {
                associatedBuff = hyperBleed,
                damageCoefficient = 1f,
                damageColorIndex = indexHyperBleed,
                interval = 0.25f
            };

            hyperBleedDotIndex = DotAPI.RegisterDotDef(hyperBleedDotDef, (self, dotStack) =>
            {
                DotController.DotStack oldDotStack = self.dotStackList.FirstOrDefault(x => x.dotIndex == dotStack.dotIndex);
                if (oldDotStack != null)
                {
                    self.RemoveDotStackAtServer(self.dotStackList.IndexOf(oldDotStack));
                }
            
                var itemCount = 1;
                //var attackerLevel = 1f;
                var damageMultiplier = strongerBleedDamage.Value;
                var isPlayerTeam = false;
                if (dotStack.attackerObject)
                {
                    //int itemCount = dotStack.attackerObject.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                    var staffHelper = dotStack.attackerObject.GetComponent<StaffToken>();
                    if (staffHelper) itemCount = staffHelper.itemCount;

                    var victimBody = self.victimBody;
                    int hyperbleedCount = 0;
                    if (victimBody)
                    {
                        hyperbleedCount = victimBody.GetBuffCount(hyperBleed);
                        //if (hyperbleedCount > 0)
                        //{
                        //    victimBody.RemoveBuff(hyperBleed);
                        //    for (int i = 0; i <= hyperbleedCount + 1; i++)
                        //    {
                        //        float duration = durationBase.Value + (durationStacking.Value * (itemCount - 1));
                        //        victimBody.AddTimedBuff(hyperBleed, duration);
                        //    }
                        //
                        //    //victimBody.AddTimedBuff(hyperBleed, duration);
                        //}
                    }


                    var attackerBody = dotStack.attackerObject.GetComponent<CharacterBody>();
                    if (attackerBody)
                    {
                        isPlayerTeam = attackerBody.teamComponent.teamIndex == TeamIndex.Player;
                        dotStack.damage = attackerBody.damage * hyperbleedCount * hyperBleedDotDef.interval;
                    }
                }
            });
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlStaffDisplay.prefab");

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
            //rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
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
            //rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Body",
            //        localPos = new Vector3(0F, 0.00347F, -0.00126F),
            //        localAngles = new Vector3(0F, 90F, 0F),
            //        localScale = new Vector3(0.01241F, 0.01241F, 0.01241F)
            //    }
            //});
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

            return rules;

        }

        public override void Hooks()
        {
            //On.RoR2.HealthComponent.UpdateLastHitTime += BreakItem;
            //RoR2.SceneDirector.onPrePopulateSceneServer += HelpDirector;
            //RecalculateStatsAPI.GetStatCoefficients += CalculateStatsStaffHook;

            On.RoR2.HealthComponent.TakeDamage += AddSanguineWeepDot;
            On.RoR2.CharacterBody.OnInventoryChanged += AddStaffTokenOnPickup;

            On.RoR2.DotController.AddDot += TrackDotApplication;
            On.RoR2.DotController.OnDotStackRemovedServer += TrackDotRemoval;
            
            //Debug.Log("adding hook");
        }

        private void TrackDotApplication(On.RoR2.DotController.orig_AddDot orig, DotController self, GameObject attackerObject, float duration, DotController.DotIndex dotIndex, float damageMultiplier, uint? maxStacksFromAttacker, float? totalDamage)
        {
            if(dotIndex == hyperBleedDotIndex)
            {
                var staffToken = attackerObject.GetComponent<StaffToken>();
                if (staffToken)
                {
                    staffToken.enemiesEffected++;
                }
            }
        }
        private void TrackDotRemoval(On.RoR2.DotController.orig_OnDotStackRemovedServer orig, DotController self, object dotStack)
        {
            throw new NotImplementedException();
        }

        private void AddStaffTokenOnPickup(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            var staffToken = self.gameObject.GetComponent<StaffToken>();
            if (!staffToken)
            {
                self.gameObject.AddComponent<StaffToken>();
                staffToken.enemiesEffected = 0;
            }
            staffToken.itemCount = self.inventory.GetItemCount(ItemBase<WickedStaff>.instance.ItemDef);
        }

        private void AddSanguineWeepDot(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            CharacterBody victimBody = self.body;
            if (damageInfo.attacker && damageInfo.attacker.GetComponent<CharacterBody>() && !(damageInfo.damageType.ToString().Equals("DoT")))
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody.inventory)
                {
                    var stackCount = GetCount(attackerBody);

                    if (stackCount > 0)
                    {
                        var victimObject = self.body.gameObject;

                        if (victimBody && victimObject)
                        {
                            float duration = durationBase.Value + (durationStacking.Value * (stackCount - 1));
                            RoR2.DotController.InflictDot(victimObject, damageInfo.attacker, hyperBleedDotIndex, duration, strongerBleedDamage.Value / 100f, null);
                            
                            int debuffCount = victimBody.GetBuffCount(hyperBleed);
                        }
                    }
                }
            }
            orig(self, damageInfo);
        }
        //private static void CalculateStatsStaffHook(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        //{
        //    if (sender && sender.inventory)
        //    {
        //        //float levelBonus = sender.level - 1f;
        //        int glassesCount = sender.inventory.GetItemCount(RoR2Content.Items.CritGlasses);
        //        int orreryCount = sender.inventory.GetItemCount(ItemBase<LensOrrery>.instance.ItemDef);
        //        if (orreryCount > 0)
        //        {
        //            args.bleed += baseCrit.Value;
        //            if (glassesCount > 0)
        //            {
        //                args.critAdd += (glassesCount * 10 * (newLensBonus.Value + ((orreryCount - 1) * newStackingLensBonus.Value)));
        //            }
        //        }
        //    }
        //
        //}


        //private void HelpDirector(SceneDirector obj)
        //{
        //    if (alwaysHappen.Value || obj.interactableCredit != 0) {
        //        //Debug.Log("function starting, interactable credits: " + obj.interactableCredit);
        //        int itemCount = 0;
        //        foreach (var player in PlayerCharacterMasterController.instances)
        //        {
        //            itemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
        //        }
        //        obj.interactableCredit += (int)(directorBuff.Value + (stackingBuff.Value * (itemCount - 1)));
        //    }
        //    //Debug.Log("function ending, interactable credits after: " + obj.interactableCredit);
        //}
        //
        //private void BreakItem(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, HealthComponent self, float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker)
        //{
        //    orig.Invoke(self, damageValue, damagePosition, damageIsSilent, attacker);
        //    if (NetworkServer.active && (bool)self && (bool)self.body && ItemBase<ClockworkMechanism>.instance.GetCount(self.body) > 0 && self.isHealthLow && !(self.GetComponent<CharacterBody>().GetBuffCount(recentBreak) > 0) )
        //    {
        //        self.GetComponent<CharacterBody>().AddTimedBuff(recentBreak, breakCooldown.Value);
        //        if (watchVoidRng == null)
        //        {
        //            watchVoidRng = new Xoroshiro128Plus(Run.instance.seed);
        //        }
        //
        //        List<ItemIndex> list = new List<ItemIndex>(self.body.inventory.itemAcquisitionOrder);
        //        ItemIndex itemIndex = ItemIndex.None;
        //        Util.ShuffleList(list, watchVoidRng);
        //        foreach (ItemIndex item in list)
        //        {
        //            
        //            ItemDef itemDef = ItemCatalog.GetItemDef(item);
        //            if ((bool)itemDef && itemDef.tier != ItemTier.NoTier)
        //            {
        //                itemIndex = item;
        //                break;
        //            }
        //            
        //        }
        //        if (itemIndex != ItemIndex.None)
        //        {
        //            self.body.inventory.RemoveItem(itemIndex);
        //            self.body.inventory.GiveItem(ItemBase<BrokenClockworkMechanism>.instance.ItemDef);
        //            CharacterMasterNotificationQueue.PushItemTransformNotification(self.body.master, itemIndex, ItemBase<BrokenClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
        //        }
        //
        //        //List<ItemIndex> itemList = new List<ItemIndex>(self.body.inventory.itemAcquisitionOrder);
        //        //Util.ShuffleList(itemList, watchVoidRng);
        //
        //        //self.body.inventory.GiveItem(ItemBase<BrokenClockworkMechanism>.instance.ItemDef, 1);
        //        //self.body.inventory.RemoveItem(ItemCatalog.GetItemDef(itemList[0]), 1);
        //        //CharacterMasterNotificationQueue.PushItemTransformNotification(self.body.master, ItemCatalog.GetItemDef(itemList[0]).itemIndex, ItemBase<BrokenClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
        //        EffectData effectData = new EffectData
        //        {
        //            origin = self.transform.position
        //        };
        //        effectData.SetNetworkedObjectReference(self.gameObject);
        //        EffectManager.SpawnEffect(HealthComponent.AssetReferences.fragileDamageBonusBreakEffectPrefab, effectData, transmit: true);
        //    }
        //    orig(self, damageValue, damagePosition, damageIsSilent, attacker);
        //}
    }
}
