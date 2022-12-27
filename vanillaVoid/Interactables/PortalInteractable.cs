using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Hologram;
using RoR2.Items;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace vanillaVoid.Interactables
{
    public class ShatteredMonolith : InteractableBase<ShatteredMonolith>
    {
        public ConfigEntry<bool> EnableBuffCatalogSelection;

        public override string InteractableName => "Shattered Monolith";

        public override string InteractableContext => "Tear a hole in reality?";

        public override string InteractableLangToken => "SHATTERED_SHRINE";

        public override GameObject InteractableModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlVoidShrine.prefab");

        public static GameObject InteractableBodyModelPrefab;

        public static InteractableSpawnCard InteractableSpawnCard;

        public static InteractableSpawnCard VoidFieldsPortalCard;

        public static GameObject voidFieldPortalObject;

        public static List<PickupIndex> voidItemsList;

        public static CostTypeDef voidCostDef;
        public static int voidCostTypeIndex;

        public bool hasAddedMonolith;
        public static DirectorCard MonolithCard;
        //public static GameObject BrazierBuffFlameOrb;
        //
        //public static GameObject BrazierBuffOrbitOrb;
        //
        //public static GameObject BrazierFieldEffectPrefab;

        //public static List<BrazierBuffCuratedType> CuratedBuffList = new List<BrazierBuffCuratedType>();

        public override void Init(ConfigFile config)
        {
            hasAddedMonolith = false;
            CreateConfig(config);
            CreateLang();

            CostTypeCatalog.modHelper.getAdditionalEntries += addVoidItemType;
            On.RoR2.CampDirector.SelectCard += VoidCampAddMonolith;
            //voidItemsList = new List<PickupIndex>();
            //voidItemsList.Union(Run.instance.availableVoidBossDropList).Union(Run.instance.availableVoidTier1DropList).Union(Run.instance.availableVoidTier2DropList).Union(Run.instance.availableVoidTier3DropList);

            CreateInteractable();
            CreateInteractableSpawnCard();

            //Hooks();
        }

        //private void getAdditionalEntries2(List<CostTypeDef> obj)
        //{
        //    voidCostDef = new CostTypeDef();
        //    voidCostDef.costStringFormatToken = "COST_VOIDITEM_FORMAT";
        //    //voidCostDef.isAffordable = new CostTypeDef.IsAffordableDelegate(VoidItemCostTypeHelper.IsAffordable);
        //    voidCostDef.isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
        //    {
        //        CharacterBody component = context.activator.GetComponent<CharacterBody>();
        //        if (!component)
        //        {
        //            return false;
        //        }
        //        Inventory inventory = component.inventory;
        //        if (!inventory)
        //        {
        //            return false;
        //        }
        //        int cost = context.cost;
        //        //int num = 0;
        //        int itemCount = inventory.GetTotalItemCountOfTier(ItemTier.VoidTier1) + inventory.GetTotalItemCountOfTier(ItemTier.VoidTier2) + inventory.GetTotalItemCountOfTier(ItemTier.VoidTier3) + inventory.GetTotalItemCountOfTier(ItemTier.VoidBoss);
        //        if (itemCount >= cost)
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    };
        //    voidCostDef.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
        //    {
        //        Inventory inventory = context.activator.GetComponent<CharacterBody>().inventory;
        //        int cost = context.cost;
        //
        //        for (int k = 0; k < cost; k++)
        //        {
        //            TakeOne();
        //        }
        //
        //        MultiShopCardUtils.OnNonMoneyPurchase(context);
        //        void TakeOne()
        //        {
        //            voidItemsList = new List<PickupIndex>();
        //            voidItemsList.Union(Run.instance.availableVoidBossDropList).Union(Run.instance.availableVoidTier1DropList).Union(Run.instance.availableVoidTier2DropList).Union(Run.instance.availableVoidTier3DropList);
        //
        //            var list = voidItemsList;
        //            Util.ShuffleList(list, context.rng);
        //            for (int i = 0; i < list.Count(); i++)
        //            {
        //                if (inventory.GetItemCount(list[i].itemIndex) > 0)
        //                {
        //                    inventory.RemoveItem(list[i].itemIndex);
        //                    context.results.itemsTaken.Add(list[i].itemIndex);
        //                    cost--;
        //
        //                }
        //                if (cost <= 0)
        //                {
        //                    break;
        //                }
        //            }
        //        }
        //    };
        //
        //    voidCostDef.colorIndex = ColorCatalog.ColorIndex.VoidItem;
        //    voidCostDef.saturateWorldStyledCostString = true;
        //    voidCostDef.darkenWorldStyledCostString = false;
        //    voidCostTypeIndex = CostTypeCatalog.costTypeDefs.Length + obj.Count;
        //    Debug.Log("voidcosttypeindex: " + voidCostTypeIndex);
        //    obj.Add(voidCostDef);
        //}


        private void addVoidItemType(List<CostTypeDef> obj)
        {
            //CostTypeIndex voidItem = new CostTypeIndex();
            voidCostDef = new CostTypeDef();
            voidCostDef.costStringFormatToken = "COST_VOIDITEM_FORMAT";
            voidCostDef.isAffordable = new CostTypeDef.IsAffordableDelegate(VoidItemCostTypeHelper.IsAffordable);
            voidCostDef.payCost = new CostTypeDef.PayCostDelegate(VoidItemCostTypeHelper.PayCost);
            voidCostDef.colorIndex = ColorCatalog.ColorIndex.VoidItem;
            voidCostDef.saturateWorldStyledCostString = true;
            voidCostDef.darkenWorldStyledCostString = false;
            voidCostTypeIndex = CostTypeCatalog.costTypeDefs.Length + obj.Count;
            obj.Add(voidCostDef);
        }

        //private void PopualateVoidItemList()
        //{
        //
        //    //ItemDef[] array = RoR2.ItemCatalog.;
        //    //var voiddroptable = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/Common/dtVoidChest.asset"); //RoR2/DLC1/TreasureCacheVoid/dtVoidLockbox.asset??
        //    //voiddroptable.
        //    List<PickupIndex> voiditems = (Run.instance.availableVoidBossDropList);
        //    voiditems.Union(Run.instance.availableVoidTier1DropList).Union(Run.instance.availableVoidTier2DropList).Union(Run.instance.availableVoidTier3DropList);
        //    //voiditems = (Run.instance.availableVoidBossDropList).Union(Run.instance.availableVoidTier1DropList).Union(Run.instance.availableVoidTier2DropList).Union(Run.instance.availableVoidTier3DropList);
        //    //voiditems.Union(Run.instance.availableVoidTier1DropList);//.Append(Run.instance.availableVoidTier1DropList);// app Run.instance.availableVoidTier1DropList
        //
        //
        //    //throw new NotImplementedException();
        //}

        private void CreateConfig(ConfigFile config)
        {
            //EnableBuffCatalogSelection = config.Bind<bool>("Interactable: " + InteractableName, "Enable All BuffCatalog Entries for Flame Selection?", false, "If set to true, the Buff Brazier will select buffs from the entire buff catalog instead of the curated list.");
        }

        //private void CreateEffect()
        //{
        //    BrazierBuffFlameOrb = MainAssets.LoadAsset<GameObject>("BuffBrazierOrbEffect.prefab");
        //
        //    var effectComponent = BrazierBuffFlameOrb.AddComponent<EffectComponent>();
        //
        //    var vfxAttributes = BrazierBuffFlameOrb.AddComponent<VFXAttributes>();
        //    vfxAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Low;
        //    vfxAttributes.vfxPriority = VFXAttributes.VFXPriority.Always;
        //
        //    BrazierBuffFlameOrb.AddComponent<NetworkIdentity>();
        //
        //    var orbEffect = BrazierBuffFlameOrb.AddComponent<OrbEffect>();
        //    //orbEffect.startEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShieldBreakEffect");
        //    //orbEffect.endEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MuzzleFlashes/MuzzleFlashMageIce");
        //    orbEffect.startVelocity1 = new Vector3(-10, 10, -10);
        //    orbEffect.startVelocity2 = new Vector3(10, 13, 10);
        //    orbEffect.endVelocity1 = new Vector3(-10, 0, -10);
        //    orbEffect.endVelocity2 = new Vector3(10, 5, 10);
        //    orbEffect.movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        //
        //    var detachParticleOnDestroy = BrazierBuffFlameOrb.AddComponent<DetachParticleOnDestroyAndEndEmission>();
        //    detachParticleOnDestroy.particleSystem = BrazierBuffFlameOrb.transform.Find("Fire Icon/Fire Icon Particle System/Fire Icon Trail").gameObject.GetComponent<ParticleSystem>();
        //
        //    BrazierBuffFlameOrb.transform.Find("Fire Icon").gameObject.AddComponent<Billboard>();
        //
        //    var visualController = BrazierBuffFlameOrb.AddComponent<BuffBrazierOrbVisualController>();
        //    visualController.IsVisualOrb = true;
        //
        //    if (BrazierBuffFlameOrb) PrefabAPI.RegisterNetworkPrefab(BrazierBuffFlameOrb);
        //    ContentAddition.AddEffect(BrazierBuffFlameOrb);
        //
        //    OrbAPI.AddOrb(typeof(Effect.BuffBrazierFlameOrb));
        //}

        //private void CreateNetworkObjects()
        //{
        //    BrazierBuffOrbitOrb = MainAssets.LoadAsset<GameObject>("BuffBrazierOrbitOrb.prefab");
        //
        //    BrazierBuffOrbitOrb.AddComponent<NetworkIdentity>();
        //
        //    BrazierBuffOrbitOrb.AddComponent<SetDontDestroyOnLoad>();
        //
        //    BrazierBuffOrbitOrb.AddComponent<BuffBrazierOrbitVisualAndNetworkController>();
        //
        //    BrazierBuffOrbitOrb.transform.Find("Fire Icon").gameObject.AddComponent<Billboard>();
        //
        //    var scaleCurve = BrazierBuffOrbitOrb.AddComponent<ObjectScaleCurve>();
        //    scaleCurve.overallCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.1f, 0.6f));
        //    scaleCurve.useOverallCurveOnly = true;
        //
        //    if (BrazierBuffOrbitOrb) { PrefabAPI.RegisterNetworkPrefab(BrazierBuffOrbitOrb); }
        //
        //    BrazierFieldEffectPrefab = MainAssets.LoadAsset<GameObject>("BuffBrazierActiveField.prefab");
        //    BrazierFieldEffectPrefab.AddComponent<BuffBrazierFieldController>();
        //
        //    PrefabAPI.RegisterNetworkPrefab(BrazierFieldEffectPrefab);
        //}

        public void CreateInteractable()
        {
            //CostTypeCatalog.modHelper.getAdditionalEntries += (List<CostTypeDef> list) => { };

            //CostTypeIndex voidItem = new CostTypeIndex();
            //CostTypeCatalog.Register(voidItem, new CostTypeDef
            //{
            //    costStringFormatToken = "COST_VOIDITEM_FORMAT",
            //    isAffordable = new CostTypeDef.IsAffordableDelegate(VoidItemCostTypeHelper.IsAffordable),
            //    payCost = new CostTypeDef.PayCostDelegate(VoidItemCostTypeHelper.PayCost),
            //    colorIndex = ColorCatalog.ColorIndex.VoidItem,
            //    //itemTier = ItemTier.VoidTier1
            //});

            InteractableBodyModelPrefab = InteractableModel;
            InteractableBodyModelPrefab.AddComponent<NetworkIdentity>();

            var purchaseInteraction = InteractableBodyModelPrefab.AddComponent<PurchaseInteraction>();
            purchaseInteraction.displayNameToken = $"INTERACTABLE_{InteractableLangToken}_NAME";
            purchaseInteraction.contextToken = $"INTERACTABLE_{InteractableLangToken}_CONTEXT";
            purchaseInteraction.costType = (CostTypeIndex)voidCostTypeIndex;
            purchaseInteraction.automaticallyScaleCostWithDifficulty = false;
            purchaseInteraction.cost = 1;
            purchaseInteraction.available = true;
            purchaseInteraction.setUnavailableOnTeleporterActivated = true;
            purchaseInteraction.isShrine = true;
            purchaseInteraction.isGoldShrine = false;

            //voidCostDef.inde
            //CostTypeIndex voidItem = new CostTypeIndex();
            //CostTypeDef voidDef = new CostTypeDef();
            //voidDef.costStringFormatToken = "COST_VOIDITEM_FORMAT";
            //voidDef.saturateWorldStyledCostString = false;
            //voidDef.darkenWorldStyledCostString = true;
            //voidDef.isAffordable = delegate(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
            //{
            //    Inventory component = context.activator.GetComponent<Inventory>();
            //    return component 
            //}

            


            var pingInfoProvider = InteractableBodyModelPrefab.AddComponent<PingInfoProvider>();
            pingInfoProvider.pingIconOverride = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("texShrineIconOutlined");

            //var entityStateMachine = InteractableBodyModelPrefab.AddComponent<EntityStateMachine>();
            //entityStateMachine.customName = "Body";
            //entityStateMachine.initialStateType.stateType = typeof(MyEntityStates.BuffBrazier.BuffBrazierMainState);
            //entityStateMachine.mainStateType.stateType = typeof(MyEntityStates.BuffBrazier.BuffBrazierMainState);

            //var networkStateMachine = InteractableBodyModelPrefab.AddComponent<NetworkStateMachine>();
            //networkStateMachine.stateMachines = new EntityStateMachine[] { entityStateMachine };

            var genericNameDisplay = InteractableBodyModelPrefab.AddComponent<GenericDisplayNameProvider>();
            genericNameDisplay.displayToken = $"INTERACTABLE_{InteractableLangToken}_NAME";

            var interactionToken = InteractableBodyModelPrefab.AddComponent<PortalInteractableToken>();
            interactionToken.PurchaseInteraction = purchaseInteraction;

            //string templeObelisk = "RoR2/Base/wispgraveyard/matTempleObelisk.mat";
            //string otherMaterial = "RoR2/Base/Common/TrimSheets/matTrimsheetPurpleStoneGrassy.mat";
            ////
            //var meshrenderer = InteractableBodyModelPrefab.GetComponentsInChildren<MeshRenderer>().Where(x => x.gameObject.name.Contains("mdlVoidShrine")).First();
            //meshrenderer.material = Addressables.LoadAssetAsync<Material>(otherMaterial).WaitForCompletion();
            //for(int i = 0; i < materials.Length; i++)
            //{
            //    Debug.Log("fart!!! " + i + ": " + materials[i].name);
            //}
            //materials[1] = Addressables.LoadAssetAsync<Material>(templeObelisk).WaitForCompletion();
            //materials[2] = Addressables.LoadAssetAsync<Material>(otherMaterial).WaitForCompletion();
            //interactableMain.material = Addressables.LoadAssetAsync<Material>(templeObelisk).WaitForCompletion();
            //
            //var vialGlassDisplay = InteractableBodyModelPrefab.transform.Find("shrineBorder").GetComponent<MeshRenderer>();
            //vialGlassDisplay.material = Addressables.LoadAssetAsync<Material>(templeObelisk).WaitForCompletion();

            var entityLocator = InteractableBodyModelPrefab.GetComponentInChildren<MeshCollider>().gameObject.AddComponent<EntityLocator>();
            entityLocator.entity = InteractableBodyModelPrefab;

            var modelLocator = InteractableBodyModelPrefab.AddComponent<ModelLocator>();
            modelLocator.modelTransform = InteractableBodyModelPrefab.transform.Find("mdlVoidShrine");
            modelLocator.modelBaseTransform = modelLocator.modelTransform;//InteractableBodyModelPrefab.transform.Find("Base");
            modelLocator.dontDetatchFromParent = true;
            modelLocator.autoUpdateModelTransform = true;

            var highlightController = InteractableBodyModelPrefab.GetComponent<Highlight>();
            highlightController.targetRenderer = InteractableBodyModelPrefab.GetComponentsInChildren<MeshRenderer>().Where(x => x.gameObject.name.Contains("mdlVoidShrine")).First();
            highlightController.strength = 1;
            highlightController.highlightColor = Highlight.HighlightColor.interactive;

            var hologramController = InteractableBodyModelPrefab.AddComponent<HologramProjector>();
            hologramController.hologramPivot = InteractableBodyModelPrefab.transform.Find("HologramPivot");
            hologramController.displayDistance = 10;
            hologramController.disableHologramRotation = true;

            var childLocator = InteractableBodyModelPrefab.AddComponent<ChildLocator>(); //matMSObelisk
            childLocator.transformPairs = new ChildLocator.NameTransformPair[]
            {
                new ChildLocator.NameTransformPair()
                {
                    name = "FireworkOrigin",
                    transform = InteractableBodyModelPrefab.transform.Find("FireworkEmitter")
                }
            };

            //var billboard = InteractableBodyModelPrefab.transform.Find("BillboardPivot").gameObject.AddComponent<Billboard>();

            //ContentAddition.AddEntityState<MyEntityStates.BuffBrazier.BuffBrazierMainState>(out _);
            //ContentAddition.AddEntityState<MyEntityStates.BuffBrazier.BuffBrazierPurchased>(out _);
            PrefabAPI.RegisterNetworkPrefab(InteractableBodyModelPrefab);

            voidFieldPortalObject = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion();
            //var eventflag = voidFieldPortalObject.AddComponent<RunEventFlagResponse>();
            //eventflag.flagName = "ArenaPortalTaken";
            //var eventfunc = voidFieldPortalObject.AddComponent<EventFunctions>();
            var exitctr = voidFieldPortalObject.GetComponent<SceneExitController>();
            exitctr.useRunNextStageScene = false;
            //var tempLight = portalObject.GetComponentInChildren<Light>();
            //if (tempLight)
            //{
            //    tempLight.enabled = false;
            //}

        }

        public void CreateInteractableSpawnCard()
        {
            InteractableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            InteractableSpawnCard.name = "iscVoidPortalInteractable";
            InteractableSpawnCard.prefab = InteractableBodyModelPrefab;
            InteractableSpawnCard.sendOverNetwork = true;
            InteractableSpawnCard.hullSize = HullClassification.Golem;
            InteractableSpawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            InteractableSpawnCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            InteractableSpawnCard.forbiddenFlags = RoR2.Navigation.NodeFlags.NoShrineSpawn | RoR2.Navigation.NodeFlags.NoChestSpawn;
            InteractableSpawnCard.directorCreditCost = 20;
            InteractableSpawnCard.occupyPosition = true;
            InteractableSpawnCard.orientToFloor = false;
            InteractableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;
            InteractableSpawnCard.maxSpawnsPerStage = 1;

            MonolithCard = new DirectorCard
            {
                selectionWeight = 10,
                spawnCard = InteractableSpawnCard,
                minimumStageCompletions = 0,
                //allowAmbushSpawn = true, TODO removed i think?
            };

            //DirectorAPI.Helpers.AddNewInteractable(directorCard, DirectorAPI.InteractableCategory.VoidStuff);

            //something something vanillavoid something "the shit way" etc
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.AbandonedAqueduct);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.AbyssalDepths);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.AphelianSanctuary);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.DistantRoost);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.RallypointDelta);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.ScorchedAcres);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.SiphonedForest);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.SirensCall);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.SkyMeadow);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.SulfurPools);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.SunderedGrove);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.TitanicPlains);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.WetlandAspect);

            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.Custom, "FBLScene");
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.Custom, "drybasin");


            //DirectorAPI.DirectorCardHolder.
            //CampDirector.cardSelector.AddChoice(directorCard, 1f);


            VoidFieldsPortalCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            VoidFieldsPortalCard.name = "iscSpecialVoidFieldPortal";
            VoidFieldsPortalCard.prefab = voidFieldPortalObject;
            VoidFieldsPortalCard.sendOverNetwork = true;
            VoidFieldsPortalCard.hullSize = HullClassification.Golem;
            VoidFieldsPortalCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            VoidFieldsPortalCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            VoidFieldsPortalCard.forbiddenFlags = RoR2.Navigation.NodeFlags.None;
            VoidFieldsPortalCard.directorCreditCost = 0;
            VoidFieldsPortalCard.occupyPosition = true;
            VoidFieldsPortalCard.orientToFloor = false;
            VoidFieldsPortalCard.skipSpawnWhenSacrificeArtifactEnabled = false;
            VoidFieldsPortalCard.maxSpawnsPerStage = 0;

            DirectorCard directorCard2 = new DirectorCard
            {
                selectionWeight = 0,
                spawnCard = VoidFieldsPortalCard,
                minimumStageCompletions = 0,
                //allowAmbushSpawn = true, TODO removed i think?
            };
            //DirectorAPI.DirectorCardHolder dirCardHolder2 = new DirectorAPI.DirectorCardHolder
            //{
            //    Card = directorCard2,
            //    MonsterCategory = DirectorAPI.MonsterCategory.Invalid,
            //    InteractableCategory = DirectorAPI.InteractableCategory.
            //};
            DirectorAPI.Helpers.AddNewInteractable(directorCard2, DirectorAPI.InteractableCategory.VoidStuff);

        }

        private DirectorCard VoidCampAddMonolith(On.RoR2.CampDirector.orig_SelectCard orig, CampDirector self, WeightedSelection<DirectorCard> deck, int maxCost)
        {
            hasAddedMonolith = false;
            if (self.name == "Camp 1 - Void Monsters & Interactables")
            {
                for (int i = deck.Count - 1; i >= 0; i--)
                {
                    if (deck.GetChoice(i).value.spawnCard.name == "iscVoidPortalInteractable")
                    {
                        hasAddedMonolith = true;
                        break;
                    }
                    //Debug.Log("card name: " + deck.GetChoice(i).value.spawnCard.name + " | weight: " + deck.GetChoice(i).weight);
                }
                //Debug.Log("name: " + self.name + " | elite: " + self.eliteDef);
                //WeightedSelection<DirectorCard> ah = new WeightedSelection<DirectorCard>.ChoiceInfo()
                if (!hasAddedMonolith)
                {
                    deck.AddChoice(MonolithCard, .2f);
                    //hasAddedMonolith = true;
                }
            }
            return orig(self, deck, maxCost);
        }

        private static class VoidItemCostTypeHelper
        {
            public static bool IsAffordable(CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
            {
                CharacterBody component = context.activator.GetComponent<CharacterBody>();
                if (!component)
                {
                    return false;
                }
                Inventory inventory = component.inventory;
                if (!inventory)
                {
                    return false;
                }
                int cost = context.cost;
                //int num = 0;
                int itemCount = inventory.GetTotalItemCountOfTier(ItemTier.VoidTier1) + inventory.GetTotalItemCountOfTier(ItemTier.VoidTier2) + inventory.GetTotalItemCountOfTier(ItemTier.VoidTier3) + inventory.GetTotalItemCountOfTier(ItemTier.VoidBoss);
                if (itemCount >= cost)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public static void PayCost(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
            {
                Inventory inventory = context.activator.GetComponent<CharacterBody>().inventory;
                int cost = context.cost;

                List<ItemIndex> list = new List<ItemIndex>(inventory.itemAcquisitionOrder);

                List<ItemIndex> optionList = new List<ItemIndex>();
                foreach(ItemIndex item in list){
                    ItemDef itemDef = ItemCatalog.GetItemDef(item);
                    if(itemDef.tier == ItemTier.VoidTier1 || itemDef.tier == ItemTier.VoidTier2 || itemDef.tier == ItemTier.VoidTier3 || itemDef.tier == ItemTier.VoidBoss)
                    {
                        optionList.Add(item);
                    }

                }
                Util.ShuffleList(optionList, context.rng);

                for (int k = 0; k < cost; k++)
                {
                    TakeOne();
                }
                MultiShopCardUtils.OnNonMoneyPurchase(context);
                void TakeOne()//Inventory inventory, CostTypeDef.PayCostContext context, int cost)
                {
                    for (int i = 0; i < optionList.Count(); i++)
                    {
                        if(inventory.GetItemCount(optionList[i]) > 0)
                        {
                            
                            inventory.RemoveItem(optionList[i]);
                            context.results.itemsTaken.Add(optionList[i]);
                            break;
                            
                        }
                    }
                }
            }

            //private void OpenVoidPortal(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
            //{
            //    if(self.displayNameToken == $"INTERACTABLE_{InteractableLangToken}_NAME")
            //    {
            //        var body = activator.GetComponent<CharacterBody>();
            //        if(body && body.master)
            //        {
            //
            //        }
            //    }
            //}

            //private void CreateCuratedBuffList(On.RoR2.Run.orig_Start orig, Run self)
            //{
            //    orig(self);
            //    CreateBaseCuratedBuffList();
            //}

            //private Interactability StopInteractionIfRedundant(On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor activator)
            //{
            //    if (self.displayNameToken == $"INTERACTABLE_{InteractableLangToken}_NAME" && activator)
            //    {
            //        var body = activator.GetComponent<CharacterBody>();
            //        var buffBrazierManager = self.gameObject.GetComponent<BuffBrazierManager>();
            //        if (body && body.master && buffBrazierManager)
            //        {
            //            var flameOrbController = body.master.GetComponent<BuffBrazierFlameOrbController>();
            //            if (flameOrbController && flameOrbController.FlameOrbs.Any(x => x.CuratedType.BuffDef == buffBrazierManager.ChosenBuffBrazierBuff.BuffDef))
            //            {
            //                return Interactability.ConditionsNotMet;
            //            }
            //        }
            //    }
            //
            //    return orig(self, activator);
            //}

            //private string AppendBuffName(On.RoR2.PurchaseInteraction.orig_GetDisplayName orig, PurchaseInteraction self)
            //{
            //    if (self.displayNameToken == $"INTERACTABLE_{InteractableLangToken}_NAME")
            //    {
            //        var brazierManagerComponent = self.gameObject.GetComponent<BuffBrazierManager>();
            //        if (brazierManagerComponent)
            //        {
            //            if (brazierManagerComponent.ChosenBuffBrazierBuff.BuffDef)
            //            {
            //                return $"Buff Brazier ({brazierManagerComponent.ChosenBuffBrazierBuff.DisplayName})";
            //            }
            //        }
            //    }
            //    return orig(self);
            //}

            //private void SpendFlame(On.RoR2.TeleporterInteraction.orig_OnInteractionBegin orig, TeleporterInteraction self, Interactor activator)
            //{
            //    orig(self, activator);
            //
            //    if (activator && !self.isCharged)
            //    {
            //        var body = activator.GetComponent<CharacterBody>();
            //        if (body && body.master)
            //        {
            //            //var flameOrbController = body.master.GetComponent<BuffBrazierFlameOrbController>();
            //            //if (flameOrbController)
            //            //{
            //            //    flameOrbController.StartCoroutine(flameOrbController.StaggerDeploymentToTeleporter(self.gameObject, 0.3f));
            //            //}
            //        }
            //    }
            //}

            // [ConCommand(commandName = "spawn_buff_brazier", flags = ConVarFlags.ExecuteOnServer, helpText = "Spawns a buff brazier at the Aim position.")]
            //public static void CCSpawnBuffBrazier(ConCommandArgs args)
            //{
            //    var body = args.GetSenderBody();
            //    if (body && body.inputBank)
            //    {
            //        var surfaceAlignmentInfo = Utils.MiscUtils.GetAimSurfaceAlignmentInfo(body.inputBank.GetAimRay(), LayerIndex.world.mask, 10000);
            //        if (surfaceAlignmentInfo.Count > 0)
            //        {
            //            var brazier = UnityEngine.Object.Instantiate(BuffBrazier.InteractableBodyModelPrefab, surfaceAlignmentInfo["Position"], Util.QuaternionSafeLookRotation(surfaceAlignmentInfo["Forward"], surfaceAlignmentInfo["Up"]));
            //            if (NetworkServer.active)
            //            {
            //                NetworkServer.Spawn(brazier);
            //            }
            //        }
            //    }
            //}

            //public struct BrazierBuffCuratedType
            //{
            //    public string DisplayName;
            //    public BuffDef BuffDef;
            //    public Color FlameColor;
            //    public float CostModifier;
            //    public bool IsDebuff;
            //
            //    public BrazierBuffCuratedType(string displayName, BuffDef buffDef, Color flameColor, float costModifier, bool isDebuff)
            //    {
            //        DisplayName = displayName;
            //        BuffDef = buffDef;
            //        FlameColor = flameColor;
            //        CostModifier = costModifier;
            //        IsDebuff = isDebuff;
            //    }
            //}
            //
            //public struct BrazierBuffFlameOrbType
            //{
            //    public BrazierBuffCuratedType CuratedType;
            //    public GameObject FlameOrbObject;
            //
            //    public BrazierBuffFlameOrbType(BrazierBuffCuratedType curatedType, GameObject flameOrbObject)
            //    {
            //        CuratedType = curatedType;
            //        FlameOrbObject = flameOrbObject;
            //    }
            //}
        }

        public class PortalInteractableToken : NetworkBehaviour
        {
            //public CharacterBody Owner;
            //public CharacterBody LastActivator;
            //public Transform selfpos;
            public PurchaseInteraction PurchaseInteraction;

            public void Start()
            {
                if (NetworkServer.active && Run.instance)
                {
                    PurchaseInteraction.SetAvailableTrue();
                }
                PurchaseInteraction.costType = (CostTypeIndex)voidCostTypeIndex;
                PurchaseInteraction.onPurchase.AddListener(PortalPurchaseAttempt);
                
                //BuffBrazierStateMachine = EntityStateMachine.FindByCustomName(gameObject, "Body");

                //ConstructFlameChoice();
                //BaseCostDetermination = (int)(PurchaseInteraction.cost * ChosenBuffBrazierBuff.CostModifier);
                //SetCost();

            }
            public void PortalPurchaseAttempt(Interactor interactor)
            {
                if (!interactor) { return; }

                var body = interactor.GetComponent<CharacterBody>();
                if (body && body.master)
                {
                    if (NetworkServer.active)
                    {
                        AttemptSpawnVoidPortal();
                        GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/ShrineUseEffect.prefab").WaitForCompletion();
                        EffectManager.SimpleImpactEffect(effectPrefab, this.transform.position, new Vector3(0, 0, 0), true);
                        
                        //GameObject portal = UnityEngine.Object.Instantiate<GameObject>(ShatteredMonolith.voidFieldPortalObject, this.transform.position, new Quaternion(0, 70, 0, 0));
                        //NetworkServer.Spawn(portal);
                        PurchaseInteraction.SetAvailable(false);

                    }
                }

            }
            //private bool AttemptSpawnPortal(SpawnCard portalSpawnCard, float minDistance, float maxDistance, string successChatToken)
            //{
            //    
            //    GameObject exists = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(portalSpawnCard, new DirectorPlacementRule
            //    {
            //        minDistance = minDistance,
            //        maxDistance = maxDistance,
            //        placementMode = DirectorPlacementRule.PlacementMode.Approximate,
            //        position = base.transform.position,
            //        spawnOnTarget = base.transform
            //    }, this.rng));
            //    if (exists)
            //    {
            //        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            //        {
            //            baseToken = successChatToken
            //        });
            //    }
            //    return exists;
            //}
            private bool AttemptSpawnVoidPortal()
            {
                //InteractableSpawnCard portalSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/PortalShop/iscShopPortal.asset").WaitForCompletion();
                string spawnMessageToken = "<color=#DD7AC6>The rift opens...</color>";
                GameObject exists = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(ShatteredMonolith.VoidFieldsPortalCard, new DirectorPlacementRule
                {
                    minDistance = 1,
                    maxDistance = 25,
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    position = base.transform.position,
                    spawnOnTarget = transform
                }, Run.instance.stageRng));
                if (exists && !string.IsNullOrEmpty(spawnMessageToken))
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = spawnMessageToken
                    });
                }
                return exists;
            }

        }
    }
}