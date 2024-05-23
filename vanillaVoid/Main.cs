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
//using static vanillaVoid.Utils.Components.MaterialControllerComponents;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace vanillaVoid
{
    [BepInPlugin(ModGuid, ModName, ModVer)]

    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.content_management", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.items", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.language", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.prefab", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.recalculatestats", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.director", BepInDependency.DependencyFlags.HardDependency)]
    //[BepInDependency("com.bepis.r2api.networking", BepInDependency.DependencyFlags.HardDependency)]

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
        public const string ModVer = "1.5.14";

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

        public Xoroshiro128Plus genericRng;

        //public static ConfigEntry<bool> orreryCompat;
        public static ConfigEntry<bool> locusEarlyExit;
        public static ConfigEntry<bool> locusExit;
        public static ConfigEntry<int> LocusBonus;

        public static ConfigEntry<bool> lockVoidsBehindPair;
        public static ConfigEntry<bool> doVoidPickupBorders;
        public static ConfigEntry<bool> doVoidCommandVFX;

        public static ConfigEntry<int> LotusVariant;
        public static ConfigEntry<float> LotusDuration;
        public static ConfigEntry<float> LotusSlowPercent;

        GameObject lotusEffect;

        Vector3 heightAdjust = new Vector3(0, 2.212f, 0);
        Vector3 heightAdjustPulse = new Vector3(0, 2.5f, 0);
        float previousPulseFraction = 0;
        float currentCharge = 0;
        float secondsUntilBarrierAttempt = 0;

        public float lotusTimer;
        //public float lotusDuration = 25f;
        AnimationCurve speedCurve;

        GameObject tier1Clone;
        GameObject tier2Clone;
        GameObject tier3Clone;
        GameObject tier4Clone;
        bool hasAdjustedTiers;
        bool hasAddedCommand;

        public static BuffDef lotusSlow { get; private set; }

        private void Awake(){
            //orreryCompat = Config.Bind<bool>("Mod Compatability", "Enable Lost Seers Buff", true, "Should generally stay on, but if you're having a strange issue (ex. health bars not showing up on enemies) edit this to be false.");
            locusExit = Config.Bind<bool>("Tweaks: Void Locus", "Exit Portal", true, "If enabled, spawns a portal in the void locus letting you return to normal stages if you want to.");
            locusEarlyExit = Config.Bind<bool>("Tweaks: Void Locus", "Early Exit Portal", false, "If enabled, spawns the exit portal in void locus immediately upon entering the stage. Requires the exit portal to actually be enabled.");
            LocusBonus = Config.Bind<int>("Tweaks: Void Locus", "Locus Bonus Credits", 0, "If you want to make going to the void locus have a little more of a reward, increase this number. Should be increased in at least multiples of 50ish");

            lockVoidsBehindPair = Config.Bind<bool>("Tweaks: Void Items", "Require Original Item Unlocked", true, "If enabled, makes it so void items are locked until the non-void pair is unlocked. Ex. Pluripotent is locked until the profile has unlocked Dios. Only applies to void items which do not already have unlocks, in the event a mod adds special unlocks for a void item.");
            doVoidPickupBorders = Config.Bind<bool>("Tweaks: Void Items", "Improved Pickup Highlights", true, "If enabled, picking up a void item will show tier-appropriate item highlights rather the the default white highlights.");
            doVoidCommandVFX = Config.Bind<bool>("Tweaks: Void Items", "Improved Command VFX", true, "If enabled, void command cubes will have appropriate void vfx in the style of typical command VFX based on the actual void item VFX.");

            string lotusname = "Crystalline Lotus";

            LotusVariant = Config.Bind<int>("Item: " + lotusname, "Variant of Item", 0, "Adjust which version of " + lotusname + " you'd prefer to use. Variant 0 releases slowing novas per pulse, which reduce enemy and projectile speed, while Variant 1 provides 50% barrier per pulse.");
            LotusDuration = Config.Bind<float>("Item: " + lotusname, "Slow Duration", 30f, "Variant 0: Adjust how long the slow should last per pulse. A given slow is replaced by the next slow, so with enough lotuses, the full duration won't get used. However, increasing this also decreases the rate at which the slow fades.");
            LotusSlowPercent = Config.Bind<float>("Item: " + lotusname, "Slow Percent", 0.075f, "Variant 0: Adjust the strongest slow percent (between 0 and 1). Increasing this also makes it so the slow 'feels' shorter, as high values (near 1) feel very minor. Note that this is inverted, where 0 = 100% slow and 1 = 0% slow.");

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

            //GlobalEventManager.onCharacterDeathGlobal += ExeBladeExtraDeath;


            On.RoR2.CharacterBody.OnInventoryChanged += AddLotusOnPickup;
            On.RoR2.HoldoutZoneController.UpdateHealingNovas += CrystalLotusNova;
            //On.RoR2.HoldoutZoneController.FixedUpdate += LotusSlowNova;

            On.RoR2.SceneDirector.PlaceTeleporter += PrimoridalTeleporterCheck;

            Stage.onServerStageBegin += AddLocusStuff;
            On.RoR2.VoidStageMissionController.OnBatteryActivated += SpawnLocusPortal;
            RoR2.SceneDirector.onPrePopulateSceneServer += LocusDirectorHelp;
            //On.RoR2.Projectile.SlowDownProjectiles.OnTriggerEnter += fuck;
            RecalculateStatsAPI.GetStatCoefficients += LotusSlowStatsHook;
            //On.RoR2.CharacterBody.FixedUpdate += LotusSlowVisuals;
            //n.RoR2.CharacterModel.UpdateOverlays += AddLotusMaterial;
            On.RoR2.CharacterBody.FixedUpdate += LastTry;


            //On.RoR2.ItemCatalog.Init += ItemCatalog_Init;

            //IL.RoR2.GenericSkill.RunRecharge += Ah;
            //On.RoR2.GenericSkill.RunRecharge += Ah2;

            On.RoR2.Language.GetLocalizedStringByToken += (orig, self, token) => {
                if (ItemBase.TokenToVoidPair.ContainsKey(token))
                {
                    ItemIndex idx = ItemCatalog.FindItemIndex(ItemBase.TokenToVoidPair[token]);
                    if (idx != ItemIndex.None) return orig(self, token).Replace("{CORRUPTION}", MiscUtils.GetPlural(orig(self, ItemCatalog.GetItemDef(idx).nameToken)));
                }
                return orig(self, token);
            };

            lotusObject = MainAssets.LoadAsset<GameObject>("mdlLotusWorldObject2.prefab");
            lotusObject.AddComponent<TeamFilter>();
            lotusObject.AddComponent<NetworkIdentity>();

            lotusCollider = MainAssets.LoadAsset<GameObject>("LotusTeleporterCollider.prefab");
            lotusCollider.AddComponent<TeamFilter>();
            lotusCollider.AddComponent<NetworkIdentity>();
            //lotusCollider.AddComponent<LotusColliderToken>();
            lotusCollider.AddComponent<BuffWard>();
            lotusCollider.AddComponent<SlowDownProjectiles>();

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


            PrefabAPI.RegisterNetworkPrefab(lotusObject);
            PrefabAPI.RegisterNetworkPrefab(platformObject);

            string effect = "RoR2/DLC1/VoidRaidCrab/VoidRaidCrabDeathPending.prefab";
            GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>(effect).WaitForCompletion();
            //effectPrefab.AddComponent<NetworkIdentity>();

            lotusEffect = PrefabAPI.InstantiateClone(effectPrefab, "lotusEffect");
            lotusEffect.AddComponent<NetworkIdentity>();
            var effectcomp = lotusEffect.GetComponent<EffectComponent>();
            if (effectcomp)
            {
                //Debug.Log(effectcomp + " < | > " + effectcomp.soundName);
                effectcomp.soundName = "";
                //Debug.Log(effectcomp + " < | > " + effectcomp.soundName);
            }

            var timer = lotusEffect.AddComponent<DestroyOnTimer>();
            float delay = 1.15f;
            timer.duration = delay;

            ContentAddition.AddEffect(lotusEffect);

            speedCurve = new AnimationCurve();
            speedCurve.keys = new Keyframe[] {
            new Keyframe(0, LotusSlowPercent.Value, 0.33f, 0.33f),
            new Keyframe(0.5f, 0.3f, 0.33f, 0.33f),
            new Keyframe(1, 1, 0.33f, 0.33f)
            };

            CreateLotusBuff();

            SetupVoidTierHighlights();

            //GameObject commandCube = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Command/CommandCube.prefab").WaitForCompletion();
            ////commandCube.transform.Find("PickupDisplay");
            //
            //GameObject voidsys = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("VoidSystem.prefab");
            //
            //voidsys.transform.SetParent(commandCube.transform.Find("PickupDisplay"));

            //GameObject holder = new GameObject("VoidTierSystem");
            //holder.transform.SetParent(commandCube.transform.Find("PickupDisplay"));
            //holder.layer = 10;

            //GameObject loops = new GameObject("Loops");
            //loops.transform.SetParent(holder.transform);

            //GameObject distant = new GameObject("DistantSoftGlow");
            //loops.transform.SetParent(loops.transform);



            //var voidtier1def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier1);
            //GameObject v1prefab = voidtier1def.highlightPrefab;
            //
            //var hgrect1 = v1prefab.GetComponent<RoR2.UI.HighlightRect>();
            //if (hgrect1)
            //{
            //    hgrect1.highlightColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.VoidItem);
            //    hgrect1.cornerImage = MainAssets.LoadAsset<Sprite>("texUICornerTier1");
            //}
            //
            //var voidtier2def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier2);
            //GameObject v2prefab = voidtier2def.highlightPrefab;
            //
            //var hgrect2 = v2prefab.GetComponent<RoR2.UI.HighlightRect>();
            //if (hgrect2)
            //{
            //    hgrect2.highlightColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.VoidItem);
            //    hgrect2.cornerImage = MainAssets.LoadAsset<Sprite>("texUICornerTier2");
            //}
            //
            //var voidtier3def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier3);
            //GameObject v3prefab = voidtier3def.highlightPrefab;
            //
            //var hgrect3 = v3prefab.GetComponent<RoR2.UI.HighlightRect>();
            //if (hgrect3)
            //{
            //    hgrect3.highlightColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.VoidItem);
            //    hgrect3.cornerImage = MainAssets.LoadAsset<Sprite>("texUICornerTier3");
            //}
            //
            //var voidtier4def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidBoss);
            //GameObject v4prefab = voidtier4def.highlightPrefab;
            //
            //var hgrect4 = v4prefab.GetComponent<RoR2.UI.HighlightRect>();
            //if (hgrect4)
            //{
            //    hgrect4.highlightColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.VoidItem);
            //    hgrect4.cornerImage = MainAssets.LoadAsset<Sprite>("texUICornerTier1");
            //}

            //ExtraterrestrialExhaust.RocketProjectile.
            //R2API.ContentAddition.AddNetworkedObject(bladeObject);
            //R2API.ContentAddition.AddNetworkedObject(lotusObject);

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

            foreach (var itemType in ItemTypes)
            {

                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                if (ValidateItem(item, Items))
                {

                    //string itemTempName = item.ItemName;
                    //if (itemTempName.Contains('\''))
                    //{
                    //    itemTempName.Replace('\'', ' ');
                    //}
                    item.Init(Config);

                    var tags = item.ItemTags;
                    bool aiValid = true;
                    bool aiBlacklist = false;
                    if (item.ItemDef.deprecatedTier == ItemTier.NoTier)
                    {
                        aiBlacklist = true;
                        aiValid = false;
                    }
                    string name = item.ItemName;
                    //Debug.Log("prename " + name);
                    name = name.Replace("'", "");
                    //Debug.Log("postname " + name);

                    foreach (var tag in tags)
                    {
                        if (tag == ItemTag.AIBlacklist)
                        {
                            aiBlacklist = true;
                            aiValid = false;
                            break;
                        }
                    }
                    if (aiValid)
                    {
                        aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;
                    }
                    else
                    {
                        aiBlacklist = true;
                    }

                    if (aiBlacklist)
                    {
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

        private void AddUnlocksToVoidItems(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();
            if (lockVoidsBehindPair.Value)
            {
                foreach (var voidpair in ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem])
                {
                    if (voidpair.itemDef1.unlockableDef != null && voidpair.itemDef2.unlockableDef == null)
                    {
                        Debug.Log("Updating unlock condition for " + voidpair.itemDef2.nameToken + " to " + voidpair.itemDef1.nameToken + "'s.");
                        voidpair.itemDef2.unlockableDef = voidpair.itemDef1.unlockableDef;
                    }
                    //Debug.Log("voidpair: " + voidpair.itemDef1 + " | " + voidpair.itemDef2 + " | " + voidpair.ToString());

                }
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
                    Debug.Log("adding pair " + item);
                    Debug.Log("itemname: " + item.ItemLangTokenName);
                    item.AddVoidPair(newVoidPairs);
                }
                else
                {
                    Debug.Log("Skipping " + item.ItemName);
                }
            }
            var key = DLC1Content.ItemRelationshipTypes.ContagiousItem;
            Debug.Log(key);
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

        Vector3 teleporterPos;
        GameObject tempLotusObject;
        GameObject tempLotusCollider;
        bool lotusSpawned = false;
        bool isPrimoridal = false;
        string teleporterName = "";
        public float slowCoeffValue = 1f;
        bool voidfields = false;
        //bool detonationTime = false;

        //private Material lotusSlowMaterial;
        //lotusSlowMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matSlow80Debuff.mat").WaitForCompletion();
        //lotusSlowMaterial.mainTexture = MainAssets.LoadAsset<Texture>("texRampIce2.png");

        private void PrimoridalTeleporterCheck(On.RoR2.SceneDirector.orig_PlaceTeleporter orig, SceneDirector self)
        {

            string sceneName = SceneCatalog.GetSceneDefForCurrentScene().baseSceneName;
            if (sceneName != "arena" && sceneName != "moon2" && sceneName != "voidstage" && sceneName != "voidraid" && sceneName != "artifactworld" && sceneName != "bazaar" && sceneName != "goldshores" && sceneName != "limbo" && sceneName != "mysteryspace" && sceneName != "itancientloft" && sceneName != "itdampcave" && sceneName != "itfrozenwall" && sceneName != "itgolemplains" && sceneName != "itgoolake" && sceneName != "itmoon" && sceneName != "itskymeadow")
            {
                voidfields = false;
                StartCoroutine(LotusDelayedPlacement(self));
            }
            else if (sceneName == "arena")
            {
                voidfields = true;
            }
            orig(self);
        }

        IEnumerator LotusDelayedPlacement(SceneDirector self)
        {
            yield return new WaitForSeconds(4f);
            if (self)
            {
                if (self.teleporterSpawnCard)
                {
                    teleporterName = self.teleporterSpawnCard.ToString();

                    lotusSpawned = false;
                    teleporterPos = self.teleporterInstance.transform.position;
                    int itemCount = 0;
                    TeamIndex teamDex = default;

                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        itemCount += player.master.inventory.GetItemCount(ItemBase<CrystalLotus>.instance.ItemDef);
                        teamDex = player.master.teamIndex;
                    }

                    if (itemCount > 0)
                    {
                        teleporterPos = self.teleporterInstance.transform.position;
                        if (teleporterName.Contains("iscLunarTeleporter"))
                        {
                            Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
                            teleporterPos += celestialAdjust;
                        }

                        Quaternion rot = Quaternion.Euler(1.52666613f, 180, 9.999999f);
                        var tempLotus = Instantiate(lotusObject, teleporterPos, rot);
                        tempLotus.GetComponent<TeamFilter>().teamIndex = teamDex;
                        tempLotus.transform.position = teleporterPos + heightAdjust;
                        NetworkServer.Spawn(tempLotus);
                        tempLotusObject = tempLotus;
                        lotusSpawned = true;

                        EffectData effectData = new EffectData { origin = tempLotus.transform.position };
                        effectData.SetNetworkedObjectReference(tempLotus.gameObject);
                        EffectManager.SpawnEffect(HealthComponent.AssetReferences.crowbarImpactEffectPrefab, effectData, transmit: true);
                    }
                    previousPulseFraction = 0;
                }
            }
        }

        private void AddLotusOnPickup(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            if (self)
            {
                orig(self);
                if (!lotusSpawned)
                {
                    int itemCount = 0;
                    TeamIndex teamDex = default;
                    foreach (var player in PlayerCharacterMasterController.instances)
                    {
                        itemCount += player.master.inventory.GetItemCount(ItemBase<CrystalLotus>.instance.ItemDef);
                        teamDex = player.master.teamIndex;
                    }

                    if (itemCount > 0)
                    {
                        if (teleporterName.Contains("iscLunarTeleporter"))
                        {
                            Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
                            teleporterPos += celestialAdjust;
                        }

                        string sceneName = SceneCatalog.GetSceneDefForCurrentScene().baseSceneName;
                        if (sceneName != "arena" && sceneName != "moon2" && sceneName != "voidstage" && sceneName != "voidraid" && sceneName != "artifactworld" && sceneName != "bazaar" && sceneName != "goldshores" && sceneName != "limbo" && sceneName != "mysteryspace" && sceneName != "itancientloft" && sceneName != "itdampcave" && sceneName != "itfrozenwall" && sceneName != "itgolemplains" && sceneName != "itgoolake" && sceneName != "itmoon" && sceneName != "itskymeadow")
                        {
                            Quaternion rot = Quaternion.Euler(1.52666613f, 180, 9.999999f);
                            var tempLotus = Instantiate(lotusObject, teleporterPos, rot);
                            tempLotus.GetComponent<TeamFilter>().teamIndex = teamDex;
                            tempLotus.transform.position = teleporterPos + heightAdjust;
                            NetworkServer.Spawn(tempLotus);
                            tempLotusObject = tempLotus;
                            lotusSpawned = true;
                            voidfields = false;

                            EffectData effectData = new EffectData
                            {
                                origin = tempLotus.transform.position
                            };
                            effectData.SetNetworkedObjectReference(tempLotus.gameObject);
                            EffectManager.SpawnEffect(HealthComponent.AssetReferences.crowbarImpactEffectPrefab, effectData, transmit: true);
                        }
                        else if (sceneName == "arena")
                        {
                            voidfields = true;
                        }
                    }
                }
            }
        }

        private void CrystalLotusNova(On.RoR2.HoldoutZoneController.orig_UpdateHealingNovas orig, HoldoutZoneController self, bool isCharging)
        {
            int itemCount = 0;
            TeamIndex teamDex = default;
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                itemCount += player.master.inventory.GetItemCount(ItemBase<CrystalLotus>.instance.ItemDef);
                teamDex = player.master.teamIndex;
            }

            if (slowCoeffValue < 1)
            {
                lotusTimer += Time.fixedDeltaTime;
                slowCoeffValue = speedCurve.Evaluate(lotusTimer / LotusDuration.Value);
            }
            else
            {
                slowCoeffValue = 1;
            }

            if (itemCount > 0 && isCharging)
            {
                if (NetworkServer.active && Time.fixedDeltaTime > 0f)
                {
                    if (!tempLotusCollider)
                    {
                        Vector3 holdoutpos = self.gameObject.transform.position;
                        tempLotusCollider = UnityEngine.Object.Instantiate<GameObject>(lotusCollider, holdoutpos, new Quaternion(0, 0, 0, 0));

                        NetworkServer.Spawn(tempLotusCollider);

                        var temp = tempLotusCollider.GetComponent<TeamFilter>();
                        temp.teamIndex = teamDex;
                        tempLotusCollider.layer = 12;

                        TeamFilter filter = tempLotusCollider.GetComponent<TeamFilter>();
                        var tempcomp = tempLotusCollider.GetComponent<SlowDownProjectiles>();
                        tempcomp.teamFilter = filter;

                        var tempward = tempLotusCollider.GetComponent<BuffWard>();
                        tempward.radius = self.currentRadius;
                        tempward.buffDef = lotusSlow;
                        tempward.invertTeamFilter = true;
                        tempward.enabled = false;
                        tempward.buffDuration = 1;

                    }
                    else
                    {
                        Vector3 holdoutpos = self.gameObject.transform.position;
                        tempLotusCollider.transform.position = holdoutpos;
                    }

                    var spcl = tempLotusCollider.GetComponent<SphereCollider>();

                    spcl.radius = self.currentRadius;

                    var ward = tempLotusCollider.GetComponent<BuffWard>();
                    ward.radius = self.currentRadius;

                    var comp = tempLotusCollider.GetComponent<SlowDownProjectiles>();
                    comp.slowDownCoefficient = slowCoeffValue;

                    if (secondsUntilBarrierAttempt > 0f)
                    {
                        secondsUntilBarrierAttempt -= Time.fixedDeltaTime;
                    }
                    else
                    {
                        if (currentCharge > self.charge)
                        {
                            previousPulseFraction = 0;
                            currentCharge = self.charge;
                        }
                        if (self.charge >= 1)
                        {
                            if (LotusVariant.Value == 0)
                            {
                                if (tempLotusCollider)
                                {
                                    StartCoroutine(SlowLotusDelayedEnd());
                                }
                            }
                        }

                        float nextPulseFraction = CalcNextPulseFraction(itemCount * (int)ItemBase<CrystalLotus>.instance.pulseCountStacking.Value, previousPulseFraction);
                        currentCharge = self.charge;

                        if (nextPulseFraction <= currentCharge)
                        {
                            if (LotusVariant.Value == 1)
                            {
                                string nova = "RoR2/Base/TPHealingNova/TeleporterHealNovaPulse.prefab";
                                GameObject novaPrefab = Addressables.LoadAssetAsync<GameObject>(nova).WaitForCompletion();
                                novaPrefab.GetComponent<TeamFilter>().teamIndex = teamDex;
                                NetworkServer.Spawn(novaPrefab);
                                StartCoroutine(LotusDelayedBarrier(self, teamDex));
                            }
                            else
                            {
                                ward.enabled = true;

                                lotusTimer = 0;

                                slowCoeffValue = speedCurve.Evaluate(lotusTimer / LotusDuration.Value);

                                StartCoroutine(Lotus2ExplosionThing(self.gameObject));

                            }

                            previousPulseFraction = nextPulseFraction;
                            secondsUntilBarrierAttempt = 1f;

                            string effect2 = "RoR2/DLC1/VoidSuppressor/SuppressorClapEffect.prefab";
                            GameObject effect2Prefab = Addressables.LoadAssetAsync<GameObject>(effect2).WaitForCompletion();
                            var ef2efc = effect2Prefab.GetComponent<EffectComponent>();
                            ef2efc.applyScale = true;
                            ef2efc.referencedObject = effect2Prefab;
                            effect2Prefab.transform.localScale *= 4;
                            EffectManager.SimpleImpactEffect(effect2Prefab, teleporterPos, new Vector3(0, 0, 0), true);
                        }
                    }
                }
            }

            orig(self, isCharging);
        }
        private static float CalcNextPulseFraction(int itemCount, float prevPulseFraction)
        {
            //if(charge < .02 && prevPulseFraction > 1)
            //{
            //    Debug.Log("fixing dumb jank in calc" + prevPulseFraction);
            //    prevPulseFraction = 0;
            //}
            float healFraction = 1f / (float)(itemCount + 1);
            //Debug.Log(healFraction + " hela fraction");
            for (int i = 1; i <= itemCount; i++)
            {
                float temp = (float)i * healFraction;
                //Debug.Log("temp: " + temp + " | previous: " + prevPulseFraction);
                if (temp > prevPulseFraction)
                {
                    return temp;
                }
            }
            return 1.1f;
        }
        IEnumerator LotusDelayedBarrier(HoldoutZoneController self, TeamIndex teamDex)
        {
            yield return new WaitForSeconds(.5f);
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                if (self.IsBodyInChargingRadius(player.body) && player.body.teamComponent.teamIndex == teamDex)
                {
                    //var playerHealthComp = player.GetComponent<HealthComponent>();
                    //player.body.healthComponent;
                    if (player.body.healthComponent)
                    {
                        //Debug.Log("yoo health component!!");
                        player.body.healthComponent.AddBarrier(player.body.healthComponent.fullCombinedHealth * ItemBase<CrystalLotus>.instance.barrierAmount.Value); //25% 
                        //string effect2 = "RoR2/DLC1/VoidSuppressor/SuppressorClapEffect.prefab";
                        //GameObject effect2Prefab = Addressables.LoadAssetAsync<GameObject>(effect2).WaitForCompletion();
                        //EffectManager.SimpleImpactEffect(effect2Prefab, player.body.transform.position, player.body.aimOrigin, true);
                    }
                    else
                    {
                        //Debug.Log("no suitable health component.");
                    }

                }
            }



            //List<CharacterMaster> CharMasters(bool playersOnly = false)
            //{
            //    return CharacterMaster.readOnlyInstancesList.Where(x => x.hasBody && x.GetBody().healthComponent.alive && (x.GetBody().teamComponent.teamIndex != teamDex)).ToList();
            //}
            //if (CharMasters().Count > 3)
            //{
            //    if (genericRng == null)
            //    {
            //        genericRng = new Xoroshiro128Plus(Run.instance.seed);
            //    }
            //    int index = genericRng.RangeInt(0, CharMasters().Count - 3);
            //    //var target1 = CharMasters().ElementAt(index);
            //    //var target2 = CharMasters().ElementAt(index + 1);
            //    //var target3 = CharMasters().ElementAt(index + 2);
            //    //target1.gameObject.AddComponent<LotusToken>();
            //    //target2.gameObject.AddComponent<LotusToken>();
            //    //target3.gameObject.AddComponent<LotusToken>();
            //    for (int i = index; i < index + 3; i++)
            //    {
            //        var target = CharMasters().ElementAt(i);
            //        var token = target.gameObject.AddComponent<LotusBodyToken>();
            //        token.self = target.GetBody();
            //    }
            //}
            //else
            //{
            //    for (int i = 0; i < CharMasters().Count(); i++)
            //    {
            //        var target = CharMasters().ElementAt(i);
            //        var token = target.gameObject.AddComponent<LotusBodyToken>();
            //        token.self = target.GetBody();
            //    }
            //}
            //yield return new WaitForSeconds(.25f);
            //player.body.healthComponent.AddBarrier(player.body.healthComponent.health * ItemBase<BarrierLotus>.instance.barrierAmount.Value); //25% 

        }

        IEnumerator Lotus2ExplosionThing(GameObject gameobject)
        {
            var soundID = Util.PlaySound("Play_voidRaid_death", gameobject);
            //detonationTime = true;
            //string effect = "RoR2/DLC1/VoidRaidCrab/VoidRaidCrabDeathPending.prefab";
            //GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>(effect).WaitForCompletion();
            ////effectPrefab.AddComponent<NetworkIdentity>();
            //
            //GameObject effectpfrb = PrefabAPI.InstantiateClone(effectPrefab, "lotusEffect");
            //
            //var effectcomp = effectpfrb.GetComponent<EffectComponent>();
            //if (effectpfrb)
            //{
            //    Debug.Log(effectcomp + " < | > " + effectcomp.soundName);
            //    effectcomp.soundName = "";
            //    Debug.Log(effectcomp + " < | > " + effectcomp.soundName);
            //}
            //
            //var timer = effectpfrb.AddComponent<DestroyOnTimer>();
            //float delay = 1.1f;
            //timer.duration = delay;
            Vector3 pulsepos;
            if (voidfields)
            {
                pulsepos = gameobject.gameObject.transform.position + (heightAdjustPulse / 2);
            }
            else
            {
                pulsepos = teleporterPos + heightAdjustPulse;
            }

            EffectManager.SimpleEffect(lotusEffect, pulsepos, new Quaternion(0, 0, 0, 0), true);

            yield return new WaitForSeconds(1.15f);

            AkSoundEngine.StopPlayingID(soundID);

            //detonationTime = false;
            //gameobject.gameObject.transform.position
            string effect1 = "RoR2/DLC1/VoidSuppressor/SuppressorRetreatToShellEffect.prefab"; //"RoR2/DLC1/VoidRaidCrab/LaserImpactEffect.prefab";
            GameObject effect1Prefab = Addressables.LoadAssetAsync<GameObject>(effect1).WaitForCompletion();
            EffectManager.SimpleImpactEffect(effect1Prefab, pulsepos, new Vector3(0, 0, 0), true);
        }

        IEnumerator SlowLotusDelayedEnd()
        {
            //Debug.Log("Delayed end called");
            var comp = tempLotusCollider.GetComponent<SlowDownProjectiles>();
            while (slowCoeffValue < 1)
            {
                yield return .1f;
                slowCoeffValue += .0015f;
                comp.slowDownCoefficient = slowCoeffValue;
            }
            slowCoeffValue = 1;
            comp.slowDownCoefficient = 1;

        }

        private void LotusSlowStatsHook(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            //Debug.Log("slow hook being called");
            if (sender)
            {
                //Debug.Log("is buff? : " + sender.HasBuff(lotusSlow));
                // Debug.Log("warbanner? : " + sender.HasBuff(RoR2Content.Buffs.Warbanner));
                //var token = sender.gameObject.GetComponent<LotusBodyToken>();
                //Debug.Log("token? : " + token);
                //Debug.Log("sender: " + sender + " | " + sender.name);
                if (sender.HasBuff(lotusSlow))
                {
                    float slow = (1 - slowCoeffValue);
                    //Debug.Log("slow: " + slow);
                    args.moveSpeedReductionMultAdd += slow;
                    args.attackSpeedMultAdd -= slow / 2;
                }
            }
        }

        private void LastTry(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            orig(self);
            if (self.GetBuffCount(lotusSlow) > 0 && slowCoeffValue < 1)
            {
                var token = self.gameObject.GetComponent<LotusBodyToken>();
                if (!token)
                {
                    token = self.gameObject.AddComponent<LotusBodyToken>();
                    token.body = self;
                    token.coeff = slowCoeffValue;
                    token.duration = LotusDuration.Value;
                    token.Begin();
                    //startTime = Time.time;
                    //Debug.Log("start time: " + startTime);
                }
                else
                {
                    if (slowCoeffValue < token.coeff)
                    {
                        token.End();
                        Destroy(token);
                        int count = self.GetBuffCount(lotusSlow);
                        for (int i = 0; i < count; i++)
                        {
                            self.RemoveOldestTimedBuff(lotusSlow);
                        }

                        token = self.gameObject.AddComponent<LotusBodyToken>();
                        token.body = self;
                        token.coeff = slowCoeffValue;
                        token.duration = LotusDuration.Value;
                        token.Begin();

                    }
                    token.coeff = slowCoeffValue;
                }
            }
            else if (slowCoeffValue >= 1)
            {
                int count = self.GetBuffCount(lotusSlow);
                for (int i = 0; i < count; i++)
                {
                    self.RemoveOldestTimedBuff(lotusSlow);
                }

                var token = self.gameObject.GetComponent<LotusBodyToken>();
                if (token)
                {
                    token.End();
                    Destroy(token);
                    //ndTime = Time.time;
                    //ebug.Log("end time: " + endTime + " | " + (startTime - endTime));
                }
            }
        }

        public class LotusBodyToken : MonoBehaviour
        {
            public CharacterBody body;
            public TemporaryOverlay overlay;
            public float coeff;
            public Material material;
            public Material matInstance;
            public float duration;
            //public float oldCoeff;

            public void Begin()
            {
                Texture tex = MainAssets.LoadAsset<Texture>("texRampIce4.png");
                material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matSlow80Debuff.mat").WaitForCompletion();
                matInstance = Instantiate(material);



                //Debug.Log("boost: " + matInstance.GetFloat("_Boost"));
                matInstance.SetFloat("_Boost", matInstance.GetFloat("_Boost") - (coeff * 2));
                //matInstance.SetTexture("_RemapTex", tex);
                //Debug.Log("remap: " + matInstance.GetTexture("_RemapTex"));
                matInstance.SetTexture("_RemapTex", tex);
                //Debug.Log("remap: " + matInstance.GetTexture("_RemapTex"));
                //Color newcolor = new Color(0.549f, 0.090f, 0.906f, material.color.a);
                //material.color = newcolor;

                //matInstance.shader.SetFieldValue("_RemapTex", tex); //???????????????????????????????????????????????????????????????????????????????????????????????????????????????????????

                overlay = body.modelLocator.modelTransform.gameObject.AddComponent<RoR2.TemporaryOverlay>();
                overlay.duration = duration;
                overlay.alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, .5f);

                overlay.animateShaderAlpha = true;
                overlay.destroyComponentOnEnd = true;
                overlay.originalMaterial = matInstance;
                overlay.AddToCharacerModel(body.modelLocator.modelTransform.GetComponent<RoR2.CharacterModel>());

            }

            public void End()
            {
                overlay.RemoveFromCharacterModel();
            }

            void FixedUpdate()
            {
                matInstance.SetFloat("_Boost", 1 - coeff);
                //Debug.Log("boost: " + matInstance.GetFloat("_Boost"));
            }
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

        public void CreateLotusBuff()
        {
            var buffColor = new Color(0.5215f, 0.3764f, 0.8549f);
            lotusSlow = ScriptableObject.CreateInstance<BuffDef>();
            lotusSlow.buffColor = buffColor;
            lotusSlow.canStack = false;
            lotusSlow.isDebuff = true;
            //lotusSlow.isHidden = true;
            lotusSlow.name = "ZnVV" + "lotusSlow";
            lotusSlow.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("lotusSlow");
            ContentAddition.AddBuffDef(lotusSlow);
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
                Debug.Log("material: " + mat.name + " | with shader: " + mat.shader.name);
                switch (mat.shader.name)
                {
                    case "Stubbed Hopoo Games/Deferred/Standard":
                        mat.shader = Resources.Load<Shader>("shaders/deferred/hgstandard");
                        break;
                    case "Stubbed Hopoo Games/Deferred/Snow Topped":
                        mat.shader = Resources.Load<Shader>("shaders/deferred/hgsnowtopped");
                        break;
                    case "Stubbed Hopoo Games/FX/Cloud Remap":
                        Debug.Log("Switching material: " + mat.name);
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgcloudremap");
                        Debug.Log("Swapped: " + mat.shader);
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

        private void ItemCatalog_Init(On.RoR2.ItemCatalog.orig_Init orig)
        {
            orig();

            var t1Infect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/ItemInfection, White.prefab").WaitForCompletion();
            var t2Infect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/ItemInfection, Green.prefab").WaitForCompletion();
            var t3Infect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/ItemInfection, Red.prefab").WaitForCompletion();
            var luInfect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/ItemInfection, Blue.prefab").WaitForCompletion();

            var v1Infect = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("void1Infection.prefab");
            var v1dis = v1Infect.AddComponent<ItemDisplay>();
            v1dis.rendererInfos = new CharacterModel.RendererInfo[1];
            v1dis.rendererInfos[0].renderer = v1Infect.GetComponent<MeshRenderer>();
            v1dis.rendererInfos[0].defaultMaterial = vanillaVoidPlugin.MainAssets.LoadAsset<Material>("voidInfectionT1.mat");

            var v2Infect = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("void2Infection.prefab");
            var v2dis = v2Infect.AddComponent<ItemDisplay>();
            v2dis.rendererInfos = new CharacterModel.RendererInfo[1];
            v2dis.rendererInfos[0].renderer = v2Infect.GetComponent<MeshRenderer>();
            v2dis.rendererInfos[0].defaultMaterial = vanillaVoidPlugin.MainAssets.LoadAsset<Material>("voidInfectionT2.mat");

            var v3Infect = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("void3Infection.prefab");
            var v3dis = v3Infect.AddComponent<ItemDisplay>();
            v3dis.rendererInfos = new CharacterModel.RendererInfo[1];
            v3dis.rendererInfos[0].renderer = v3Infect.GetComponent<MeshRenderer>();
            v3dis.rendererInfos[0].defaultMaterial = vanillaVoidPlugin.MainAssets.LoadAsset<Material>("voidInfectionT3.mat");

            var bsInfect = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("t4Infection.prefab");
            var bsdis = bsInfect.AddComponent<ItemDisplay>();
            bsdis.rendererInfos = new CharacterModel.RendererInfo[1];
            bsdis.rendererInfos[0].renderer = bsInfect.GetComponent<MeshRenderer>();
            bsdis.rendererInfos[0].defaultMaterial = vanillaVoidPlugin.MainAssets.LoadAsset<Material>("InfectionT4.mat");

            //string[] bones = {"HandL", "HandR", "chest", "UpperArmL", "UpperArmR", "LowerArmL", "LowerArmR", "ThighL", "ThighR", "CalfL", "CalfR,", "Head", "Neck", "Stomach", "Pelvis"};
            //string[] bones = {"chest", "UpperArmL", "UpperArmR", "LowerArmL", "LowerArmR", "ThighL", "ThighR", "CalfL", "CalfR,", "Head", "Neck", "Stomach", "Pelvis" };
            //0-2 large, 3, thin, 4 small, 5-12 tall
            string[] bones = { "chest", "Stomach", "Pelvis", "Head", "Neck", "UpperArmL", "UpperArmR", "LowerArmL", "LowerArmR", "ThighL", "ThighR", "CalfL", "CalfR" };

            Xoroshiro128Plus mithRand = new Xoroshiro128Plus(3691);


            var idrs = Addressables.LoadAssetAsync<ItemDisplayRuleSet>("RoR2/Base/Brother/idrsBrother.asset").WaitForCompletion();
            int i = 0;
            foreach (var drs in idrs.keyAssetRuleGroups)
            {
                Debug.Log(++i + ": keyAssetRuleGroups - " + drs.keyAsset + " | " + drs.displayRuleGroup.rules[0].localPos + " | " + drs.displayRuleGroup.rules[0].childName + " | ");
            }
            ItemDisplayRuleSet.KeyAssetRuleGroup[] itemRuleGroups = idrs.keyAssetRuleGroups;

            ReadOnlyContentPack? sotvPack = ContentManager.FindContentPack("RoR2.DLC1");
            if (sotvPack.HasValue)
            {
                var pack = sotvPack.Value;
                var items = pack.itemDefs;

                foreach (var item in items)
                {
                    Debug.Log(++i + ": Item: " + item.nameToken + " | " + item.tier + " | " + item.deprecatedTier);

                    if (item.tier == ItemTier.Tier1 || item.tier == ItemTier.Tier2 || item.tier == ItemTier.Tier3 || item.tier == ItemTier.Boss || item.tier == ItemTier.Lunar)
                    {
                        ItemDisplayRuleSet.KeyAssetRuleGroup a;
                        a.keyAsset = item;

                        var rand = mithRand.RangeInt(0, bones.Length);

                        a.displayRuleGroup = new DisplayRuleGroup();
                        a.displayRuleGroup.AddDisplayRule(new RoR2.ItemDisplayRule
                        {
                            ruleType = ItemDisplayRuleType.ParentedPrefab,
                            followerPrefab = (item.tier == ItemTier.Tier1 ? t1Infect : item.tier == ItemTier.Tier2 ? t2Infect : item.tier == ItemTier.Tier3 ? t3Infect : item.tier == ItemTier.Boss ? bsInfect : luInfect),
                            childName = bones[rand],
                            localPos = GeneratePositionFromRand(mithRand, rand), //new Vector3(mithRand.RangeFloat(-.225f, .225f), mithRand.RangeFloat(-.225f, .225f), mithRand.RangeFloat(-.225f, .225f)),
                            localAngles = new Vector3(mithRand.RangeFloat(0, 360), mithRand.RangeFloat(0, 360), mithRand.RangeFloat(0, 360)),
                            localScale = new Vector3(mithRand.RangeFloat(.105f, .115f), mithRand.RangeFloat(.105f, .115f), mithRand.RangeFloat(.105f, .115f))
                        });
                        itemRuleGroups = itemRuleGroups.AddItem(a).ToArray();

                        //itemRuleGroups = itemRuleGroups.AddToArray(a);
                    }

                }
                idrs.keyAssetRuleGroups = itemRuleGroups;

                Debug.Log("done with Mith Rick of Rain idrs");
            }
        }

        Vector3 GeneratePositionFromRand(Xoroshiro128Plus rand, int randVal)
        {
            if (randVal <= 2)
            { //large
                return new Vector3(rand.RangeFloat(-.2f, .2f), rand.RangeFloat(-.225f, .225f), rand.RangeFloat(-.2f, .2f));
            }
            else if (randVal == 3)
            { //thin
                return new Vector3(rand.RangeFloat(-.2f, .2f), rand.RangeFloat(-.015f, .0675f), rand.RangeFloat(-.2f, .2f));
            }
            else if (randVal == 4)
            { //small
                return new Vector3(rand.RangeFloat(-.0375f, .0375f), rand.RangeFloat(-.0375f, .0375f), rand.RangeFloat(-.0375f, .0375f));
            }
            else
            { //tall
                return new Vector3(rand.RangeFloat(-.1f, .1f), rand.RangeFloat(-0.075f, .375f), rand.RangeFloat(-.1f, .1f));
            }
            //pelvis needs to be not tall

            //tall needs to be thinner
            //rotation needs to aim at bone origin - aim at the bones position, ignore y axis
        }
    }
}