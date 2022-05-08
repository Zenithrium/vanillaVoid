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
        public override string ItemName => "Empty Vials";

        public override string ItemLangTokenName => "EMPTY_VIALS";

        public override string ItemPickupDesc => "The experiment has completed...";

        public override string ItemFullDescription => $"The experiment has completed...";

        public override string ItemLore => $"Hi! Hope you're enjoying the mod, or whatever you're doing in order to have seen this. Have a nice day!";

        public override ItemTier Tier => ItemTier.NoTier;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlEmptyVialsPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("emptyVialsIcon.png");

        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.AIBlacklist };

        public override void Init(ConfigFile config)
        {
            //CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;

            //Hooks();


            //string orbTransp = "RoR2/Base/Croco/matBlighted.mat";
            //
            //var OrbsModelTransp = ItemModel.transform.Find("purpleguard").GetComponent<MeshRenderer>();
            //OrbsModelTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            //
            //var OrbsDisplayTransp = ItemBodyModelPrefab.transform.Find("purpleguard").GetComponent<MeshRenderer>();
            //OrbsDisplayTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlVialsDisplay.prefab");
            string glass = "RoR2/DLC1/HealingPotion/matHealingPotionGlass.mat";
 
            var vialGlass = ItemModel.transform.Find("_Vials").GetComponent<MeshRenderer>();
            vialGlass.material = Addressables.LoadAssetAsync<Material>(glass).WaitForCompletion();

            var vialGlassDisplay = ItemBodyModelPrefab.transform.Find("_Vials").GetComponent<MeshRenderer>();
            vialGlassDisplay.material = Addressables.LoadAssetAsync<Material>(glass).WaitForCompletion();

            //var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            //itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlEmptyVialsDisplay.prefab");

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
