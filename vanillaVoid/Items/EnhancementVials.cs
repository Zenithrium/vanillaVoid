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
    public class EnhancementVials : ItemBase<EnhancementVials>
    {
        //public ConfigEntry<bool> consumeStack;

        public ConfigEntry<string> voidPair;

        private Xoroshiro128Plus potionVoidRng;

        public override string ItemName => "Enhancement Vials";

        public override string ItemLangTokenName => "EHANCE_VIALS_ITEM";

        public override string ItemPickupDesc => "<style=cIsUtility>Upgrade</style> an item at low health. Consumed on use. <style=cIsVoid>Corrupts all Power Elixirs</style>.";

        public override string ItemFullDescription => $"Taking damage to below <style=cIsHealth>25% health</style> <style=cIsUtility>consumes</style> this item, <style=cIsUtility>upgrading</style> another item. <style=cIsVoid>Corrupts all Power Elixirs</style>.";

        public override string ItemLore => $"\"What an experiment this will be...our first forray into the void! Gather round, for this will forever change each and every one of our lives!\" \n\nA few days later, a janitor discovered a strange pile of objects scattered around various empty test tubes. They thought little of it.";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlInvertedVialsPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("vialsIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Utility, ItemTag.LowHealth };

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
            //consumeStack = config.Bind<bool>("Item: " + ItemName, "Consume Stack", false, "Adjust if each potion should upgrade a whole stack, like benthic, or only one.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "HealingPotion", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlInvertedVialsDisplay.prefab");
            string glass = "RoR2/DLC1/HealingPotion/matHealingPotionGlass.mat"; 
            //string orbCore = "RoR2/DLC1/voidstage/matVoidCoralPlatformPurple.mat";

            //string orbTransp = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorLightning.mat";
            //string orbCore = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorPod.mat";
            //
            var vialGlass = ItemModel.transform.Find("_Vials").GetComponent<MeshRenderer>();
            //var adzeOrbsModelCore = ItemModel.transform.Find("orbCore").GetComponent<MeshRenderer>();
            vialGlass.material = Addressables.LoadAssetAsync<Material>(glass).WaitForCompletion();
            //adzeOrbsModelCore.material = Addressables.LoadAssetAsync<Material>(orbCore).WaitForCompletion();
            //
            var vialGlassDisplay = ItemBodyModelPrefab.transform.Find("_Vials").GetComponent<MeshRenderer>();
            //var adzeOrbsDisplayCore = ItemBodyModelPrefab.transform.Find("orbCore").GetComponent<MeshRenderer>();
            vialGlassDisplay.material = Addressables.LoadAssetAsync<Material>(glass).WaitForCompletion();
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
                    localPos = new Vector3(-0.1982696f, -0.07318481f, 0.007869355f),
                    localAngles = new Vector3(2.331187f, 354.4543f, 191.166f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.1741157f, -0.04303819f, -0.03905206f),
                    localAngles = new Vector3(340.8726f, 87.29208f, 181.0539f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.04389894f, 0.1595491f, 0.08947299f),
                    localAngles = new Vector3(317.8894f, 17.79786f, 199.7844f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(1.887562f, 2.821436f, 2.098963f),
                    localAngles = new Vector3(0, 0, 0),
                    localScale = new Vector3(0.2f, 0.2f, 0.2f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CannonHeadL",
                    localPos = new Vector3(-0.0586996f, 0.3035001f, 0.2334503f),
                    localAngles = new Vector3(358.7363f, 89.44573f, 91.36291f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.4293001f, 1.404367f, -1.049775f),
                    localAngles = new Vector3(0F, 270F, 0f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)

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
                    localPos = new Vector3(0.1063199f, 0.1112932f, 0.07790551f),
                    localAngles = new Vector3(328.3492f, 328.3492f, 186.0959f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
                
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1090616f, -0.02028957f, 0.01990319f),
                    localAngles = new Vector3(359.3058f, 251.9959f, 219.8436f),
                    localScale = new Vector3(0.03f, 0.03f, 0.03f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "PlatformBase",
                    localPos = new Vector3(-0.1768964f, 0.8038302f, -0.5721697f),
                    localAngles = new Vector3(0, 315.5349f, 0),
                    localScale = new Vector3(.05f, .05f, .05f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MechBase",
                    localPos = new Vector3(-0.1305189f, 0.4092634f, 0.04044753f),
                    localAngles = new Vector3(19.87004f, 79.40923f, 356.8562f),
                    localScale = new Vector3(0.03f, 0.03f, 0.03f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Stomach",
                    localPos = new Vector3(2.011216f, -2.933047f, 1.118379f),
                    localAngles = new Vector3(10.3437f, 167.6057f, 167.6057f),
                    localScale = new Vector3(0.25f, 0.25f, 0.25f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Stomach",
                    localPos = new Vector3(0.145084f, 0.1121265f, 0.1518372f),
                    localAngles = new Vector3(354.9935f, 91.62019f, 358.043f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.09856766f, 0.1388181f, -0.1056417f),
                    localAngles = new Vector3(358.7816f, 93.55044f, 199.4389f),
                    localScale = new Vector3(0.025f, 0.025f, 0.025f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.05666163f, 0.009669475f, 0.1821246f),
                    localAngles = new Vector3(352.0249f, 137.3781f, 133.2941f),
                    localScale = new Vector3(0.035f, 0.035f, 0.035f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(3.808075f, 4.49922f, 3.978485f),
                    localAngles = new Vector3(354.9038f, 287.8108f, 319.2085f),
                    localScale = new Vector3(.6f, .6f, .6f)
                }
            });
            return rules;

        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.UpdateLastHitTime += BreakItem;
        }

        private void BreakItem(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, HealthComponent self, float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker)
        {
            orig.Invoke(self, damageValue, damagePosition, damageIsSilent, attacker);
            if (NetworkServer.active && (bool)attacker && (bool)self && (bool)self.body && ItemBase<EnhancementVials>.instance.GetCount(self.body) > 0 && self.isHealthLow)
            {
                if (potionVoidRng == null)
                {
                    potionVoidRng = new Xoroshiro128Plus(Run.instance.seed);
                }
                bool isDone = false;
                int potionCount = ItemBase<EnhancementVials>.instance.GetCount(self.body);
                int potionLeftCount = potionCount;
                int oldItemCount = 0;
                while (!isDone)
                {
                    List<ItemIndex> inventoryList = new List<ItemIndex>(self.body.inventory.itemAcquisitionOrder);
                    List<PickupIndex> greenList = new List<PickupIndex>(Run.instance.availableTier2DropList);
                    List<PickupIndex> redList = new List<PickupIndex>(Run.instance.availableTier3DropList);

                    ItemIndex itemIndex = ItemIndex.None;
                    ItemIndex itemResult = ItemIndex.None;
                    Util.ShuffleList(inventoryList, potionVoidRng);
                    foreach (ItemIndex item in inventoryList)
                    {

                        ItemDef itemDef = ItemCatalog.GetItemDef(item);
                        if ((bool)itemDef && itemDef.tier != ItemTier.NoTier)
                        {
                            if(itemDef.tier == ItemTier.Tier1)
                            {
                                itemIndex = item;
                                oldItemCount = self.body.inventory.GetItemCount(item);
                                
                                Util.ShuffleList(greenList, potionVoidRng);
                                itemResult = greenList[0].itemIndex;
                                break;
                            }else if(itemDef.tier == ItemTier.Tier2)
                            {
                                itemIndex = item;
                                oldItemCount = self.body.inventory.GetItemCount(item);
                                
                                Util.ShuffleList(redList, potionVoidRng);
                                itemResult = redList[0].itemIndex;
                                break;
                            }
                            //itemIndex = item;
                            //oldItemCount = self.body.inventory.GetItemCount(item);
                            //break;
                        }

                    }
                    if (itemIndex != ItemIndex.None)
                    {
                        if(oldItemCount > potionLeftCount)
                        {
                            oldItemCount = potionLeftCount;
                        }

                        self.body.inventory.RemoveItem(itemIndex, oldItemCount);
                        self.body.inventory.GiveItem(itemResult, oldItemCount);
                        CharacterMasterNotificationQueue.PushItemTransformNotification(self.body.master, itemIndex, itemResult, CharacterMasterNotificationQueue.TransformationType.CloverVoid);
                        potionLeftCount -= oldItemCount;
                        if(potionLeftCount < 1)
                        {
                            isDone = true;
                        }
                    }
                    else
                    {
                        isDone = true;
                    }

                    //List<ItemIndex> itemList = new List<ItemIndex>(self.body.inventory.itemAcquisitionOrder);
                    //Util.ShuffleList(itemList, watchVoidRng);

                    //self.body.inventory.GiveItem(ItemBase<BrokenClockworkMechanism>.instance.ItemDef, 1);
                    //self.body.inventory.RemoveItem(ItemCatalog.GetItemDef(itemList[0]), 1);
                    //CharacterMasterNotificationQueue.PushItemTransformNotification(self.body.master, ItemCatalog.GetItemDef(itemList[0]).itemIndex, ItemBase<BrokenClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                }
                self.body.inventory.RemoveItem(ItemBase<EnhancementVials>.instance.ItemDef, potionCount);
                self.body.inventory.GiveItem(ItemBase<EmptyVials>.instance.ItemDef, potionCount);
                CharacterMasterNotificationQueue.PushItemTransformNotification(self.body.master, ItemBase<EnhancementVials>.instance.ItemDef.itemIndex, ItemBase<EmptyVials>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.CloverVoid);
                EffectData effectData = new EffectData
                {
                    origin = self.transform.position
                };
                effectData.SetNetworkedObjectReference(self.gameObject);
                EffectManager.SpawnEffect(HealthComponent.AssetReferences.shieldBreakEffectPrefab, effectData, transmit: true);
            }
            orig(self, damageValue, damagePosition, damageIsSilent, attacker);
        }
    }

}
