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
using System.Collections;
using RoR2.Projectile;

namespace vanillaVoid.Items
{
    public class ExeBlade : ItemBase<ExeBlade>
    {
        //public ConfigEntry<float> luckBonus;

        public ConfigEntry<float> additionalProcs;

        public ConfigEntry<float> deathDelay;

        public ConfigEntry<float> additionalDuration;

        public ConfigEntry<float> baseDamageAOEExe;

        public ConfigEntry<float> aoeRangeBaseExe;

        public ConfigEntry<bool> enableOnDeathDamage;

        //public ConfigEntry<float> aoeRangeStackingExe;

        public override string ItemName => "Executioner's Burden";

        public override string ItemLangTokenName => "EXEBLADE_ITEM";

        public override string ItemPickupDesc => tempItemPickupDesc;

        public override string ItemFullDescription => tempItemFullDescription;

        public override string ItemLore => $"<style=cMono>//-- AUTO-TRANSCRIPTION FROM CARGO BAY 14 OF UES [Redacted] --//</style>" +
            "\n\n\"Hey Joe, how are things g....what is all that. Why do you have so many swords.\"" +
            "\n\n\"Oh hi! Remember when I started, uh, trading with the void? Found a new candidate. Those rusty guillitones? Void loves 'em.\"" +
            "\n\n\"I..sure. Wait a sec, how many guillotines did you have?\"" +
            "\n\n\"...enough? Why's it matter?\"\n\n\"No no it's just that like, how are you supposed to use like, more than one of these things? Doesn't seem as applicable than having a guillotine strapped to everything.\"" +
            "\n\n\"That doesn't make any sense either though! Why did that ship have so many [REDACTED] guillotines anyway!\"";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlBladePickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("bladeIcon512.png");

        public static GameObject Projectile;

        public static GameObject ItemBodyModelPrefab;

        string tempItemPickupDesc;
        string tempItemFullDescription;

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.Damage };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            if (enableOnDeathDamage.Value)
            {
                tempItemPickupDesc = $"Your 'On-Kill' effects occur an additional time upon killing an elite. Additionally causes a damaging AOE upon elite kill. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                tempItemFullDescription = $"Your <style=cIsDamage>On-Kill</style> effects occur <style=cIsDamage>{additionalProcs.Value}</style> <style=cStack>(+{additionalProcs.Value} per stack)</style> additional times upon killing an elite. Additionally causes a <style=cIsDamage>{aoeRangeBaseExe.Value}m</style> explosion, dealing <style=cIsDamage>{baseDamageAOEExe.Value * 100}%</style> base damage. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

            }
            else
            {
                tempItemPickupDesc = $"Your 'On-Kill' effects occur an additional time upon killing an elite. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";
                tempItemFullDescription = $"Your <style=cIsDamage>On-Kill</style> effects occur <style=cIsDamage>{additionalProcs.Value}</style> <style=cStack>(+{additionalProcs.Value} per stack)</style> additional times upon killing an elite. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

            }
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);

            Hooks(); 
        }

        public override void CreateConfig(ConfigFile config)
        {
            string name = ItemName == "Executioner's Burden" ? "Executioners Burden" : ItemName;

            //luckBonus = config.Bind<float>("Item: " + name, "Luck Bonus", 1f, "Adjust the luck added to the hit that kills an elite.");
            additionalProcs = config.Bind<float>("Item: " + name, "Number of Addtional Procs", 1f, "Adjust the number of additional times on kill effects occur per stack.");
            deathDelay = config.Bind<float>("Item: " + name, "Time between Extra Procs", .3f, "Adjust the amount of time between each additional on-kill proc.");
            additionalDuration = config.Bind<float>("Item: " + name, "Additional Duration", 2.5f, "Adjust the amount of time the sword exists after the on-kill procs are finished.");

            enableOnDeathDamage = config.Bind<bool>("Item: " + name, "Enable Damage AOE", true, "Enable or disable the additional AOE without having to set the next two configs to zero. ");
            baseDamageAOEExe = config.Bind<float>("Item: " + name, "Percent Base Damage", 1f, "Adjust the percent base damage the AOE does.");
            aoeRangeBaseExe = config.Bind<float>("Item: " + name, "Range of AOE", 12f, "Adjust the range of the damaging AOE on the first stack.");
            voidPair = config.Bind<string>("Item: " + name, "Item to Corrupt", "ExecuteLowHealthElite", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlBladeDisplay.prefab");
            //string orbTransp = "RoR2/DLC1/voidraid/matVoidRaidPlanetPurpleWave.mat"; 
            //string orbCore = "RoR2/DLC1/voidstage/matVoidCoralPlatformPurple.mat";

            //string orbTransp = "RoR2/DLC1/voidraid/matVoidRaidPlanetPurpleWave.mat";
            //string orbCore = "RoR2/DLC1/voidstage/matVoidFoam.mat";
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
                    childName = "Chest",
                    localPos = new Vector3(0.05684097f, 0.3799726f, -0.1878726f),
                    localAngles = new Vector3(13.62447f, 90.2953f, 38.50036f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1555227f, 0.1579392f, -0.06899721f),
                    localAngles = new Vector3(2.563096f, 34.56156f, 7.501457f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0.1920169f, -0.04241098f, -0.02298578f),
                    localAngles = new Vector3(60.66765f, 200.7879f, 26.70174f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(1.890986f, 0.6897769f, -1.422848f),
                    localAngles = new Vector3(331.613f, 356.0543f, 7.945525f),
                    localScale = new Vector3(.5f, .5f, .5f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00722686f, 0.3719998f, -0.2702792f),
                    localAngles = new Vector3(349.887f, 86.67003f, 27.15041f),
                    localScale = new Vector3(0.175f, 0.175f, 0.175f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.1254412f, 0.02308899f, 0.1981006f),
                    localAngles = new Vector3(62.12778f, 82.70875f, 358.3321f),
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
                    childName = "Chest",
                    localPos = new Vector3(-0.01370448f, 0.1987826f, -0.2920466f),
                    localAngles = new Vector3(354.5892f, 82.3866f, 18.983f),
                    localScale = new Vector3(.12f, .12f, .12f)
                }
                
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.04337082f, -0.0548192f, -0.174685f),
                    localAngles = new Vector3(55.62988f, 21.04336f, 295.0692f),
                    localScale = new Vector3(0.115f, 0.115f, 0.115f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "FootBackR",
                    localPos = new Vector3(-0.0001340785f, 0.4513009f, -0.08311531f),
                    localAngles = new Vector3(12.71165f, 95.05572f, 190.8498f),
                    localScale = new Vector3(0.2f, 0.2f, 0.2f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.002651015f, -0.04257223f, -0.3807302f),
                    localAngles = new Vector3(349.9466f, 84.35776f, 18.38718f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.0140248f, -0.6587424f, 4.050778f),
                    localAngles = new Vector3(344.4937f, 89.13557f, 32.49933f),
                    localScale = new Vector3(1.1f, 1.1f, 1.1f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.007421299f, 0.0434005f, -0.2499133f),
                    localAngles = new Vector3(347.7402f, 95.98297f, 4.725613f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "TopRail",
                    localPos = new Vector3(-0.0006022891f, 0.6622809f, 0.02868086f),
                    localAngles = new Vector3(11.65705f, 357.5795f, 186.7869f),
                    localScale = new Vector3(0.125f, 0.125f, 0.125f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmR",
                    localPos = new Vector3(0.2549843f, 0.3083106f, -0.1155708f),
                    localAngles = new Vector3(5.404367f, 219.7214f, 190.5286f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(-1.861564f, 13.43366f, 4.458276f),
                    localAngles = new Vector3(345.1214f, 245.0656f, 9.101425f),
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
                    childName =  "Shield",
                    localPos =   new Vector3(0.4780795f, -0.6616167f, 0.5085409f),
                    localAngles = new Vector3(327.1772f, 30.54325f, 268.9095f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos =   new Vector3(0.007075419f, 0.01237386f, 0.005717664f),
                    localAngles = new Vector3(283.9196f, 224.3569f, 144.0466f),
                    localScale = new Vector3(.01f, .009f, .01f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] //these ones don't work for some reason!
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos =   new Vector3(-0.1096776f, 0.07879563f, -0.2880353f),
                    localAngles = new Vector3(13.29552f, 102.2941f, 170.1359f),
                    localScale = new Vector3(.175f, .15f, .175f)
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
            //        localScale = new Vector3(0f, 0f, 0f)
            //    }
            //});
            rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LegR",
                    localPos =   new Vector3(0f, 0f, 0.001307127f),
                    localAngles = new Vector3(12.1725f, 95.70362f, 186.0707f),
                    localScale = new Vector3(.0015f, .0015f, .0015f)
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
            //        localScale = new Vector3(0f, 0f, 0f)
            //    }
            //});
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos =   new Vector3(-0.1375525f, 0.06662301f, 0.1021552f),
                    localAngles = new Vector3(19.61578f, 27.82838f, 181.4652f),
                    localScale = new Vector3(.13f, .13f, .13f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Sheath",
                    localPos =   new Vector3(0.003468025f, 0.5852519f, 0.14202f),
                    localAngles = new Vector3(14.6782f, 356.295f, 188.6961f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }
            });
            rules.Add("mdlExecutioner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.0008111957f, -0.0005103727f, -0.001211213f),
                    localAngles = new Vector3(358.2127f, 67.5713f, 333.6733f),
                    localScale = new Vector3(0.0010f, 0.0010f, 0.0010f)
                }
            });
            rules.Add("mdlNemmando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Sword",
                    localPos = new Vector3(-0.0005331703f, 0.01267182f, 0.00001262315f),
                    localAngles = new Vector3(12.08878f, 117.7268f, 185.4063f),
                    localScale = new Vector3(0.0010f, 0.0010f, 0.0010f)
                }
            });
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(0.05391715f, 0.3403379f, -0.01506396f),
                    localAngles = new Vector3(7.134398f, 180.3909f, 180.3603f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(-0.09054253f, 0.05356936f, 0.024171f),
                    localAngles = new Vector3(14.07468f, 179.7917f, 192.2729f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
            });
            return rules;

        }

        public override void Hooks()
        {
            //On.RoR2.GlobalEventManager.OnHitEnemy += HitProcBonus;
            On.RoR2.GlobalEventManager.OnCharacterDeath += DeathProcBonus;
        }

        private void HitProcBonus(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            CharacterMaster attackerMaster = null;
            bool hasExeBlade = false;
            bool isKillingBlowElite = false;
            if (damageInfo.attacker && damageInfo.attacker.TryGetComponent<CharacterBody>(out var attackerBody))
            {
                attackerMaster = attackerBody.master;
                if (attackerBody.inventory)
                {
                    if(attackerBody.inventory.GetItemCount(ItemBase<ExeBlade>.instance.ItemDef) > 0)
                    {
                        //Debug.Log("player has blade");
                        hasExeBlade = true;
                    }
                }
            }
            
            if(victim.TryGetComponent<HealthComponent>(out var victimHC))
            {
                if (damageInfo.damage > victimHC.health && victimHC.body.isElite)
                {
                    isKillingBlowElite = true;
                    //Debug.Log("its a killing blow");
                }
            }

            //if (attackerMaster && hasExeBlade && isKillingBlowElite) attackerMaster.luck += luckBonus.Value;

            orig(self, damageInfo, victim);

            //if (attackerMaster && hasExeBlade && isKillingBlowElite) attackerMaster.luck -= luckBonus.Value;
        }

        private void DeathProcBonus(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            CharacterMaster attackerMaster = null;
            bool hasExeBlade = false;
            if (damageReport.attacker && damageReport.attacker.TryGetComponent<CharacterBody>(out var attackerBody))
            {
                attackerMaster = attackerBody.master;
                if (attackerBody.inventory)
                {
                    if (attackerBody.inventory.GetItemCount(ItemBase<ExeBlade>.instance.ItemDef) > 0)
                    {
                        hasExeBlade = true;
                    }
                }
            }

            //if (attackerMaster && hasExeBlade && damageReport.victim.body.isElite) attackerMaster.luck += luckBonus.Value;

            orig(self, damageReport);

            //if (attackerMaster && hasExeBlade && damageReport.victim.body.isElite) attackerMaster.luck -= luckBonus.Value;
        }
    }

}
