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
    public class ConsumedClockworkMechanism : ItemBase<ConsumedClockworkMechanism>
    {
        public override string ItemName => "Broken Mess";

        public override string ItemLangTokenName => "BROKEN_MESS";

        public override string ItemPickupDesc => tempItemPickupDesc;

        public override string ItemFullDescription => tempItemFullDescription;

        public override string ItemLore => $"Make it stop!\n\nIt's out of my hands, I'm only a clock \nDon't worry, I'm sure you'll be fine \nBut eventually everyone runs out of time";

        public override ItemTier Tier => ItemTier.NoTier;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlMessPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("brokenMessIcon512.png");

        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.AIBlacklist };

        string tempItemPickupDesc;
        string tempItemFullDescription;
        public override void Init(ConfigFile config)
        {
            //CreateConfig(config);
            if(ClockworkMechanism.instance.itemVariant.Value == 0 || ClockworkMechanism.instance.itemVariant.Value == 1)
            {
                tempItemPickupDesc = "Its time has run out...";
                tempItemFullDescription = "Its time has run out...";
            }
            else
            {
                tempItemPickupDesc = "May your greed know no bounds.";
                tempItemFullDescription = "May your greed know no bounds.";
            }
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;

            //Hooks();


            string orbTransp = "RoR2/Base/Croco/matBlighted.mat";
            
            var OrbsModelTransp = ItemModel.transform.Find("purpleguard").GetComponent<MeshRenderer>();
            OrbsModelTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            
            var OrbsDisplayTransp = ItemBodyModelPrefab.transform.Find("purpleguard").GetComponent<MeshRenderer>();
            OrbsDisplayTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlMessDisplay.prefab");
            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[] //i'm too lazy to ask how to have it return null, so i'm doing this. the scavenger sHOULD never get this anyway. if it does it's in a funny spot
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(-0.7875283f, 9.592388f, -1.154644f),
                    localAngles = new Vector3(0.310706f, 300.0273f, 346.814f),
                    localScale = new Vector3(2f, 2f, 2f)
                }
            });
            return rules;

        }
    }
}
