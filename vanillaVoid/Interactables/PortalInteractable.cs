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
        public ConfigEntry<float> voidSeedWeight;
        public ConfigEntry<int> normalWeight;
        public ConfigEntry<int> spawnCost;

        public override string InteractableName => "Shattered Monolith";

        public override string InteractableContext => "Tear a hole in reality?";

        public override string InteractableInspect => "Allows survivors to sacrifice a random void item to create a portal to the Void Fields.";

        public override string InteractableInspectTitle => "Shattered Monolith";

        public override string InteractableLangToken => "SHATTERED_SHRINE";

        public override GameObject InteractableModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("VoidShrine.prefab");

        public static GameObject InteractableBodyModelPrefab;

        public static InteractableSpawnCard interactableSpawnCard;

        public static InteractableSpawnCard VoidFieldsPortalCard;

        public static GameObject voidFieldPortalObject;

        public static List<PickupIndex> voidItemsList;

        public static CostTypeDef voidCostDef;
        public static int voidCostTypeIndex;

        public int hasAddedMonolith;
        public static DirectorCard MonolithCard;
        //public Transform symbolTransform;

        public override void Init(ConfigFile config)
        {
            hasAddedMonolith = -1;
            CreateConfig(config);
            CreateLang();

            CostTypeCatalog.modHelper.getAdditionalEntries += addVoidCostType;
            On.RoR2.CampDirector.SelectCard += VoidCampAddMonolith;
            On.RoR2.PurchaseInteraction.GetDisplayName += MonolithName;
            RoR2.SceneDirector.onPrePopulateSceneServer += SetPortalCard;
            //On.RoR2.CampDirector.SelectCard
            //Stage.onServerStageBegin += HopefullyFixIncompat;
            //On.RoR2.SceneDirector.Start += Test;
            //voidItemsList = new List<PickupIndex>();
            //voidItemsList.Union(Run.instance.availableVoidBossDropList).Union(Run.instance.availableVoidTier1DropList).Union(Run.instance.availableVoidTier2DropList).Union(Run.instance.availableVoidTier3DropList);

            CreateInteractable();
            CreateInteractableSpawnCard();

            //Hooks();
        }

        private void SetPortalCard(SceneDirector obj)
        {
            hasAddedMonolith = -1 ;
        }

        private void addVoidCostType(List<CostTypeDef> obj)
        {
            //CostTypeIndex voidItem = new CostTypeIndex();
            voidCostDef = new CostTypeDef();
            voidCostDef.costStringFormatToken = "VV_COST_VOIDITEM_FORMAT";
            voidCostDef.isAffordable = new CostTypeDef.IsAffordableDelegate(VoidItemCostTypeHelper.IsAffordable);
            voidCostDef.payCost = new CostTypeDef.PayCostDelegate(VoidItemCostTypeHelper.PayCost);
            voidCostDef.colorIndex = ColorCatalog.ColorIndex.VoidItem;
            voidCostDef.saturateWorldStyledCostString = true;
            voidCostDef.darkenWorldStyledCostString = false;
            voidCostTypeIndex = CostTypeCatalog.costTypeDefs.Length + obj.Count;
            LanguageAPI.Add("VV_COST_VOIDITEM_FORMAT", "1 Item(s)");
            obj.Add(voidCostDef);
        }

        private void CreateConfig(ConfigFile config)
        {
            voidSeedWeight = config.Bind<float>("Interactable: " + InteractableName, "Void Seed Selection Weight", .125f, "How likely should this interactable be chosen to spawn in a void seed? (For reference - 1 = Void Coin Barrel, .5 = Void Cradle, .333 = Void Potential Chest)");
            normalWeight = config.Bind<int>("Interactable: " + InteractableName, "Regular Stage Selection Weight", 1, "How likely should this be to spawn outside of void seeds? (For reference, 24 = Normal Chest, 8 = Multishop, 1 = Lunar Pod (depends on stage, but generally these are accurate))");
            spawnCost = config.Bind<int>("Interactable: " + InteractableName, "Credit Cost", 10, "How expensive should this interactable be? (For reference, 15 = Normal Chest, 20 = Multishop & Chance Shrine, 25 = Lunar Pod)");
        }

        public void CreateInteractable()
        {

            InteractableBodyModelPrefab = InteractableModel;
            InteractableBodyModelPrefab.AddComponent<NetworkIdentity>();
            var expReqComp = InteractableBodyModelPrefab.AddComponent<RoR2.ExpansionManagement.ExpansionRequirementComponent>();
            expReqComp.requiredExpansion = vanillaVoidPlugin.sotvDLC;


            var inspect = ScriptableObject.CreateInstance<InspectDef>();
            var info = inspect.Info = new RoR2.UI.InspectInfo();

            info.Visual = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("texShrineIconOutlined");
            info.DescriptionToken = $"VV_INTERACTABLE_{InteractableLangToken}_INSPECT";
            info.TitleToken = $"VV_INTERACTABLE_{InteractableLangToken}_TITLE";
            inspect.Info = info;

            var giip = InteractableBodyModelPrefab.gameObject.AddComponent<GenericInspectInfoProvider>();
            giip.InspectInfo = inspect;

            var purchaseInteraction = InteractableBodyModelPrefab.AddComponent<PurchaseInteraction>();
            purchaseInteraction.displayNameToken = $"VV_INTERACTABLE_{InteractableLangToken}_NAME";
            purchaseInteraction.contextToken = $"VV_INTERACTABLE_{InteractableLangToken}_CONTEXT";
            purchaseInteraction.costType = (CostTypeIndex)voidCostTypeIndex;
            purchaseInteraction.automaticallyScaleCostWithDifficulty = false;
            purchaseInteraction.cost = 1;
            purchaseInteraction.available = true;
            purchaseInteraction.setUnavailableOnTeleporterActivated = true;
            purchaseInteraction.isShrine = true;
            purchaseInteraction.isGoldShrine = false;


            var pingInfoProvider = InteractableBodyModelPrefab.AddComponent<PingInfoProvider>();
            pingInfoProvider.pingIconOverride = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("texShrineIconOutlined");

            //var entityStateMachine = InteractableBodyModelPrefab.AddComponent<EntityStateMachine>();
            //entityStateMachine.customName = "Body";
            //entityStateMachine.initialStateType.stateType = typeof(MyEntityStates.BuffBrazier.BuffBrazierMainState);
            //entityStateMachine.mainStateType.stateType = typeof(MyEntityStates.BuffBrazier.BuffBrazierMainState);

            //var networkStateMachine = InteractableBodyModelPrefab.AddComponent<NetworkStateMachine>();
            //networkStateMachine.stateMachines = new EntityStateMachine[] { entityStateMachine };

            var genericNameDisplay = InteractableBodyModelPrefab.AddComponent<GenericDisplayNameProvider>();
            genericNameDisplay.displayToken = $"VV_INTERACTABLE_{InteractableLangToken}_NAME";

            var symbolTransform = InteractableBodyModelPrefab.transform.Find("Symbol");  //0.39607r maybe?
            symbolTransform.gameObject.AddComponent<Billboard>();

            //var parent = transform.parent.gameObject;
            //Debug.Log("parent: " + parent + " | " + parent.name);
            //var symbolTransform = parent.transform.Find("Symbol");

            var interactionToken = InteractableBodyModelPrefab.AddComponent<PortalInteractableToken>();
            interactionToken.PurchaseInteraction = purchaseInteraction;
            interactionToken.symbolTransform = symbolTransform;

            var entityLocator = InteractableBodyModelPrefab.GetComponentInChildren<MeshCollider>().gameObject.AddComponent<EntityLocator>();
            entityLocator.entity = InteractableBodyModelPrefab;

            var modelLocator = InteractableBodyModelPrefab.AddComponent<ModelLocator>();
            modelLocator.modelTransform = InteractableBodyModelPrefab.transform.Find("mdlVoidShrine");
            modelLocator.modelBaseTransform = modelLocator.modelTransform;//InteractableBodyModelPrefab.transform.Find("Base");
            modelLocator.dontDetatchFromParent = true;
            modelLocator.autoUpdateModelTransform = true;

            var highlightController = InteractableBodyModelPrefab.GetComponent<Highlight>();
            highlightController.targetRenderer = InteractableBodyModelPrefab.GetComponentsInChildren<MeshRenderer>().Where(x => x.gameObject.name.Contains("VoidShrine")).First();
            highlightController.strength = 1;
            highlightController.highlightColor = Highlight.HighlightColor.interactive;

            var hologramController = InteractableBodyModelPrefab.AddComponent<HologramProjector>();
            hologramController.hologramPivot = InteractableBodyModelPrefab.transform.Find("HologramPivot");
            hologramController.displayDistance = 10;
            hologramController.disableHologramRotation = true;

            //var dithmodel = InteractableBodyModelPrefab.AddComponent<DitherModel>();
            //dithmodel.bounds = InteractableBodyModelPrefab.GetComponentInChildren<MeshCollider>();


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

            GameObject portalObjectTemp = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion();
            //voidFieldPortalObject = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/PortalArena/PortalArena.prefab").WaitForCompletion();
            voidFieldPortalObject = PrefabAPI.InstantiateClone(portalObjectTemp, "VoidShrineVoidPortal");
            var exitctr = voidFieldPortalObject.GetComponent<SceneExitController>();
            exitctr.useRunNextStageScene = false;

        }

        public void CreateInteractableSpawnCard()
        {
            interactableSpawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();
            interactableSpawnCard.name = "iscVoidPortalInteractable";
            interactableSpawnCard.prefab = InteractableBodyModelPrefab;
            interactableSpawnCard.sendOverNetwork = true;
            interactableSpawnCard.hullSize = HullClassification.Golem;
            interactableSpawnCard.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
            interactableSpawnCard.requiredFlags = RoR2.Navigation.NodeFlags.None;
            interactableSpawnCard.forbiddenFlags = RoR2.Navigation.NodeFlags.NoShrineSpawn | RoR2.Navigation.NodeFlags.NoChestSpawn;

            interactableSpawnCard.directorCreditCost = spawnCost.Value;
            
            interactableSpawnCard.occupyPosition = true;
            interactableSpawnCard.orientToFloor = false;
            interactableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;
            interactableSpawnCard.maxSpawnsPerStage = 1;

            MonolithCard = new DirectorCard
            {
                selectionWeight = normalWeight.Value,
                spawnCard = interactableSpawnCard,
                minimumStageCompletions = 1,
                
                //allowAmbushSpawn = true, TODO removed i think?
            };

            //DirectorAPI.Helpers.AddNewInteractable(directorCard, DirectorAPI.InteractableCategory.VoidStuff);

            //            RoR2/Base/SceneGroups/sgStage1.asset 	RoR2.SceneCollection
            //RoR2/Base/SceneGroups/sgStage2.asset 	RoR2.SceneCollection
            //RoR2/Base/SceneGroups/sgStage3.asset 	RoR2.SceneCollection
            //RoR2/Base/SceneGroups/sgStage4.asset 	RoR2.SceneCollection
            //RoR2/Base/SceneGroups/sgStage5.asset 
            //var sg1 = Addressables.LoadAssetAsync<RoR2.SceneCollection>("RoR2/Base/SceneGroups/sgStage1.asset").WaitForCompletion();

            //foreach(var stage in R2API.StageRegistration.stageVariantDictionary)
            //{
            //    //DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.AbandonedAqueduct);
            //    var yeah = stage.Value[0];
            //    
            //
            //}

            //Debug.Log("yeahg");
            //DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.Custom, "golemplains");
            //Debug.Log("Yeah 2 ");

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

            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.VerdantFalls);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.ViscousFalls);

            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.ShatteredAbodes);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.DisturbedImpact);

            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.TreebornColony);
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.GoldenDieback);

            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.ReformedAltar);

            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.HelminthHatchery);


            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.Custom, "FBLScene");
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.Custom, "drybasin");
            DirectorAPI.Helpers.AddNewInteractableToStage(MonolithCard, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.Custom, "slumberingsatellite");
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
            VoidFieldsPortalCard.directorCreditCost = 999999;
            VoidFieldsPortalCard.occupyPosition = true;
            VoidFieldsPortalCard.orientToFloor = false;
            VoidFieldsPortalCard.skipSpawnWhenSacrificeArtifactEnabled = false;
            VoidFieldsPortalCard.maxSpawnsPerStage = 0;

            //DirectorCard directorCard2 = new DirectorCard
            //{
            //    selectionWeight = 0,
            //    spawnCard = VoidFieldsPortalCard,
            //    minimumStageCompletions = 999999,
            //};
            //DirectorAPI.Helpers.AddNewInteractableToStage(directorCard2, DirectorAPI.InteractableCategory.VoidStuff, DirectorAPI.Stage.Bazaar);

            //DirectorAPI.Helpers.AddNewInteractable(directorCard2, DirectorAPI.InteractableCategory.VoidStuff);

        }

        private string MonolithName(On.RoR2.PurchaseInteraction.orig_GetDisplayName orig, PurchaseInteraction self)
        {
            //Debug.Log("name: " + self.displayNameToken + " | cost: ");
            if (self.displayNameToken == $"VV_INTERACTABLE_{InteractableLangToken}_NAME")
            {
                return InteractableName;
            }
            return orig(self);
        }

        //private void HopefullyFixIncompat(Stage obj)
        //{
        //    Debug.Log("begin fix - " + obj.sceneDef.cachedName + " | " + obj.sceneDef.nameToken + " | ");
        //    if(obj.sceneDef.cachedName == "forgottenhaven")
        //    {
        //
        //    }
        //    
        //}

        //private void Test(On.RoR2.SceneDirector.orig_Start orig, SceneDirector self)
        //{
        //    Debug.Log("begin test - " + self.name);
        //    foreach (Transform g in self.transform.GetComponentsInChildren<Transform>())
        //    {
        //        Debug.Log(g.name);
        //    }
        //    orig(self);
        //}

        private DirectorCard VoidCampAddMonolith(On.RoR2.CampDirector.orig_SelectCard orig, CampDirector self, WeightedSelection<DirectorCard> deck, int maxCost)
        {
            //hasAddedMonolith = false;
            //Debug.Log("void camp add monolith start ! " + hasAddedMonolith);
            if (self.name == "Camp 1 - Void Monsters & Interactables" && hasAddedMonolith == -1)
            {
                for (int i = deck.Count - 1; i >= 0; i--)
                {
                    //Debug.Log("name: " + deck.GetChoice(i).value.spawnCard.name + " | cost: " + deck.GetChoice(i).value.cost);
                    if (deck.GetChoice(i).value.spawnCard.name == "iscVoidPortalInteractable")
                    {
                        //hasAddedMonolith = 0;
                        hasAddedMonolith = 0;
                        break;
                    }
                    //Debug.Log("card name: " + deck.GetChoice(i).value.spawnCard.name + " | weight: " + deck.GetChoice(i).weight);
                }
                //Debug.Log("name: " + self.name + " | elite: " + self.eliteDef);
                //WeightedSelection<DirectorCard> ah = new WeightedSelection<DirectorCard>.ChoiceInfo()-
                if(MonolithCard == null)
                {
                    Debug.Log("MonolithCard was not available.");
                    CreateInteractableSpawnCard();
                }
                
                if (MonolithCard != null && hasAddedMonolith == -1)
                {
                    deck.AddChoice(MonolithCard, voidSeedWeight.Value);
                    hasAddedMonolith = 0;
                    //Debug.Log("adding monolith");
                    //hasAddedMonolith = true;
                }
                else if(MonolithCard == null){
                    Debug.Log("MonolithCard was STILL not available?");
                }
                //else if (!MonolithCard.IsAvailable())
                //{
                //    Debug.Log("MonolithCard was not available.");
                //    //CreateInteractableSpawnCard();
                //}
            }

            var yeah = orig(self, deck, maxCost);
            if (yeah != null)
            {
                //Debug.Log("yeah: " + yeah + " is available");
                if (yeah.IsAvailable())
                {
                    if (yeah.spawnCard)
                    {
                        //Debug.Log("yeah: " + yeah.spawnCard + " exists");
                        if (yeah.spawnCard.name == MonolithCard.spawnCard.name)
                        {
                            for (int i = deck.Count - 1; i >= 0; i--)
                            {
                                //Debug.Log("name: " + deck.GetChoice(i).value.spawnCard.name + " | cost: " + deck.GetChoice(i).value.cost);
                                if (deck.GetChoice(i).value.spawnCard.name == "iscVoidPortalInteractable")
                                {
                                    //Debug.Log("removing monolith");
                                    deck.RemoveChoice(i);
                                    hasAddedMonolith = 1;
                                    break;
                                }
                                //Debug.Log("card name: " + deck.GetChoice(i).value.spawnCard.name + " | weight: " + deck.GetChoice(i).weight);
                            }
                        }
                    }
                }
            }
            //Debug.Log("yeah: " + yeah + " | " + yeah.spawnCard + " | " + yeah.spawnCard);


            return yeah;
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
        }

        public class PortalInteractableToken : NetworkBehaviour
        {
            //public CharacterBody Owner;
            public CharacterBody LastActivator;
            //public Transform selfpos;
            public PurchaseInteraction PurchaseInteraction;
            public Transform symbolTransform;

            public void Start()
            {
                if (NetworkServer.active && Run.instance)
                {
                    PurchaseInteraction.SetAvailableTrue();
                }
                PurchaseInteraction.costType = (CostTypeIndex)voidCostTypeIndex;
                PurchaseInteraction.onPurchase.AddListener(PortalPurchaseAttempt);

                //InteractableBodyModelPrefab.transform.Find("Symbol");
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
                        AttemptSpawnVoidPortal(body);
                        GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/ShrineUseEffect.prefab").WaitForCompletion();
                        EffectManager.SimpleImpactEffect(effectPrefab, this.transform.position, new Vector3(0, 0, 0), true);

                        //GameObject portal = UnityEngine.Object.Instantiate<GameObject>(ShatteredMonolith.voidFieldPortalObject, this.transform.position, new Quaternion(0, 70, 0, 0));
                        //NetworkServer.Spawn(portal);
                        LastActivator = body;
                        
                        symbolTransform.gameObject.SetActive(false);
                        PurchaseInteraction.SetAvailable(false);


                    }
                }

            }
            
            private bool AttemptSpawnVoidPortal(CharacterBody body)
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
                    //Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    //{
                    //    baseToken = spawnMessageToken
                    //});

                    Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                    {
                        //subjectAsCharacterBody = body,
                        baseToken = spawnMessageToken
                    });

                }
                return exists;
            }

        }
    }
}