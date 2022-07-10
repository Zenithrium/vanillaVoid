using BepInEx;
using R2API;
using R2API.Utils;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Collections;
using vanillaVoid.Artifact;
using vanillaVoid.Equipment;
using vanillaVoid.Equipment.EliteEquipment;
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
using vanillaVoid.Misc;
using EntityStates.TeleporterHealNovaController;

namespace vanillaVoid
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(EliteAPI), nameof(RecalculateStatsAPI), nameof(PrefabAPI), nameof(DotAPI), nameof(LegacyResourcesAPI))]

    [BepInDependency(VoidItemAPI.VoidItemAPI.MODGUID)]

    public class vanillaVoidPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.Zenithrium.vanillaVoid";
        public const string ModName = "vanillaVoid";
        public const string ModVer = "1.1.8";

        public static ExpansionDef sotvDLC; 

        public static AssetBundle MainAssets;

        public List<ArtifactBase> Artifacts = new List<ArtifactBase>();
        public List<ItemBase> Items = new List<ItemBase>();
        public List<EquipmentBase> Equipments = new List<EquipmentBase>();
        public List<EliteEquipmentBase> EliteEquipments = new List<EliteEquipmentBase>();

        //Provides a direct access to this plugin's logger for use in any of your other classes.
        public static BepInEx.Logging.ManualLogSource ModLogger;

        public static GameObject bladeObject;

        public static GameObject lotusObject;
        public static GameObject lotusPulse;

        Vector3 heightAdjust = new Vector3(0, 2.312f, 0);
        Vector3 heightAdjustPulse = new Vector3(0, 2.5f, 0);
        float previousPulseFraction = 0;
        float secondsUntilBarrierAttempt = 0;
        private void Awake()
        {
            ModLogger = Logger;

            var harm = new Harmony(Info.Metadata.GUID);
            new PatchClassProcessor(harm, typeof(ModdedDamageColors)).Patch();

            IL.RoR2.HealthComponent.TakeDamage += (il) => //LensOrrery and lost seer's interaction 
            {
                ILCursor c = new ILCursor(il);
                if(c.TryGotoNext(
                    x => x.MatchCallOrCallvirt<CharacterBody>("get_inventory"),
                    x => x.MatchLdsfld(typeof(DLC1Content.Items), "CritGlassesVoid"),
                    x => x.MatchCallOrCallvirt<Inventory>("GetItemCount"),
                    x => x.MatchConvR4(),
                    x => x.MatchLdcR4(0.5f)
                    ))
                {
                    c.Index += 4;
                    c.Remove();
                    c.Emit(OpCodes.Ldloc_1);
                    //c.TryGotoNext();
                    c.EmitDelegate<Func<CharacterBody, float>>((cb) =>
                    {
                        if (cb.master.inventory)
                        {
                            int orrery = cb.master.inventory.GetItemCount(ItemBase<LensOrrery>.instance.ItemDef);
                            if (orrery > 0)
                            {
                                return 0.5f + (.5f * (LensOrrery.newLensBonus.Value + (LensOrrery.newStackingLensBonus.Value * (orrery - 1))));
                            }
                        }
                        return 0.5f;
                    });
                }
                else
                {
                    Logger.LogError("Failed to apply Lost Seer's Orrery Hook");
                }
            };


            //IL.RoR2.HealthComponent.TakeDamage += (il) => // just stop doing crit checks if you have orrery because its being handled in LensOrrery.cs 
            //{
            //    ILCursor c = new ILCursor(il);
            //    ILLabel label = null;
            //    c.GotoNext(MoveType.After,
            //        x => x.MatchLdarg(1),
            //        x => x.MatchLdfld<DamageInfo>("crit"),
            //        x => x.MatchBrfalse(out label)
            //        );
            //    c.Emit(OpCodes.Ldloc_1);
            //    c.EmitDelegate<Func<CharacterBody, int>>((cb) => { return cb.master.inventory.GetItemCount(ItemBase<LensOrrery>.instance.ItemDef); });
            //    c.Emit(OpCodes.Brtrue, label);
            //};


            sotvDLC = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.nameToken == "DLC1_NAME");  //learn what sotv is 

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("vanillaVoid.vanillavoidassets"))
            {
                MainAssets = AssetBundle.LoadFromStream(stream);
            }
            
            //var swordClass = new ExeSwordObject();
            //swordClass.Initalize();
            
            On.RoR2.CharacterBody.OnSkillActivated += ExtExhaustFireProjectile;

            GlobalEventManager.onCharacterDeathGlobal += ExeBladeExtraDeath;

            //RoR2.SceneDirector.onPostPopulateSceneServer += AddLotusOnEnter;
            On.RoR2.CharacterBody.OnInventoryChanged += AddLotusOnPickup;
            On.RoR2.HoldoutZoneController.UpdateHealingNovas += BarrierLotusNova;

            On.RoR2.SceneDirector.PlaceTeleporter += PrimoridalTeleporterCheck;
            //On.RoR2.Stage.RespawnCharacter += ClockworkItemDrops;

            //if(itemVariant.Value == 0 || itemVariant.Value == 1)){
            //    On.RoR2.CharacterBody.OnInventoryChanged += AddWatchTokenOnPickup;
            //}
            //On.RoR2.CharacterBody.OnInventoryChanged += AddWatchTokenOnPickup;
            //ExeBladeCreateProjectile();


            //PrefabAPI.InstantiateClone(bladeObject, "exeBladePrefab", true);
            bladeObject = MainAssets.LoadAsset<GameObject>("mdlBladeWorldObject.prefab"); 
            bladeObject.AddComponent<TeamFilter>();
            bladeObject.AddComponent<HealthComponent>();
            bladeObject.AddComponent<NetworkIdentity>();
            bladeObject.AddComponent<BoxCollider>();
            bladeObject.AddComponent<Rigidbody>();


            lotusObject = MainAssets.LoadAsset<GameObject>("mdlLotusWorldObject.prefab"); 
            lotusObject.AddComponent<TeamFilter>();
            lotusObject.AddComponent<NetworkIdentity>();

            //bladeObject.AddComponent<Rigidbody>();
            PrefabAPI.RegisterNetworkPrefab(bladeObject);
            PrefabAPI.RegisterNetworkPrefab(lotusObject);
            //R2API.ContentAddition.AddNetworkedObject(bladeObject);
            //R2API.ContentAddition.AddNetworkedObject(lotusObject);

            // Don't know how to create/use an asset bundle, or don't have a unity project set up?
            // Look here for info on how to set these up: https://github.com/KomradeSpectre/AetheriumMod/blob/rewrite-master/Tutorials/Item%20Mod%20Creation.md#unity-project

            //This section automatically scans the project for all artifacts
            var ArtifactTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ArtifactBase)));

            foreach (var artifactType in ArtifactTypes)
            {
                ArtifactBase artifact = (ArtifactBase)Activator.CreateInstance(artifactType);
                if (ValidateArtifact(artifact, Artifacts))
                {
                    artifact.Init(Config);
                }
            }

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

            //this section automatically scans the project for all elite equipment
            var EliteEquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EliteEquipmentBase)));

            foreach (var eliteEquipmentType in EliteEquipmentTypes)
            {
                EliteEquipmentBase eliteEquipment = (EliteEquipmentBase)System.Activator.CreateInstance(eliteEquipmentType);
                if (ValidateEliteEquipment(eliteEquipment, EliteEquipments))
                {
                    eliteEquipment.Init(Config);

                }
            }

        }

        /// <summary>
        /// A helper to easily set up and initialize an artifact from your artifact classes if the user has it enabled in their configuration files.
        /// </summary>
        /// <param name="artifact">A new instance of an ArtifactBase class."</param>
        /// <param name="artifactList">The list you would like to add this to if it passes the config check.</param>
        public bool ValidateArtifact(ArtifactBase artifact, List<ArtifactBase> artifactList)
        {
            var enabled = Config.Bind<bool>("Artifact: " + artifact.ArtifactName, "Enable Artifact?", true, "Should this artifact appear for selection?").Value;

            if (enabled)
            {
                artifactList.Add(artifact);
            }
            return enabled;
        }

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
            enabled = Config.Bind<bool>("Item: " + name, "Enable Item?", true, "Should this item appear in runs?").Value;
            aiBlacklist = Config.Bind<bool>("Item: " + name, "Blacklist Item from AI Use?", false, "Should the AI not be able to obtain this item?").Value;

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
        public bool ValidateEliteEquipment(EliteEquipmentBase eliteEquipment, List<EliteEquipmentBase> eliteEquipmentList)
        {
            var enabled = Config.Bind<bool>("Equipment: " + eliteEquipment.EliteEquipmentName, "Enable Elite Equipment?", true, "Should this elite equipment appear in runs? If disabled, the associated elite will not appear in runs either.").Value;

            if (enabled)
            {
                eliteEquipmentList.Add(eliteEquipment);
                return true;
            }
            return false;
        }
        private void ExtExhaustFireProjectile(On.RoR2.CharacterBody.orig_OnSkillActivated orig, RoR2.CharacterBody self, RoR2.GenericSkill skill)
        {
            var inventoryCount = self.inventory.GetItemCount(ItemBase<ExtraterrestrialExhaust>.instance.ItemDef);
            if (inventoryCount > 0 && skill.cooldownRemaining > 0) //maybe make this higher
            {
                var playerPos = self.GetComponent<CharacterBody>().corePosition;
                float skillCD = skill.baseRechargeInterval;
                
                //Debug.Log("cooldown is " + skillCD);
                int missleCount = (int)Math.Ceiling(skillCD / ItemBase<ExtraterrestrialExhaust>.instance.secondsPerRocket.Value);
                //Debug.Log("rockets firing: " + missleCount);
                
                StartCoroutine(delayedRockets(self, missleCount, inventoryCount)); //this can probably be done better
            }

            orig(self, skill);
        }

        IEnumerator delayedRockets(RoR2.CharacterBody player, int missileCount, int inventoryCount)
        {
            int icbmMod = 1;
            if(player.inventory.GetItemCount(DLC1Content.Items.MoreMissile) > 0)
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
                Debug.Log(((ItemBase<ExtraterrestrialExhaust>.instance.rocketDamage.Value + (ItemBase<ExtraterrestrialExhaust>.instance.rocketDamageStacking.Value * (inventoryCount - 1))) / 100));
                float rocketDamage = player.damage * ((ItemBase<ExtraterrestrialExhaust>.instance.rocketDamage.Value + (ItemBase<ExtraterrestrialExhaust>.instance.rocketDamageStacking.Value * (inventoryCount - 1))) / 100);
                for(int j = 0; j < icbmMod; j++)
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
                    ProjectileManager.instance.FireProjectileServer(fireProjectileInfo);
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
            if (attackerBody.inventory)
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
            //Debug.Log("hello, i am the ienumerator");
            bladeObject.AddComponent<ExeToken>(); //oopsies!!! don't break game
            
            bladeObject.AddComponent<Rigidbody>();
            var bladeRigid = bladeObject.GetComponent<Rigidbody>();
            var bladeCollider = bladeObject.GetComponent<BoxCollider>(); // default size = (0.8, 4.3, 1.8)

            bladeRigid.drag = .5f;
            //bladeRigid.mass = 10f;
            //Vector3 colliderSize = new Vector3(0.1f, 2f, 0.1f);
            float randomHeight = UnityEngine.Random.Range(2.45f, 2.95f);
            bladeCollider.size = new Vector3(0.1f, randomHeight, 0.1f);

            bladeRigid.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            //bladeRigid.isKinematic = true;

            float randomX = UnityEngine.Random.Range(-20, 10);
            float randomY = UnityEngine.Random.Range(0, 360);
            float randomZ = UnityEngine.Random.Range(-20, 20);
            Quaternion rot = Quaternion.Euler(randomX, randomY, randomZ);
            bladeObject.transform.SetPositionAndRotation(bladeObject.transform.position, rot);

            var damage = dmgReport.damageInfo.damage;
            var cmbHP = dmgReport.victim.combinedHealth;
            var bladeObjHPC = bladeObject.GetComponent<HealthComponent>();
            CharacterBody attackerBody = dmgReport.attackerBody;
            for (int i = 0; i < (bladeCount * ItemBase<ExeBlade>.instance.additionalProcs.Value); i++) {
                if (attackerBody)
                {
                    yield return new WaitForSeconds(ItemBase<ExeBlade>.instance.deathDelay.Value);
                    DamageInfo damageInfo = new DamageInfo
                    {
                        attacker = attackerBody.gameObject,
                        crit = attackerBody.RollCrit(),
                        damage = 1,
                        position = bladeObject.transform.position,
                        procCoefficient = 1,
                        damageType = DamageType.AOE,
                        damageColorIndex = DamageColorIndex.Default,
                    };
                    DamageReport damageReport = new DamageReport(damageInfo, bladeObjHPC, damage, cmbHP);
                    GlobalEventManager.instance.OnCharacterDeath(damageReport);
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
        bool lotusSpawned = false;
        bool isPrimoridal = false;
        string teleporterName = "";

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
            if (sceneName != "moon2" && sceneName != "voidstage" && sceneName != "voidraid" && sceneName != "artifactworld" && sceneName != "bazaar" && sceneName != "goldshores" && sceneName != "limbo" && sceneName != "mysteryspace" && sceneName != "itancientloft" && sceneName != "itdampcave" && sceneName != "itfrozenwall" && sceneName != "itgolemplains" && sceneName != "itgoolake" && sceneName != "itmoon" && sceneName != "itskymeadow")
            {
                StartCoroutine(LotusDelayedPlacement(self));
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
                itemCount += player.master.inventory.GetItemCount(ItemBase<BarrierLotus>.instance.ItemDef);
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


                Quaternion rot = Quaternion.Euler(0, 180, 0);
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
                itemCount += player.master.inventory.GetItemCount(ItemBase<BarrierLotus>.instance.ItemDef);
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
                if (sceneName != "moon2" && sceneName != "voidstage" && sceneName != "voidraid" && sceneName != "artifactworld" && sceneName != "bazaar" && sceneName != "goldshores" && sceneName != "limbo" && sceneName != "mysteryspace" && sceneName != "itancientloft" && sceneName != "itdampcave" && sceneName != "itfrozenwall" && sceneName != "itgolemplains" && sceneName != "itgoolake" && sceneName != "itmoon" && sceneName != "itskymeadow")
                {
                    Quaternion rot = Quaternion.Euler(0, 180, 0);
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
                    itemCount += player.master.inventory.GetItemCount(ItemBase<BarrierLotus>.instance.ItemDef);
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
                    if (sceneName != "moon2" && sceneName != "voidstage" && sceneName != "voidraid" && sceneName != "artifactworld" && sceneName != "bazaar" && sceneName != "goldshores" && sceneName != "limbo" && sceneName != "mysteryspace" && sceneName != "itancientloft" && sceneName != "itdampcave" && sceneName != "itfrozenwall" && sceneName != "itgolemplains" && sceneName != "itgoolake" && sceneName != "itmoon" && sceneName != "itskymeadow")
                    {
                        Quaternion rot = Quaternion.Euler(0, 180, 0);
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
                }
            }
            orig(self);
        }
        //Vector3 heightAdjust = new Vector3(0, 1.5f, 0);
        //float previousPulseFraction = 0;
        //float secondsUntilBarrierAttempt = 0;
        private void BarrierLotusNova(On.RoR2.HoldoutZoneController.orig_UpdateHealingNovas orig, HoldoutZoneController self, bool isCharging)
        {
            int itemCount = 0;
            TeamIndex teamDex = default;
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                itemCount += player.master.inventory.GetItemCount(ItemBase<BarrierLotus>.instance.ItemDef);
                teamDex = player.master.teamIndex;
            }

            if (itemCount > 0 && isCharging)
            {
                if(NetworkServer.active && Time.fixedDeltaTime > 0f)
                {
                    
                    if(secondsUntilBarrierAttempt > 0f)
                    {
                        //Debug.Log("waiting");
                        secondsUntilBarrierAttempt -= Time.fixedDeltaTime;
                    }
                    else
                    {
                        //if(self.charge < (1f / (float)(itemCount + 1)) && previousPulseFraction > 1)
                        //{
                        //    Debug.Log("fixing dumb jank " + previousPulseFraction);
                        //    previousPulseFraction = 0; //jank fix?
                        //    //Debug.Log("fixing dumb jank " + previousPulseFraction);
                        //}
                        //Debug.Log("previous was: " + previousPulseFraction);
                        float nextPulseFraction = CalcNextPulseFraction(itemCount * (int)ItemBase<BarrierLotus>.instance.pulseCountStacking.Value, previousPulseFraction);
                        float currentCharge = self.charge;
                        //Debug.Log("waiting for " + nextPulseFraction + " | we are at " + currentCharge);
                        if (nextPulseFraction <= currentCharge)
                        {
                            Quaternion Upwards = Quaternion.Euler(270, 0, 0);
                            GameObject pulsePrefab = UnityEngine.Object.Instantiate<GameObject>(TeleporterHealNovaGeneratorMain.pulsePrefab, teleporterPos + heightAdjustPulse, Upwards, base.transform.parent);
                            pulsePrefab.GetComponent<TeamFilter>().teamIndex = teamDex;
                            NetworkServer.Spawn(pulsePrefab);
                            previousPulseFraction = nextPulseFraction;
                            secondsUntilBarrierAttempt = 1f;
                            //Debug.Log("holy shit!!!!!!!!!!!!!");

                            StartCoroutine(LotusDelayedBarrier(self, teamDex));

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
            for(int i = 1; i <= itemCount; i++)
            {
                float temp = (float)i * healFraction;
                //Debug.Log("temp: " + temp + " | previous: " + prevPulseFraction);
                if(temp > prevPulseFraction)
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
                        player.body.healthComponent.AddBarrier(player.body.healthComponent.fullCombinedHealth * ItemBase<BarrierLotus>.instance.barrierAmount.Value); //25% 
                    }
                    else
                    {
                        //Debug.Log("no suitable health component.");
                    }

                }
            }
            //yield return new WaitForSeconds(.25f);
            //player.body.healthComponent.AddBarrier(player.body.healthComponent.health * ItemBase<BarrierLotus>.instance.barrierAmount.Value); //25% 

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
        //
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

    }


}
