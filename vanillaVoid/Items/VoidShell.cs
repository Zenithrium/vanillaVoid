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
using RoR2.ExpansionManagement;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace vanillaVoid.Items
{
    public class VoidShell : ItemBase<VoidShell>
    {
        public ConfigEntry<float> baseDamageBuff;

        public ConfigEntry<float> stackingBuff;

        //public ConfigEntry<string> voidPair;

        public override string ItemName => "Ceaseless Cornucopia";

        public override string ItemLangTokenName => "CORNUCOPIACELL_ITEM";

        public override string ItemPickupDesc => $"Recieve a special delivery each stage with powerful rewards. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"<style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"<style=cMono>Welcome to DataScraper (v3.2.02 ???「alph?a br??anch』)\n$ 『Scraping memory... don???e.\n$ Resolvingggggggggggggggggdone.】\n$ Combing for ?????         ... done.\n\n\n" +
            $"Complete!\n【Outputttt??tt:」</style>" +
            $"\n「SSBzYXcgdGhlbSBhZ2Fpbi4gVGhleSdyZSBiYWNr????LiBXZSBkaXNzZWN0ZWQgdGhlbS4gSHVud』GVkIHRoZW0gdG8gZXh0aW5jd?G??lvbiB?mb3IgdGhlaXIgc2hlbGwuIFRoZXkga】25vdy4KClRoaXMgaXMgb3Vy『IHJld2????FyZC4=」" +
            $"\n\nNotes: Th?is is unusable. Yo?u said y??ou』had made a 『brea?kthroug??h.\n" +
            $"What did 「yo??u do.」」】";

        //public string InteractableLangToken => "SHATTERED_SHRINE";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlWhorlPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("adzeIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public static GameObject PortalBattery;

        public Xoroshiro128Plus voidCellRng;

        public static InteractableSpawnCard cellInteractableCard;

        public static DirectorCard CellCard;

        public static GameObject InteractableBodyModelPrefab;

        public static GameObject voidPotentialPrefab;

        public static GameObject commandCubePrefab;

        //public static BasicPickupDropTable[] tables = new BasicPickupDropTable[4];

        BasicPickupDropTable table;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Utility, ItemTag.AIBlacklist };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);

            CreateInteractable();
            //CreateVoidCellInteractable();
            //PortalBattery = Addressables.LoadAssetAsync<GameObject>("DeepVoidPortalBattery.prefab").WaitForCompletion();
            Hooks();
        }

        public override void CreateConfig(ConfigFile config)
        {
            //baseDamageBuff = config.Bind<float>("Item: " + ItemName, "Percent Damage Increase", .4f, "Adjust the percent of extra damage dealt on the first stack.");
            //stackingBuff = config.Bind<float>("Item: " + ItemName, "Percent Damage Increase per Stack", .4f, "Adjust the percent of extra damage dealt per stack.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "FreeChest", "Adjust which item this is the void pair of.");
        }

        //public void ShellDropRewards()
        //{
        //
        //}

        public void CreateInteractable()
        {
            voidPotentialPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();

            table = new BasicPickupDropTable();
            table.voidTier1Weight = 6f;
            table.voidTier2Weight = 3f;
            table.voidTier3Weight = .75f;
            table.tier1Weight = 7.5f;
            table.tier2Weight = 4;
            table.tier3Weight = 1f;

            Vector3 zero = new Vector3(0, 0, 0);

            var tempPortalBattery = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DeepVoidPortalBattery/DeepVoidPortalBattery.prefab").WaitForCompletion();
            PortalBattery = PrefabAPI.InstantiateClone(tempPortalBattery, "VoidShellBattery");

            PortalBattery.transform.position = zero;

            var hzc = PortalBattery.GetComponent<HoldoutZoneController>();
            if (hzc)
            {
                //Debug.Log("found hzc");
                hzc.baseRadius = 25;
                hzc.baseChargeDuration = 30;
                hzc.inBoundsObjectiveToken = "OBJECTIVE_VOID_BATTERY";
                hzc.outOfBoundsObjectiveToken = "OBJECTIVE_VOID_BATTERY_OOB";
                //LanguageAPI.Add("VV_OBJECTIVE_SHELL", "Charge the Void Battery"); //look at treasure map
                //LanguageAPI.Add("VV_OBJECTIVE_SHELL_OOB");

                //hzc.onCharged.AddListener(ShellDropRewards);
                //hzc.onCharged = new HoldoutZoneController.HoldoutZoneControllerChargedUnityEvent();
                //hzc.onCharged.AddListener(zone => CompleteShellCharge(zone));
            }

            CombatSquad squad = new CombatSquad();
            //CharacterMaster.instancesList
            var jailer = MasterCatalog.FindMasterPrefab("VoidJailerBody");

            if (jailer)
            {
                Debug.Log("found jailer " + jailer + " | " + jailer.GetComponent<CharacterMaster>());
                //jailer.GetComponent<CharacterMaster>();

            }

            var barnacle = MasterCatalog.FindMasterPrefab("VoidBarnacleBody");

            if (barnacle)
            {
                Debug.Log("found barnacle " + barnacle + " | " + barnacle.GetComponent<CharacterMaster>());
            }

            var nullifier = MasterCatalog.FindMasterPrefab("NullifierBody");

            if (nullifier)
            {
                Debug.Log("found barnacle " + nullifier + " | " + nullifier.GetComponent<CharacterMaster>());
            }

            //EliteDef[] elites = EliteCatalog.eliteDefs;
            //EliteDef real = null;
            //foreach(EliteDef elite in elites){
            //    Debug.Log("elite: " + elite + " | " + elite.name + " | " + elite.eliteEquipmentDef + " | " + elite.eliteIndex);
            //    if(elite.eliteEquipmentDef == DLC1Content.Equipment.EliteVoidEquipment)
            //    {
            //        real = elite;
            //    }
            //    DLC1Content.Elites.Void
            //}

            var combatdir = PortalBattery.GetComponent<CombatDirector>();
            if (combatdir)
            {
                combatdir.monsterCredit = 300;
                //if (real != null)
                //{
                combatdir.currentActiveEliteDef = DLC1Content.Elites.Void;

                //}
                //combatdir.fallBackToStageMonsterCards = false;
                //combatdir.combatSquad = 
            }

            Transform center = PortalBattery.transform.Find("Model");
            var tempSeed = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidCamp/VoidCamp.prefab").WaitForCompletion();
            var seed = PrefabAPI.InstantiateClone(tempSeed, "voidCampFragment");

            try
            {
                Transform beam = PortalBattery.transform.Find("Model").Find("mdlVoidSignal").Find("IdleFX").Find("Beam, Strong"); //yeah
                if (beam)
                {

                    beam.transform.rotation = Quaternion.Euler(0, 0, 90);
                    beam.transform.position = new Vector3(0, 102, 0);
                    beam.transform.localScale = new Vector3(1, 1.5f, 1);

                    //Debug.Log("Adjusted scale");

                }
            }
            catch (NullReferenceException e)
            {
                //Debug.Log(":( failed to adjust scale of beam (" + e + ")");
            }


            Transform decaltransf = seed.transform.Find("Decal");

            if (decaltransf)
            {
                decaltransf.SetParent(PortalBattery.transform);
                decaltransf.transform.position = zero;
                decaltransf.transform.localScale = new Vector3(75, 75, 75);
                //Debug.Log("set decal's parent");
            }

            Transform camp2transf = seed.transform.Find("Camp 2 - Flavor Props & Void Elites");

            if (camp2transf)
            {
                var combat = camp2transf.GetComponent<CombatDirector>();
                if (combat)
                {
                    //combat.monsterCredit = 30;
                    combat.goldRewardCoefficient = .5f;
                    combat.transform.position = zero;
                    //GameObject.Destroy(combat);
                }

                var camp = camp2transf.GetComponent<CampDirector>();
                if (camp)
                {
                    camp.campMaximumRadius = 23.5f;
                    //Debug.Log("camp credits: " + camp.baseInteractableCredit);
                    camp.baseInteractableCredit = 25;
                    camp.combatDirector = combat;
                    camp.campCenterTransform = center;
                    camp.baseMonsterCredit = 15;
                    camp.transform.position = zero;
                }

                camp2transf.SetParent(PortalBattery.transform);
                camp2transf.transform.position = zero;

                //Debug.Log("set camps's parent");
            }


            //var holder = new GameObject("DecalObject");
            //holder.transform.SetParent(PortalBattery.transform);

            //var pee = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/voidraid/RaidVoidDecal, Huge.prefab").WaitForCompletion();
            //var filter = pee.GetComponent<MeshFilter>();
            //filter.mesh = cube

            var exprc = PortalBattery.GetComponent<ExpansionRequirementComponent>();
            if (exprc)
            {
                //Debug.Log("exprc found");
                exprc.requiredExpansion = null;
                GameObject.Destroy(exprc); //i promise this isnt anything bad 
            }
            Transform[] bees = PortalBattery.GetComponentsInChildren<Transform>();

            //int i = 0;
            //foreach (Transform bee in bees)
            //{
            //    Debug.Log(++i + ": " + bee);
            //}

            //var hzc = PortalBattery.GetComponent<HoldoutZoneController>();
            //hzc.baseRadius = 22.5f;
            PrefabAPI.RegisterNetworkPrefab(PortalBattery);

            cellInteractableCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            cellInteractableCard.name = "iscWhorlCellInteractable";
            cellInteractableCard.prefab = PortalBattery;
            cellInteractableCard.sendOverNetwork = true;
            cellInteractableCard.hullSize = HullClassification.Golem;
            cellInteractableCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            cellInteractableCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            cellInteractableCard.forbiddenFlags = RoR2.Navigation.NodeFlags.None;

            cellInteractableCard.directorCreditCost = 0;

            cellInteractableCard.occupyPosition = true;
            cellInteractableCard.orientToFloor = false;
            cellInteractableCard.skipSpawnWhenSacrificeArtifactEnabled = false;
            cellInteractableCard.maxSpawnsPerStage = 1;

            //var filter = PortalBattery.AddComponent<TeamFilter>();
            //filter.teamIndex = TeamIndex.Void;
            //filter.defaultTeam = TeamIndex.Void;
            //
            //var voidfog = PortalBattery.AddComponent<FogDamageController>();
            //
            //#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            //voidfog.teamFilter = filter;
            //voidfog.tickPeriodSeconds = .2f;
            //voidfog.healthFractionPerSecond = .05f;
            //voidfog.healthFractionRampCoefficientPerSecond = .1f;
            //voidfog.dangerBuffDef = RoR2Content.Buffs.VoidFogMild;
            //voidfog.dangerBuffDuration = .4f;
            //#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            //
            //var objective = PortalBattery.AddComponent<GenericObjectiveProvider>();
            //objective.objectiveToken = "Charge the Lost Battery!";
            //objective.markCompletedOnRetired = true;


            //var voidcontroller = PortalBattery.AddComponent<VoidStageMissionController>();
            //voidcontroller.batteryCount = 1;
            //voidcontroller.batterySpawnCard = cellInteractableCard;
            //voidcontroller.deepVoidPortalObjectiveProvider = objective;
            //voidcontroller.batteryObjectiveToken = "Charge the Lost Battery";
            //voidcontroller.fogDamageController = voidfog;

            var identifier = PortalBattery.AddComponent<VoidShellIdentifierToken>();
            //PickupDropTable table = new PickupDropTable();

            //GenericPickupController.CreatePickupInfo pickupInfo = new GenericPickupController.CreatePickupInfo
            //{
            //    pickerOptions = PickupPickerController.GenerateOptionsFromArray(choices),
            //    position = position,
            //    rotation = Quaternion.identity,
            //    prefabOverride = (choices.Length > 3) ? commandCubePrefab : voidPotentialPrefab,
            //    pickupIndex = pickupIndex
            //};

            //CellCard = new DirectorCard
            //{
            //    selectionWeight = 0,
            //    spawnCard = cellInteractableCard,
            //    minimumStageCompletions = 1,
            //
            //    //allowAmbushSpawn = true, TODO removed i think?
            //};

            //InteractableBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("WhorlCellShell.prefab");
            //InteractableBodyModelPrefab.AddComponent<NetworkIdentity>();
            //var expReqComp = InteractableBodyModelPrefab.AddComponent<RoR2.ExpansionManagement.ExpansionRequirementComponent>();
            //expReqComp.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            //
            //var pingInfoProvider = InteractableBodyModelPrefab.AddComponent<PingInfoProvider>();
            //pingInfoProvider.pingIconOverride = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("texMysteryIcon");
            //
            //var genericNameDisplay = InteractableBodyModelPrefab.AddComponent<GenericDisplayNameProvider>();
            //genericNameDisplay.displayToken = $"VV_INTERACTABLE_WHORLCELLCAMP_NAME";
            //
            //var campDir = InteractableBodyModelPrefab.AddComponent<CampDirector>();
            //campDir.interactableDirectorCards = Addressables.LoadAssetAsync<DirectorCardCategorySelection>("dccsVoidCampFlavorProps").WaitForCompletion();
            //campDir.baseMonsterCredit = 0;
            //campDir.baseInteractableCredit = 2;
            //campDir.campMinimumRadius = 5;
            //campDir.campMaximumRadius = 20;
        }

        private void CompleteShellCharge(HoldoutZoneController zone)
        {
            Debug.Log("Good job!");
            if (NetworkServer.active)
            {
                if (voidCellRng == null)
                {
                    voidCellRng = new Xoroshiro128Plus(Run.instance.seed);
                }

                PickupIndex[] inds = new PickupIndex[3];

                inds[0] = table.GenerateDrop(voidCellRng);
                inds[1] = table.GenerateDrop(voidCellRng);
                inds[2] = table.GenerateDrop(voidCellRng);

                GenericPickupController.CreatePickupInfo pickupInfo = new GenericPickupController.CreatePickupInfo
                {
                    pickerOptions = PickupPickerController.GenerateOptionsFromArray(inds),
                    position = zone.transform.position,
                    rotation = Quaternion.identity,
                    prefabOverride = voidPotentialPrefab,
                    pickupIndex = inds[0]
                };
                //PickupDropletController.CreatePickupDroplet(dropPickup, dropTransform.position + Vector3.up * 1.5f, vector);
            }
            //throw new NotImplementedException();
        }

        //private void ShellDropRewards(HoldoutZoneController arg0)
        //{
        //    Debug.Log("Good job!");
        //    // arg0.transform.position;
        //    //PickupDropletController.CreatePickupDroplet(pickupOverwrite, holdoutZone.transform.position + rewardPositionOffset, vector);
        //}

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlWhorlPickup.prefab");

            string orbTexture = "RoR2/DLC1/voidstage/matVoidAsteroid.mat";
            string orbOuter = "RoR2/DLC1/VoidCamp/matVoidCampLock.mat";

            var itemModelInterm = ItemModel.transform.Find("Intermediate");
            var itemBodyInterm = ItemBodyModelPrefab.transform.Find("Intermediate");


            var orbPartPickup1 = itemModelInterm.Find("Orb").GetComponent<MeshRenderer>();
            var orbOuterPickup1 = itemModelInterm.Find("OrbAura").GetComponent<MeshRenderer>();
            orbPartPickup1.material = Addressables.LoadAssetAsync<Material>(orbTexture).WaitForCompletion();
            orbOuterPickup1.material = Addressables.LoadAssetAsync<Material>(orbOuter).WaitForCompletion();

            var orbPartDisplay = itemBodyInterm.Find("Orb").GetComponent<MeshRenderer>();
            var orbOuterDisplay = itemBodyInterm.Find("OrbAura").GetComponent<MeshRenderer>();
            orbPartDisplay.material = Addressables.LoadAssetAsync<Material>(orbTexture).WaitForCompletion();
            orbOuterDisplay.material = Addressables.LoadAssetAsync<Material>(orbOuter).WaitForCompletion();

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);



            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.02629241f, 0.2568354f, -0.2131178f),
                    localAngles = new Vector3(351.7242f, 10.67858f, 20.43508f),
                    localScale = new Vector3(0.08f, 0.08f, 0.08f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1503672f, 0.1435245f, -0.07638646f),
                    localAngles = new Vector3(345.9114f, 300.3137f, 23.08318f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.07648633f, 0.07626516f, -0.171931f),
                    localAngles = new Vector3(4.41012f, 156.408f, 333.5214f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.7626196f, 0.8972478f, -2.416836f),
                    localAngles = new Vector3(352.209f, 276.9412f, 21.69027f),
                    localScale = new Vector3(.5f, .5f, .5f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1661014f, 0.2427287f, -0.2980944f),
                    localAngles = new Vector3(353.9857f, 276.0242f, 30.12733f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.571964f, 0.2234386f, -0.2234011f),
                    localAngles = new Vector3(351.7031f, 89.96729f, 109.932f),
                    localScale = new Vector3(.25f, .25f, .25f)

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
                    localPos = new Vector3(0.1125494f, 0.1737099f, -0.3271036f),
                    localAngles = new Vector3(5.788457f, 7.310323f, 19.54668f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1414333f, 0.1708212f, -0.205414f),
                    localAngles = new Vector3(352.4888f, 291.1599f, 19.03975f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfBackL",
                    localPos = new Vector3(0.08891746f, 0.5175744f, -0.03669554f),
                    localAngles = new Vector3(352.6626f, 273.883f, 23.80008f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1394217f, 0.1633563f, -0.3964019f),
                    localAngles = new Vector3(357.2906f, 279.8901f, 17.20597f),
                    localScale = new Vector3(0.09f, 0.09f, 0.09f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-1.443816f, -0.6864427f, 3.308026f),
                    localAngles = new Vector3(26.11133f, 5.543665f, 25.21973f),
                    localScale = new Vector3(.8f, .8f, .8f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.1425444f, 0.1892054f, -0.2536568f),
                    localAngles = new Vector3(349.48f, 296.4531f, 17.46299f),
                    localScale = new Vector3(.115f, .115f, .115f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(0.2669638f, -0.08863433f, -0.07332691f),
                    localAngles = new Vector3(355.9068f, 102.4288f, 11.93598f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfR",
                    localPos = new Vector3(0.02665846f, 0.2549812f, -0.07270494f),
                    localAngles = new Vector3(11.88894f, 359.9499f, 204.7378f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(1.91149f, 11.57303f, 4.621446f),
                    localAngles = new Vector3(353.9657f, 129.2633f, 20.15013f),
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
                    childName =  "Shield",
                    localPos =   new Vector3(0.1429525f, 0.009444445f, -0.231735f),
                    localAngles = new Vector3(0.949871f, 227.3962f, 30.76947f),
                    localScale = new Vector3(0.085f, 0.085f, 0.085f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.001040328f, 0.0004106277f, 0.001044341f),
                    localAngles = new Vector3(350.0445f, 351.373f, 112.076f),
                    localScale = new Vector3(0.003f, 0.004f, 0.0035f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] //these ones don't work for some reason!
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.2842848f, -0.1576135f, 0.01475417f),
                    localAngles = new Vector3(4.996761f, 302.908f, 315.8754f),
                    localScale = new Vector3(0.1f, 0.1f, 0.1f)
                }
            });
            //rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Door",
            //        localPos = new Vector3(0F, 0.00347F, -0.00126F),
            //        localAngles = new Vector3(0F, 90F, 0F),
            //        localScale = new Vector3(0.01241F, 0.01241F, 0.01241F)
            //    }
            //});
            rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "PickL",
                    localPos = new Vector3(-0.003641347f, 0.001164402f, 0.000302475f),
                    localAngles = new Vector3(352.0699f, 17.21215f, 12.00122f),
                    localScale = new Vector3(0.001f, 0.001f, 0.001f)
                }
            });
            //rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Body",
            //        localPos = new Vector3(0F, 0.00347F, -0.00126F),
            //        localAngles = new Vector3(0F, 90F, 0F),
            //        localScale = new Vector3(0.01241F, 0.01241F, 0.01241F)
            //    }
            //});
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.1678362f, 0.2800805f, -0.1426394f),
                    localAngles = new Vector3(5.870443f, 265.1015f, 331.878f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperTorso",
                    localPos = new Vector3(-0.0004795195f, 0.03674114f, -0.1753576f),
                    localAngles = new Vector3(4.924208f, 197.0649f, 346.5102f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }
            });
            rules.Add("mdlExecutioner", new RoR2.ItemDisplayRule[]
{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.002098578f, -0.0005844539f, 0.0005783288f),
                    localAngles = new Vector3(3.540956f, 305.3824f, 5.553184f),
                    localScale = new Vector3(0.00035f, 0.00035f, 0.00035f)
                }
});
            rules.Add("mdlNemmando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.002061183f, -0.0009356125f, 0.0005527574f),
                    localAngles = new Vector3(0.4865296f, 272.5422f, 17.22349f),
                    localScale = new Vector3(0.00035f, 0.00035f, 0.00035f)
                }
            });

            return rules;
        }

        public override void Hooks()
        {
            On.RoR2.SceneDirector.PopulateScene += AddCornucopiaCamp;
            //On.RoR2.VoidStageMissionController.OnBatteryActivated += ShellReward;
            //
            //On.EntityStates.DeepVoidPortalBattery.Charged.OnEnter += OverrideBatteryCompletion;

            IL.EntityStates.DeepVoidPortalBattery.Charged.OnEnter += OverrideBatteryChargedEnter;
            //SceneDirector.onPrePopulateMonstersSceneServer += AddCornucopiaCamp2;
            //On.RoR2.HealthComponent.TakeDamage += AdzeDamageBonus;
        }

        private void OverrideBatteryChargedEnter(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            bool ILFound = c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(0),
            x => x.MatchCallOrCallvirt(typeof(EntityStates.DeepVoidPortalBattery.BaseDeepVoidPortalBatteryState).GetMethod(nameof(EntityStates.DeepVoidPortalBattery.BaseDeepVoidPortalBatteryState.OnEnter))
            ));
            if (ILFound)
            {
                //c.Index += 2;
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Action<EntityStates.DeepVoidPortalBattery.Charged>>((self) =>
                {
                    var token = self.GetComponent<VoidShellIdentifierToken>();
                    if (token)
                    {
                        if (NetworkServer.active)
                        {
                            if (voidCellRng == null)
                            {
                                voidCellRng = new Xoroshiro128Plus(Run.instance.seed);
                            }

                            int validPlayers = 0;
                            foreach (var player in PlayerCharacterMasterController.instances)
                            {
                                int itemCount = player.master.inventory.GetItemCount(ItemBase<VoidShell>.instance.ItemDef);
                                if (itemCount > 0)
                                {
                                    ++validPlayers;
                                }
                            }

                            int num = validPlayers;
                            float angle = 360f / (float)num;
                            Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * (float)20f + Vector3.forward * 5f);
                            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                            Vector3 position = self.transform.position + (1.5f * Vector3.up);

                            int i = 0;
                            while (i < num)
                            {
                                PickupPickerController.Option[] options = PickupPickerController.GenerateOptionsFromDropTable(3, table, voidCellRng);

                                PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
                                {
                                    pickerOptions = options,
                                    rotation = Quaternion.identity,
                                    prefabOverride = voidPotentialPrefab,
                                    pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.VoidTier1),
                                }, position, vector);
                                ++i;
                                vector = rotation * vector;
                            }

                        }
                        return;
                    }
                });

            }
            else
            {
                Debug.Log("error with Shell IL hook!!!");
            }
            //throw new NotImplementedException();
        }

        //private void OverrideBatteryCompletion(On.EntityStates.DeepVoidPortalBattery.Charged.orig_OnEnter orig, EntityStates.DeepVoidPortalBattery.Charged self)
        //{
        //    //Debug.Log("HELLO! " + self + " | " + self.GetComponent<VoidShellIdentifierToken>());
        //    var token = self.GetComponent<VoidShellIdentifierToken>();
        //    //var token2 = self.transform.
        //    if (token)
        //    {
        //        //self.outer.SetNextState();
        //        //Debug.Log("hi!");
        //        if (NetworkServer.active)
        //        {
        //            if (voidCellRng == null)
        //            {
        //                voidCellRng = new Xoroshiro128Plus(Run.instance.seed);
        //            }
        //
        //            //PickupIndex[] inds = new PickupIndex[3];
        //            //
        //            //inds[0] = table.GenerateDrop(voidCellRng);
        //            //inds[1] = table.GenerateDrop(voidCellRng);
        //            //inds[2] = table.GenerateDrop(voidCellRng);
        //            int validPlayers = 0;
        //            foreach (var player in PlayerCharacterMasterController.instances)
        //            {
        //                int itemCount = player.master.inventory.GetItemCount(ItemBase<VoidShell>.instance.ItemDef);
        //                if (itemCount > 0)
        //                {
        //                    ++validPlayers;
        //                    //PickupPickerController.Option[] options = PickupPickerController.GenerateOptionsFromDropTable(3, table, voidCellRng);
        //                    //
        //                    //GenericPickupController.CreatePickupInfo pickupInfo = new GenericPickupController.CreatePickupInfo
        //                    //{
        //                    //    pickerOptions = options,
        //                    //    position = self.transform.position + (Vector3.up * 2.5f),
        //                    //    rotation = Quaternion.identity,
        //                    //    prefabOverride = voidPotentialPrefab,
        //                    //    pickupIndex = options[0].pickupIndex
        //                    //};
        //                    ////Debug.Log("hi!!!!!!!!!!!!!!!!!! " + inds[0] + " | " + self.transform.position);
        //                    //PickupDropletController.CreatePickupDroplet(pickupInfo, (self.transform.position + (Vector3.up * 2f)), (Vector3.up * 2f));
        //                    //PickupDropletController.CreatePickupDroplet(pickupInfo, dropTransform.position + Vector3.up * 1.5f, vector);
        //                }
        //            }
        //
        //            int num = validPlayers;
        //            float angle = 360f / (float)num;
        //            Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * (float)20f + Vector3.forward * 5f);
        //            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
        //            Vector3 position = self.transform.position + (1.5f * Vector3.up);
        //            int i = 0;
        //            while (i < num)
        //            {
        //                PickupPickerController.Option[] options = PickupPickerController.GenerateOptionsFromDropTable(3, table, voidCellRng);
        //
        //                PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
        //                {
        //                    pickerOptions = options,
        //                    rotation = Quaternion.identity,
        //                    prefabOverride = voidPotentialPrefab,
        //                    pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.VoidTier1),
        //                }, position, vector);
        //                ++i;
        //                vector = rotation * vector;
        //            }
        //
        //        }
        //    }
        //    else
        //    {
        //        orig(self);
        //    }
        //
        //    //Transform[] transfs = self.transform.GetComponentsInChildren<Transform>();
        //    //foreach (Transform trans in transfs)
        //    //{
        //    //    Debug.Log("- " + trans);
        //    //}
        //    //
        //    //orig(self);
        //}



        //private void ShellReward(On.RoR2.VoidStageMissionController.orig_OnBatteryActivated orig, VoidStageMissionController self)
        //{
        //    Debug.Log("ahghhhhhh");
        //    Transform[] transforms = self.GetComponentsInChildren<Transform>();
        //    foreach (Transform trans in transforms)
        //    {
        //        Debug.Log("- " + trans);
        //    }
        //    //self.
        //
        //    orig(self);
        //}

        private void AddCornucopiaCamp(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self) //this causes a weird error having to do with isExpansionEnabled somehow
        {
            orig(self);
            int validPlayers = 0;
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                int itemCount = player.master.inventory.GetItemCount(ItemBase<VoidShell>.instance.ItemDef);
                if (itemCount > 0)
                {
                    ++validPlayers;
                }
            }

            if(validPlayers > 0)
            {
                if (voidCellRng == null)
                {
                    voidCellRng = new Xoroshiro128Plus(Run.instance.seed);
                }

                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(cellInteractableCard, new DirectorPlacementRule
                { placementMode = DirectorPlacementRule.PlacementMode.Random }, voidCellRng));
            }

            //foreach (var player in PlayerCharacterMasterController.instances)
            //{
            //    int itemCount = player.master.inventory.GetItemCount(ItemBase<VoidShell>.instance.ItemDef);
            //    if (itemCount > 0)
            //    {
            //        //Debug.Log("found player with it");
            //        if (voidCellRng == null)
            //        {
            //            voidCellRng = new Xoroshiro128Plus(Run.instance.seed);
            //        }
            //
            //        DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(cellInteractableCard, new DirectorPlacementRule
            //        { placementMode = DirectorPlacementRule.PlacementMode.Random }, voidCellRng));
            //
            //    }
            //}

        }


        //private void AddCornucopiaCamp2(SceneDirector obj)
        //{
        //    Debug.Log("ahhh");
        //    foreach (var player in PlayerCharacterMasterController.instances)
        //    {
        //        int itemCount = player.master.inventory.GetItemCount(ItemBase<VoidShell>.instance.ItemDef);
        //        if (itemCount > 0)
        //        {
        //            Debug.Log("found player with it");
        //            if (voidCellRng == null)
        //            {
        //                voidCellRng = new Xoroshiro128Plus(Run.instance.seed);
        //            }
        //
        //            DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(cellInteractableCard, new DirectorPlacementRule
        //            { placementMode = DirectorPlacementRule.PlacementMode.Random }, voidCellRng));
        //
        //        }
        //    }
        //}
        
        //private void AddCornucopiaCamp2(SceneDirector obj)
        //{
        //    throw new NotImplementedException();
        //}


        //private void AddCornucopiaCamp(SceneDirector obj)
        //{
        //
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
        //                //var healthPercentage = self.health / self.fullCombinedHealth;
        //                var healthFraction = Mathf.Clamp((1 - self.combinedHealthFraction), 0f, 1f);
        //                //Debug.Log("health fraction: " + healthFraction);
        //                var mult = healthFraction * (baseDamageBuff.Value + (stackingBuff.Value * (stackCount - 1)));
        //                
        //                damageInfo.damage = damageInfo.damage + (damageInfo.damage * mult);
        //                float maxDamage = initialDmg + (initialDmg * (baseDamageBuff.Value + (stackingBuff.Value * (stackCount - 1))));
        //                //Debug.Log("max damage: " + maxDamage + " | actual damage: " + damageInfo.damage + " | original damage: " + initialDmg);
        //                //damageInfo.damage = damageInfo.damage * (1 + (victimBody.GetBuffCount(adzeDebuff) * dmgPerDebuff.Value));
        //                //if(damageInfo.damage > maxDamage)
        //                //{
        //                //    //Debug.Log("damage was too high! oopsies!!!");
        //                //    damageInfo.damage = maxDamage; // i don't know if this is a needed check, but i *think* i was noticing insanely high damage numbers with adze on the end score screen. maybe this'll fix that? or maybe it was another mod entirely
        //                //}
        //                damageInfo.damage = Mathf.Min(damageInfo.damage, maxDamage);
        //            }
        //        }
        //    }
        //    
        //    orig(self, damageInfo);
        //}
    }
    public class VoidShellIdentifierToken : MonoBehaviour
    {

    }

}
