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

namespace vanillaVoid.Items // this item isn't finished. it basically does what its supposed to, but has little to no visuals and doesn't unslow/unfreeze
{
    public class CryoCanister : ItemBase<CryoCanister>
    {
        public ConfigEntry<float> baseDamageAOE;

        public ConfigEntry<float> stackingDamageAOE;

        public ConfigEntry<float> aoeRangeBase;

        public ConfigEntry<float> aoeRangeStacking;

        public ConfigEntry<float> requiredStacksForFreeze;

        public ConfigEntry<float> requiredStacksForBossFreeze;

        public ConfigEntry<float> slowDuration;

        public ConfigEntry<float> slowPercentage;

        public ConfigEntry<string> voidPair;

        public override string ItemName => "Supercritical Coolant";

        public override string ItemLangTokenName => "CRYOCANISTER_ITEM";

        public override string ItemPickupDesc => "Killing an enemy slows and eventually freezes other nearby enemies. <style=cIsVoid>Corrupts all Gasolines</style>.";

        public override string ItemFullDescription => $"Killing an enemy <style=cIsUtility>slows</style> all enemies within <style=cIsDamage>10m</style> <style=cStack>(+2.5m per stack)</style>, which lasts for <style=cIsUtility>5</style> <style=cStack>(+2.5 per stack)</style> seconds. Upon applying <style=cIsUtility>{requiredStacksForFreeze.Value} stacks</style> of <style=cIsUtility>slow</style> to an enemy, they are <style=cIsDamage>frozen</style>. Less effective on bosses. <style=cIsVoid>Corrupts all Gasolines</style>.";

        public override string ItemLore => $"<style=cSub>Order: Lens-Maker's Orrery \nTracking Number: ******** \nEstimated Delivery: 1/13/2072 \nShipping Method: High Priority/Fragile/Confidiential \nShipping Address: [REDACTED] \nShipping Details: \n\n</style>" +
            "The Lens-Maker, as mysterious as they are influential. From my research I have surmised that she has been appointed to \"Final Verdict\", the most prestigious role of leadership in the House Beyond. Our team managed to locate a workshop of hers where she was supposedly working on some never-before concieved tech - but something was off. " +
            "Looking through her schematics and trinkets I found something odd - something unlike what I was anticipating. A simple orrery, clearly her design, but without her classic red, replaced with a peculiar purple. At first I worried that when she learned of our arrival, when she left in a rush, that we had ruined some of her masterpieces...but maybe it's best we interrupted her. " +
            "\n\nGiven that this is one of a kind, and quite a special work of hers at that; I expect much more than just currency in payment.";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCryoPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("cryoIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Damage, ItemTag.OnKillEffect };

        public BuffDef preFreezeSlow { get; private set; }

        public GameObject iceDeathAOEObject;// = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteIce/AffixWhiteExplosion.prefab").WaitForCompletion();
        GameObject iceDeathObject;

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            CreateBuff();

            string aoePath = "RoR2/Base/EliteIce/AffixWhiteExplosion.prefab";
            iceDeathAOEObject = Addressables.LoadAssetAsync<GameObject>(aoePath).WaitForCompletion();

            GameObject iceDeathObject = UnityEngine.Object.Instantiate<GameObject>(iceDeathAOEObject);
            iceDeathObject.AddComponent<NetworkIdentity>();

            Hooks(); 
        }
        public void CreateBuff()
        {
            preFreezeSlow = ScriptableObject.CreateInstance<BuffDef>();
            preFreezeSlow.buffColor = Color.blue;
            preFreezeSlow.canStack = true;
            preFreezeSlow.isDebuff = true;
            preFreezeSlow.name = "ZnVV" + "preFreezeSlow";
            preFreezeSlow.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("preFreezeSlow");
            ContentAddition.AddBuffDef(preFreezeSlow);
        }

        public override void CreateConfig(ConfigFile config)
        {
            baseDamageAOE = config.Bind<float>("Item: " + ItemName, "Percent Base Damage", .25f, "Adjust the percent base damage the AOE does.");
            stackingDamageAOE = config.Bind<float>("Item: " + ItemName, "Percent Base Damage Stacking", .25f, "Adjust the percent base damage the AOE gain per stack.");
            aoeRangeBase = config.Bind<float>("Item: " + ItemName, "Range of AOE", 10f, "Adjust the range of the slow AOE on the first stack.");
            aoeRangeStacking = config.Bind<float>("Item: " + ItemName, "Range Increase per Stack", 2.5f, "Adjust the range the slow AOE gains per stack.");
            requiredStacksForFreeze = config.Bind<float>("Item: " + ItemName, "Stacks Required for Freeze", 2f, "Adjust the number of stacks needed to freeze an enemy.");
            requiredStacksForBossFreeze = config.Bind<float>("Item: " + ItemName, "Stacks Required for Boss Freeze", 10f, "Adjust the number of stacks needed to freeze a boss.");
            slowPercentage = config.Bind<float>("Item: " + ItemName, "Percent Slow", .5f, "Adjust the percentage slow the buff causes.");
            slowDuration = config.Bind<float>("Item: " + ItemName, "Slow Duration", 5, "Adjust the duration the slow lasts, in seconds.");


 

            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "IgniteOnKill", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCryoDisplay.prefab");
 
            string fluidMat = "RoR2/Base/Huntress/matHuntressGlaive.mat";

            var cryoFluidModel = ItemModel.transform.Find("Glowybits").GetComponent<MeshRenderer>();
            cryoFluidModel.material = Addressables.LoadAssetAsync<Material>(fluidMat).WaitForCompletion();

            var cryoFluidDisplay = ItemBodyModelPrefab.transform.Find("Glowybits").GetComponent<MeshRenderer>();
            cryoFluidDisplay.material = Addressables.LoadAssetAsync<Material>(fluidMat).WaitForCompletion();


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

            return rules;
        }

        public override void Hooks()
        {
            //On.RoR2.HealthComponent.TakeDamage += AdzeDamageBonus;
            GlobalEventManager.onCharacterDeathGlobal += CryoCanisterAOE;
            RecalculateStatsAPI.GetStatCoefficients += CalculateStatsCryoHook;            
        }

        private void CalculateStatsCryoHook(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender)
            {
                if (sender.GetBuffCount(preFreezeSlow) > 0)
                {
                    args.moveSpeedReductionMultAdd = slowPercentage.Value;
                    //args.critAdd += baseCrit.Value;
                    //if (glassesCount > 0)
                    //{
                    //    args.critAdd += (glassesCount * 10 * (newLensBonus.Value + ((orreryCount - 1) * newStackingLensBonus.Value)));
                    //}
                }
            }
        }

        private static readonly SphereSearch cryoAOESphereSearch = new SphereSearch();
        private static readonly List<HurtBox> cryoAOEHurtBoxBuffer = new List<HurtBox>();
        //string aoePath = "RoR2/Base/EliteIce/AffixWhiteExplosion.prefab";
        //public GameObject iceDeathAOE = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteIce/AffixWhiteExplosion.prefab").WaitForCompletion();
        private void CryoCanisterAOE(DamageReport dmgReport)
        {
            if (!dmgReport.attacker || !dmgReport.attackerBody || !dmgReport.victim || !dmgReport.victimBody)
            {
                Debug.Log("fake");
                return; //end func if death wasn't killed by something real enough
            }

            //var cryoComponent = dmgReport.victimBody.GetComponent<CryoToken>();
            //if (cryoComponent)
            //{
            //    return; //prevent game crash  
            //}
            //
            //CharacterBody victimBody = dmgReport.victimBody;
            //dmgReport.victimBody.gameObject.AddComponent<CryoToken>();

            CharacterBody victimBody = dmgReport.victimBody;
            //dmgReport.victimBody.gameObject.AddComponent<ExeToken>();
            CharacterBody attackerBody = dmgReport.attackerBody;
            if (attackerBody.inventory)
            {
                var cryoCount = attackerBody.inventory.GetItemCount(ItemBase<CryoCanister>.instance.ItemDef);
                if (cryoCount > 0)
                {
                    float stackRadius = aoeRangeBase.Value + (aoeRangeStacking.Value * (float)(cryoCount - 1));
                    float victimRadius = victimBody.radius;
                    float effectiveRadius = stackRadius + victimRadius;
                    float AOEDamageMult = baseDamageAOE.Value + (stackingDamageAOE.Value * (float)(cryoCount - 1));
                    
                    float AOEDamage = dmgReport.attackerBody.damage * AOEDamageMult;
                    Vector3 corePosition = victimBody.corePosition;

                    cryoAOESphereSearch.origin = corePosition;
                    cryoAOESphereSearch.mask = LayerIndex.entityPrecise.mask;
                    cryoAOESphereSearch.radius = effectiveRadius;
                    cryoAOESphereSearch.RefreshCandidates();
                    cryoAOESphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(dmgReport.attackerBody.teamComponent.teamIndex));
                    cryoAOESphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                    cryoAOESphereSearch.OrderCandidatesByDistance();
                    cryoAOESphereSearch.GetHurtBoxes(cryoAOEHurtBoxBuffer);
                    cryoAOESphereSearch.ClearCandidates();
                    Debug.Log("found: " + cryoAOEHurtBoxBuffer.Count);
                    for (int i = 0; i < cryoAOEHurtBoxBuffer.Count; i++)
                    {
                        HurtBox hurtBox = cryoAOEHurtBoxBuffer[i];
                        if (hurtBox.healthComponent && hurtBox.healthComponent.body)
                        {
                            Debug.Log("found a health component and hc body");
                            hurtBox.healthComponent.body.AddTimedBuff(preFreezeSlow, slowDuration.Value);
                            DamageInfo damageInfo = new DamageInfo
                            {
                                attacker = attackerBody.gameObject,
                                crit = attackerBody.RollCrit(),
                                damage = AOEDamage,
                                position = corePosition,
                                procCoefficient = 1,
                                damageType = DamageType.AOE,
                                damageColorIndex = DamageColorIndex.Default,
                            };
                            hurtBox.healthComponent.TakeDamage(damageInfo);
                            Debug.Log("sent take damage");
                            //self.GetComponent<CharacterBody>().AddTimedBuff(preFreezeSlow, slowDuration.Value);
                            if (hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow) >= requiredStacksForFreeze.Value)
                            {
                                if (!hurtBox.healthComponent.body.isBoss)
                                {
                                    hurtBox.healthComponent.isInFrozenState = true;
                                }
                                else if(hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow) >= requiredStacksForBossFreeze.Value)
                                {
                                    hurtBox.healthComponent.isInFrozenState = true;
                                }
                                
                                EffectData effectData = new EffectData
                                {
                                    origin = victimBody.corePosition
                                };
                                //EffectManager.SpawnEffect(GlobalEventManager.CommonAssets.igniteOnKillExplosionEffectPrefab, effectData, transmit: true);

                                //Quaternion Quat = Quaternion.Euler(0, 0, 0);
                                //GameObject iceDeathObject = UnityEngine.Object.Instantiate<GameObject>(iceDeathAOEObject, victimBody.corePosition, Quat);
                                //iceDeathObject.GetComponent<TeamFilter>().teamIndex = attackerBody.teamComponent.teamIndex;

                                //NetworkServer.Spawn(iceDeathObject);
                                EffectManager.SpawnEffect(iceDeathObject, effectData, true);
                            }
                            //Quaternion rot = Quaternion.Euler(0, 180, 0);
                            //var tempBlade = Instantiate(bladeObject, victimBody.corePosition, rot);
                            //tempBlade.GetComponent<TeamFilter>().teamIndex = attackerBody.teamComponent.teamIndex;
                            //tempBlade.transform.position = victimBody.corePosition;
                            //NetworkServer.Spawn(tempBlade);
                            //EffectData effectData = new EffectData
                            //{
                            //    origin = victimBody.corePosition
                            //};
                            //effectData.SetNetworkedObjectReference(tempBlade);
                            //EffectManager.SpawnEffect(GlobalEventManager.CommonAssets.igniteOnKillExplosionEffectPrefab, effectData, transmit: true);
                            //StartCoroutine(ExeBladeDelayedExecutions(bladeCount, tempBlade, dmgReport));
                        }
                    }
                }
            }
        }

        public class CryoToken : MonoBehaviour
        {
        }
        public class preFreezeToken : MonoBehaviour
        {

        }
    }

}
