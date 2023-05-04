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
    public class EmptyVials : ItemBase<EmptyVials>
    {

        public ConfigEntry<int> refreshAmount;

        public override string ItemName => "Empty Vials";

        public override string ItemLangTokenName => "EMPTY_VIALS";

        public override string ItemPickupDesc => "The experiment has completed, for now...";

        public override string ItemFullDescription => $"The experiment has completed, for now...";

        public override string ItemLore => $"Hi! Hope you're enjoying the mod, or whatever you're doing in order to have seen this. Have a nice day!";

        public override ItemTier Tier => ItemTier.NoTier;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlInvertedEmptyPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("emptyVialsIcon512.png");

        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.AIBlacklist };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;

            Hooks();


            //string orbTransp = "RoR2/Base/Croco/matBlighted.mat";
            //
            //var OrbsModelTransp = ItemModel.transform.Find("purpleguard").GetComponent<MeshRenderer>();
            //OrbsModelTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            //
            //var OrbsDisplayTransp = ItemBodyModelPrefab.transform.Find("purpleguard").GetComponent<MeshRenderer>();
            //OrbsDisplayTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
        }
        public override void Hooks()
        {
            RoR2.SceneDirector.onPrePopulateSceneServer += RefreshVials;
        }

        public override void CreateConfig(ConfigFile config)
        {
            //consumeStack = config.Bind<bool>("Item: " + ItemName, "Consume Stack", false, "Adjust if each potion should upgrade a whole stack, like benthic, or only one.");
            refreshAmount = config.Bind<int>("Item: " + ItemName, "Refresh Amount", 1, "Adjust how many empty vials refresh at the start of a new stage. A negative number will refresh all stacks.");
        }

        private void RefreshVials(SceneDirector obj)
        {
            int refreshAmnt = refreshAmount.Value;
            if (refreshAmnt != 0)
            {
                //Debug.Log("function starting, interactable credits: " + obj.interactableCredit);
                //int itemCount = 0;
                foreach (var player in PlayerCharacterMasterController.instances)
                {
                    int itemCount = 0;
                    itemCount += player.master.inventory.GetItemCount(ItemBase<EmptyVials>.instance.ItemDef);
                    if(itemCount > 0 && refreshAmnt < 0)
                    {
                        player.master.inventory.GiveItem(ItemBase<EnhancementVials>.instance.ItemDef, itemCount);
                        player.master.inventory.RemoveItem(ItemBase<EmptyVials>.instance.ItemDef, itemCount);
                        CharacterMasterNotificationQueue.PushItemTransformNotification(player.master, ItemBase<EmptyVials>.instance.ItemDef.itemIndex, ItemBase<EnhancementVials>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);

                    }
                    else if (itemCount > 0 && itemCount > refreshAmnt)
                    {
                        player.master.inventory.GiveItem(ItemBase<EnhancementVials>.instance.ItemDef, refreshAmnt);
                        player.master.inventory.RemoveItem(ItemBase<EmptyVials>.instance.ItemDef, refreshAmnt);
                        CharacterMasterNotificationQueue.PushItemTransformNotification(player.master, ItemBase<EmptyVials>.instance.ItemDef.itemIndex, ItemBase<EnhancementVials>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);

                    }
                    else if(itemCount > 0 && itemCount <= refreshAmnt)
                    {
                        player.master.inventory.GiveItem(ItemBase<EnhancementVials>.instance.ItemDef, itemCount);
                        player.master.inventory.RemoveItem(ItemBase<EmptyVials>.instance.ItemDef, itemCount);
                        CharacterMasterNotificationQueue.PushItemTransformNotification(player.master, ItemBase<EmptyVials>.instance.ItemDef.itemIndex, ItemBase<EnhancementVials>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);
                    }
                }  
            }
            //Debug.Log("function ending, interactable credits after: " + obj.interactableCredit);
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlInvertedEmptyDisplay.prefab");
            string glass = "RoR2/DLC1/HealingPotion/matHealingPotionGlass.mat";
 
            var vialGlass = ItemModel.transform.Find("_Vials").GetComponent<MeshRenderer>();
            vialGlass.material = Addressables.LoadAssetAsync<Material>(glass).WaitForCompletion();

            var vialGlassDisplay = ItemBodyModelPrefab.transform.Find("_Vials").GetComponent<MeshRenderer>();
            vialGlassDisplay.material = Addressables.LoadAssetAsync<Material>(glass).WaitForCompletion();

            //var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            //itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            //ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlInvertedEmptyDisplay.prefab");

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[] //i'm too lazy to ask how to have it return null, so i'm doing this. the scavenger sHOULD never get this anyway.
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(0.1523769f, 10.91676f, -0.1861038f),
                    localAngles = new Vector3(0.310706f, 300.0273f, 346.814f),
                    localScale = new Vector3(.001f, .001f, .001f)
                }
            });
            return rules;

        }



    }
}
