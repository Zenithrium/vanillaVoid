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
    public class BarrierLotus : ItemBase<BarrierLotus>
    {
        public ConfigEntry<float> barrierAmount;

        public ConfigEntry<float> pulseCountStacking;

        public ConfigEntry<string> voidPair;

        public override string ItemName => "Crystalline Lotus";

        public override string ItemLangTokenName => "BARRIERLOTUS_ITEM";

        public override string ItemPickupDesc => "Periodically release a barrier nova during the Teleporter event and 'Holdout Zones' such as the Void Fields. <style=cIsVoid>Corrupts all Lepton Daisies</style>.";

        public override string ItemFullDescription => $"Release a <style=cIsHealing>barrier nova</style> during the Teleporter Event, <style=cIsHealing>providing a barrier</style> to all nearby allies for <style=cIsHealing>{barrierAmount.Value * 100}%</style> of their max health. Occurs <style=cIsHealing>{pulseCountStacking.Value}</style> <style=cStack>(+{pulseCountStacking.Value} per stack)</style> times. <style=cIsVoid>Corrupts all Lepton Daisies</style>.";

        public override string ItemLore => $"\"I've located an...interesting specimen. You know those inane myths and theories people have about healing crystals, magical herbs, all that nonsense? You'll never believe me, but uh...I found something that roughly matches those descriptions. There's no doubt it's a coincidence... but it makes me wonder. What if some of these objects...these.. discoveries... aren't so new?\"\n\n- Lost Journal, Recovered from Petrichor V";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlFinalLotusPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("crystalLotusIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[5] { ItemTag.Utility, ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.CannotCopy, ItemTag.HoldoutZoneRelated };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);

            //lotusObject = MainAssets.LoadAsset<GameObject>("mdlLotusWorldObject.prefab"); //lmao it makes the pickup spin WILDLY if you use mdlBladePickup
            //lotusObject.AddComponent<TeamFilter>();
            //lotusObject.AddComponent<NetworkIdentity>();

            //lotusObject.AddComponent<HealthComponent>();
            //lotusObject.AddComponent<BoxCollider>();
            //lotusObject.AddComponent<Rigidbody>();

            Hooks(); 
        }

        public override void CreateConfig(ConfigFile config)
        {
            barrierAmount = config.Bind<float>("Item: " + ItemName, "Percent Barrier", .3f, "Adjust percent of health that the barrier pulse provides.");
            pulseCountStacking = config.Bind<float>("Item: " + ItemName, "Activations per Stack", 1f, "Adjust the percent of extra damage dealt per stack.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "TPHealingNova", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlFinalLotusDisplay.prefab");

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1726241f, 0.2653433f, 0.212837f),
                    localAngles = new Vector3(6.086947f, 8.317881f, 2.610492f),
                    localScale = new Vector3(.2f, .2f, .2f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.05854443f, 0.1524417f, 0.1972835f),
                    localAngles = new Vector3(8.609409f, 349.0558f, 354.7103f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hat",
                    localPos = new Vector3(0.1022652f, -0.009553856f, -0.09913615f),
                    localAngles = new Vector3(320.1294f, 104f, 317.2372f),
                    localScale = new Vector3(.125f, .125f, .125f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(1.881902f, 1.444218f, 3.373417f),
                    localAngles = new Vector3(26.07172f, 353.373f, 5.523437f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.2419884f, 0.1126657f, 0.1895919f),
                    localAngles = new Vector3(44.81784f, 26.5198f, 12.03686f),
                    localScale = new Vector3(.2f, .2f, .2f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.003674691f, -0.6024528f, 0.2619569f),
                    localAngles = new Vector3(29.69731f, 353.7802f, 358.004f),
                    localScale = new Vector3(.5f, .5f, .5f)

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
                    localPos = new Vector3(0.07465521f, 0.1513727f, 0.1474627f),
                    localAngles = new Vector3(1.975409f, 0.1417529f, 12.61099f),
                    localScale = new Vector3(.1f, .1f, .1f)
                }

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1092708f, 0.1483147f, 0.1939543f),
                    localAngles = new Vector3(9.869252f, 7.184193f, 356.6933f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "PlatformBase",
                    localPos = new Vector3(0.6196117f, -0.1732304f, 0.1983203f),
                    localAngles = new Vector3(29.56468f, 112.3599f, 83.46249f),
                    localScale = new Vector3(.4f, .4f, .4f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.09584545f, 0.1744341f, 0.204406f),
                    localAngles = new Vector3(9.84764f, 345.7096f, 357.1768f),
                    localScale = new Vector3(.165f, .165f, .16f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.556223f, -1.21355f, -1.983067f),
                    localAngles = new Vector3(7.949906f, 192.6268f, 190.9715f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.09959833f, 0.1406649f, 0.2082557f),
                    localAngles = new Vector3(31.40045f, 1.59294f, 1.171426f),
                    localScale = new Vector3(.2f, .2f, .2f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(-0.02802477f, 0.3978654f, -0.117903f),
                    localAngles = new Vector3(296.3392f, 200.9765f, 160.3061f),
                    localScale = new Vector3(.125f, .125f, .125f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmL",
                    localPos = new Vector3(0.1217875f, -0.005072067f, 0.01755153f),
                    localAngles = new Vector3(51.58237f, 182.4142f, 194.6872f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-2.347142f, 0.02130795f, -9.950616f),
                    localAngles = new Vector3(300.3647f, 214.8425f, 165.5008f),
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
                    childName =  "Chest",
                    localPos = new Vector3(-0.2344948f, 0.3370056f, 0.006313583f),
                    localAngles = new Vector3(12.62754f, 266.1613f, 9.178808f),
                    localScale = new Vector3(.125f, .125f, .115f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.006118589f, 0.002633904f, -0.003834995f),
                    localAngles = new Vector3(29.91726f, 229.2298f, 342.4258f),
                    localScale = new Vector3(.005f, .005f, .005f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] 
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.02318558f, 0.2201249f, -0.1634783f),
                    localAngles = new Vector3(339.486f, 149.8592f, 275.7268f),
                    localScale = new Vector3(.2f, .2f, .2f)
                }
            });
            //rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Door",
            //        localPos = new Vector3(1f, 1f, 1f),
            //        localAngles = new Vector3(1f, 1f, 1f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ShoulderL",
                    localPos = new Vector3(0.001272175f, 0f, 0.0004509923f),
                    localAngles = new Vector3(7.583423f, 74.48707f, 298.3278f),
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
            //        localPos = new Vector3(1f, 1f, 1f),
            //        localAngles = new Vector3(1f, 1f, 1f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmR",
                    localPos = new Vector3(-0.1377259f, 0.1770405f, -0.140108f),
                    localAngles = new Vector3(328.466f, 254.5523f, 133.0548f),
                    localScale = new Vector3(.15f, .15f, .15f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.07692271f, 0.2731504f, 0.0435502f),
                    localAngles = new Vector3(355.1563f, 290.847f, 8.419727f),
                    localScale = new Vector3(.125f, .125f, .125f)
                }
            });

            return rules;
        }

        public override void Hooks()
        {
            
            //On.RoR2.HealthComponent.TakeDamage += AdzeDamageBonus;
            //On.RoR2.HoldoutZoneController.UpdateHealingNovas += BarrierLotusNova;
        }
        //int chargesRemaining;
        //private void BarrierLotusNova(On.RoR2.HoldoutZoneController.orig_UpdateHealingNovas orig, HoldoutZoneController self, bool isCharging)
        //{
        //    if (isCharging)
        //    {
        //        
        //    }
        //    //Debug.Log("Hello, I am the HoldoutZoneController UpdatHealingNovas BarrierLotusNova Hook");
        //    orig(self, isCharging);
        //}

        //private void AdzeDamageBonus(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo) {
        //    CharacterBody victimBody = self.body;
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
        //                var healthPercentage = self.health / self.fullCombinedHealth;
        //                var mult = (1 - self.combinedHealthFraction) * (baseDamageBuff.Value + (stackingBuff.Value * (stackCount - 1)));
        //
        //                damageInfo.damage = damageInfo.damage + (damageInfo.damage * mult);
        //                float maxDamage = initialDmg * (mult * stackCount);
        //                //damageInfo.damage = damageInfo.damage * (1 + (victimBody.GetBuffCount(adzeDebuff) * dmgPerDebuff.Value));
        //                if(damageInfo.damage > maxDamage)
        //                {
        //                    //Debug.Log("damage was too high! oopsies!!!");
        //                    damageInfo.damage = maxDamage; // i don't know if this is a needed check, but i *think* i was noticing insanely high damage numbers with adze on the end score screen. maybe this'll fix that? or maybe it was another mod entirely
        //                }
        //            }
        //        }
        //    }
        //    
        //    orig(self, damageInfo);
        //}
    }
}
