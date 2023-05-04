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

namespace vanillaVoid.Items
{
    public class WhorlShell : ItemBase<WhorlShell>
    {
        public ConfigEntry<float> baseChance;

        public ConfigEntry<float> regenAmountBase;

        public ConfigEntry<float> regenAmountStacking;

        public ConfigEntry<string> voidPair;

        public override string ItemName => "Ceaseless Cornucopia";

        public override string ItemLangTokenName => "CORNUCOPIA_ITEM";

        public override string ItemPickupDesc => "Overkilling an enemy massively increases regen for a short time. <style=cIsVoid>Corrupts all Harvester's Scythes</style>.";

        public override string ItemFullDescription => $"Killing an enemy has a Z% chance to increase health regen by X (+X per stack) for Y seconds. Chance increases the more damage the killing blow dealt. <style=cIsVoid>Corrupts all Harvester's Scythes</style>.";

        public override string ItemLore => $"Corn Lore";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlWhorlPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("adzeIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.Healing };

        public BuffDef WhorlBuff { get; private set; }
        public static ProcChainMask ignoredProcs;
        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            CreateBuff();
            Hooks(); 
        }

        public override void CreateConfig(ConfigFile config)
        {
            baseChance = config.Bind<float>("Item: " + ItemName, "Base Chance to Activate", .05f, "Adjust the base chance of the on death proc activating.");
            regenAmountBase = config.Bind<float>("Item: " + ItemName, "Heal Amount", 8f, "Healing.");
            regenAmountStacking = config.Bind<float>("Item: " + ItemName, "Heal Amount Stacking", 4f, "Healing.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "", "Adjust which item this is the void pair of.");
        }

        public void CreateBuff()
        {
            WhorlBuff = ScriptableObject.CreateInstance<BuffDef>();
            WhorlBuff.buffColor = Color.white;
            WhorlBuff.canStack = true;
            WhorlBuff.isDebuff = false;
            WhorlBuff.name = "ZnVV" + "WhorlBuff";
            WhorlBuff.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("whorlRegenBuffIcon");
            ContentAddition.AddBuffDef(WhorlBuff);
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlWhorlDisplay.prefab");

            string orbTexture = "RoR2/DLC1/voidstage/matVoidAsteroid.mat";
            string orbOuter = "RoR2/DLC1/VoidCamp/matVoidCampLock.mat";

            var orbPartPickup1 = ItemModel.transform.Find("Orb").GetComponent<MeshRenderer>();
            var orbOuterPickup1 = ItemModel.transform.Find("OrbAura").GetComponent<MeshRenderer>();
            orbPartPickup1.material = Addressables.LoadAssetAsync<Material>(orbTexture).WaitForCompletion();
            orbOuterPickup1.material = Addressables.LoadAssetAsync<Material>(orbOuter).WaitForCompletion();

            var orbPartDisplay = ItemBodyModelPrefab.transform.Find("Orb").GetComponent<MeshRenderer>();
            var orbOuterDisplay = ItemBodyModelPrefab.transform.Find("OrbAura").GetComponent<MeshRenderer>();
            orbPartDisplay.material = Addressables.LoadAssetAsync<Material>(orbTexture).WaitForCompletion();
            orbOuterDisplay.material = Addressables.LoadAssetAsync<Material>(orbOuter).WaitForCompletion();

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
                    localPos = new Vector3(-0.1678362f, 0.2800805f, -0.1426394f),
                    localAngles = new Vector3(5.870443f, 265.1015f, 331.878f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("NemmandoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.1678362f, 0.2800805f, -0.1426394f),
                    localAngles = new Vector3(5.870443f, 265.1015f, 331.878f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });

            return rules;
        }

        
        public override void Hooks()
        {
            //On.RoR2.HealthComponent.TakeDamage += WhorlOverkillBonusHit;
            On.RoR2.GlobalEventManager.OnCharacterDeath += WhorlOverkillBonus;
            RecalculateStatsAPI.GetStatCoefficients += CalculateStatsWhorlHook;


        }

        private void CalculateStatsWhorlHook(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender && sender.inventory)
            {
                int buffStacks = sender.GetBuffCount(WhorlBuff);
                int count = sender.inventory.GetItemCount(ItemBase<WhorlShell>.instance.ItemDef);
                if (sender.GetBuffCount(WhorlBuff) > 0)
                {
                    
                    //float regenAmnt = regenAmountBase.Value;
                    //if(count > 0)
                    //{
                    //    regenAmnt += regenAmountStacking.Value * (count - 1);
                    //}

                    //args.baseRegenAdd += (regenAmnt + ((regenAmnt / 5) * sender.level)) * buffStacks; //original code not taken from bitter root becasue i wasnt lazy x2

                    args.baseHealthAdd += 25 * buffStacks;
                    //args.baseRegenAdd += slowPercentage.Value;
                }
            }
        }

        private void WhorlOverkillBonusHit(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            Debug.Log(" health: " + self.health + " fullhealth: " + self.fullHealth + " low?:" + self.isHealthLow + " killtype: " + self.killingDamageType + " damageinfo: " + damageInfo.damage + " damagetype" + damageInfo.damageType);
            orig(self, damageInfo);
            
        }

        private void WhorlOverkillBonus(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            //damageReport.victim.health
            Debug.Log(" health: " + damageReport.victim.health + " fullhealth: " + damageReport.victim.fullHealth + " low?:" + damageReport.victim.isHealthLow + " killtype: " + damageReport.victim.killingDamageType + " damageinfo: " + damageReport.damageInfo.damage + " damagetype" + damageReport.damageInfo.damageType);
            orig(self, damageReport);
            CharacterBody attacker = damageReport.attackerBody;
            if (attacker && damageReport.victim)
            {
                if (attacker.inventory)
                {
                    if (attacker.inventory.GetItemCount(ItemBase<WhorlShell>.instance.ItemDef) > 0)
                    {
                        //if (CalculateOverkill(damageReport))
                        //{
                        //    
                        //    attacker.AddTimedBuffAuthority(WhorlRegen.buffIndex, 5);
                        //}
                        float overkillAmount = Mathf.Abs(damageReport.victim.health);
                        attacker.healthComponent.Heal(.05f * overkillAmount, ignoredProcs, true);
                        attacker.AddTimedBuffAuthority(WhorlBuff.buffIndex, 5);
                    }
                }
            }
            
        }

        private bool CalculateOverkill(DamageReport damageReport)
        {
            if(damageReport.victim.killingDamageType == DamageType.VoidDeath)
            {
                return true;
            }
            HealthComponent victim = damageReport.victim;
            float overkillAmount = Mathf.Abs(victim.health);
            float maxHealth = victim.fullHealth;
            float chance = (overkillAmount / (maxHealth / 2)) * 100; //overkill bonus guaranteed if final hit makes the enemy to -33% hp
            Debug.Log("chance: " + chance);
            return Util.CheckRoll(chance);
            //return true;
        }

        //private void AdzeDamageBonus(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo) {
        //    //CharacterBody victimBody = self.body;
        //    float initialDmg = damageInfo.damage;
        //    if (damageInfo.attacker && damageInfo.attacker.GetComponent<CharacterBody>())
        //    {
        //        CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
        //        if (attackerBody.inventory)
        //        {
        //            var stackCount = GetCount(attackerBody);
        //
        //            if (stackCount > 0)
        //            {
        //                //var healthPercentage = self.health / self.fullCombinedHealth;
        //                var healthFraction = Mathf.Clamp((1 - self.combinedHealthFraction), 0f, 1f);
        //                //Debug.Log("health fraction: " + healthFraction);
        //                var mult = healthFraction * (baseDamageBuff.Value + (stackingBuff.Value * (stackCount - 1)));
        //                
        //                damageInfo.damage = damageInfo.damage + (damageInfo.damage * mult);
        //                float maxDamage = initialDmg + (initialDmg * (baseDamageBuff.Value + (stackingBuff.Value * (stackCount - 1))));
        //                //Debug.Log("max damage: " + maxDamage + " | actual damage: " + damageInfo.damage + " | original damage: " + initialDmg);
        //                //damageInfo.damage = damageInfo.damage * (1 + (victimBody.GetBuffCount(adzeDebuff) * dmgPerDebuff.Value));
        //                //if(damageInfo.damage > maxDamage)
        //                //{
        //                //    //Debug.Log("damage was too high! oopsies!!!");
        //                //    damageInfo.damage = maxDamage; // i don't know if this is a needed check, but i *think* i was noticing insanely high damage numbers with adze on the end score screen. maybe this'll fix that? or maybe it was another mod entirely
        //                //}
        //                damageInfo.damage = Mathf.Min(damageInfo.damage, maxDamage);
        //            }
        //        }
        //    }
        //    
        //    orig(self, damageInfo);
        //}
    }

}
