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
using VoidItemAPI;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace vanillaVoid.Items
{
    public class LensOrrery : ItemBase<LensOrrery>
    {
        public static ConfigEntry<float> lensBonus;

        public static ConfigEntry<float> stackingLensBonus;

        public static ConfigEntry<float> baseCrit;

        public ConfigEntry<float> additionalCritLevels;

        public ConfigEntry<string> voidPair;

        public override string ItemName => "Lens-Maker's Orrery";

        public override string ItemLangTokenName => "ORRERY_ITEM";

        public override string ItemPickupDesc => "<style=cIsUtility>Increased effectiveness</style> of <style=cIsUtility>lens-related</style> items. Your <style=cIsDamage>Critical strikes</style> can dip an <style=cIsDamage>additional time</style>. <style=cIsVoid>Corrupts all Laser Scopes</style>.";

        public override string ItemFullDescription => $"Gain <style=cIsDamage>{baseCrit.Value}% critical chance</style>. Lens-Maker's Glasses and Lost Seer's Lenses are <style=cIsUtility>{lensBonus.Value * 100}%</style> <style=cStack>(+{stackingLensBonus.Value * 100}% per stack)</style> <style=cIsUtility>more effective</style>. <style=cIsDamage>Critical strikes</style> can dip <style=cIsDamage>{additionalCritLevels.Value}</style> <style=cStack>(+{additionalCritLevels.Value} per stack)</style> additional times. <style=cIsVoid>Corrupts all Laser Scopes</style>.";

        public override string ItemLore => $"<style=cSub>Order: Lens-Maker's Orrery \nTracking Number: ******** \nEstimated Delivery: 1/13/2072 \nShipping Method: High Priority/Fragile/Confidiential \nShipping Address: [REDACTED] \nShipping Details: \n\n</style>" + 
            "The Lens-Maker, as mysterious as they are influential. From my research I have surmised that she has been appointed to \"Final Verdict\", the most prestigious role of leadership in the House Beyond. Our team managed to locate a workshop of hers where she was supposedly working on some never-before concieved tech - but something was off. " +
            "Looking through her schematics and trinkets I found something odd - something unlike what I was anticipating. A simple orrery, clearly her design, but without her classic red, replaced with a peculiar purple. At first I worried that when she learned of our arrival, when she left in a rush, that we had ruined some of her masterpieces...but maybe it's best we interrupted her. " +
            "\n\nGiven that this is one of a kind, and quite a special work of hers at that; I expect much more than just currency in payment.";

        public override ItemTier Tier => ItemTier.VoidTier3;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlOrreryPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("orreryIcon.png");


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
            string name = ItemName == "Lens-Maker's Orrery" ? "Lens-Makers Orrery" : ItemName;

            lensBonus = config.Bind<float>("Item: " + name, "Crit Glasses Buff", .2f, "Adjust the percent buff to crit glasses on the first stack.");
            stackingLensBonus = config.Bind<float>("Item: " + name, "Crit Glasses Buff per Stack", .1f, "Adjust the percent buff to crit glasses per stack.");
            additionalCritLevels = config.Bind<float>("Item: " + name, "Additional Crit Levels", 1f, "Adjust the number of additional crit levels each stack allows.");
            baseCrit = config.Bind<float>("Item: " + name, "Base Crit Increase", 5f, "Adjust the percent crit increase the first stack provides.");
            voidPair = config.Bind<string>("Item: " + name, "Item to Corrupt", "CritDamage", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlOrreryDisplay.prefab");

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "GunR",
                    localPos = new Vector3(-0.3150219f, 0.07268968f, 0.001457882f),
                    localAngles = new Vector3(21.02493f, 182.9109f, 268.9313f),
                    localScale = new Vector3(0.025f, 0.0275f, 0.025f)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "GunL",
                    localPos = new Vector3(0.314453f, 0.07321943f, 0.000545822f),
                    localAngles = new Vector3(340.4638f, 179.7426f, 90.17532f),
                    localScale = new Vector3(0.025f, 0.0275f, 0.025f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "BowBase",
                    localPos = new Vector3(0.0003995618f, -0.01066974f, -0.02548181f),
                    localAngles = new Vector3(53.28633f, 90.95795f, 271.0485f),
                    localScale = new Vector3(0.02f, 0.02f, 0.02f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "SideWeapon",
                    localPos = new Vector3(0.001457527f, -0.3454178f, 0.1164442f),
                    localAngles = new Vector3(1.021756f, 61.6526f, 181.9272f),
                    localScale = new Vector3(0.021f, 0.021f, 0.021f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(1.720154f, 2.793335f, -0.5041912f),
                    localAngles = new Vector3(328.8118f, 359.6891f, 269.949f),
                    localScale = new Vector3(0.25f, 0.25f, 0.25f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //i thought it'd be funny
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CannonHeadL",
                    localPos = new Vector3(0.1727509f, 0.2344139f, 0.1739555f),
                    localAngles = new Vector3(359.3283f, 134.9317f, 89.44088f),
                    localScale = new Vector3(0.03f, 0.03f, 0.03f)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CannonHeadL",
                    localPos = new Vector3(0.1712845f, 0.3824237f, 0.1750213f),
                    localAngles = new Vector3(45.06831f, 314.9317f, 269.0563f),
                    localScale = new Vector3(0.03f, 0.03f, 0.03f)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CannonHeadR",
                    localPos = new Vector3(-0.1721647f, 0.3835272f, 0.1749064f),
                    localAngles = new Vector3(359.4401f, 44.99557f, 90.67099f),
                    localScale = new Vector3(0.03f, 0.03f, 0.03f)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CannonHeadR",
                    localPos = new Vector3(-0.1714954f, 0.234437f, 0.1763944f),
                    localAngles = new Vector3(359.4401f, 44.99557f, 90.67099f),
                    localScale = new Vector3(0.03f, 0.03f, 0.03f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.56402f, 0.2794803f, -0.6070232f),
                    localAngles = new Vector3(0F, 270F, 256.4133f),
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
                    childName = "LowerArmL",
                    localPos = new Vector3(0.01558512f, 0.1889396f, -0.112668f),
                    localAngles = new Vector3(357.0237f, 87.78452f, 270.8785f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                },
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(0.03190426f, 0.1868501f, 0.105431f),
                    localAngles = new Vector3(3.080307f, 277.278f, 269.6242f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                },

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.1444179f, 0.0711498f, 0.05627137f),
                    localAngles = new Vector3(88.13691f, 236.2245f, 147.7779f),
                    localScale = new Vector3(0.0225f, 0.0225f, 0.0225f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "WeaponPlatform",
                    localPos = new Vector3(-0.000464663f, 0.2993166f, 0.3180331f),
                    localAngles = new Vector3(0f, 270f, 270f),
                    localScale = new Vector3(0.0525f, 0.0525f, 0.0525f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.1332231f, 0.153714f, 0.007588402f),
                    localAngles = new Vector3(0f, 0f, 281.5431f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(-0.7995564f, 3.801572f, -1.064525f),
                    localAngles = new Vector3(300.2426f, 317.4773f, 77.4535f),
                    localScale = new Vector3(0.4f, 0.4f, 0.4f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(0.06454472f, 0.4170405f, 0.006606602f),
                    localAngles = new Vector3(0, 121.5928f, 0),
                    localScale = new Vector3(0.02f, 0.02f, 0.02f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(0.0140196f, 0.2451563f, 0.04593971f),
                    localAngles = new Vector3(0f, 270f, 0f),
                    localScale = new Vector3(0.02f, 0.02f, 0.02f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hand",
                    localPos = new Vector3(0.04298216f, 0.09566361f, 0.009505212f),
                    localAngles = new Vector3(349.6481f, 185.366f, 91.24784f),
                    localScale = new Vector3(0.02f, 0.02f, 0.02f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(3.098453f, -0.4452723f, 2.468437f),
                    localAngles = new Vector3(1.632177f, 320.1595f, 268.687f),
                    localScale = new Vector3(0.9f, 0.9f, 0.9f)
                }
            });
            return rules;

        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += OrreryCritBonus;
            RecalculateStatsAPI.GetStatCoefficients += CalculateStatsHook;
        }

        private static void CalculateStatsHook(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender && sender.inventory)
            {
                float levelBonus = sender.level - 1f;
                int glassesCount = sender.inventory.GetItemCount(RoR2Content.Items.CritGlasses);
                int orreryCount = sender.inventory.GetItemCount(ItemBase<LensOrrery>.instance.ItemDef);
                if (orreryCount > 0)
                {
                    args.critAdd += baseCrit.Value;
                    if (glassesCount > 0)
                    {
                        args.critAdd += (glassesCount * 10 * (lensBonus.Value + ((orreryCount - 1) * stackingLensBonus.Value)));
                    }
                }
            }
        }

        private void OrreryCritBonus(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo) {
            CharacterBody victimBody = self.body;
            if (damageInfo.attacker && damageInfo.attacker.GetComponent<CharacterBody>())
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody.inventory)
                {
                    var orreryCount = GetCount(attackerBody);
                    //int glassesCount = attackerBody.inventory.GetItemCount(RoR2Content.Items.CritGlasses);
                    if (orreryCount > 0)
                    {
                        var critChance = attackerBody.crit;
                        //self.crit returns 100 if you have 100% chance

                        if (critChance > 100)
                        {
                            float critMod = critChance % 100; //chance for next tier of crit
                            float baseLevel = ((critChance - critMod) / 100);
                            Debug.Log("crit bonus level is " + baseLevel);
                            if (baseLevel >= orreryCount + 1)
                            {
                                baseLevel = orreryCount + 1; //cap it based on number of orrerys
                                Debug.Log("crit was too high! bonus level is now " + baseLevel);
                            }
                            else
                            {
                                if (Util.CheckRoll(critMod, attackerBody.master))
                                {
                                    baseLevel += 1;

                                    Debug.Log("crited! bonus level is" + baseLevel);
                                }
                                else
                                {
                                    //Debug.Log("no crit. bonus level is" + baseLevel);
                                }
                            }
                            //Debug.Log("damage was " + damageInfo.damage);
                            if (baseLevel > 1)
                            {
                                damageInfo.damage *= (attackerBody.critMultiplier * baseLevel);
                                //damageInfo.damageType |= DamageType.VoidDeath; 
                                damageInfo.damageColorIndex = DamageColorIndex.Void;
                                damageInfo.damage /= attackerBody.critMultiplier; //this is because the last crit (the normal one) isn't really handled here, and i didn't want to do an IL hook again
                            }
                            //Debug.Log("damage is " + damageInfo.damage);

                            //sorry this is complete ass coding. i am stupid today.
                        }
                    }

                }
                
            }
            orig(self, damageInfo);
        }
    }
}
