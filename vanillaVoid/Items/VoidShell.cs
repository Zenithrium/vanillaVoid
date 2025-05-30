﻿using BepInEx.Configuration;
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
using RoR2.ExpansionManagement;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using EntityStates;

namespace vanillaVoid.Items
{
    public class VoidShell : ItemBase<VoidShell>
    {
        //public ConfigEntry<float> baseDamageBuff;
        //
        //public ConfigEntry<float> stackingBuff;

        public ConfigEntry<bool> enableFog;

        public ConfigEntry<bool> playerFog;

        public ConfigEntry<float> fogTickPeriod;

        public ConfigEntry<float> fogHealthFraction;

        public ConfigEntry<float> fogHealthFractionRamp;

        public ConfigEntry<float> monsterCredits;

        public ConfigEntry<float> bossMonsterCredits;

        public ConfigEntry<float> bossMonsterNullifierWeight;

        public ConfigEntry<float> bossMonsterJailerWeight;

        public ConfigEntry<float> bossMonsterDevastatorWeight;

        public ConfigEntry<float> ShellTier1Weight;

        public ConfigEntry<float> ShellTier2Weight;

        public ConfigEntry<float> ShellTier3Weight;

        public ConfigEntry<float> ShellTier1VoidWeight;

        public ConfigEntry<float> ShellTier2VoidWeight;

        public ConfigEntry<float> ShellTier3VoidWeight;

        public ConfigEntry<bool> showLaser;

        public ConfigEntry<bool> specialHappen;

        public ConfigEntry<bool> multiplayerScaling;

        //public ConfigEntry<string> voidPair;

        public override string ItemName => "Ceaseless Cornucopia";

        public override string ItemLangTokenName => "CORNUCOPIACELL_ITEM";

        public override string ItemPickupDesc => $"Recieve a special, dangerous delivery with powerful rewards. <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemFullDescription => $"A <style=cIsVoid>special</style> delivery containing items (" +
            (ShellTier1Weight.Value != 0 ? $"<color=#FFFFFF>{ShellTier1Weight.Value * 100}%</color>" : "") +
            (ShellTier1Weight.Value != 0 && ShellTier2Weight.Value != 0 ? "/" : "") +
            (ShellTier2Weight.Value != 0 ? $"<color=#9CE562>{ShellTier2Weight.Value * 100}%</color>" : "") +
            ((ShellTier1Weight.Value != 0 || ShellTier2Weight.Value != 0) && ShellTier3Weight.Value != 0 ? "/" : "") +
            (ShellTier3Weight.Value != 0 ? $"<color=#E15141>{ShellTier3Weight.Value * 100}%</color>" : "") +
            ((ShellTier1Weight.Value != 0 || ShellTier2Weight.Value != 0 || ShellTier3Weight.Value != 0) && ShellTier1VoidWeight.Value != 0 ? "/" : "") +
            (ShellTier1VoidWeight.Value != 0 ? $"<color=#DD7AC6>{ShellTier1VoidWeight.Value * 100}%</color>" : "") + //this is the same as cIsVoid
            ((ShellTier1Weight.Value != 0 || ShellTier2Weight.Value != 0 || ShellTier3Weight.Value != 0 || ShellTier1VoidWeight.Value != 0) && ShellTier2VoidWeight.Value != 0 ? "/" : "") +
            (ShellTier2VoidWeight.Value != 0 ? $"<color=#CE5CB2>{ShellTier2VoidWeight.Value * 100}%</color>" : "") +
            ((ShellTier1Weight.Value != 0 || ShellTier2Weight.Value != 0 || ShellTier3Weight.Value != 0 || ShellTier1VoidWeight.Value != 0 || ShellTier2VoidWeight.Value != 0) && ShellTier3VoidWeight.Value != 0 ? "/" : "") +
            (ShellTier3VoidWeight.Value != 0 ? $"<color=#BC499F>{ShellTier3VoidWeight.Value * 100}%</color>" : "") +
            $") will appear in a random location <style=cIsUtility>on each stage</style>. <style=cStack>(Increases rarity chances of the items per stack).</style> <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>."; 

        public override string ItemLore => $"<style=cMono>Welcome to DataScraper (v3.2.02 ?「alph?a br??anch』)\n$ 『Scraping memory... don???e.\n$ Resolvingggggggggggggggggdone.】\n$ Combing for ?????         ... done.\n\n\n" +
            $"Complete!\n【Outputttt??tt:」</style>" +
            $"\n「SSBzYXcgdGhlbSBhZ2Fpbi4gVGhleSdyZSBiYWNr????LiBXZSBkaXNzZWN0ZWQgdGhlbS4gSHVud』GVkIHRoZW0gdG8gZXh0aW5jd?G??lvbiB?mb3IgdGhlaXIgc2hlbGwuIFRoZXkga】25vdy4KClRoaXMgaXMgb3Vy『IHJld2????FyZC4=」" +
            $"\n\nNotes: Th?is is unusable. Yo?u said y??ou』had made a 『brea?kthroug??h.\n" +
            $"What did 「yo??u do.」」】";

        //public string InteractableLangToken => "SHATTERED_SHRINE";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlWhorlPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("cornucopiaIcon512.png");

        public static GameObject ItemBodyModelPrefab;

        public static GameObject PortalBattery;

        public Xoroshiro128Plus voidCellRng;

        public static InteractableSpawnCard cellInteractableCard;

        public static DirectorCard CellCard;

        public static GameObject InteractableBodyModelPrefab;

        public static GameObject voidPotentialPrefab;

        public VoidShellDropTable dropTable;

        //public static BasicPickupDropTable[] tables = new BasicPickupDropTable[4];

        //public static BasicPickupDropTable table;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Utility, ItemTag.AIBlacklist };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            //VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);

            CreateInteractable();
            //CreateVoidCellInteractable();
            //PortalBattery = Addressables.LoadAssetAsync<GameObject>("DeepVoidPortalBattery.prefab").WaitForCompletion();
            Hooks();
        }

        public override void CreateConfig(ConfigFile config){
            enableFog = config.Bind<bool>("Item: " + ItemName, "Enable Void Fog", true, "Adjust whether the Lost Battery should have void fog while charging (like void fields).");
            playerFog = config.Bind<bool>("Item: " + ItemName, "Player Only Void Fog", true, "Adjust whether void fog should only target players rather than targeting everyone except void enemies.");

            fogTickPeriod = config.Bind<float>("Item: " + ItemName, "Void Fog Tick Rate", .2f, "Adjust the rate at which the void fog ticks. Lower means the damage ticks faster.");
            fogHealthFraction = config.Bind<float>("Item: " + ItemName, "Void Fog Health Fraction", .025f, "Adjust how much damage the void fog should do at base. This is half of the normal void fog in Void Locus.");
            fogHealthFractionRamp = config.Bind<float>("Item: " + ItemName, "Void Fog Health Fraction Ramp", .05f, "Adjust how much damage the void fog should scale. This is half of the normal void fog in Void Locus.");

            monsterCredits = config.Bind<float>("Item: " + ItemName, "Void Barnacle Credits", 50, "Adjust amount of credits the regular director gets to spawn void monsters. This one usually just spawns barnacles.");
            bossMonsterCredits = config.Bind<float>("Item: " + ItemName, "Void Boss Credits", 800, "Adjust the amount of credits the boss director gets to spawn a larger void threat. Nullifiers cost 300, Jailers cost 450, and Devastators cost 800.");
            
            bossMonsterNullifierWeight = config.Bind<float>("Item: " + ItemName, "Boss Nullifier Weight", .5225f, "Adjust the weight of Nullifiers being spawned as the 'boss' of the Lost Battery.");
            bossMonsterJailerWeight = config.Bind<float>("Item: " + ItemName, "Boss Jailer Weight", .4275f, "Adjust the weight of Jailer being spawned as the 'boss' of the Lost Battery.");
            bossMonsterDevastatorWeight = config.Bind<float>("Item: " + ItemName, "Boss Devastator Weight", .05f, "Adjust the weight of Devastator being spawned as the 'boss' of the Lost Battery.");

            ShellTier1Weight = config.Bind<float>("Item: " + ItemName, "Tier 1 Weight", .316f, "Adjust weight of Tier 1 items.");
            ShellTier2Weight = config.Bind<float>("Item: " + ItemName, "Tier 2 Weight", .08f, "Adjust weight of Tier 2 items.");
            ShellTier3Weight = config.Bind<float>("Item: " + ItemName, "Tier 3 Weight", .004f, "Adjust weight of Tier 3 items.");
            ShellTier1VoidWeight = config.Bind<float>("Item: " + ItemName, "Void Tier 1 Weight", .474f, "Adjust weight of Void Tier 1 items.");
            ShellTier2VoidWeight = config.Bind<float>("Item: " + ItemName, "Void Tier 2 Weight", .12f, "Adjust weight of Void Tier 2 items.");
            ShellTier3VoidWeight = config.Bind<float>("Item: " + ItemName, "Void Tier 3 Weight", .006f, "Adjust weight of Void Tier 3 items.");

            showLaser = config.Bind<bool>("Item: " + ItemName, "Show Laser", true, "Adjust whether the special delivery should be marked with a laser.");

            specialHappen = config.Bind<bool>("Item: " + ItemName, "Spawn in Special Stages", true, "Adjust whether the special delivery should spawn in Commencement and Gold Shores. Every other special environment (such as bazaar or limbo) are already banned.");

            multiplayerScaling = config.Bind<bool>("Item: " + ItemName, "Potential Per Player", false, "Adjust if the battery should drop an item for each player. Rarity of the items dependant on the total number of Cornucopias the entire team has.");

            //stackingBuff = config.Bind<float>("Item: " + ItemName, "Percent Damage Increase per Stack", .4f, "Adjust the percent of extra damage dealt per stack.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "FreeChest", "Adjust which item this is the void pair of.");
        }

        public void CreateInteractable(){
            voidPotentialPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();

            dropTable = ScriptableObject.CreateInstance<VoidShellDropTable>();

            Vector3 zero = new Vector3(0, 0, 0);

            var locusCards = Addressables.LoadAssetAsync<DirectorCardCategorySelection>("RoR2/DLC1/voidstage/dccsVoidStageMonsters.asset").WaitForCompletion(); //maybe "RoR2/Base/Common/dccsNullifiersOnly.asset"
            //var nullifierCards = Addressables.LoadAssetAsync<DirectorCardCategorySelection>("RoR2/Base/Common/dccsNullifiersOnly.asset").WaitForCompletion(); 
            
            //DirectorCardCategorySelection voidThreats = new DirectorCardCategorySelection();
            var voidThreats = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
            //Debug.Log("Category Selection Made - " + voidThreats);
            //var fodder = newCards.AddCategory("Void Fodder", .9f);
            //var category0 = voidThreats.AddCategory("All", 1);
            var category1 = voidThreats.AddCategory("Nullifiers", bossMonsterNullifierWeight.Value);
            var category2 = voidThreats.AddCategory("Jailer", bossMonsterJailerWeight.Value);
            var category3 = voidThreats.AddCategory("Devastators", bossMonsterDevastatorWeight.Value);
            
            var card1 = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Nullifier/cscNullifier.asset").WaitForCompletion();
            var dc1 = new DirectorCard();
            dc1.spawnCard = card1;
            dc1.minimumStageCompletions = 0;
            dc1.selectionWeight = 1;
            dc1.spawnDistance = DirectorCore.MonsterSpawnDistance.Standard;
            //Debug.Log("Cost of Nullifier: " + card1.directorCreditCost);

            var card2 = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidJailer/cscVoidJailer.asset").WaitForCompletion();
            var dc2 = new DirectorCard();
            dc2.spawnCard = card2;
            dc2.minimumStageCompletions = 0;
            dc2.selectionWeight = 1;
            dc2.spawnDistance = DirectorCore.MonsterSpawnDistance.Standard;
            //Debug.Log("Cost of Jailer: " + card2.directorCreditCost);

            var card3 = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset").WaitForCompletion();
            var dc3 = new DirectorCard();
            dc3.spawnCard = card3;
            dc3.minimumStageCompletions = 0;
            dc3.selectionWeight = 1;
            dc3.spawnDistance = DirectorCore.MonsterSpawnDistance.Standard;
            //Debug.Log("Cost of Mega Crab: " + card3.directorCreditCost);

            voidThreats.AddCard(category1, dc1);
            voidThreats.AddCard(category2, dc2);
            voidThreats.AddCard(category3, dc3);
            //newCards.AddCard(fodder, );

            var tempPortalBattery = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/DeepVoidPortalBattery/DeepVoidPortalBattery.prefab").WaitForCompletion();
            PortalBattery = PrefabAPI.InstantiateClone(tempPortalBattery, "VoidShellBattery");

            var vfx = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("WhorlShellVFX.prefab");

            var vfxclone = PrefabAPI.InstantiateClone(vfx, "VoidShellVFX");
            vfxclone.transform.SetParent(PortalBattery.transform);

            PortalBattery.transform.position = zero;

            var pinter = PortalBattery.GetComponent<PurchaseInteraction>();
            pinter.displayNameToken = "VV_SHELL_NAME";
            pinter.contextToken = "VV_SHELL_CONTEXT";
            LanguageAPI.Add("VV_SHELL_NAME", "Lost Battery");
            LanguageAPI.Add("VV_SHELL_CONTEXT", "Activate the Lost Battery..?"); 
            //oidShellDropTable tableController = new VoidShellDropTable();
            //PortalBattery.AddComponent<VoidShellDropTable>();
            var teamFilter = PortalBattery.AddComponent<TeamFilter>();
            var fogcontroller = PortalBattery.AddComponent<FogDamageController>();
            if (playerFog.Value)
            {
                teamFilter.teamIndex = TeamIndex.Player;
                teamFilter.defaultTeam = TeamIndex.Player;
                fogcontroller.invertTeamFilter = false;
            }
            else
            {
                teamFilter.teamIndex = TeamIndex.Void;
                teamFilter.defaultTeam = TeamIndex.Void;
                fogcontroller.invertTeamFilter = true;
            }
            //var teamFilter = PortalBattery.AddComponent<TeamFilter>();

            fogcontroller.enabled = false;
            fogcontroller.teamFilter = teamFilter;
            fogcontroller.tickPeriodSeconds = fogTickPeriod.Value;
            fogcontroller.healthFractionPerSecond = fogHealthFraction.Value;
            fogcontroller.healthFractionRampCoefficientPerSecond = fogHealthFractionRamp.Value;
            fogcontroller.dangerBuffDef = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Common/bdVoidFogMild.asset").WaitForCompletion();
            fogcontroller.dangerBuffDuration = .4f;

            var hzc = PortalBattery.GetComponent<HoldoutZoneController>();
            if (hzc)
            {
                //Debug.Log("found hzc");
                hzc.baseRadius = 25;
                hzc.baseChargeDuration = 30;
                hzc.inBoundsObjectiveToken = "VV_OBJECTIVE_SHELL";
                hzc.outOfBoundsObjectiveToken = "VV_OBJECTIVE_SHELL_OOB";
                //hzc.
                LanguageAPI.Add("VV_OBJECTIVE_SHELL", "Charge the <style=cIsVoid>Lost Battery</style> ({0}%)"); //look at treasure map
                LanguageAPI.Add("VV_OBJECTIVE_SHELL_OOB", "Enter the <style=cIsVoid>Lost Battery's radius!</style> ({0}%)");

                //hzc.onCharged.AddListener(ShellDropRewards);
                //hzc.onCharged = new HoldoutZoneController.HoldoutZoneControllerChargedUnityEvent();
                //hzc.onCharged.AddListener(zone => CompleteShellCharge(zone));
            }

            var combatdir = PortalBattery.GetComponent<CombatDirector>();
            if (combatdir)
            {
                combatdir.monsterCredit = monsterCredits.Value;
                //if (real != null)
                //{
                //combatdir.currentActiveEliteDef = DLC1Content.Elites.Void;
                combatdir.customName = "LostBatteryDirector";
                combatdir.monsterCards = locusCards;
                combatdir.eliteBias = 0;
                combatdir.moneyWaveIntervals = new RangeFloat[] { new RangeFloat { min = 1, max = 1 } };
                combatdir.shouldSpawnOneWave = false;
                //combatdir.minSpawnRange
                combatdir.targetPlayers = true;
                combatdir.creditMultiplier = 3;
                combatdir.ignoreTeamSizeLimit = true;
                combatdir.maxSpawnDistance = 999999;
                combatdir.minSpawnRange = 0;
                combatdir.skipSpawnIfTooCheap = false;
                combatdir.teamIndex = TeamIndex.Void;
                combatdir.monsterSpawnTimer = 0;
                
            }

            Transform center = PortalBattery.transform.Find("Model");
            var tempSeed = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidCamp/VoidCamp.prefab").WaitForCompletion();
            var seed = PrefabAPI.InstantiateClone(tempSeed, "voidCampFragment");

            if (center)
            {
                var cd2 = center.gameObject.AddComponent<CombatDirector>();
                cd2.monsterCredit = bossMonsterCredits.Value;
                cd2.customName = "LostBatteryDirectorBoss";
                cd2.monsterCards = voidThreats;
                cd2.eliteBias = 0;
                //cd2.num
                //cd2.moneyWaveIntervals = new RangeFloat[] { new RangeFloat { min = 1, max = 1 } };
                cd2.shouldSpawnOneWave = false;
                cd2.targetPlayers = true;
                cd2.creditMultiplier = 1;
                cd2.ignoreTeamSizeLimit = true;
                cd2.skipSpawnIfTooCheap = false;
                cd2.teamIndex = TeamIndex.Void;
                cd2.monsterSpawnTimer = 5;
                cd2.maximumNumberToSpawnBeforeSkipping = 1;
                cd2.enabled = false;
                cd2.maxSpawnDistance = 999999f;
                cd2.minSpawnRange = 0;
            }

            try
            {
                Transform beam = PortalBattery.transform.Find("Model").Find("mdlVoidSignal").Find("IdleFX").Find("Beam, Strong"); //yeah
                if (beam)
                {
                    if (showLaser.Value)
                    {
                        beam.transform.rotation = Quaternion.Euler(0, 0, 90);
                        beam.transform.position = new Vector3(0, 102, 0);
                        beam.transform.localScale = new Vector3(1, 1.5f, 1);
                    }
                    else
                    {
                        beam.gameObject.SetActive(false);
                    }
                }
            }
            catch (NullReferenceException e)
            {
                Debug.Log(":( failed to adjust scale of beam (" + e + ")");
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
                    //var cards = combat.monsterCards;
                    combat.monsterCards = locusCards;
                    combat.maxSpawnDistance = 999999;
                    combat.minSpawnRange = 0;
                    combat.shouldSpawnOneWave = true;
                    //combat.eliteBias = 0;
                    //GameObject.Destroy(combat);
                }

                var camp = camp2transf.GetComponent<CampDirector>();
                if (camp)
                {
                    camp.campMaximumRadius = 23.5f;
                    //camp.campMinimumRadius = 7.5f;
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

            var exprc = PortalBattery.GetComponent<ExpansionRequirementComponent>();
            //if (exprc)
            //{
            //    //Debug.Log("exprc found");
            //    exprc.requiredExpansion = null;
            //    GameObject.Destroy(exprc); //i promise this isnt anything bad 
            //}


            var identifier = PortalBattery.AddComponent<VoidShellIdentifierToken>();

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

        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlWhorlDisplay.prefab");

            //string orbTexture = "RoR2/DLC1/voidstage/matVoidAsteroid.mat";
            //string orbOuter = "RoR2/DLC1/VoidCamp/matVoidCampLock.mat";
            //
            //var itemModelInterm = ItemModel.transform.Find("Intermediate");
            //var itemBodyInterm = ItemBodyModelPrefab.transform.Find("Intermediate");
            //
            //
            //var orbPartPickup1 = itemModelInterm.Find("Orb").GetComponent<MeshRenderer>();
            //var orbOuterPickup1 = itemModelInterm.Find("OrbAura").GetComponent<MeshRenderer>();
            //orbPartPickup1.material = Addressables.LoadAssetAsync<Material>(orbTexture).WaitForCompletion();
            //orbOuterPickup1.material = Addressables.LoadAssetAsync<Material>(orbOuter).WaitForCompletion();
            //
            //var orbPartDisplay = itemBodyInterm.Find("Orb").GetComponent<MeshRenderer>();
            //var orbOuterDisplay = itemBodyInterm.Find("OrbAura").GetComponent<MeshRenderer>();
            //orbPartDisplay.material = Addressables.LoadAssetAsync<Material>(orbTexture).WaitForCompletion();
            //orbOuterDisplay.material = Addressables.LoadAssetAsync<Material>(orbOuter).WaitForCompletion();

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            var mpp = ItemModel.AddComponent<ModelPanelParameters>();
            mpp.focusPointTransform = ItemModel.transform.Find("Target");
            mpp.cameraPositionTransform = ItemModel.transform.Find("Source");
            mpp.minDistance = 5f;
            mpp.maxDistance = 10f;
            mpp.modelRotation = Quaternion.Euler(new Vector3(0, 0, 0));

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.12163F, 0.017F, -0.04006F),
                    localAngles = new Vector3(0.86547F, 131.1143F, 167.6714F),
                    localScale = new Vector3(0.025F, 0.025F, 0.025F)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.06166F, -0.03951F, 0.0044F),
                    localAngles = new Vector3(359.4635F, 259.1087F, 215.5728F),
                    localScale = new Vector3(0.0225F, 0.0225F, 0.0225F)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Stomach",
                    localPos = new Vector3(-0.06626F, 0.03034F, -0.14021F),
                    localAngles = new Vector3(353.1991F, 356.5443F, 351.2719F),
                    localScale = new Vector3(0.025F, 0.025F, 0.025F)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(2.56418F, 1.98398F, 1.62287F),
                    localAngles = new Vector3(-0.00003F, 254.7362F, 0F),
                    localScale = new Vector3(0.225F, 0.225F, 0.225F)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CannonHeadR",
                    localPos = new Vector3(-0.218F, 0.28773F, -0.05025F),
                    localAngles = new Vector3(15.27972F, 58.84064F, 27.52047F),
                    localScale = new Vector3(0.0285F, 0.0285F, 0.0285F)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0F, 0.54292F, -1.54192F),
                    localAngles = new Vector3(0F, 14.68683F, 239.1591F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)

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
                    childName = "LowerArmR",
                    localPos = new Vector3(-0.012F, 0.16605F, 0.08735F),
                    localAngles = new Vector3(342.3825F, 343.321F, 223.6601F),
                    localScale = new Vector3(0.0325F, 0.0325F, 0.0325F)
                }

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmR",
                    localPos = new Vector3(-0.17265F, 0.02279F, -0.02364F),
                    localAngles = new Vector3(347.6715F, 112.0617F, 163.8508F),
                    localScale = new Vector3(0.025F, 0.025F, 0.025F)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "FootBackR",
                    localPos = new Vector3(0.00569F, 0.43374F, -0.08325F),
                    localAngles = new Vector3(340.9593F, 35.95332F, 225.7514F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MechBase",
                    localPos = new Vector3(0.20751F, 0.15713F, 0.26423F),
                    localAngles = new Vector3(343.4162F, 241.1119F, 329.9341F),
                    localScale = new Vector3(0.0425F, 0.0425F, 0.0425F)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-1.6616F, 1.17074F, 0.05715F),
                    localAngles = new Vector3(17.48985F, 116.9149F, 151.8853F),
                    localScale = new Vector3(0.225F, 0.225F, 0.225F)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0.16616F, 0.05832F, -0.15121F),
                    localAngles = new Vector3(5.15558F, 2.87122F, 182.7047F),
                    localScale = new Vector3(0.0325F, 0.0325F, 0.0325F)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(-0.12057F, -0.26632F, -0.12956F),
                    localAngles = new Vector3(2.59167F, 321.3517F, 358.4254F),
                    localScale = new Vector3(0.03F, 0.03F, 0.03F)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmR",
                    localPos = new Vector3(0.23037F, 0.406F, -0.15827F),
                    localAngles = new Vector3(30.50583F, 307.6779F, 108.479F),
                    localScale = new Vector3(0.0325F, 0.0325F, 0.0325F)
                }
            });
            rules.Add("mdlSeeker", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.13024F, 0.02634F, -0.02825F),
                    localAngles = new Vector3(345.1675F, 330.8004F, 169.3298F),
                    localScale = new Vector3(0.0185F, 0.0185F, 0.0185F)
                }
            });
            rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.29044F, -0.51197F, 0.02651F),
                    localAngles = new Vector3(316.3288F, 336.8533F, 74.11308F),
                    localScale = new Vector3(0.04F, 0.04F, 0.04F)
                }
            });
            rules.Add("mdlFalseSon", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.18826F, 0.32683F, -0.02996F),
                    localAngles = new Vector3(346.8619F, 106.0506F, 237.3382F),
                    localScale = new Vector3(0.0325F, 0.0325F, 0.0325F)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(2.02744F, 4.16847F, -3.79723F),
                    localAngles = new Vector3(327.7698F, 162.1231F, 284.9452F),
                    localScale = new Vector3(0.8F, 0.8F, 0.8F)
                }
            });

            //Modded Chars 
            rules.Add("EnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "CalfL",
                    localPos = new Vector3(-0.15982F, 0.06808F, -0.00838F),
                    localAngles = new Vector3(359.9521F, 121.1408F, 153.0061F),
                    localScale = new Vector3(0.0325F, 0.0325F, 0.0325F)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00611F, 0.0034F, -0.00416F),
                    localAngles = new Vector3(11.57737F, 25.63622F, 13.12972F),
                    localScale = new Vector3(0.001F, 0.001F, 0.001F)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(-0.11139F, 0.02734F, 0.03691F),
                    localAngles = new Vector3(7.69428F, 144.7946F, 187.6393F),
                    localScale = new Vector3(0.045F, 0.045F, 0.045F)
                }
            });
            //rules.Add("mdlChef", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Door",
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            rules.Add("mdlMiner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.00189F, 0.00154F, -0.00106F),
                    localAngles = new Vector3(348.1617F, 19.799F, 353.5999F),
                    localScale = new Vector3(0.00035F, 0.00035F, 0.00035F)
                }
            });
            rules.Add("mdlSniper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.06477F, 0.41519F, -0.24461F),
                    localAngles = new Vector3(67.29826F, 309.5822F, 271.2281F),
                    localScale = new Vector3(0.025F, 0.025F, 0.025F)
                }
            });
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.09449F, -0.12542F, 0.05142F),
                    localAngles = new Vector3(325.0607F, 306.898F, 197.5119F),
                    localScale = new Vector3(0.035F, 0.035F, 0.035F)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Sheath",
                    localPos = new Vector3(-0.03694F, 0.28826F, 0.00881F),
                    localAngles = new Vector3(327.2277F, 78.62277F, 295.7784F),
                    localScale = new Vector3(0.025F, 0.025F, 0.025F)
                }
            });
            //rules.Add("ExecutionerBody", new RoR2.ItemDisplayRule[] //i just dont want to do these rn
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0, 0, 0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
            //rules.Add("NemmandoBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0, 0, 0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
            rules.Add("mdlDeputy", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Scarf10",
                    localPos = new Vector3(0.00039F, 0.00713F, -0.00379F),
                    localAngles = new Vector3(7.54985F, 323.0166F, 16.47126F),
                    localScale = new Vector3(0.02F, 0.02F, 0.02F)
                }
            });
            rules.Add("mdlPathfinder", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.00868F, 0.31539F, 0.06701F),
                    localAngles = new Vector3(344.5747F, 24.60089F, 181.7561F),
                    localScale = new Vector3(0.02F, 0.02F, 0.02F)
                }
            });
            rules.Add("mdlHANDOverclocked", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.54485F, 0.3602F, -1.1066F),
                    localAngles = new Vector3(327.0255F, 341.1051F, 302.752F),
                    localScale = new Vector3(0.125F, 0.125F, 0.125F)
                }
            });
            rules.Add("mdlRocket", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.22044F, 0.14516F, 0.26083F),
                    localAngles = new Vector3(12.03786F, 325.8667F, 14.64504F),
                    localScale = new Vector3(0.025F, 0.025F, 0.025F)
                }
            });
            //rules.Add("mdlDaredevil", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0, 0, 0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
            rules.Add("mdlRMOR", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.80316F, 1.02949F, -0.3804F),
                    localAngles = new Vector3(350.3488F, 31.96596F, 202.6191F),
                    localScale = new Vector3(0.125F, 0.125F, 0.125F)
                }
            });
            //rules.Add("Spearman", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0, 0, 0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});
            rules.Add("mdlAssassin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "leg_bone2.R",
                    localPos = new Vector3(0.26549F, 0.2136F, 0.08081F),
                    localAngles = new Vector3(2.21937F, 106.0493F, 151.2813F),
                    localScale = new Vector3(0.05F, 0.05F, 0.05F)
                }
            });
            rules.Add("mdlExecutioner2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.06362F, -0.18213F, 0.09059F),
                    localAngles = new Vector3(30.5121F, 307.7704F, 8.7679F),
                    localScale = new Vector3(0.0125F, 0.0125F, 0.0125F)
                }
            });
            rules.Add("mdlNemCommando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(0.74333F, 0.58681F, 0.47747F),
                    localAngles = new Vector3(22.97359F, 86.93414F, 130.3491F),
                    localScale = new Vector3(0.075F, 0.075F, 0.075F)
                }
            });
            rules.Add("mdlNemMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.1141284f, -0.01527959f, -0.1664781f),
                    localAngles = new Vector3(25.04979f, 65.70757f, 156.8913f),
                    localScale = new Vector3(.02f, .02f, .02f)
                }
            });
            rules.Add("mdlChirr", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.23808F, -0.05379F, -0.31237F),
                    localAngles = new Vector3(341.6718F, 30.94658F, 334.0133F),
                    localScale = new Vector3(0.045F, 0.045F, 0.045F)
                }
            });
            //rules.Add("RobDriverBody", new RoR2.ItemDisplayRule[]
            //{
            //    new RoR2.ItemDisplayRule
            //    {
            //        ruleType = ItemDisplayRuleType.ParentedPrefab,
            //        followerPrefab = ItemBodyModelPrefab,
            //        childName = "Pelvis",
            //        localPos = new Vector3(0, 0, -0),
            //        localAngles = new Vector3(0, 0, 0),
            //        localScale = new Vector3(1, 1, 1)
            //    }
            //});

            rules.Add("mdlTeslaTrooper", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.13354F, -0.03074F, 0.307F),
                    localAngles = new Vector3(335.7875F, 132.2111F, 307.9876F),
                    localScale = new Vector3(0.02F, 0.02F, 0.02F)
                }
            });
            rules.Add("mdlDesolator", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(-0.20529F, 0.16745F, 0.34091F),
                    localAngles = new Vector3(23.22827F, 299.3157F, 29.11657F),
                    localScale = new Vector3(0.0175F, 0.02018F, 0.0175F)
                }
            });
            rules.Add("mdlArsonist", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperLegL",
                    localPos = new Vector3(-0.08814F, 0.01758F, -0.13407F),
                    localAngles = new Vector3(8.06379F, 253.637F, 155.5879F),
                    localScale = new Vector3(0.02F, 0.02F, 0.02F)
                }
            });

            rules.Add("RA2ChronoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(-0.07913F, 0.34324F, -0.1756F),
                    localAngles = new Vector3(9.55309F, 56.66245F, 156.6636F),
                    localScale = new Vector3(0.035F, 0.035F, 0.035F)
                }
            });
            rules.Add("RobRavagerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1186F, 0.04692F, -0.04895F),
                    localAngles = new Vector3(352.705F, 312.8735F, 192.3628F),
                    localScale = new Vector3(0.025F, 0.025F, 0.025F)
                }
            });
            rules.Add("mdlMorris", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.10511F, 0.01306F, 0.28336F),
                    localAngles = new Vector3(344.4371F, 9.65494F, 216.0121F),
                    localScale = new Vector3(0.04F, 0.04F, 0.04F)
                }
            });
            return rules;
        }

        public override void Hooks()
        {
            On.RoR2.SceneDirector.PopulateScene += AddCornucopiaCamp;

            IL.EntityStates.DeepVoidPortalBattery.Charging.OnEnter += OverrideBatteryChargingEnter;
            IL.EntityStates.DeepVoidPortalBattery.Charged.OnEnter += OverrideBatteryChargedEnter;
        }

        private void OverrideBatteryChargingEnter(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool ILFound = c.TryGotoNext(MoveType.Before,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<EntityStates.DeepVoidPortalBattery.Charging>(nameof(EntityStates.DeepVoidPortalBattery.Charging.holdoutZoneController)),
            x => x.MatchStfld<RoR2.UI.ChargeIndicatorController>(nameof(RoR2.UI.ChargeIndicatorController.holdoutZoneController)),
            x => x.MatchCallOrCallvirt<NetworkServer>("get_" + nameof(NetworkServer.active))
            );
            if (ILFound)
            {
                c.Index += 4;
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Func<bool, EntityStates.DeepVoidPortalBattery.Charging, bool>>((boolean, self) =>
                {
                    if (boolean)
                    {
                        var token = self.GetComponent<VoidShellIdentifierToken>();
                        if (token)
                        {
                            var vfx = self.transform.Find("VoidShellVFX");
                            if (vfx) 
                            {
                                vfx.gameObject.SetActive(false);
                            }
                            var combat = self.GetComponent<CombatDirector>();
                            if (combat)
                            {
                                combat.enabled = true;
                            }
                            Transform center = self.transform.Find("Model");
                            //Debug.Log("ahhh!! " + center);
                            if (center)
                            {
                                var cd2 = center.gameObject.GetComponent<CombatDirector>();
                                cd2.enabled = true;
                                cd2.monsterSpawnTimer = 0;
                                //cd2.SetNextSpawnAsBoss();
                            }

                            var fogcontroller = self.GetComponent<FogDamageController>();
                            //Debug.Log("ahhh!! " + fogcontroller);
                            if (fogcontroller && enableFog.Value)
                            {
                                var hzc = self.GetComponent<HoldoutZoneController>();
                                fogcontroller.enabled = true;
                                fogcontroller.AddSafeZone(hzc);
                                //Debug.Log("ahhh!! " + fogcontroller + " | " + hzc);
                                //fogcontroller.initialSafeZones = 1;
                            }

                            return false;
                        }
                    }
                    return boolean;
                });
            }
            else
            {
                Debug.Log("charging hook failed");
            }
        }

        private void OverrideBatteryChargedEnter(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool ILFound = c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt(typeof(VoidStageMissionController).GetMethod("get_instance")),
            x => x.MatchCallOrCallvirt(typeof(UnityEngine.Object).GetMethod("op_Implicit"))
            );

            if (ILFound)
            {
                //c.Index += 2;
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Func<bool, EntityStates.DeepVoidPortalBattery.Charged, bool>>((boolean, self) =>
                {
                    if (NetworkServer.active)
                    {
                        var token = self.GetComponent<VoidShellIdentifierToken>();
                        if (token)
                        {
                            var combat = self.GetComponent<CombatDirector>();
                            if (combat)
                            {
                                combat.enabled = false;
                                combat.monsterCredit = 0;
                            }
                            Transform center = self.transform.Find("Model");
                            if (center)
                            {
                                var combat2 = center.gameObject.GetComponent<CombatDirector>();
                                combat2.enabled = false;
                                combat2.monsterCredit = 0;
                                
                            }

                            var fogcontroller = self.GetComponent<FogDamageController>();
                            if (fogcontroller && enableFog.Value)
                            {
                                fogcontroller.enabled = false;
                            }

                            if (voidCellRng == null)
                            {
                                voidCellRng = new Xoroshiro128Plus(Run.instance.seed);
                            }

                            //int validPlayers = 0;
                            List<int> validPlayerList = new List<int>();
                            int total = 0;
                            int players = 0;
                            foreach (var player in PlayerCharacterMasterController.instances)
                            {
                                int itemCount = player.master.inventory.GetItemCount(ItemBase<VoidShell>.instance.ItemDef);
                                if (itemCount > 0)
                                {
                                    //++validPlayers;
                                    validPlayerList.Add(itemCount);
                                    total += itemCount;
                                }
                                ++players;
                            }


                            if (multiplayerScaling.Value)
                            {
                                //int num = validPlayerList.Count;
                                float angle = 360f / (float)players;
                                Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * (float)20f + Vector3.forward * 5f);
                                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                                Vector3 position = self.transform.position + (1.5f * Vector3.up);
                                //VoidShellDropTable dropTable = ScriptableObject.CreateInstance<VoidShellDropTable>();
                                //int i = 0;
                                for (int i = 0; i < players; ++i)
                                {
                                    VoidShellTriple triple = dropTable.GenerateDropPreReplacementForPlayer(voidCellRng, total);
                                    PickupPickerController.Option[] options = new PickupPickerController.Option[3];

                                    options[0].pickupIndex = triple.slot1;
                                    options[1].pickupIndex = triple.slot2;
                                    options[2].pickupIndex = triple.slot3;

                                    options[0].available = true;
                                    options[1].available = true;
                                    options[2].available = true;

                                    PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo{
                                        pickerOptions = options,
                                        rotation = Quaternion.identity,
                                        prefabOverride = voidPotentialPrefab,
                                        pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.VoidTier1),
                                        
                                    }, position, vector);
                                    vector = rotation * vector;
                                }
                            }
                            else
                            {
                                //int num = validPlayerList.Count;
                                float angle = 360f / (float)validPlayerList.Count;
                                Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * (float)20f + Vector3.forward * 5f);
                                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                                Vector3 position = self.transform.position + (1.5f * Vector3.up);
                                //VoidShellDropTable dropTable = ScriptableObject.CreateInstance<VoidShellDropTable>();
                                //int i = 0;
                                foreach (int player in validPlayerList){
                                    VoidShellTriple triple = dropTable.GenerateDropPreReplacementForPlayer(voidCellRng, player);
                                    PickupPickerController.Option[] options = new PickupPickerController.Option[3];

                                    options[0].pickupIndex = triple.slot1;
                                    options[1].pickupIndex = triple.slot2;
                                    options[2].pickupIndex = triple.slot3;

                                    options[0].available = true;
                                    options[1].available = true;
                                    options[2].available = true;

                                    PickupDropletController.CreatePickupDroplet(new GenericPickupController.CreatePickupInfo
                                    {
                                        pickerOptions = options,
                                        rotation = Quaternion.identity,
                                        prefabOverride = voidPotentialPrefab,
                                        pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.VoidTier1),
                                    }, position, vector);
                                    vector = rotation * vector;
                                }
                            }
                            return false;
                        }
                        else if (VoidStageMissionController.instance)
                        {
                            //Debug.Log("was mission controller");
                            return true;
                        }
                        else
                        {
                            //Debug.Log("wasnt mission controller");
                            return false;
                        }
                    }
                    else
                    {
                        return boolean;
                    }
                });

            }
            else
            {
                Debug.Log("error with Shell IL hook");
            }
        }

        private void AddCornucopiaCamp(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self) 
        {
            orig(self);

            var sceneName = SceneCatalog.GetSceneDefForCurrentScene().baseSceneName;

            if (sceneName == "bazaar" || sceneName == "limbo" || sceneName == "mysteryspace" || sceneName == "voidraid" || sceneName == "artifactworld")
            {
                return;
            }
            else if (sceneName == "moon" || sceneName == "moon2" || sceneName == "goldshores")
            {
                if (!specialHappen.Value)
                {
                    return;
                }
            }

            if (voidCellRng == null)
            {
                voidCellRng = new Xoroshiro128Plus(Run.instance.seed);
            }

            var moneywaves = new CombatDirector.DirectorMoneyWave[2];
            for (int i = 0; i < 2; i++)
            {
                moneywaves[i] = new CombatDirector.DirectorMoneyWave
                {
                    interval = voidCellRng.RangeFloat(1, 1),
                    multiplier = 2
                };
            }

            //var dir = cellInteractableCard.prefab.GetComponent<CombatDirector>();
            //if (dir)
            //{
            //    //dir.moneyWaveIntervals = 2;
            //    //dir.moneyWaves = moneywaves;
            //}

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
                DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(cellInteractableCard, new DirectorPlacementRule
                { placementMode = DirectorPlacementRule.PlacementMode.Random }, voidCellRng));
            }
           
        }
    }

    public class VoidShellIdentifierToken : MonoBehaviour
    {

    }

    public class VoidShellTriple : MonoBehaviour
    {
        public PickupIndex slot1 = PickupIndex.none;
        public PickupIndex slot2 = PickupIndex.none;
        public PickupIndex slot3 = PickupIndex.none;
    }

    public class VoidShellDropTable : PickupDropTable
    {
        // Token: 0x06001A87 RID: 6791 RVA: 0x00071DE0 File Offset: 0x0006FFE0
        private void Add(List<PickupIndex> sourceDropList, float listWeight)
        {
            if (listWeight <= 0f || sourceDropList.Count == 0)
            {
                return;
            }
            float weight = listWeight / (float)sourceDropList.Count;
            foreach (PickupIndex value in sourceDropList)
            {
                selector.AddChoice(value, weight);
            }
        }

        // Token: 0x06001A88 RID: 6792 RVA: 0x00071E50 File Offset: 0x00070050
        public override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
        {
            int num = 0;
            foreach (var player in PlayerCharacterMasterController.instances)
            {
                int itemCount = player.master.inventory.GetItemCount(DLC1Content.Items.FreeChest);
                num += itemCount;
            }
            selector.Clear();
            Add(Run.instance.availableTier1DropList, tier1Weight);
            Add(Run.instance.availableTier2DropList, tier2Weight * (float)num);
            Add(Run.instance.availableTier3DropList, tier3Weight * Mathf.Pow((float)num, 2f));
            Add(Run.instance.availableVoidTier1DropList, tier1WeightVoid);
            Add(Run.instance.availableVoidTier2DropList, tier2WeightVoid * (float)num);
            Add(Run.instance.availableVoidTier3DropList, tier3WeightVoid * Mathf.Pow((float)num, 2.05f));

            return PickupDropTable.GenerateDropFromWeightedSelection(rng, selector);
        }

        public VoidShellTriple GenerateDropPreReplacementForPlayer(Xoroshiro128Plus rng, int count)
        {
            selector.Clear();
            Add(Run.instance.availableTier1DropList, tier1Weight);
            Add(Run.instance.availableTier2DropList, tier2Weight * (float)count);
            Add(Run.instance.availableTier3DropList, tier3Weight * Mathf.Pow((float)count, 2f));
            Add(Run.instance.availableVoidTier1DropList, tier1WeightVoid);
            Add(Run.instance.availableVoidTier2DropList, tier2WeightVoid * (float)count);
            Add(Run.instance.availableVoidTier3DropList, tier3WeightVoid * Mathf.Pow((float)count, 2.05f));

            VoidShellTriple triple = new VoidShellTriple();

            triple.slot1 = PickupDropTable.GenerateDropFromWeightedSelection(rng, selector);
            triple.slot2 = PickupDropTable.GenerateDropFromWeightedSelection(rng, selector); 
            triple.slot3 = PickupDropTable.GenerateDropFromWeightedSelection(rng, selector);
            
            while(triple.slot1 == triple.slot2 || triple.slot2 == triple.slot3)
            {
                triple.slot2 = PickupDropTable.GenerateDropFromWeightedSelection(rng, selector);
            }

            while (triple.slot1 == triple.slot3)
            {
                triple.slot3 = PickupDropTable.GenerateDropFromWeightedSelection(rng, selector);
            }

            return triple;
        }

        // Token: 0x06001A89 RID: 6793 RVA: 0x00071F14 File Offset: 0x00070114
        public override int GetPickupCount()
        {
            return selector.Count;
        }

        // Token: 0x06001A8A RID: 6794 RVA: 0x00071F21 File Offset: 0x00070121
        public override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
        {
            return PickupDropTable.GenerateUniqueDropsFromWeightedSelection(maxDrops, rng, selector);
        }

        //private float tier1Weight = 0.79f;
        //
        //private float tier2Weight = 0.2f;
        //
        //private float tier3Weight = 0.01f;

        private float tier1Weight = VoidShell.instance.ShellTier1Weight.Value; //.316f;

        private float tier2Weight = VoidShell.instance.ShellTier2Weight.Value; //.08f;

        private float tier3Weight = VoidShell.instance.ShellTier3Weight.Value; //.004f;

        private float tier1WeightVoid = VoidShell.instance.ShellTier1VoidWeight.Value; //.474f;

        private float tier2WeightVoid = VoidShell.instance.ShellTier2VoidWeight.Value; //.12f;

        private float tier3WeightVoid = VoidShell.instance.ShellTier3VoidWeight.Value; //.006f;

        private readonly WeightedSelection<PickupIndex> selector = new WeightedSelection<PickupIndex>(8);
    }

    //public class LostBatteryBaseState : BaseState
    //{
    //    public override void OnEnter()
    //    {
    //        base.OnEnter();
    //
    //    }
    //
    //    public override void OnExit()
    //    {
    //        base.OnExit();
    //    }
    //}
    //
    //public class LostBatteryIdle : LostBatteryBaseState
    //{
    //    public override void OnEnter()
    //    {
    //        base.OnEnter();
    //
    //    }
    //
    //    public override void OnExit()
    //    {
    //        base.OnExit();
    //    }
    //}
    //
    //public class LostBatteryCharging : LostBatteryBaseState
    //{
    //    public override void OnEnter()
    //    {
    //        base.OnEnter();
    //
    //    }
    //
    //    public override void OnExit()
    //    {
    //        base.OnExit();
    //    }
    //}
    //
    //public class LostBatteryCharged: LostBatteryBaseState
    //{
    //    public override void OnEnter()
    //    {
    //        base.OnEnter();
    //
    //    }
    //
    //    public override void OnExit()
    //    {
    //        base.OnExit();
    //    }
    //}
}
