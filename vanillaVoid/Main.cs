using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Collections;
//using vanillaVoid.Artifact;
using vanillaVoid.Equipment;
//using vanillaVoid.Equipment.EliteEquipment;
using vanillaVoid.Items;
using RoR2;
using HarmonyLib;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using RoR2.Projectile;
using vanillaVoid.Interactables;
using vanillaVoid.Misc;
using vanillaVoid.Utils;
using MonoMod.Cil;
using RoR2.EntitlementManagement;
using On.RoR2.Items;
using RoR2.ContentManagement;
using AK.Wwise;
//using static vanillaVoid.Utils.Components.MaterialControllerComponents;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace vanillaVoid {
    [BepInPlugin(ModGuid, ModName, ModVer)]

    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.content_management", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.items", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.language", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.prefab", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.recalculatestats", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.director", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.orb", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.networking", BepInDependency.DependencyFlags.HardDependency)]

    //[BepInDependency("com.RumblingJOSEPH.VoidItemAPI", BepInDependency.DependencyFlags.HardDependency)]
    //[BepInDependency(VoidItemAPI.VoidItemAPI.MODGUID)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    //[BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    //[R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(RecalculateStatsAPI), nameof(PrefabAPI), nameof(LegacyResourcesAPI))]

    //[BepInDependency(VoidItemAPI.VoidItemAPI.MODGUID)]

    public class vanillaVoidPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.Zenithrium.vanillaVoid";
        public const string ModName = "vanillaVoid";
        public const string ModVer = "1.6.0";

        public static ExpansionDef sotvDLC;
        public static ExpansionDef sotvDLC2;
        public static AssetBundle MainAssets;

        //public List<ArtifactBase> Artifacts = new List<ArtifactBase>();
        public List<ItemBase> Items = new List<ItemBase>();
        public List<EquipmentBase> Equipments = new List<EquipmentBase>();
        public List<InteractableBase> Interactables = new List<InteractableBase>();
        //public List<EliteEquipmentBase> EliteEquipments = new List<EliteEquipmentBase>();

        //Provides a direct access to this plugin's logger for use in any of your other classes.
        public static BepInEx.Logging.ManualLogSource ModLogger;

        public static GameObject platformObject;
        public static GameObject portalObject;

        public static GameObject lotusObject;
        public static GameObject lotusPulse;
        public static GameObject lotusCollider;

        public static GameObject exhaustVFX;

        public static List<ItemDef.Pair> corruptibleItems = new List<ItemDef.Pair>();

        public Xoroshiro128Plus genericRng;

        //public static ConfigEntry<bool> orreryCompat;
        public static ConfigEntry<bool> locusEarlyExit;
        public static ConfigEntry<bool> locusExit;
        public static ConfigEntry<int> LocusBonus;

        public static ConfigEntry<bool> lockVoidsBehindPair;
        public static ConfigEntry<bool> doVoidPickupBorders;
        public static ConfigEntry<bool> doVoidCommandVFX;
        public static ConfigEntry<bool> doSaleCradle;

        GameObject tier1Clone;
        GameObject tier2Clone;
        GameObject tier3Clone;
        GameObject tier4Clone;
        bool hasAdjustedTiers;
        bool hasAddedCommand;


        private void Awake(){
            //orreryCompat = Config.Bind<bool>("Mod Compatability", "Enable Lost Seers Buff", true, "Should generally stay on, but if you're having a strange issue (ex. health bars not showing up on enemies) edit this to be false.");
            locusExit = Config.Bind<bool>("Tweaks: Void Locus", "Exit Portal", true, "If enabled, spawns a portal in the void locus letting you return to normal stages if you want to.");
            locusEarlyExit = Config.Bind<bool>("Tweaks: Void Locus", "Early Exit Portal", false, "If enabled, spawns the exit portal in void locus immediately upon entering the stage. Requires the exit portal to actually be enabled.");
            LocusBonus = Config.Bind<int>("Tweaks: Void Locus", "Locus Bonus Credits", 0, "If you want to make going to the void locus have a little more of a reward, increase this number. Should be increased in at least multiples of 50ish");

            lockVoidsBehindPair = Config.Bind<bool>("Tweaks: Void Items", "Require Original Item Unlocked", true, "If enabled, makes it so void items are locked until the non-void pair is unlocked. Ex. Pluripotent is locked until the profile has unlocked Dios. Only applies to void items which do not already have unlocks, in the event a mod adds special unlocks for a void item.");
            doVoidPickupBorders = Config.Bind<bool>("Tweaks: Void Items", "Improved Pickup Highlights", true, "If enabled, picking up a void item will show tier-appropriate item highlights rather the the default white highlights.");
            doVoidCommandVFX = Config.Bind<bool>("Tweaks: Void Items", "Improved Command VFX", true, "If enabled, void command cubes will have appropriate void vfx in the style of typical command VFX based on the actual void item VFX.");
            doSaleCradle = Config.Bind<bool>("Tweaks: Void Cradles", "Sale Star Functionality", true, "If enabled, Sale Star will work on Void Cradles.");

            ModLogger = Logger;

            var harm = new Harmony(Info.Metadata.GUID);
            new PatchClassProcessor(harm, typeof(ModdedDamageColors)).Patch();

            //sotvDLC = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.nameToken == "DLC1_NAME");  //learn what sotv is 

            //sotvDLC2 = LegacyResourcesAPI.Load<ExpansionDef>("ExpansionDefs/DLC1");
            sotvDLC = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
            //expansionDef.enabledChoice
            //EntitlementDef dlc1Entitlemnt = LegacyResourcesAPI.Load<EntitlementDef>("EntitlementDefs/entitlementDLC1");

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("vanillaVoid.vanillavoidassets"))
            {
                MainAssets = AssetBundle.LoadFromStream(stream);
            }
            Swapallshaders(MainAssets);

            On.RoR2.Items.ContagiousItemManager.Init += AddVoidItemsToDict;
            On.RoR2.ItemCatalog.Init += AddUnlocksToVoidItems;

            Stage.onServerStageBegin += AddLocusStuff;
            On.RoR2.VoidStageMissionController.OnBatteryActivated += SpawnLocusPortal;
            RoR2.SceneDirector.onPrePopulateSceneServer += LocusDirectorHelp;
            //On.RoR2.Projectile.SlowDownProjectiles.OnTriggerEnter += fuck;

            //On.RoR2.ItemCatalog.Init += ItemCatalog_Init;

            //IL.RoR2.GenericSkill.RunRecharge += Ah;
            //On.RoR2.GenericSkill.RunRecharge += Ah2;

            On.RoR2.Language.GetLocalizedStringByToken += (orig, self, token) => {
                //Debug.Log("token: " + token);
                if (ItemBase.TokenToVoidPair.ContainsKey(token))
                {
                    ItemIndex idx = ItemCatalog.FindItemIndex(ItemBase.TokenToVoidPair[token]);
                    if (idx != ItemIndex.None) return orig(self, token).Replace("{CORRUPTION}", MiscUtils.GetPlural(orig(self, ItemCatalog.GetItemDef(idx).nameToken)));
                }
                return orig(self, token);
            };

            exhaustVFX = MainAssets.LoadAsset<GameObject>("ExhaustVFX.prefab");
            var exhefc = exhaustVFX.AddComponent<EffectComponent>();
            exhefc.applyScale = true;
            var exhvfx = exhaustVFX.AddComponent<VFXAttributes>();
            exhvfx.vfxIntensity = VFXAttributes.VFXIntensity.Low;
            exhvfx.vfxPriority = VFXAttributes.VFXPriority.Low;
            var exhdestroy = exhaustVFX.AddComponent<DestroyOnTimer>();
            exhdestroy.duration = 5;

            ContentAddition.AddEffect(exhaustVFX);
            //List<LotusBodyToken> bodyTokens = new List<LotusBodyToken>();


            platformObject = MainAssets.LoadAsset<GameObject>("mdlPlatformSeparate.prefab");
            platformObject.AddComponent<NetworkIdentity>();
            var tempSDP = platformObject.AddComponent<SurfaceDefProvider>();
            tempSDP.surfaceDef = Addressables.LoadAssetAsync<SurfaceDef>("RoR2/Base/Common/sdMetal.asset").WaitForCompletion();

            string platmat = "RoR2/DLC1/Common/matVoidmetalTrim.mat"; //"RoR2/DLC1/voidstage/matVoidMetalTrimGrassyVertexColorsOnly.mat"; // what the game uses, but it looks bad for some reason :(
            var pflower = platformObject.transform.Find("platformlower").GetComponent<MeshRenderer>();
            var pfupper = platformObject.transform.Find("platformupper").GetComponent<MeshRenderer>();
            pflower.material = Addressables.LoadAssetAsync<Material>(platmat).WaitForCompletion();
            pfupper.material = Addressables.LoadAssetAsync<Material>(platmat).WaitForCompletion();

            GameObject portalObjectTemp = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion();
            portalObject = PrefabAPI.InstantiateClone(portalObjectTemp, "LocusVoidPortal");
            var tempLight = portalObject.GetComponentInChildren<Light>();
            if (tempLight)
            {
                tempLight.enabled = false;
            }

            PrefabAPI.RegisterNetworkPrefab(platformObject);

            SetupVoidTierHighlights();

            if (doSaleCradle.Value){
                var cradle = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidChest/VoidChest.prefab").WaitForCompletion();
                if (cradle){
                    var pi = cradle.GetComponent<PurchaseInteraction>();
                    if (pi){
                        pi.saleStarCompatible = true;
                        On.RoR2.PurchaseInteraction.OnInteractionBegin += ImproveStarCradle;
                    }
                }
            }

            //var triple = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidTriple/VoidTriple.prefab").WaitForCompletion();
            //if (triple){
            //    var pi = triple.GetComponent<PurchaseInteraction>();
            //    if (pi){
            //        pi.saleStarCompatible =  true;
            //    }
            //}
            //bark.GetComponent<PurchaseInteraction>().saleStarCompatible = true;
            // Don't know how to create/use an asset bundle, or don't have a unity project set up?
            // Look here for info on how to set these up: https://github.com/KomradeSpectre/AetheriumMod/blob/rewrite-master/Tutorials/Item%20Mod%20Creation.md#unity-project


            //This section automatically scans the project for all artifacts
            //var ArtifactTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ArtifactBase)));
            //
            //foreach (var artifactType in ArtifactTypes)
            //{
            //    ArtifactBase artifact = (ArtifactBase)Activator.CreateInstance(artifactType);
            //    if (ValidateArtifact(artifact, Artifacts))
            //    {
            //        artifact.Init(Config);
            //    }
            //}

            //var voidtier1def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier1);
            //GameObject prefab = voidtier1def.highlightPrefab;


            //This section automatically scans the project for all items
            var ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));

            List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair>();

            foreach (var itemType in ItemTypes){
                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                if (ValidateItem(item, Items)){

                    item.Init(Config);

                    var tags = item.ItemTags;
                    bool aiValid = true;
                    bool aiBlacklist = false;
                    if (item.ItemDef.deprecatedTier == ItemTier.NoTier){
                        aiBlacklist = true;
                        aiValid = false;
                    }
                    string name = item.ItemName;
                    //Debug.Log("prename " + name);
                    name = name.Replace("'", "");
                    //Debug.Log("postname " + name);

                    foreach (var tag in tags){
                        if (tag == ItemTag.AIBlacklist){
                            aiBlacklist = true;
                            aiValid = false;
                            break;
                        }
                    }
                    if (aiValid){
                        aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;
                    }else{
                        aiBlacklist = true;
                    }

                    if (aiBlacklist){
                        item.AIBlacklisted = true;
                    }
                }
            }

            //this section automatically scans the project for all equipment
            var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EquipmentBase)));

            foreach (var equipmentType in EquipmentTypes)
            {
                EquipmentBase equipment = (EquipmentBase)System.Activator.CreateInstance(equipmentType);
                if (ValidateEquipment(equipment, Equipments))
                {
                    equipment.Init(Config);
                }
            }


            var InteractableTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(InteractableBase)));

            //ModLogger.LogInfo("---VV INTERACTABLES---");

            foreach (var interactableType in InteractableTypes)
            {
                InteractableBase interactable = (InteractableBase)System.Activator.CreateInstance(interactableType);
                if (ValidateInteractable(interactable, Interactables))
                {
                    interactable.Init(Config);
                    ModLogger.LogInfo("Interactable: " + interactable.InteractableName + " Initialized!");
                }
            }

            ///this section automatically scans the project for all elite equipment
            //var EliteEquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EliteEquipmentBase)));
            //
            //foreach (var eliteEquipmentType in EliteEquipmentTypes)
            //{
            //    EliteEquipmentBase eliteEquipment = (EliteEquipmentBase)System.Activator.CreateInstance(eliteEquipmentType);
            //    if (ValidateEliteEquipment(eliteEquipment, EliteEquipments))
            //    {
            //        eliteEquipment.Init(Config);
            //
            //    }
            //}

        }

        private void ImproveStarCradle(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            if(self.displayNameToken == "VOID_CHEST_NAME")
            {
                var count = activator.GetComponent<CharacterBody>().inventory.GetItemCount(DLC2Content.Items.LowerPricedChests);
                var chestb = self.GetComponent<ChestBehavior>();
                if (chestb && count > 0)
                {
                    chestb.dropForwardVelocityStrength = 6;
                }
            }
            

            //if(self.GetComponent<ChestBehavior>()))
            orig(self, activator);
        }

        private void AddUnlocksToVoidItems(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();
            foreach (var voidpair in ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem])
            {
                //corruptibleItems.Add(voidpair);
                if (lockVoidsBehindPair.Value)
                {
                    if (voidpair.itemDef1.unlockableDef != null && voidpair.itemDef2.unlockableDef == null)
                    {
                        Debug.Log("Updating unlock condition for " + voidpair.itemDef2.nameToken + " to " + voidpair.itemDef1.nameToken + "'s.");
                        voidpair.itemDef2.unlockableDef = voidpair.itemDef1.unlockableDef;
                    }
                }
                //Debug.Log("voidpair: " + voidpair.itemDef1 + " | " + voidpair.itemDef2 + " | " + voidpair.ToString());

            }
        }

        private void AddVoidItemsToDict(ContagiousItemManager.orig_Init orig)
        {
            List<ItemDef.Pair> newVoidPairs = new List<ItemDef.Pair>();
            Debug.Log("Adding VanillaVoid item transformations...");
            foreach (var item in Items)
            {
                if (item.ItemDef.deprecatedTier != ItemTier.NoTier) //safe assumption i think
                {
                    //Debug.Log("adding pair " + item);
                    Debug.Log("Item Name: " + item.ItemName);
                    item.AddVoidPair(newVoidPairs);
                }
                else
                {
                    Debug.Log("Skipping " + item.ItemName);
                }
            }
           // var key = DLC1Content.ItemRelationshipTypes.ContagiousItem;
            //Debug.Log(key);
            var voidPairs = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem];
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = voidPairs.Union(newVoidPairs).ToArray();
            Debug.Log("Finishing appending VanillaVoid item transformations.");

            orig();

        }

        /// <summary>
        /// A helper to easily set up and initialize an artifact from your artifact classes if the user has it enabled in their configuration files.
        /// </summary>
        /// <param name="artifact">A new instance of an ArtifactBase class."</param>
        /// <param name="artifactList">The list you would like to add this to if it passes the config check.</param>
        //public bool ValidateArtifact(ArtifactBase artifact, List<ArtifactBase> artifactList)
        //{
        //    var enabled = Config.Bind<bool>("Artifact: " + artifact.ArtifactName, "Enable Artifact?", true, "Should this artifact appear for selection?").Value;
        //
        //    if (enabled)
        //    {
        //        artifactList.Add(artifact);
        //    }
        //    return enabled;
        //}

        /// <summary>
        /// A helper to easily set up and initialize an item from your item classes if the user has it enabled in their configuration files.
        /// <para>Additionally, it generates a configuration for each item to allow blacklisting it from AI.</para>
        /// </summary>
        /// <param name="item">A new instance of an ItemBase class."</param>
        /// <param name="itemList">The list you would like to add this to if it passes the config check.</param>
        /// string name = item.name == “your item name here” ? “Your item name here without apostrophe” : itemDef.name
        public bool ValidateItem(ItemBase item, List<ItemBase> itemList)
        {
            string name = item.ItemName.Replace("'", string.Empty);
            //string name = item.ItemName == "Lens-Maker's Orrery" ? "Lens-Makers Orrery" : item.ItemName;
            bool enabled = false;
            //if (name.Equals("Empty Vials") || name.Equals("Broken Mess"))
            //{
            //    //enabled = true; //override config option
            //    //aiBlacklist = true;
            //    Debug.Log("Disabling config for " + name);
            //}
            //else
            //{

            //Debug.Log("stats on: " + item.ItemName + " | tier: " + item.Tier + " | icon:" + item.ItemIcon + " | tags: " + item.ItemTags);


            if (item.Tier == ItemTier.NoTier)
            {
                enabled = true;
                item.AIBlacklisted = true;
                //aiBlacklist = true;
                //Debug.Log("Adding Broken Item: " + item.ItemName);
            }
            else
            {
                //Debug.Log("ignoring config for: " + item.ItemName);
                //enabled = true;
                //aiBlacklist = true;
                //Debug.Log("Adding Normal Item: " + item.ItemName);
                enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
                //var tags = item.ItemTags;
                //bool aiValid = true;
                //foreach(var tag in tags)
                //{
                //    if(tag == ItemTag.AIBlacklist)
                //    {
                //        aiBlacklist = true;
                //        break;
                //    }
                //}
                //if (aiValid)
                //{
                //    aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;
                //}
                //else
                //{
                //    aiBlacklist = true;
                //}
                //aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;
            }

            //enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
            //aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;

            //}
            //var enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
            //var aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;

            if (enabled)
            {
                itemList.Add(item);
                //if (aiBlacklist)
                //{
                //    item.AIBlacklisted = true;
                //}
            }
            return enabled;
        }

        /// <summary>
        /// A helper to easily set up and initialize an equipment from your equipment classes if the user has it enabled in their configuration files.
        /// </summary>
        /// <param name="equipment">A new instance of an EquipmentBase class."</param>
        /// <param name="equipmentList">The list you would like to add this to if it passes the config check.</param>
        public bool ValidateEquipment(EquipmentBase equipment, List<EquipmentBase> equipmentList)
        {
            if (Config.Bind<bool>("Equipment: " + equipment.EquipmentName, "Enable Equipment?", true, "Should this equipment appear in runs?").Value)
            {
                equipmentList.Add(equipment);
                return true;
            }
            return false;
        }

        /// <summary>
        /// A helper to easily set up and initialize an elite equipment from your elite equipment classes if the user has it enabled in their configuration files.
        /// </summary>
        /// <param name="eliteEquipment">A new instance of an EliteEquipmentBase class.</param>
        /// <param name="eliteEquipmentList">The list you would like to add this to if it passes the config check.</param>
        /// <returns></returns>
        //public bool ValidateEliteEquipment(EliteEquipmentBase eliteEquipment, List<EliteEquipmentBase> eliteEquipmentList)
        //{
        //    var enabled = Config.Bind<bool>("Equipment: " + eliteEquipment.EliteEquipmentName, "Enable Elite Equipment?", true, "Should this elite equipment appear in runs? If disabled, the associated elite will not appear in runs either.").Value;
        //
        //    if (enabled)
        //    {
        //        eliteEquipmentList.Add(eliteEquipment);
        //        return true;
        //    }
        //    return false;
        //}

        public bool ValidateInteractable(InteractableBase interactable, List<InteractableBase> interactableList)
        {
            var enabled = Config.Bind<bool>("Interactable: " + interactable.InteractableName, "Enable Interactable?", true, "Should this interactable appear in runs?").Value;

            //InteractableStatusDictionary.Add(interactable, enabled);

            if (enabled)
            {
                interactableList.Add(interactable);
                return true;
            }
            return false;
        }


        public void SetupVoidTierHighlights()
        {
            GameObject tier1prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HighlightTier1Item.prefab").WaitForCompletion();
            tier1prefab.AddComponent<NetworkIdentity>();
            tier1Clone = PrefabAPI.InstantiateClone(tier1prefab, "void1HighlightPrefab");
            var rect1 = tier1Clone.GetComponent<RoR2.UI.HighlightRect>();
            if (rect1)
            {
                rect1.highlightColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.VoidItem);
                rect1.cornerImage = MainAssets.LoadAsset<Sprite>("texUICornerTier1");
            }

            //GameObject tier2prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HighlightTier1Item.prefab").WaitForCompletion();
            tier2Clone = PrefabAPI.InstantiateClone(tier1prefab, "void2HighlightPrefab");
            var rect2 = tier2Clone.GetComponent<RoR2.UI.HighlightRect>();
            if (rect2)
            {
                rect2.highlightColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.VoidItem);
                rect2.cornerImage = MainAssets.LoadAsset<Sprite>("texUICornerTier2");
            }

            tier3Clone = PrefabAPI.InstantiateClone(tier1prefab, "void3HighlightPrefab");
            var rect3 = tier3Clone.GetComponent<RoR2.UI.HighlightRect>();
            if (rect3)
            {
                rect3.highlightColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.VoidItem);
                rect3.cornerImage = MainAssets.LoadAsset<Sprite>("texUICornerTier3");
            }

            tier4Clone = PrefabAPI.InstantiateClone(tier1prefab, "void4HighlightPrefab");
            var rect4 = tier4Clone.GetComponent<RoR2.UI.HighlightRect>();
            if (rect4)
            {
                rect4.highlightColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.VoidItem);
                rect4.cornerImage = MainAssets.LoadAsset<Sprite>("texUICornerTier1");
            }

            hasAdjustedTiers = false;
            if (!doVoidCommandVFX.Value)
            {
                hasAddedCommand = true;
            }
            else
            {
                hasAddedCommand = false;
            }
        }

        public void ApplyTierHighlights()
        {
            if (!hasAdjustedTiers && doVoidPickupBorders.Value)
            {
                var voidtier1def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier1);
                if (voidtier1def)
                {
                    voidtier1def.highlightPrefab = tier1Clone;
                }
                var voidtier2def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier2);
                if (voidtier2def)
                {
                    voidtier2def.highlightPrefab = tier2Clone;
                }
                var voidtier3def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier3);
                if (voidtier3def)
                {
                    voidtier3def.highlightPrefab = tier3Clone;
                }
                var voidtier4def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidBoss);
                if (voidtier4def)
                {
                    voidtier4def.highlightPrefab = tier4Clone;
                }

                hasAdjustedTiers = true;
            }
        }

        private void AddLocusStuff(Stage obj)
        {

            ApplyTierHighlights();
            if (!hasAddedCommand && doVoidCommandVFX.Value)
            {
                hasAddedCommand = true;
                GameObject commandCube = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Command/CommandCube.prefab").WaitForCompletion();
                //commandCube.transform.Find("PickupDisplay");

                GameObject voidsys = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("VoidSystem.prefab");

                if (commandCube)
                {
                    if (commandCube.transform)
                    {
                        var trans = commandCube.transform.Find("PickupDisplay");
                        if (trans)
                        {
                            var se = voidsys.AddComponent<ShakeEmitter>();
                            var wave = new Wave();
                            wave.amplitude = 1;
                            wave.frequency = 60;
                            wave.cycleOffset = 0;
                            se.wave = wave;
                            se.shakeOnStart = true;
                            se.shakeOnEnable = false;
                            se.duration = .1f;
                            se.radius = 30;
                            se.scaleShakeRadiusWithLocalScale = false;
                            se.amplitudeTimeDecay = true;

                            //something something vanillavoid something "the shit way" TWO
                            var bright = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matTracerBright.mat").WaitForCompletion();
                            var flash = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matGenericFlash.mat").WaitForCompletion();
                            try
                            {
                                voidsys.transform.Find("Loops").Find("DistantSoftGlow").gameObject.GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matGlowItemPickup.mat").WaitForCompletion();
                                var swirls = voidsys.transform.Find("Loops").Find("Swirls").gameObject.GetComponent<ParticleSystemRenderer>();
                                if (swirls)
                                {
                                    List<Material> mats1 = new List<Material>();
                                    mats1.Add(Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matGlow2Soft.mat").WaitForCompletion());
                                    mats1.Add(bright);
                                    swirls.SetMaterials(mats1);
                                }

                                voidsys.transform.Find("Loops").Find("Glowies").gameObject.GetComponent<ParticleSystemRenderer>().material = flash;

                                voidsys.transform.Find("Burst").Find("Vacuum Stars, Distortion").gameObject.GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matInverseDistortion.mat").WaitForCompletion();
                                var particle = voidsys.transform.Find("Burst").Find("Vacuum Stars, Trails").gameObject.GetComponent<ParticleSystemRenderer>();
                                if (particle)
                                {
                                    List<Material> mats = new List<Material>();
                                    mats.Add(bright);
                                    mats.Add(Addressables.LoadAssetAsync<Material>("RoR2/Base/Nullifier/matNullifierStarTrail.mat").WaitForCompletion());
                                    particle.SetMaterials(mats);

                                }

                                voidsys.transform.Find("Burst").Find("Flash").gameObject.GetComponent<ParticleSystemRenderer>().material = flash;
                                voidsys.transform.Find("Burst").Find("Vacuum Radial").gameObject.GetComponent<ParticleSystemRenderer>().material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Nullifier/matNullifierStarPortalEdge.mat").WaitForCompletion();

                                voidsys.transform.Find("HarshGlow").gameObject.GetComponent<ParticleSystemRenderer>().material = flash;

                                voidsys.gameObject.SetActive(false);

                                voidsys.transform.SetParent(trans);

                                trans.GetComponent<PickupDisplay>().voidParticleEffect = voidsys;

                                voidsys.transform.position = new Vector3(0, 0, 0);
                            }
                            catch (Exception e)
                            {
                                Debug.Log("VV Exception (Command VFX): " + e);
                            }
                        }
                    }
                }
            }

            //voidsys.transform.SetParent(commandCube.transform.Find("PickupDisplay"));


            if (obj.sceneDef == SceneCatalog.GetSceneDefFromSceneName("voidstage") && locusExit.Value)
            {
                //Debug.Log("attempting");

                if (locusEarlyExit.Value)
                {
                    GameObject portal = UnityEngine.Object.Instantiate<GameObject>(portalObject, new Vector3(-37.5f, 19.5f, -284.05f), new Quaternion(0, 70, 0, 0));
                    NetworkServer.Spawn(portal);
                }
                GameObject platform = UnityEngine.Object.Instantiate<GameObject>(platformObject, new Vector3(-37.5f, 15.25f, -273.5f), new Quaternion(0, 0, 0, 0));
                NetworkServer.Spawn(platform);

            }

        }

        private void SpawnLocusPortal(On.RoR2.VoidStageMissionController.orig_OnBatteryActivated orig, VoidStageMissionController self)
        {
            orig(self);
            if (self.numBatteriesActivated >= self.numBatteriesSpawned && !locusEarlyExit.Value)
            {
                GameObject portal = UnityEngine.Object.Instantiate<GameObject>(portalObject, new Vector3(-37.5f, 19.5f, -284.05f), new Quaternion(0, 70, 0, 0));
                NetworkServer.Spawn(portal);
            }
        }

        private void LocusDirectorHelp(SceneDirector obj)
        {
            string sceneName = SceneCatalog.GetSceneDefForCurrentScene().baseSceneName;
            if (LocusBonus.Value > 0 && sceneName == "voidstage")
            {

                obj.interactableCredit += LocusBonus.Value;

            }
        }

        public void Swapallshaders(AssetBundle bundle)
        {
            //Debug.Log("beginning test");
            Material[] allMaterials = bundle.LoadAllAssets<Material>();
            foreach (Material mat in allMaterials)
            {
                //Debug.Log("material: " + mat.name + " | with shader: " + mat.shader.name);
                switch (mat.shader.name)
                {
                    case "Stubbed Hopoo Games/Deferred/Standard":
                        mat.shader = Resources.Load<Shader>("shaders/deferred/hgstandard");
                        break;
                    case "Stubbed Hopoo Games/Deferred/Snow Topped":
                        mat.shader = Resources.Load<Shader>("shaders/deferred/hgsnowtopped");
                        break;
                    case "Stubbed Hopoo Games/FX/Cloud Remap":
                        //Debug.Log("Switching material: " + mat.name);
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgcloudremap");
                        //Debug.Log("Swapped: " + mat.shader);
                        break;

                    case "Stubbed Hopoo Games/FX/Cloud Intersection Remap":
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgintersectioncloudremap");
                        break;
                    case "Stubbed Hopoo Games/FX/Opaque Cloud Remap":
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgopaquecloudremap");
                        break;
                    case "Stubbed Hopoo Games/FX/Distortion":
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgdistortion");
                        break;
                    case "Stubbed Hopoo Games/FX/Solid Parallax":
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgsolidparallax");
                        break;
                    case "Stubbed Hopoo Games/Environment/Distant Water":
                        mat.shader = Resources.Load<Shader>("shaders/environment/hgdistantwater");
                        break;
                    case "StubbedRoR2/Base/Shaders/HGCloudRemap":
                        //Debug.Log("Switching material: " + mat.name);
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgcloudremap");
                        break;
                    default:
                        break;
                }

            }
        }
    }
}