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
    public class AbyssalAdze : ItemBase<AbyssalAdze>
    {
        public ConfigEntry<float> baseDamageBuff;

        public ConfigEntry<float> stackingBuff;

        public ConfigEntry<string> voidPair;

        public override string ItemName => "Abyss-Touched Adze";

        public override string ItemLangTokenName => "ADZE_ITEM";

        public override string ItemPickupDesc => "Deal more damage to enemies with lower health. <style=cIsVoid>Corrupts all Crowbars</style>.";

        public override string ItemFullDescription => $"Deal up to <style=cIsDamage>+{baseDamageBuff.Value * 100}%</style> <style=cStack>(+{stackingBuff.Value * 100}% per stack)</style> damage to enemies with lower health. <style=cIsVoid>Corrupts all Crowbars</style>.";

        public override string ItemLore => $"<style=cMono>//-- AUTO-TRANSCRIPTION FROM CARGO BAY 6 OF UES [Redacted] --//</style>" + 
            "\n\n\"So you're saying you destroyed-\" \n\n\"Traded!\"" +
            "\n\n\"...traded, our only crowbar. For that.\" \n\n\"Don't be so sour, come on! It's much better than a crowbar.\"" +
            "\n\n\"I don't even know what it is.\" \n\n\"It's an adze. It's like an...old time-y crowbar. More or less.\"" +
            "\n\n\"Ohh, so you decided our modern tools were too sensical, too useful for you?\" \n\n\"Oh quit the whining. This thing's a relic, it'd be worth way more than a crowbar. And it's probably way more useful, too. Just give it some time.\"" +
            "\n\n\"It'd better be.\"";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlAdzePickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("adzeIcon512.png");


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
            baseDamageBuff = config.Bind<float>("Item: " + ItemName, "Percent Damage Increase", .4f, "Adjust the percent of extra damage dealt on the first stack.");
            stackingBuff = config.Bind<float>("Item: " + ItemName, "Percent Damage Increase per Stack", .4f, "Adjust the percent of extra damage dealt per stack.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "Crowbar", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlAdzeDisplay.prefab");
            //string orbTransp = "RoR2/DLC1/voidraid/matVoidRaidPlanetPurpleWave.mat"; 
            //string orbCore = "RoR2/DLC1/voidstage/matVoidCoralPlatformPurple.mat";

            string orbTransp = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorLightning.mat";
            string orbCore = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorPod.mat";

            var adzeOrbsModelTransp = ItemModel.transform.Find("orbTransp").GetComponent<MeshRenderer>();
            var adzeOrbsModelCore = ItemModel.transform.Find("orbCore").GetComponent<MeshRenderer>();
            adzeOrbsModelTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            adzeOrbsModelCore.material = Addressables.LoadAssetAsync<Material>(orbCore).WaitForCompletion();

            var adzeOrbsDisplayTransp = ItemBodyModelPrefab.transform.Find("orbTransp").GetComponent<MeshRenderer>();
            var adzeOrbsDisplayCore = ItemBodyModelPrefab.transform.Find("orbCore").GetComponent<MeshRenderer>();
            adzeOrbsDisplayTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            adzeOrbsDisplayCore.material = Addressables.LoadAssetAsync<Material>(orbCore).WaitForCompletion();

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.02629241f, 0.2568354f, -0.2131178f),
                    localAngles = new Vector3(351.7242f, 10.67858f, 20.43508f),
                    localScale = new Vector3(0.08f, 0.08f, 0.08f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1503672f, 0.1435245f, -0.07638646f),
                    localAngles = new Vector3(345.9114f, 300.3137f, 23.08318f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.07648633f, 0.07626516f, -0.171931f),
                    localAngles = new Vector3(4.41012f, 156.408f, 333.5214f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.7626196f, 0.8972478f, -2.416836f),
                    localAngles = new Vector3(352.209f, 276.9412f, 21.69027f),
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
                    localPos = new Vector3(0.1661014f, 0.2427287f, -0.2980944f),
                    localAngles = new Vector3(353.9857f, 276.0242f, 30.12733f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.571964f, 0.2234386f, -0.2234011f),
                    localAngles = new Vector3(351.7031f, 89.96729f, 109.932f),
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
                    localPos = new Vector3(0.1125494f, 0.1737099f, -0.3271036f),
                    localAngles = new Vector3(5.788457f, 7.310323f, 19.54668f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
                
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1414333f, 0.1708212f, -0.205414f),
                    localAngles = new Vector3(352.4888f, 291.1599f, 19.03975f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfBackL",
                    localPos = new Vector3(0.08891746f, 0.5175744f, -0.03669554f),
                    localAngles = new Vector3(352.6626f, 273.883f, 23.80008f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1394217f, 0.1633563f, -0.3964019f),
                    localAngles = new Vector3(357.2906f, 279.8901f, 17.20597f),
                    localScale = new Vector3(0.09f, 0.09f, 0.09f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-1.443816f, -0.6864427f, 3.308026f),
                    localAngles = new Vector3(26.11133f, 5.543665f, 25.21973f),
                    localScale = new Vector3(.8f, .8f, .8f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1425444f, 0.1892054f, -0.2536568f),
                    localAngles = new Vector3(349.48f, 296.4531f, 17.46299f),
                    localScale = new Vector3(.115f, .115f, .115f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(0.2669638f, -0.08863433f, -0.07332691f),
                    localAngles = new Vector3(355.9068f, 102.4288f, 11.93598f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfR",
                    localPos = new Vector3(0.02665846f, 0.2549812f, -0.07270494f),
                    localAngles = new Vector3(11.88894f, 359.9499f, 204.7378f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(1.91149f, 11.57303f, 4.621446f),
                    localAngles = new Vector3(353.9657f, 129.2633f, 20.15013f),
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
                    localPos =   new Vector3(0.1429525f, 0.009444445f, -0.231735f),
                    localAngles = new Vector3(0.949871f, 227.3962f, 30.76947f),
                    localScale = new Vector3(0.085f, 0.085f, 0.085f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.001040328f, 0.0004106277f, 0.001044341f),
                    localAngles = new Vector3(350.0445f, 351.373f, 112.076f),
                    localScale = new Vector3(0.003f, 0.004f, 0.0035f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] //these ones don't work for some reason!
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.2842848f, -0.1576135f, 0.01475417f),
                    localAngles = new Vector3(4.996761f, 302.908f, 315.8754f),
                    localScale = new Vector3(0.1f, 0.1f, 0.1f)
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
                    childName = "PickL",
                    localPos = new Vector3(-0.003641347f, 0.001164402f, 0.000302475f),
                    localAngles = new Vector3(352.0699f, 17.21215f, 12.00122f),
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
            //        localPos = new Vector3(0F, 0.00347F, -0.00126F),
            //        localAngles = new Vector3(0F, 90F, 0F),
            //        localScale = new Vector3(0.01241F, 0.01241F, 0.01241F)
            //    }
            //});
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
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
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperTorso",
                    localPos = new Vector3(-0.0004795195f, 0.03674114f, -0.1753576f),
                    localAngles = new Vector3(4.924208f, 197.0649f, 346.5102f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }
            });

            return rules;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += AdzeDamageBonus;
        }

        private void AdzeDamageBonus(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo) {
            CharacterBody victimBody = self.body;
            float initialDmg = damageInfo.damage;
            if (damageInfo.attacker && damageInfo.attacker.GetComponent<CharacterBody>())
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody.inventory)
                {
                    var stackCount = GetCount(attackerBody);

                    if (stackCount > 0)
                    {
                        //var healthPercentage = self.health / self.fullCombinedHealth;
                        var healthFraction = Mathf.Clamp((1 - self.combinedHealthFraction), 0f, 1f);
                        //Debug.Log("health fraction: " + healthFraction);
                        var mult = healthFraction * (baseDamageBuff.Value + (stackingBuff.Value * (stackCount - 1)));
                        
                        damageInfo.damage = damageInfo.damage + (damageInfo.damage * mult);
                        float maxDamage = initialDmg + (initialDmg * (baseDamageBuff.Value + (stackingBuff.Value * (stackCount - 1))));
                        //Debug.Log("max damage: " + maxDamage + " | actual damage: " + damageInfo.damage + " | original damage: " + initialDmg);
                        //damageInfo.damage = damageInfo.damage * (1 + (victimBody.GetBuffCount(adzeDebuff) * dmgPerDebuff.Value));
                        //if(damageInfo.damage > maxDamage)
                        //{
                        //    //Debug.Log("damage was too high! oopsies!!!");
                        //    damageInfo.damage = maxDamage; // i don't know if this is a needed check, but i *think* i was noticing insanely high damage numbers with adze on the end score screen. maybe this'll fix that? or maybe it was another mod entirely
                        //}
                        damageInfo.damage = Mathf.Min(damageInfo.damage, maxDamage);
                    }
                }
            }
            
            orig(self, damageInfo);
        }
    }

}
