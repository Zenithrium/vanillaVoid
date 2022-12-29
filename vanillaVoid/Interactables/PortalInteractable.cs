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

        public override GameObject InteractableModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("VoidShrine.prefab");

        public static GameObject InteractableBodyModelPrefab;

        public static InteractableSpawnCard InteractableSpawnCard;

        public static InteractableSpawnCard VoidFieldsPortalCard;

        public static GameObject voidFieldPortalObject;

        public static List<PickupIndex> voidItemsList;

        public static CostTypeDef voidCostDef;
        public static int voidCostTypeIndex;

        public bool hasAddedMonolith;
        public static DirectorCard MonolithCard;

        public override void Init(ConfigFile config)
        {
            hasAddedMonolith = false;
            CreateConfig(config);
            CreateLang();

            CostTypeCatalog.modHelper.getAdditionalEntries += addVoidCostType;
            On.RoR2.CampDirector.SelectCard += VoidCampAddMonolith;
            On.RoR2.PurchaseInteraction.GetDisplayName += MonolithName;
            //voidItemsList = new List<PickupIndex>();
            //voidItemsList.Union(Run.instance.availableVoidBossDropList).Union(Run.instance.availableVoidTier1DropList).Union(Run.instance.availableVoidTier2DropList).Union(Run.instance.availableVoidTier3DropList);

            CreateInteractable();
            CreateInteractableSpawnCard();

            //Hooks();
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

        }

        public void CreateInteractable()
        {

            InteractableBodyModelPrefab = InteractableModel;
            InteractableBodyModelPrefab.AddComponent<NetworkIdentity>();

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

            var interactionToken = InteractableBodyModelPrefab.AddComponent<PortalInteractableToken>();
            interactionToken.PurchaseInteraction = purchaseInteraction;

            
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

            var exitctr = voidFieldPortalObject.GetComponent<SceneExitController>();
            exitctr.useRunNextStageScene = false;

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
            InteractableSpawnCard.directorCreditCost = 15;
            InteractableSpawnCard.occupyPosition = true;
            InteractableSpawnCard.orientToFloor = false;
            InteractableSpawnCard.skipSpawnWhenSacrificeArtifactEnabled = false;
            InteractableSpawnCard.maxSpawnsPerStage = 1;

            MonolithCard = new DirectorCard
            {
                selectionWeight = 6,
                spawnCard = InteractableSpawnCard,
                minimumStageCompletions = 1,
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
            };

            DirectorAPI.Helpers.AddNewInteractable(directorCard2, DirectorAPI.InteractableCategory.VoidStuff);

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

        private DirectorCard VoidCampAddMonolith(On.RoR2.CampDirector.orig_SelectCard orig, CampDirector self, WeightedSelection<DirectorCard> deck, int maxCost)
        {
            hasAddedMonolith = false;
            if (self.name == "Camp 1 - Void Monsters & Interactables")
            {
                for (int i = deck.Count - 1; i >= 0; i--)
                {
                    //Debug.Log("name: " + deck.GetChoice(i).value.spawnCard.name + " | cost: " + deck.GetChoice(i).value.cost);
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
        }

        public class PortalInteractableToken : NetworkBehaviour
        {
            //public CharacterBody Owner;
            public CharacterBody LastActivator;
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
                        LastActivator = body;
                        PurchaseInteraction.SetAvailable(false);

                    }
                }

            }
            
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