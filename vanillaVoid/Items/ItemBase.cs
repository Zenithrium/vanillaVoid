using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace vanillaVoid.Items
{
    // The directly below is entirely from TILER2 API (by ThinkInvis) specifically the Item module. Utilized to implement instancing for classes.
    // TILER2 API can be found at the following places:
    // https://github.com/ThinkInvis/RoR2-TILER2
    // https://thunderstore.io/package/ThinkInvis/TILER2/

    public abstract class ItemBase<T> : ItemBase where T : ItemBase<T>
    {
        //This, which you will see on all the -base classes, will allow both you and other modders to enter through any class with this to access internal fields/properties/etc as if they were a member inheriting this -Base too from this class.
        public static T instance { get; private set; }

        public ItemBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBase was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class ItemBase
    {
        public abstract string ItemName { get; }
        public abstract string ItemLangTokenName { get; }
        public abstract string ItemPickupDesc { get; }
        public abstract string ItemFullDescription { get; }
        public abstract string ItemLore { get; }

        public static Dictionary<string, string> TokenToVoidPair = new Dictionary<string, string>();
        public ConfigEntry<string> voidPair = null;

        public abstract ItemTier Tier { get; }
        public virtual ItemTag[] ItemTags { get; set; } = new ItemTag[] { };

        public abstract GameObject ItemModel { get; }
        public abstract Sprite ItemIcon { get; }

        public ItemDef ItemDef;
        //public abstract string VoidVariant { get; }
        public virtual bool CanRemove { get; } = true;

        public virtual bool AIBlacklisted { get; set; } = false;

        /// <summary>
        /// This method structures your code execution of this class. An example implementation inside of it would be:
        /// <para>CreateConfig(config);</para>
        /// <para>CreateLang();</para>
        /// <para>CreateItem();</para>
        /// <para>Hooks();</para>
        /// <para>This ensures that these execute in this order, one after another, and is useful for having things available to be used in later methods.</para>
        /// <para>P.S. CreateItemDisplayRules(); does not have to be called in this, as it already gets called in CreateItem();</para>
        /// </summary>
        /// <param name="config">The config file that will be passed into this from the main class.</param>
        public abstract void Init(ConfigFile config);

        public virtual void CreateConfig(ConfigFile config) { }

        protected virtual void CreateLang()
        {
            //string itemTempName = ItemName;
            //Debug.Log("current item name: " + itemTempName);
            //if (itemTempName.Contains('\''))
            //{
            //    itemTempName.Replace('\'', ' '); 
            //}
            //Debug.Log("done item name: " + itemTempName);

            LanguageAPI.Add("VV_ITEM_" + ItemLangTokenName + "_NAME", ItemName);
            LanguageAPI.Add("VV_ITEM_" + ItemLangTokenName + "_PICKUP", ItemPickupDesc);
            LanguageAPI.Add("VV_ITEM_" + ItemLangTokenName + "_DESCRIPTION", ItemFullDescription);
            if (voidPair != null)
            {
                TokenToVoidPair.Add("VV_ITEM_" + ItemLangTokenName + "_PICKUP", voidPair.Value);
                TokenToVoidPair.Add("VV_ITEM_" + ItemLangTokenName + "_DESCRIPTION", voidPair.Value);
            }
            LanguageAPI.Add("VV_ITEM_" + ItemLangTokenName + "_LORE", ItemLore);

            //LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_NAME", ItemName);
            //LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_PICKUP", ItemPickupDesc);
            //LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_DESCRIPTION", ItemFullDescription);
            //LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_LORE", ItemLore);
        }

        public virtual string VoidPair()
        {
            if (voidPair != null)
            {
                return voidPair.Value;
            }
            else
            {
                return null;
            }
        }

        //public void AddVoidPair(List<ItemDef.Pair> newVoidPairs)
        //{
        //    var voidParent = VoidParent();
        //    if (voidParent == null)
        //    {
        //        return;
        //    }
        //    //var voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].Where(x => x.itemDef1 != VoidParent.ItemDef); -- Use to overwrite other mods
        //    ItemDef.Pair newVoidPair = new ItemDef.Pair()
        //    {
        //        itemDef1 = voidParent.ItemDef,
        //        itemDef2 = ItemDef
        //    };
        //    newVoidPairs.Add(newVoidPair);
        //}

        public void AddVoidPair(List<ItemDef.Pair> newVoidPairs)
        {
            
            string pair = VoidPair();
            //Debug.Log("hello chat " + pair);
            if(pair != null)
            {
                var pairDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(pair)); //lol
                //Debug.Log("chat " + pairDef);
                if (pairDef != null)
                {
                    //Debug.Log("goodbye chat " + pairDef);
                    //var voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].Where(x => x.itemDef1 != VoidParent.ItemDef); -- Use to overwrite other mods
                    ItemDef.Pair newVoidPair = new ItemDef.Pair()
                    {
                        itemDef1 = pairDef,
                        itemDef2 = ItemDef
                    };
                    newVoidPairs.Add(newVoidPair);
                    //Debug.Log("Added new pair of " + pairDef.name + " and " + ItemDef.name);
                }
            }
        }

        public abstract ItemDisplayRuleDict CreateItemDisplayRules();

        protected void CreateItem()
        {
            
            if (AIBlacklisted)
            {
                ItemTags = new List<ItemTag>(ItemTags) { ItemTag.AIBlacklist }.ToArray();
            }

            ItemDef = ScriptableObject.CreateInstance<ItemDef>();
            ItemDef.name = "VV_ITEM_" + ItemLangTokenName;
            ItemDef.nameToken = "VV_ITEM_" + ItemLangTokenName + "_NAME";
            ItemDef.pickupToken = "VV_ITEM_" + ItemLangTokenName + "_PICKUP";
            ItemDef.descriptionToken = "VV_ITEM_" + ItemLangTokenName + "_DESCRIPTION";
            ItemDef.loreToken = "VV_ITEM_" + ItemLangTokenName + "_LORE";

            //ItemDef.name = "ITEM_" + ItemLangTokenName;
            //ItemDef.nameToken = "ITEM_" + ItemLangTokenName + "_NAME";
            //ItemDef.pickupToken = "ITEM_" + ItemLangTokenName + "_PICKUP";
            //ItemDef.descriptionToken = "ITEM_" + ItemLangTokenName + "_DESCRIPTION";
            //ItemDef.loreToken = "ITEM_" + ItemLangTokenName + "_LORE";

            ItemDef.pickupModelPrefab = ItemModel;
            ItemDef.pickupIconSprite = ItemIcon;
            ItemDef.hidden = false;
            ItemDef.canRemove = CanRemove;
            ////The tier determines what rarity the item is:
            ////Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            ////and finally NoTier is generally used for helper items, like the tonic affliction
            //#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            //myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
            //#pragma warning restore Publicizer001
            //// Instead of loading the itemtierdef directly, you can also do this like below as a workaround
            ////myItemDef.deprecatedTier = ItemTier.Tier2;
            ItemDef.deprecatedTier = Tier;

            if (ItemTags.Length > 0) { ItemDef.tags = ItemTags; }

            ItemAPI.Add(new CustomItem(ItemDef, CreateItemDisplayRules()));
        }

        public virtual void Hooks() { }

        //Based on ThinkInvis' methods
        public int GetCount(CharacterBody body)
        {
            if (!body || !body.inventory) { return 0; }

            return body.inventory.GetItemCount(ItemDef);
        }

        public int GetCount(CharacterMaster master)
        {
            if (!master || !master.inventory) { return 0; }

            return master.inventory.GetItemCount(ItemDef);
        }

        public int GetCountSpecific(CharacterBody body, ItemDef itemDef)
        {
            if (!body || !body.inventory) { return 0; }

            return body.inventory.GetItemCount(itemDef);
        }

        
    }
}
