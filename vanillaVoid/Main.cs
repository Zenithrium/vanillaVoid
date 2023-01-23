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
using VoidItemAPI;
using UnityEngine.AddressableAssets;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.Orbs;
using UnityEngine.Networking;
using RoR2.Projectile;
using vanillaVoid.Interactables;
using vanillaVoid.Misc;
using EntityStates.TeleporterHealNovaController;
//using static vanillaVoid.Utils.Components.MaterialControllerComponents;

namespace vanillaVoid
{
    [BepInPlugin(ModGuid, ModName, ModVer)]

    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.content_management", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.items", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.language", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.prefab", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.recalculatestats", BepInDependency.DependencyFlags.HardDependency)]
    //[BepInDependency("com.bepis.r2api.networking", BepInDependency.DependencyFlags.HardDependency)]

    //[BepInDependency("com.RumblingJOSEPH.VoidItemAPI", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(VoidItemAPI.VoidItemAPI.MODGUID)]
    
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    //[BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    //[R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(RecalculateStatsAPI), nameof(PrefabAPI), nameof(LegacyResourcesAPI))]

    //[BepInDependency(VoidItemAPI.VoidItemAPI.MODGUID)]

    public class vanillaVoidPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.Zenithrium.vanillaVoid";
        public const string ModName = "vanillaVoid";
        public const string ModVer = "1.4.3";

        public static ExpansionDef sotvDLC;

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

        public static GameObject bladeObject;

        public static GameObject lotusObject;
        public static GameObject lotusPulse;
        public static GameObject lotusCollider;

        public Xoroshiro128Plus genericRng;

        //public static ConfigEntry<bool> orreryCompat;
        public static ConfigEntry<bool> locusExit;
        public static ConfigEntry<int> LocusBonus;

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
        public static BuffDef lotusSlow { get; private set; }

        private void Awake()
        {
            //orreryCompat = Config.Bind<bool>("Mod Compatability", "Enable Lost Seers Buff", true, "Should generally stay on, but if you're having a strange issue (ex. health bars not showing up on enemies) edit this to be false.");
            locusExit = Config.Bind<bool>("Tweaks: Void Locus", "Exit Portal", true, "If enabled, spawns a portal in the void locus letting you return to normal stages if you want to.");
            LocusBonus = Config.Bind<int>("Tweaks: Void Locus", "Locus Bonus Credits", 0, "If you want to make going to the void locus have a little more of a reward, increase this number. Should be increased in at least multiples of 50ish");

            string lotusname = "Crystalline Lotus";

            LotusVariant = Config.Bind<int>("Item: " + lotusname, "Variant of Item", 0, "Adjust which version of " + lotusname + " you'd prefer to use. Variant 0 releases slowing novas per pulse, which reduce enemy and projectile speed, while Variant 1 provides 50% barrier per pulse.");
            LotusDuration = Config.Bind<float>("Item: " + lotusname, "Slow Duration", 30f, "Variant 0: Adjust how long the slow should last per pulse. A given slow is replaced by the next slow, so with enough lotuses, the full duration won't get used. However, increasing this also decreases the rate at which the slow fades.");
            LotusSlowPercent = Config.Bind<float>("Item: " + lotusname, "Slow Percent", 0.075f, "Variant 0: Adjust the strongest slow percent (between 0 and 1). Increasing this also makes it so the slow 'feels' shorter, as high values (near 1) feel very minor.");

            

            ModLogger = Logger;

            var harm = new Harmony(Info.Metadata.GUID);
            new PatchClassProcessor(harm, typeof(ModdedDamageColors)).Patch();

            sotvDLC = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.nameToken == "DLC1_NAME");  //learn what sotv is 

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("vanillaVoid.vanillavoidassets"))
            {
                MainAssets = AssetBundle.LoadFromStream(stream);
            }

            Swapallshaders(MainAssets);

            On.RoR2.CharacterBody.OnSkillActivated += ExtExhaustFireProjectile;

            GlobalEventManager.onCharacterDeathGlobal += ExeBladeExtraDeath;

            //RoR2.SceneDirector.onPostPopulateSceneServer += AddLotusOnEnter;
            On.RoR2.CharacterBody.OnInventoryChanged += AddLotusOnPickup;
            On.RoR2.HoldoutZoneController.UpdateHealingNovas += CrystalLotusNova;
            //On.RoR2.HoldoutZoneController.FixedUpdate += LotusSlowNova;

            On.RoR2.SceneDirector.PlaceTeleporter += PrimoridalTeleporterCheck;

            Stage.onServerStageBegin += AddLocusStuff;
            RoR2.SceneDirector.onPrePopulateSceneServer += LocusDirectorHelp;
            //On.RoR2.Projectile.SlowDownProjectiles.OnTriggerEnter += fuck;
            RecalculateStatsAPI.GetStatCoefficients += LotusSlowStatsHook;
            //On.RoR2.CharacterBody.FixedUpdate += LotusSlowVisuals;
            //n.RoR2.CharacterModel.UpdateOverlays += AddLotusMaterial;
            On.RoR2.CharacterBody.FixedUpdate += LastTry;


            bladeObject = MainAssets.LoadAsset<GameObject>("mdlBladeWorldObject.prefab");
            bladeObject.AddComponent<TeamFilter>();
            bladeObject.AddComponent<HealthComponent>();
            bladeObject.AddComponent<NetworkIdentity>();
            bladeObject.AddComponent<BoxCollider>();
            bladeObject.AddComponent<Rigidbody>();


            lotusObject = MainAssets.LoadAsset<GameObject>("mdlLotusWorldObject2.prefab");
            lotusObject.AddComponent<TeamFilter>();
            lotusObject.AddComponent<NetworkIdentity>();

            lotusCollider = MainAssets.LoadAsset<GameObject>("LotusTeleporterCollider.prefab");
            lotusCollider.AddComponent<TeamFilter>();
            lotusCollider.AddComponent<NetworkIdentity>();
            //lotusCollider.AddComponent<LotusColliderToken>();
            lotusCollider.AddComponent<BuffWard>();
            lotusCollider.AddComponent<SlowDownProjectiles>();

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

            portalObject = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion();
            var tempLight = portalObject.GetComponentInChildren<Light>();
            if (tempLight)
            {
                tempLight.enabled = false;
            }


            PrefabAPI.RegisterNetworkPrefab(bladeObject);
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

            ModLogger.LogInfo("---INTERACTABLES---");

            foreach (var interactableType in InteractableTypes)
            {
                InteractableBase interactable = (InteractableBase)System.Activator.CreateInstance(interactableType);
                if (ValidateInteractable(interactable, Interactables))
                {
                    interactable.Init(Config);
                    ModLogger.LogInfo("Interactable: " + interactable.InteractableName + " Initialized!");
                }
            }

            //this section automatically scans the project for all elite equipment
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
            bool aiBlacklist = false;
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
                aiBlacklist = true;
                //Debug.Log("Adding Broken Item: " + item.ItemName);
            }
            else
            {
                //Debug.Log("ignoring config for: " + item.ItemName);
                //enabled = true;
                //aiBlacklist = true;
                //Debug.Log("Adding Normal Item: " + item.ItemName);
                enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
                aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;
            }

            //enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
            //aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;

            //}
            //var enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
            //var aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;

            if (enabled)
            {
                itemList.Add(item);
                if (aiBlacklist)
                {
                    item.AIBlacklisted = true;
                }
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

        private void ExtExhaustFireProjectile(On.RoR2.CharacterBody.orig_OnSkillActivated orig, RoR2.CharacterBody self, RoR2.GenericSkill skill)
        {
            var inventoryCount = self.inventory.GetItemCount(ItemBase<ExtraterrestrialExhaust>.instance.ItemDef);
            if (inventoryCount > 0 && skill.cooldownRemaining > 0) //maybe make this higher
            {
                //var playerPos = self.GetComponent<CharacterBody>().corePosition;
                float skillCD = skill.baseRechargeInterval;

                //Debug.Log("cooldown is " + skillCD);
                int missleCount = (int)Math.Ceiling(skillCD / ItemBase<ExtraterrestrialExhaust>.instance.secondsPerRocket.Value);
                //Debug.Log("rockets firing: " + missleCount);
                //Debug.Log("lunar primary: " + skill.stock); 

                //var voidtier1def = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier1);
                //GameObject prefab = voidtier1def.highlightPrefab;
                //Instantiate(prefab, self.transform);
                //Debug.Log("time: " + Time.time);
                if (skill.skillDef.ToString().Contains("LunarPrimaryReplacement"))
                {
                    //if (genericRng == null)
                    //{
                    //    genericRng = new Xoroshiro128Plus(Run.instance.seed);
                    //}
                    //int roll = genericRng.RangeInt(1, 100);
                    if (skill.stock % 2 != 0)
                    {
                        missleCount = 1;
                    }
                    else
                    {
                        missleCount = 0;
                    }
                }
                StartCoroutine(delayedRockets(self, missleCount, inventoryCount)); //this can probably be done better
            }

            orig(self, skill);
        }

        IEnumerator delayedRockets(RoR2.CharacterBody player, int missileCount, int inventoryCount)
        {
            int icbmMod = 1;
            if (player.inventory.GetItemCount(DLC1Content.Items.MoreMissile) > 0)
            {
                icbmMod = 3;
            }
            for (int i = 0; i < missileCount; i++)
            {
                yield return new WaitForSeconds(.1f);
                var playerPos = player.GetComponent<CharacterBody>().corePosition;
                float random = UnityEngine.Random.Range(-30, 30);
                Quaternion UpwardsQuat = Quaternion.Euler(270, random, 0);
                Vector3 Upwards = new Vector3(270, random, 0);
                //Debug.Log(((ItemBase<ExtraterrestrialExhaust>.instance.rocketDamage.Value + (ItemBase<ExtraterrestrialExhaust>.instance.rocketDamageStacking.Value * (inventoryCount - 1))) / 100));
                float rocketDamage = player.damage * ((ItemBase<ExtraterrestrialExhaust>.instance.rocketDamage.Value + (ItemBase<ExtraterrestrialExhaust>.instance.rocketDamageStacking.Value * (inventoryCount - 1))) / 100);
                for (int j = 0; j < icbmMod; j++)
                {
                    switch (j)
                    {
                        case 0:
                            break;
                        case 1:
                            UpwardsQuat = Quaternion.Euler(225, random, 0);
                            break;
                        case 2:
                            UpwardsQuat = Quaternion.Euler(315, random, 0);
                            break;

                    }
                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo()
                    {
                        owner = player.gameObject,
                        damage = rocketDamage,
                        position = player.corePosition,
                        rotation = UpwardsQuat,
                        crit = player.RollCrit(),
                        projectilePrefab = ExtraterrestrialExhaust.RocketProjectile,
                        force = 10f,

                        //useSpeedOverride = true,
                        //speedOverride = 1f,
                    };
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                }
                //FireProjectileInfo fireProjectileInfo = new FireProjectileInfo()
                //{
                //    owner = player.gameObject,
                //    damage = rocketDamage,
                //    position = player.corePosition,
                //    rotation = UpwardsQuat,
                //    crit = player.RollCrit(),
                //    projectilePrefab = ExtraterrestrialExhaust.RocketProjectile,
                //    force = 10f,
                //    
                //    
                //    //useSpeedOverride = true,
                //    //speedOverride = 1f,
                //};
                //var targets = new List<HurtBox>();
                //var sphereSearch = new SphereSearch
                //{
                //    mask = LayerIndex.entityPrecise.mask,
                //    origin = player.transform.position,
                //    radius = 200
                //};
                //sphereSearch.RefreshCandidates();
                //sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                //sphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(player.teamComponent.teamIndex));
                //sphereSearch.OrderCandidatesByDistance();
                //sphereSearch.GetHurtBoxes(targets);
                //Debug.Log("target 1: " + targets[0].gameObject + " | " + targets.Capacity);
                //for (int j = 0; j > targets.Capacity; j++)
                //{
                //    Debug.Log("im in the j loop");
                //    if (targets[j])
                //    {
                //        Debug.Log("bouta fire misle");
                //        MissileUtils.FireMissile(
                //            player.corePosition,
                //            player,
                //            default,
                //            targets[j].gameObject,
                //            rocketDamage,
                //            player.RollCrit(),
                //            ExtraterrestrialExhaust.RocketProjectile,
                //            DamageColorIndex.Item,
                //            Upwards,
                //            10f,
                //            true
                //        );
                //        break;
                //    }
                //}
                //#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                //                ProjectileManager.instance.FireProjectileServer(fireProjectileInfo);
                //#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
        }

        private static readonly SphereSearch exeBladeSphereSearch = new SphereSearch();
        private static readonly List<HurtBox> exeBladeHurtBoxBuffer = new List<HurtBox>();

        private void ExeBladeExtraDeath(DamageReport dmgReport)
        {
            if (!dmgReport.attacker || !dmgReport.attackerBody || !dmgReport.victim || !dmgReport.victimBody || !dmgReport.victimIsElite)
            {
                return; //end func if death wasn't killed by something real enough
            }
            var exeComponent = dmgReport.victimBody.GetComponent<ExeToken>();
            if (exeComponent)
            {
                return; //prevent game crash  
            }

            CharacterBody victimBody = dmgReport.victimBody;
            dmgReport.victimBody.gameObject.AddComponent<ExeToken>();
            CharacterBody attackerBody = dmgReport.attackerBody;
            if (attackerBody.inventory && NetworkServer.active)
            {
                var bladeCount = attackerBody.inventory.GetItemCount(ItemBase<ExeBlade>.instance.ItemDef);
                if (bladeCount > 0)
                {
                    Quaternion rot = Quaternion.Euler(0, 180, 0);
                    var tempBlade = Instantiate(bladeObject, victimBody.corePosition, rot);
                    tempBlade.GetComponent<TeamFilter>().teamIndex = attackerBody.teamComponent.teamIndex;
                    tempBlade.transform.position = victimBody.corePosition;
                    NetworkServer.Spawn(tempBlade);
                    EffectData effectData = new EffectData
                    {
                        origin = victimBody.corePosition
                    };
                    effectData.SetNetworkedObjectReference(tempBlade);
                    EffectManager.SpawnEffect(HealthComponent.AssetReferences.executeEffectPrefab, effectData, transmit: true);
                    StartCoroutine(ExeBladeDelayedExecutions(bladeCount, tempBlade, dmgReport));
                }
            }
        }

        IEnumerator ExeBladeDelayedExecutions(int bladeCount, GameObject bladeObject, DamageReport dmgReport)
        {
            bladeObject.AddComponent<ExeToken>(); //oopsies!!! don't break game

            bladeObject.AddComponent<Rigidbody>();
            var bladeRigid = bladeObject.GetComponent<Rigidbody>();
            var bladeCollider = bladeObject.GetComponent<BoxCollider>(); // default size = (0.8, 4.3, 1.8)

            bladeRigid.drag = .5f;

            float randomHeight = UnityEngine.Random.Range(2.45f, 2.95f);
            bladeCollider.size = new Vector3(0.1f, randomHeight, 0.1f);

            bladeRigid.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;

            float randomX = UnityEngine.Random.Range(-20, 10);
            float randomY = UnityEngine.Random.Range(0, 360);
            float randomZ = UnityEngine.Random.Range(-20, 20);
            Quaternion rot = Quaternion.Euler(randomX, randomY, randomZ);
            bladeObject.transform.SetPositionAndRotation(bladeObject.transform.position, rot);

            var damage = dmgReport.damageInfo.damage;
            var cmbHP = dmgReport.victim.combinedHealth;
            var bladeObjHPC = bladeObject.GetComponent<HealthComponent>();
            CharacterBody attackerBody = dmgReport.attackerBody;

            //float stackRadius = ItemBase<ExeBlade>.instance.aoeRangeBaseExe.Value + (ItemBase<ExeBlade>.instance.aoeRangeStackingExe.Value * (float)(bladeCount - 1));

            float effectiveRadius = ItemBase<ExeBlade>.instance.aoeRangeBaseExe.Value;
            float AOEDamageMult = ItemBase<ExeBlade>.instance.baseDamageAOEExe.Value;

            //var tempEffect = EntityStates.ParentPod.DeathState.deathEffect;

            for (int i = 0; i < (bladeCount * ItemBase<ExeBlade>.instance.additionalProcs.Value); i++) {
                if (attackerBody)
                {
                    yield return new WaitForSeconds(ItemBase<ExeBlade>.instance.deathDelay.Value);
                    DamageInfo damageInfoDeath = new DamageInfo
                    {
                        attacker = attackerBody.gameObject,
                        crit = attackerBody.RollCrit(),
                        damage = 1,
                        position = bladeObject.transform.position,
                        procCoefficient = 1,
                        damageType = DamageType.AOE,
                        damageColorIndex = DamageColorIndex.Default,
                    };
                    DamageReport damageReport = new DamageReport(damageInfoDeath, bladeObjHPC, damage, cmbHP);
                    GlobalEventManager.instance.OnCharacterDeath(damageReport);

                    EffectData effectDataPulse = new EffectData
                    {
                        origin = bladeObject.transform.position
                    };
                    effectDataPulse.SetNetworkedObjectReference(bladeObject);
                    //var bisonEffect = EntityStates.Bison.SpawnState.spawnEffectPrefab;

                    EffectManager.SpawnEffect(HealthComponent.AssetReferences.executeEffectPrefab, effectDataPulse, true);
                    //EntityStates.BeetleGuardMonster.GroundSlam.slamEffectPrefab
                    float AOEDamage = dmgReport.attackerBody.damage * AOEDamageMult;
                    Vector3 corePosition = bladeObject.transform.position;

                    exeBladeSphereSearch.origin = corePosition;
                    exeBladeSphereSearch.mask = LayerIndex.entityPrecise.mask;
                    exeBladeSphereSearch.radius = effectiveRadius;
                    exeBladeSphereSearch.RefreshCandidates();
                    exeBladeSphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(dmgReport.attackerBody.teamComponent.teamIndex));
                    exeBladeSphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                    exeBladeSphereSearch.OrderCandidatesByDistance();
                    exeBladeSphereSearch.GetHurtBoxes(exeBladeHurtBoxBuffer);
                    exeBladeSphereSearch.ClearCandidates();

                    for (int j = 0; j < exeBladeHurtBoxBuffer.Count; j++)
                    {
                        HurtBox hurtBox = exeBladeHurtBoxBuffer[j];
                        if (hurtBox.healthComponent && hurtBox.healthComponent.body && hurtBox.healthComponent != bladeObjHPC)
                        {
                            DamageInfo damageInfoAOE = new DamageInfo
                            {
                                attacker = attackerBody.gameObject,
                                crit = attackerBody.RollCrit(),
                                damage = AOEDamage,
                                position = corePosition,
                                procCoefficient = 1,
                                damageType = DamageType.AOE,
                                damageColorIndex = DamageColorIndex.Item,
                            };
                            hurtBox.healthComponent.TakeDamage(damageInfoAOE);
                        }
                    }
                    exeBladeHurtBoxBuffer.Clear();
                }
            }

            yield return new WaitForSeconds(ItemBase<ExeBlade>.instance.additionalDuration.Value);
            EffectData effectData = new EffectData
            {
                origin = bladeObject.transform.position
            };
            effectData.SetNetworkedObjectReference(bladeObject); //pulverizedEffectPrefab
            EffectManager.SpawnEffect(HealthComponent.AssetReferences.permanentDebuffEffectPrefab, effectData, transmit: true);

            Destroy(bladeObject);
        }

        public class ExeToken : MonoBehaviour
        {
            //prevents hilarity from happening
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
            //teleporterName = self.teleporterSpawnCard.ToString();
            //Debug.Log("checking for primoridal: " + self.teleporterSpawnCard.ToString());
            ////isPrimoridal = false;
            //teleporterPos = self.teleporterInstance.transform.position;
            //if (self.teleporterSpawnCard == LegacyResourcesAPI.Load<InteractableSpawnCard>("spawncards/interactablespawncard/iscLunarTeleporter"))
            //{
            //    Debug.Log("yo it is");
            //    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
            //    teleporterPos += celestialAdjust;
            //    //isPrimoridal = true;
            //}
            //Debug.Log("checked for primoridal");
            string sceneName = SceneCatalog.GetSceneDefForCurrentScene().baseSceneName;
            if (sceneName != "arena" && sceneName != "moon2" && sceneName != "voidstage" && sceneName != "voidraid" && sceneName != "artifactworld" && sceneName != "bazaar" && sceneName != "goldshores" && sceneName != "limbo" && sceneName != "mysteryspace" && sceneName != "itancientloft" && sceneName != "itdampcave" && sceneName != "itfrozenwall" && sceneName != "itgolemplains" && sceneName != "itgoolake" && sceneName != "itmoon" && sceneName != "itskymeadow")
            {
                voidfields = false;
                StartCoroutine(LotusDelayedPlacement(self));
            }else if(sceneName == "arena")
            {
                voidfields = true;
            }
            orig(self);
        }

        IEnumerator LotusDelayedPlacement(SceneDirector self)
        {
            yield return new WaitForSeconds(4f);
            //if (SceneCatalog.GetSceneDefForCurrentScene().baseSceneName == "skymeadow")
            //{
            //    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
            //    teleporterPos += celestialAdjust;
            //}
            teleporterName = self.teleporterSpawnCard.ToString();
            //Debug.Log("checking for primoridal: " + self.teleporterSpawnCard.ToString());

            lotusSpawned = false;
            teleporterPos = self.teleporterInstance.transform.position;
            //if (obj.teleporterSpawnCard == LegacyResourcesAPI.Load<InteractableSpawnCard>("spawncards/interactablespawncard/iscLunarTeleporter"))
            //{
            //    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
            //    teleporterPos += celestialAdjust;
            //}
            //if (isPrimoridal)
            //{
            //    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
            //    teleporterPos += celestialAdjust;
            //    Debug.Log("recognized it is");
            //}
            //Debug.Log("teleporter pos: " + teleporterPos);
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
                //Debug.Log(SceneCatalog.GetSceneDefForCurrentScene().baseSceneName);
                if (teleporterName.Contains("iscLunarTeleporter"))
                {
                    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
                    teleporterPos += celestialAdjust;
                }

                //if (SceneCatalog.GetSceneDefForCurrentScene().baseSceneName == "skymeadow")
                //{
                //    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
                //    teleporterPos += celestialAdjust;
                //}


                Quaternion rot = Quaternion.Euler(1.52666613f, 180, 9.999999f);
                var tempLotus = Instantiate(lotusObject, teleporterPos, rot);
                tempLotus.GetComponent<TeamFilter>().teamIndex = teamDex;
                tempLotus.transform.position = teleporterPos + heightAdjust;
                NetworkServer.Spawn(tempLotus);
                tempLotusObject = tempLotus;
                lotusSpawned = true;

                EffectData effectData = new EffectData
                {
                    origin = tempLotus.transform.position
                };
                effectData.SetNetworkedObjectReference(tempLotus.gameObject);
                EffectManager.SpawnEffect(HealthComponent.AssetReferences.crowbarImpactEffectPrefab, effectData, transmit: true);
            }
            //Debug.Log("checking prevfrac: " + previousPulseFraction);
            previousPulseFraction = 0;
            //Debug.Log("fixed prevfrac: " + previousPulseFraction);
        }

        private void AddLotusOnEnter(SceneDirector obj)
        {
            lotusSpawned = false;
            teleporterPos = obj.teleporterInstance.transform.position;
            previousPulseFraction = 0;
            //if (obj.teleporterSpawnCard == LegacyResourcesAPI.Load<InteractableSpawnCard>("spawncards/interactablespawncard/iscLunarTeleporter"))
            //{
            //    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
            //    teleporterPos += celestialAdjust;
            //}
            //if (isPrimoridal)
            //{
            //    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
            //    teleporterPos += celestialAdjust;
            //    Debug.Log("recognized it is");
            //}
            //Debug.Log("teleporter pos: " + teleporterPos);
            int itemCount = 0;
            TeamIndex teamDex = default;
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                itemCount += player.master.inventory.GetItemCount(ItemBase<CrystalLotus>.instance.ItemDef);
                teamDex = player.master.teamIndex;
            }
            if (itemCount > 0)
            {
                teleporterPos = obj.teleporterInstance.transform.position;
                //Debug.Log(SceneCatalog.GetSceneDefForCurrentScene().baseSceneName);
                if (teleporterName.Contains("iscLunarTeleporter"))
                {
                    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
                    teleporterPos += celestialAdjust;
                }

                //if (SceneCatalog.GetSceneDefForCurrentScene().baseSceneName == "skymeadow")
                //{
                //    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
                //    teleporterPos += celestialAdjust;
                //}

                string sceneName = SceneCatalog.GetSceneDefForCurrentScene().baseSceneName;
                if (sceneName != "arena " && sceneName != "moon2" && sceneName != "voidstage" && sceneName != "voidraid" && sceneName != "artifactworld" && sceneName != "bazaar" && sceneName != "goldshores" && sceneName != "limbo" && sceneName != "mysteryspace" && sceneName != "itancientloft" && sceneName != "itdampcave" && sceneName != "itfrozenwall" && sceneName != "itgolemplains" && sceneName != "itgoolake" && sceneName != "itmoon" && sceneName != "itskymeadow")
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
                }else if(sceneName == "arena")
                {
                    voidfields = true;
                }
            }

        }

        private void AddLotusOnPickup(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
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
                    //if (SceneCatalog.GetSceneDefForCurrentScene().baseSceneName == "skymeadow")
                    //{
                    //    Vector3 celestialAdjust = new Vector3(0, -.65f, 0);
                    //    teleporterPos += celestialAdjust;
                    //}
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
                    }else if(sceneName == "arena")
                    {
                        voidfields = true;
                    }
                }
            }
            orig(self);
        }

        //Vector3 heightAdjust = new Vector3(0, 1.5f, 0);
        //float previousPulseFraction = 0;
        //float secondsUntilBarrierAttempt = 0;
        //LotusColliderToken lotusTimerToken;


        //private void LotusSlowNova(On.RoR2.HoldoutZoneController.orig_FixedUpdate orig, HoldoutZoneController self)
        //{
        //    throw new NotImplementedException();
        //}


        private void CrystalLotusNova(On.RoR2.HoldoutZoneController.orig_UpdateHealingNovas orig, HoldoutZoneController self, bool isCharging)
        {
            //Debug.Log("i am happening " + self.charge);
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
                //Debug.Log(lotusTimer);
                slowCoeffValue = speedCurve.Evaluate(lotusTimer / LotusDuration.Value);
                //Debug.Log("coeff: " + slowCoeffValue + " | formula: " + slowCoeffValue * (Time.fixedDeltaTime / 10));
            }
            else
            {
                slowCoeffValue = 1;

                //if (tempLotusCollider && self.charge >= 1)
                //{
                //    var tempward = tempLotusCollider.GetComponent<BuffWard>();
                //    tempward.enabled = false;
                //    Destroy(tempLotusCollider);
                //}
                //ward.enabled = false;
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
                        //var spcl = tempLotusCollider.GetComponent<SphereCollider>();
                        //Debug.Log(self.radiusSmoothTime);
                        //spcl.radius = self.radiusSmoothTime;
                        //var wardtemp = tempLotusCollider.GetComponent<BuffWard>();

                    }
                    else
                    {
                        Vector3 holdoutpos = self.gameObject.transform.position;
                        tempLotusCollider.transform.position = holdoutpos;
                    }
                    //slowCoeffValue += Time.deltaTime;

                    var spcl = tempLotusCollider.GetComponent<SphereCollider>();

                    spcl.radius = self.currentRadius;
                    //var token = tempLotusCollider.GetComponent<LotusColliderToken>();
                    //token.teamIndex = teamDex;

                    var ward = tempLotusCollider.GetComponent<BuffWard>();
                    ward.radius = self.currentRadius;
                    //ward.buffDef = CryoCanister.instance.preFreezeSlow;
                    //ward.invertTeamFilter = true;
                    //ward.buffDuration = 1f;

                    //TeamFilter filter = tempLotusCollider.GetComponent<TeamFilter>();
                    var comp = tempLotusCollider.GetComponent<SlowDownProjectiles>();
                    comp.slowDownCoefficient = slowCoeffValue;
                    //comp.teamFilter = filter;

                    //Debug.Log("team filter: " + comp.teamFilter.teamIndex);

                    //if (slowCoeffValue == 1)
                    //{
                    //    ward.enabled = false;
                    //}

                    //comp.slowDownCoefficient = 1;
                    //var teamComp = tempLotusCollider.GetComponent<TeamComponent>();
                    //Debug.Log("team dex: " + teamDex + " | rad: " + self.currentRadius);

                    if (secondsUntilBarrierAttempt > 0f)
                    {
                        //Debug.Log("waiting");
                        secondsUntilBarrierAttempt -= Time.fixedDeltaTime;
                    }
                    else
                    {
                        //string nova2 = "RoR2/Base/TPHealingNova/TeleporterHealNovaPulse.prefab";
                        //GameObject novaPrefab2 = Addressables.LoadAssetAsync<GameObject>(nova2).WaitForCompletion();
                        //novaPrefab2.GetComponent<TeamFilter>().teamIndex = teamDex;
                        //NetworkServer.Spawn(novaPrefab2);

                        //Quaternion Upwards = Quaternion.Euler(270, 0, 0);
                        //GameObject pulsePrefab = UnityEngine.Object.Instantiate<GameObject>(TeleporterHealNovaGeneratorMain.pulsePrefab, teleporterPos + heightAdjustPulse, Upwards, base.transform.parent);
                        //pulsePrefab.GetComponent<TeamFilter>().teamIndex = teamDex;
                        //pulsePrefab.active = false;
                        //pulsePrefab.InstantiateClone("fortnite");
                        //
                        //NetworkServer.Spawn(pulsePrefab);

                        //if(self.charge < (1f / (float)(itemCount + 1)) && previousPulseFraction > 1)
                        //{
                        //    Debug.Log("fixing dumb jank " + previousPulseFraction);
                        //    previousPulseFraction = 0; //jank fix?
                        //    //Debug.Log("fixing dumb jank " + previousPulseFraction);
                        //}
                        //Debug.Log("previous was: " + previousPulseFraction);

                        if (currentCharge > self.charge)
                        {
                            previousPulseFraction = 0;
                            currentCharge = self.charge;
                        }

                        float nextPulseFraction = CalcNextPulseFraction(itemCount * (int)ItemBase<CrystalLotus>.instance.pulseCountStacking.Value, previousPulseFraction);
                        currentCharge = self.charge;

                        //Debug.Log("waiting for " + nextPulseFraction + " | we are at " + currentCharge);
                        if (nextPulseFraction <= currentCharge)
                        {
                            if (LotusVariant.Value == 1)
                            {
                                //Quaternion Upwards = Quaternion.Euler(270, 0, 0);
                                string nova = "RoR2/Base/TPHealingNova/TeleporterHealNovaPulse.prefab";
                                GameObject novaPrefab = Addressables.LoadAssetAsync<GameObject>(nova).WaitForCompletion();
                                novaPrefab.GetComponent<TeamFilter>().teamIndex = teamDex;
                                NetworkServer.Spawn(novaPrefab);
                                StartCoroutine(LotusDelayedBarrier(self, teamDex));
                            }
                            else
                            {
                                //slowCoeffValue = .1f;
                                ward.enabled = true;

                                lotusTimer = 0;

                                slowCoeffValue = speedCurve.Evaluate(lotusTimer / LotusDuration.Value);

                                //if (tempLotusCollider)
                                //{
                                //    lotusTimerToken = tempLotusCollider.GetComponent<LotusColliderToken>();
                                //    if (lotusTimerToken)
                                //    {
                                //        lotusTimerToken.Begin();
                                //        
                                //    }
                                //}
                                //for (int i = 0; i < bodyTokens.Count; i++)
                                //{
                                //    bodyTokens[1].End();
                                //    Destroy(bodyTokens[1]);
                                //    bodyTokens.RemoveAt(1);
                                //}

                                //lotusSlowMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matSlow80Debuff.mat").WaitForCompletion();
                                //lotusSlowMaterial.mainTexture = MainAssets.LoadAsset<Texture>("texRampIce2.png");

                                //string effect1 = "RoR2/DLC1/VoidRaidCrab/LaserImpactEffect.prefab"; //"RoR2/DLC1/VoidRaidCrab/LaserImpactEffect.prefab";
                                //GameObject effect1Prefab = Addressables.LoadAssetAsync<GameObject>(effect1).WaitForCompletion();
                                ////var ef1efc = effect1Prefab.GetComponent<EffectComponent>();
                                //EffectManager.SimpleImpactEffect(effect1Prefab, teleporterPos + heightAdjustPulse, new Vector3(0, 0, 0), true);

                                StartCoroutine(Lotus2ExplosionThing(self.gameObject));

                            }

                            //Quaternion Upwards = Quaternion.Euler(270, 0, 0);
                            //string nova2 = "RoR2/Base/TPHealingNova/TeleporterHealNovaPulse.prefab";
                            //GameObject novaPrefab2 = Addressables.LoadAssetAsync<GameObject>(nova2).WaitForCompletion();
                            //novaPrefab2.GetComponent<TeamFilter>().teamIndex = teamDex;
                            //NetworkServer.Spawn(novaPrefab2);

                            ////novaPrefab.GetComponentInChildren<ParticleSystem>();
                            ////var system = novaPrefab.GetComponentInChildren<ParticleSystem>();
                            //List<GameObject> childrens = new List<GameObject>();
                            //int count = 0;
                            //while (count < gameObject.transform.childCount)
                            //{
                            //    childrens.Add(gameObject.transform.GetChild(count).gameObject);
                            //    Debug.Log(childrens[count].name);
                            //    var system = childrens[count].GetComponent<ParticleSystem>();
                            //    if (system)
                            //    {
                            //        Debug.Log(system.startColor);
                            //        system.startColor = new Color(549f, 0.090f, 0.906f, system.startColor.a);
                            //    }
                            //    count++;
                            //}
                            //
                            ////system.startColor = new Color(549f, 0.090f, 0.906f, 1);
                            //
                            //
                            //NetworkServer.Spawn(novaPrefab);
                            previousPulseFraction = nextPulseFraction;
                            secondsUntilBarrierAttempt = 1f;
                            //Debug.Log("holy shit!!!!!!!!!!!!!");

                            //StartCoroutine(LotusDelayedBarrier(self, teamDex));

                            //slowCoeffValue = .25f;
                            //ward.enabled = true;
                            //
                            ////lotusSlowMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/matSlow80Debuff.mat").WaitForCompletion();
                            ////lotusSlowMaterial.mainTexture = MainAssets.LoadAsset<Texture>("texRampIce2.png");
                            //
                            //string effect1 = "RoR2/DLC1/VoidRaidCrab/LaserImpactEffect.prefab"; //"RoR2/DLC1/VoidRaidCrab/LaserImpactEffect.prefab";
                            //GameObject effect1Prefab = Addressables.LoadAssetAsync<GameObject>(effect1).WaitForCompletion();
                            //var ef1efc = effect1Prefab.GetComponent<EffectComponent>();
                            //EffectManager.SimpleImpactEffect(effect1Prefab, teleporterPos + heightAdjustPulse, new Vector3(0, 0, 0), true);

                            string effect2 = "RoR2/DLC1/VoidSuppressor/SuppressorClapEffect.prefab";
                            GameObject effect2Prefab = Addressables.LoadAssetAsync<GameObject>(effect2).WaitForCompletion();
                            var ef2efc = effect2Prefab.GetComponent<EffectComponent>();
                            ef2efc.applyScale = true;
                            ef2efc.referencedObject = effect2Prefab;
                            effect2Prefab.transform.localScale *= 4;
                            EffectManager.SimpleImpactEffect(effect2Prefab, teleporterPos, new Vector3(0, 0, 0), true);

                            //foreach (var player in PlayerCharacterMasterController.instances)
                            //{
                            //    if (self.IsBodyInChargingRadius(player.body) && player.body.teamComponent.teamIndex == teamDex)
                            //    {
                            //        //var playerHealthComp = player.GetComponent<HealthComponent>();
                            //        //player.body.healthComponent;
                            //        if (player.body.healthComponent)
                            //        {
                            //            StartCoroutine(LotusDelayedBarrier(self, teamDex));
                            //            //Debug.Log("yoo health component!!");
                            //            //player.body.healthComponent.AddBarrier(player.body.healthComponent.health * ItemBase<BarrierLotus>.instance.barrierAmount.Value); //25% 
                            //        }
                            //        else
                            //        {
                            //            //Debug.Log("no suitable health component.");
                            //        }
                            //
                            //    }
                            //}
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
            if(self.GetBuffCount(lotusSlow) > 0 && slowCoeffValue < 1)
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
            else if(slowCoeffValue >= 1)
            {
                int count = self.GetBuffCount(lotusSlow);
                for(int i = 0; i < count; i++)
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
            //else
            //{
            //    //var token = self.gameObject.GetComponent<LotusBodyToken>();
            //    //if (token)
            //    //{
            //    //    token.End();
            //    //    Destroy(token);
            //    //}
            //}
        }

        //private void LotusSlowVisuals(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        //{
        //    orig(self);
        //    var component = self.gameObject.GetComponent<LotusBodyToken>();
        //    if (self.GetBuffCount(BarrierLotus.instance.lotusSlow) > 0)
        //    {
        //        //var component = self.gameObject.GetComponent<LotusBodyToken>();
        //        if (!component)
        //        {
        //            component = self.gameObject.AddComponent<LotusBodyToken>();
        //            component.body = self;
        //            component.material = lotusSlowMaterial;
        //            component.Begin();
        //        }
        //    }
        //    else
        //    {
        //        if (component)
        //        {
        //            Destroy(component);
        //        }
        //    }
        //
        //  }


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
                matInstance.SetFloat("_Boost", matInstance.GetFloat("_Boost") - (coeff*2));
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
        }

        public void ApplyTierHighlights()
        {
            if (!hasAdjustedTiers)
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
            //Debug.Log("dlc enabled, obviously");
            //Debug.Log(obj.sceneDef + " " + obj.sceneDef.cachedName + " ");
            //Debug.Log("item: " + ClockworkMechanism.instance.ItemLangTokenName);

            ApplyTierHighlights();

            if (obj.sceneDef == SceneCatalog.GetSceneDefFromSceneName("voidstage") && locusExit.Value)
            {
                //Debug.Log("attempting");
            
                GameObject portal = UnityEngine.Object.Instantiate<GameObject>(portalObject, new Vector3(-37.5f, 19.5f, -284.05f), new Quaternion(0, 70, 0, 0));
                NetworkServer.Spawn(portal);
                GameObject platform = UnityEngine.Object.Instantiate<GameObject>(platformObject, new Vector3(-37.5f, 15.25f, -273.5f), new Quaternion(0, 0, 0, 0));
                NetworkServer.Spawn(platform);
                
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

        //private void ClockworkItemDrops(On.RoR2.Stage.orig_RespawnCharacter orig, Stage self, CharacterMaster characterMaster)
        //{
        //    orig(self, characterMaster);
        //
        //    if (ItemBase<ClockworkMechanism>.instance.itemVariant.Value == 0)
        //    {
        //        //int itemCount = 0;
        //        foreach (var player in PlayerCharacterMasterController.instances)
        //        {
        //            int itemCount = player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
        //            if (itemCount > 0)
        //            {
        //                int rewardCount = ItemBase<ClockworkMechanism>.instance.itemsPerStage.Value + (ItemBase<ClockworkMechanism>.instance.itemsPerStageStacking.Value * (itemCount - 1));
        //                ClockworkDelayedItemDrops(rewardCount, player.gameObject.transform.position);
        //            }
        //            //if (itemCount > 0)
        //            //{
        //            //    int rewardCount = ItemBase<ClockworkMechanism>.instance.itemsPerStage.Value + (ItemBase<ClockworkMechanism>.instance.itemsPerStageStacking.Value * (itemCount - 1));
        //            //    for (int i = 0; i < rewardCount; i++)
        //            //    {
        //            //        if (ItemBase<ClockworkMechanism>.instance.watchVoidRng == null)
        //            //        {
        //            //            ItemBase<ClockworkMechanism>.instance.watchVoidRng = new Xoroshiro128Plus(Run.instance.seed);
        //            //        }
        //            //
        //            //        //ItemIndex itemResult = ItemIndex.None;
        //            //        PickupIndex pickupResult = PickupIndex.none;
        //            //        int randInt = ItemBase<ClockworkMechanism>.instance.watchVoidRng.RangeInt(1, 100); // 1-79 white // 80-99 green // 100 red
        //            //        if (randInt < 80)
        //            //        {
        //            //            List<PickupIndex> whiteList = new List<PickupIndex>(Run.instance.availableTier1DropList);
        //            //            Util.ShuffleList(whiteList, ItemBase<ClockworkMechanism>.instance.watchVoidRng);
        //            //            //itemResult = whiteList[0].itemIndex;
        //            //            pickupResult = whiteList[0];
        //            //            Debug.Log("selected a white");
        //            //        }
        //            //        else if (randInt < 100)
        //            //        {
        //            //            List<PickupIndex> greenList = new List<PickupIndex>(Run.instance.availableTier1DropList);
        //            //            Util.ShuffleList(greenList, ItemBase<ClockworkMechanism>.instance.watchVoidRng);
        //            //            //itemResult = greenList[0].itemIndex;
        //            //            pickupResult = greenList[0];
        //            //            Debug.Log("selected a green");
        //            //        }
        //            //        else
        //            //        {
        //            //            List<PickupIndex> redList = new List<PickupIndex>(Run.instance.availableTier1DropList);
        //            //            Util.ShuffleList(redList, ItemBase<ClockworkMechanism>.instance.watchVoidRng);
        //            //            //itemResult = redList[0].itemIndex;
        //            //            pickupResult = redList[0];
        //            //            Debug.Log("selected a red");
        //            //        }
        //            //        //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
        //            //        float num = 360f / (float)rewardCount;
        //            //        Vector3 a = Quaternion.AngleAxis(num * (float)i, Vector3.up) * Vector3.forward;
        //            //        Vector3 position = player.gameObject.transform.position + a * 4f + Vector3.up * 8f;
        //            //        Debug.Log("spawned it at " + position);
        //            //        PickupDropletController.CreatePickupDroplet(pickupResult, position, Vector3.zero);
        //            //        Debug.Log("spawned it");
        //            //
        //            //    }
        //            //}
        //        }
        //    }
        //}

        //IEnumerator ClockworkDelayedItemDrops(int rewardCount, Vector3 playerPos)
        //{
        //    //int rewardCount = ItemBase<ClockworkMechanism>.instance.itemsPerStage.Value + (ItemBase<ClockworkMechanism>.instance.itemsPerStageStacking.Value * (itemCount - 1));
        //    for (int i = 0; i < rewardCount; i++)
        //    {
        //        yield return new WaitForSeconds(.05f);
        //        if (ItemBase<ClockworkMechanism>.instance.watchVoidRng == null)
        //        {
        //            ItemBase<ClockworkMechanism>.instance.watchVoidRng = new Xoroshiro128Plus(Run.instance.seed);
        //        }
        //
        //        //ItemIndex itemResult = ItemIndex.None;
        //        PickupIndex pickupResult = PickupIndex.none;
        //        int randInt = ItemBase<ClockworkMechanism>.instance.watchVoidRng.RangeInt(1, 100); // 1-79 white // 80-99 green // 100 red
        //        if (randInt < 80)
        //        {
        //            List<PickupIndex> whiteList = new List<PickupIndex>(Run.instance.availableTier1DropList);
        //            Util.ShuffleList(whiteList, ItemBase<ClockworkMechanism>.instance.watchVoidRng);
        //            //itemResult = whiteList[0].itemIndex;
        //            pickupResult = whiteList[0];
        //            Debug.Log("selected a white");
        //        }
        //        else if (randInt < 100)
        //        {
        //            List<PickupIndex> greenList = new List<PickupIndex>(Run.instance.availableTier1DropList);
        //            Util.ShuffleList(greenList, ItemBase<ClockworkMechanism>.instance.watchVoidRng);
        //            //itemResult = greenList[0].itemIndex;
        //            pickupResult = greenList[0];
        //            Debug.Log("selected a green");
        //        }
        //        else
        //        {
        //            List<PickupIndex> redList = new List<PickupIndex>(Run.instance.availableTier1DropList);
        //            Util.ShuffleList(redList, ItemBase<ClockworkMechanism>.instance.watchVoidRng);
        //            //itemResult = redList[0].itemIndex;
        //            pickupResult = redList[0];
        //            Debug.Log("selected a red");
        //        }
        //        //player.master.inventory.RemoveItem(ItemBase<ClockworkMechanism>.instance.ItemDef, tempItemCount);
        //        float num = 360f / (float)rewardCount;
        //        Vector3 a = Quaternion.AngleAxis(num * (float)i, Vector3.up) * Vector3.forward;
        //        Vector3 position = playerPos + a * 4f + Vector3.up * 8f;
        //        Debug.Log("spawned it at " + position);
        //        PickupDropletController.CreatePickupDroplet(pickupResult, position, Vector3.zero);
        //        Debug.Log("spawned it");
        //
        //    }
        //}

        public void Swapallshaders(AssetBundle bundle)
        {
            Material[] allMaterials = bundle.LoadAllAssets<Material>();
            foreach (Material mat in allMaterials)
            {
                switch (mat.shader.name)
                {
                    case "Stubbed Hopoo Games/Deferred/Standard":
                        mat.shader = Resources.Load<Shader>("shaders/deferred/hgstandard");
                        break;
                    case "Stubbed Hopoo Games/Deferred/Snow Topped":
                        mat.shader = Resources.Load<Shader>("shaders/deferred/hgsnowtopped");
                        break;
                    case "Stubbed Hopoo Games/FX/Cloud Remap":
                        mat.shader = Resources.Load<Shader>("shaders/fx/hgcloudremap");
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
                    default:
                        break;
                }

            }
        }
    }


}