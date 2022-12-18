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

namespace vanillaVoid.Items
{
    public class CryoCanister : ItemBase<CryoCanister>
    {
        public ConfigEntry<float> baseDamageAOE;

        public ConfigEntry<float> stackingDamageAOE;

        public ConfigEntry<float> aoeRangeBase;

        public ConfigEntry<float> aoeRangeStacking;

        public ConfigEntry<int> requiredStacksForFreeze;

        public ConfigEntry<int> requiredStacksForBossFreeze;

        public ConfigEntry<float> slowDuration;

        public ConfigEntry<float> slowDurationStacking;

        public ConfigEntry<float> slowPercentage;


        public ConfigEntry<string> voidPair;

        public override string ItemName => "Supercritical Coolant";

        public override string ItemLangTokenName => "CRYOCANISTER_ITEM";

        public override string ItemPickupDesc => "Killing an enemy slows and eventually freezes other nearby enemies. <style=cIsVoid>Corrupts all Gasolines</style>.";

        public override string ItemFullDescription => $"Killing an enemy <style=cIsUtility>slows</style> all enemies within <style=cIsDamage>{aoeRangeBase.Value}m</style> <style=cStack>(+{aoeRangeStacking.Value}m per stack)</style> for <style=cIsDamage>{baseDamageAOE.Value * 100}%</style> <style=cStack>(+{stackingDamageAOE.Value * 100}% per stack)</style> base damage, which lasts for <style=cIsUtility>{slowDuration.Value}</style> <style=cStack>(+{slowDuration.Value * .5} per stack)</style> seconds. Upon applying <style=cIsUtility>{requiredStacksForFreeze.Value} stacks</style> of <style=cIsUtility>slow</style> to an enemy, they are <style=cIsDamage>frozen</style>. Freezing is less effective on bosses. <style=cIsVoid>Corrupts all Gasolines</style>.";

        public override string ItemLore => $"<style=cSub>Order: Supercritical Coolant \nTracking Number: 03691215 \nEstimated Delivery: 25/10/2112 \nShipping Method: High Priority/Fragile \nShipping Address: [REDACTED] \nShipping Details: \n\n</style>" +
            "Originally we studied Void occurrences from afar, observing and cataloguing the distribution of galaxies and refining cosmological evolution models. We are in a new age of cosmic exploration. Advancements in space travel partnered with determined curiosity have brought us closer to our object of study, and with it, revelation.";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlCryoPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("cryoIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Damage, ItemTag.OnKillEffect };

        public BuffDef preFreezeSlow { get; private set; }

        //public GameObject iceDeathAOEObject;// = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteIce/AffixWhiteExplosion.prefab").WaitForCompletion();
        GameObject iceDeathObject;

        public GameObject iceDeathAOEObject { get; set; } = MainAssets.LoadAsset<GameObject>("CryoAOEExplosionEffect");
        public GameObject iceDeathAOEObjectLazy;

        public GameObject iceDeathAOEObjectFucked;
        public GameObject iceDeathObjectFucked;
        //GameObject gameObject = UnityEngine.Object.Instantiate(iceDeathAOEObject, position, Quaternion.identity);


        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            CreateBuff();

            //RoR2/Base/EliteIce/AffixWhiteExplosion.prefab
            //string aoePath = "RoR2/Base/Common/VFX/OmniImpactVFXFrozen.prefab"; // "RoR2 /Base/Common/VFX/OmniImpactVFXFrozen.prefab";
            //iceDeathAOEObjectLazy = Addressables.LoadAssetAsync<GameObject>(aoePath).WaitForCompletion();
            //
            //Debug.Log(" cryo canister vfx fake: " + iceDeathAOEObjectLazy);
            //
            //string aoePath2 = "RoR2/Base/EliteIce/AffixWhiteExplosion.prefab";
            //iceDeathAOEObjectFucked = Addressables.LoadAssetAsync<GameObject>(aoePath).WaitForCompletion();
            //
            //GameObject iceDeathObjectFucked = UnityEngine.Object.Instantiate<GameObject>(iceDeathAOEObjectFucked);
            //iceDeathObjectFucked.AddComponent<NetworkIdentity>();
            //
            ////GameObject iceDeathObject = UnityEngine.Object.Instantiate<GameObject>(iceDeathAOEObject);

            //iceDeathAOEObject = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("CryoAOEExplosionEffect");
            ////iceDeathAOEObject = GlobalEventManager.CommonAssets.bleedOnHitAndExplodeImpactEffect;
            ////iceDeathAOEObject.
            //var effComp = iceDeathAOEObject.AddComponent<EffectComponent>();
            //effComp.positionAtReferencedTransform = true;
            //var vfxAtrb = iceDeathAOEObject.AddComponent<VFXAttributes>();
            //vfxAtrb.vfxIntensity = VFXAttributes.VFXIntensity.Medium;
            //vfxAtrb.vfxPriority = VFXAttributes.VFXPriority.Always;
            //ContentAddition.AddEffect(iceDeathAOEObject);
            //Debug.Log(" cryo canister vfx: " + iceDeathAOEObject);
            //iceDeathAOEObject.AddComponent<NetworkIdentity>();
            //PrefabAPI.RegisterNetworkPrefab(iceDeathAOEObject);

            //EffectDef effectDef = new EffectDef(iceDeathAOEObject);
            //ContentAddition.AddEffect(iceDeathAOEObject);
            //AssetsMainAssets.Add(effectDef);

            //i hate modding

            Hooks(); 
        }
        public void CreateBuff()
        {
            preFreezeSlow = ScriptableObject.CreateInstance<BuffDef>();
            preFreezeSlow.buffColor = Color.cyan;
            preFreezeSlow.canStack = true;
            preFreezeSlow.isDebuff = true;
            preFreezeSlow.name = "ZnVV" + "preFreezeSlow";
            preFreezeSlow.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("preFreezeSlow");
            ContentAddition.AddBuffDef(preFreezeSlow);
        }

        public override void CreateConfig(ConfigFile config)
        {
            baseDamageAOE = config.Bind<float>("Item: " + ItemName, "Base Damage Percent", .15f, "Adjust the percent base damage the AOE does.");
            stackingDamageAOE = config.Bind<float>("Item: " + ItemName, "Base Damage Percent Stacking", .15f, "Adjust the percent base damage the AOE gains per stack.");
            aoeRangeBase = config.Bind<float>("Item: " + ItemName, "Range of AOE", 10f, "Adjust the range of the slow AOE on the first stack.");
            aoeRangeStacking = config.Bind<float>("Item: " + ItemName, "Range Increase per Stack", 2.5f, "Adjust the range the slow AOE gains per stack.");
            requiredStacksForFreeze = config.Bind<int>("Item: " + ItemName, "Debuff Stacks Required for Freeze", 3, "Adjust the number of stacks needed to freeze an enemy.");
            requiredStacksForBossFreeze = config.Bind<int>("Item: " + ItemName, "Buff Stacks Required for Boss Freeze", 10, "Adjust the number of stacks needed to freeze a boss.");
            slowPercentage = config.Bind<float>("Item: " + ItemName, "Percent Slow", 5f, "Adjust the percentage slow the buff causes.");
            slowDuration = config.Bind<float>("Item: " + ItemName, "Slow Debuff Duration", 3, "Adjust the duration the slow lasts, in seconds.");
            slowDurationStacking = config.Bind<float>("Item: " + ItemName, "Slow Debuff Duration per Stack", 1, "Adjust the duration the slow gains per stack.");

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
                    childName = "ThighL",
                    localPos = new Vector3(0.1515263f, 0.06217923f, -0.01854283f),
                    localAngles = new Vector3(294.0765f, 24.31453f, 41.70001f),
                    localScale = new Vector3(0.08f, 0.08f, 0.08f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1019495f, -0.004779898f, 0.08037122f),
                    localAngles = new Vector3(307.2001f, 179.7792f, 96.47073f),
                    localScale = new Vector3(0.065f, 0.065f, 0.065f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.3334707f, -0.03016165f,  -0.1781694f),
                    localAngles = new Vector3(340.6444f, 200.5811f, 6.510907f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-1.213294f, 0.9907005f, -5.987041f),
                    localAngles = new Vector3(287.2282f, 278.8165f, 82.6935f),
                    localScale = new Vector3(.5f, .5f, .5f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1874898f, 0.1037676f, 0.04180111f),
                    localAngles = new Vector3(283.4243f, 178.2074f, 65.93817f),
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
                    localPos = new Vector3(-0.7404389f, 0.07693511f, 0.002353907f),
                    localAngles = new Vector3(61.9685f, 160.8914f, 234.2564f),
                    localScale = new Vector3(.2f, .2f, .2f)

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
                    childName = "ThighL",
                    localPos = new Vector3(0.128604f, 0.04170567f, 0.0362651f),
                    localAngles = new Vector3(294.3157f, 50.21239f, 8.014451f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
                
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1502278f, 0.02505913f, 0.005195001f),
                    localAngles = new Vector3(312.9884f, 185.7034f, 77.97595f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HeadBase",
                    localPos = new Vector3(0.6552188f, -0.1591174f, -0.1081835f),
                    localAngles = new Vector3(28.51935f, 16.06732f, 68.01345f),
                    localScale = new Vector3(.09f, .09f, .09f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1418974f, 0.1018404f, 0.1193604f),
                    localAngles = new Vector3(305.2793f, 13.87357f, 33.9735f),
                    localScale = new Vector3(0.09f, 0.09f, 0.09f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(1.189638f, -0.05416813f, -0.431556f),
                    localAngles = new Vector3(327.4012f, 170.4274f, 285.5928f),
                    localScale = new Vector3(0.5f, 0.5f, 0.5f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.1160537f, -0.05998493f, 0.09456927f),
                    localAngles = new Vector3(352.1081f, 175.0939f, 243.0748f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(-0.09908636f, -0.4335292f, 0.0002002567f),
                    localAngles = new Vector3(3.861964f, 59.13883f, 332.0835f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(0.01783761f, 0.1200769f, -0.1452967f),
                    localAngles = new Vector3(315.8733f, 81.28423f, 78.75524f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Weapon",
                    localPos = new Vector3(4.056051f, -0.3732671f, 0.59979f),
                    localAngles = new Vector3(298.3842f, 184.4189f, 56.38975f),
                    localScale = new Vector3(1f, 1f, 1f)
                }
            });

            //Modded Chars 
            rules.Add("EnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName =  "CalfL",
                    localPos =   new Vector3(0.03588771f, 0.09803209f, 0.1293422f),
                    localAngles = new Vector3(307.0168f, 268.4248f, 63.07141f),
                    localScale = new Vector3(0.085f, 0.085f, 0.085f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.0006026067f, 0.002815368f, 0.009515685f),
                    localAngles = new Vector3(308.2012f, 0.3406859f, 60.02274f),
                    localScale = new Vector3(0.003f, 0.003f, 0.003f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[] //these ones don't work for some reason!
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(0.1704589f, 0.2944336f, 0.09629773f),
                    localAngles = new Vector3(302.1373f, 171.8758f, 50.29458f),
                    localScale = new Vector3(0.09f, 0.09f, 0.09f)
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
                    childName = "LegL",
                    localPos = new Vector3(0.0001376976f, 0.000773763f, 0.001397073f),
                    localAngles = new Vector3(296.4636f, 275.2156f, 65.79694f),
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
                    childName = "ThighL",
                    localPos = new Vector3(-0.2642168f, 0.08241536f, -0.03230814f),
                    localAngles = new Vector3(80.15402f, 192.351f, 264.9937f),
                    localScale = new Vector3(0.085f, 0.085f, 0.085f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Sheath",
                    localPos = new Vector3(0.04240592f, 0.2797743f, -0.004625648f),
                    localAngles = new Vector3(61.60046f, 171.9134f, 239.4733f),
                    localScale = new Vector3(0.061f, 0.061f, 0.061f)
                }
            });
            rules.Add("mdlExecutioner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.001009098f, 0.002151658f, -0.0001070039f),
                    localAngles = new Vector3(306.6896f, 353.9325f, 64.98692f),
                    localScale = new Vector3(0.0006f, 0.0006f, 0.0006f)
                }
            });
            rules.Add("mdlNemmando", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(0.001197227f, 0.001904767f, 0.00006217563f),
                    localAngles = new Vector3(299.9001f, 176.2292f, 45.39405f),
                    localScale = new Vector3(0.0007f, 0.0007f, 0.0007f)
                }
            });

            return rules;
        }

        public override void Hooks()
        {
            //On.RoR2.HealthComponent.TakeDamage += AdzeDamageBonus;
            GlobalEventManager.onCharacterDeathGlobal += CryoCanisterAOE;
            RecalculateStatsAPI.GetStatCoefficients += CalculateStatsCryoHook;
            //On.RoR2.CharacterBody.RecalculateStats += CryoStatsHook;

        }

        //private void CryoStatsHook(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        //{
        //    orig(self);
        //    if (self)
        //    {
        //        if(self.GetBuffCount(preFreezeSlow) > 0)
        //        {
        //            self.moveSpeed *= slowPercentage.Value;
        //        }
        //    }
        //}
        //
        private void CalculateStatsCryoHook(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender)
            {
                if (sender.GetBuffCount(preFreezeSlow) > 0)
                {
                    args.moveSpeedReductionMultAdd += slowPercentage.Value;
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
                //Debug.Log("fake");
                return; //end func if death wasn't killed by something real enough
            }

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

                    EffectData effectData = new EffectData
                    {
                        origin = victimBody.corePosition
                    };
                    effectData.SetNetworkedObjectReference(victimBody.gameObject);
                    EffectManager.SpawnEffect(EntityStates.Mage.Weapon.FireIceOrb.effectPrefab, effectData, true);
                    
                        //EntityStates.Mage.Weapon.FireIceOrb.effectPrefab
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
                    //Debug.Log("found: " + cryoAOEHurtBoxBuffer.Count);
                    for (int i = 0; i < cryoAOEHurtBoxBuffer.Count; i++)
                    {
                        HurtBox hurtBox = cryoAOEHurtBoxBuffer[i];
                        if (hurtBox.healthComponent && hurtBox.healthComponent.body)
                        {
                            float duartion = slowDuration.Value + (slowDurationStacking.Value * (cryoCount - 1));
                            //Debug.Log("found a health component and hc body");
                            hurtBox.healthComponent.body.AddTimedBuffAuthority(preFreezeSlow.buffIndex, duartion);
                            DamageInfo damageInfo = new DamageInfo
                            {
                                attacker = attackerBody.gameObject,
                                crit = attackerBody.RollCrit(),
                                damage = AOEDamage,
                                position = corePosition,
                                procCoefficient = 1,
                                damageType = DamageType.AOE,
                                damageColorIndex = DamageColorIndex.Item,
                            };
                            hurtBox.healthComponent.TakeDamage(damageInfo);
                            //Debug.Log("sent take damage");
                            //self.GetComponent<CharacterBody>().AddTimedBuff(preFreezeSlow, slowDuration.Value);
                            if (hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow) >= requiredStacksForFreeze.Value)
                            {
                                //float duartion = slowDuration.Value + ((slowDuration.Value / 2f) * (cryoCount - 1));
                                if (!hurtBox.healthComponent.body.isBoss)
                                {
                                    //hurtBox.healthComponent.isInFrozenState = true;
                                    SetStateOnHurt setState = hurtBox.healthComponent.body.gameObject.GetComponent<SetStateOnHurt>();
                                    if (setState)
                                    {
                                        int buffCount = hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow);
                                        for(int j = 0; j < buffCount; j++)
                                        {
                                            hurtBox.healthComponent.body.RemoveOldestTimedBuff(preFreezeSlow);
                                        }

                                        setState.SetFrozen(duartion);
                                    }
                                }
                                else if(hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow) >= requiredStacksForBossFreeze.Value)
                                {
                                    //hurtBox.healthComponent.isInFrozenState = true;
                                    //SetStateOnHurt setState = hurtBox.healthComponent.body.gameObject.GetComponent<SetStateOnHurt>();
                                    //if (setState)
                                    //{
                                        int buffCount = hurtBox.healthComponent.body.GetBuffCount(preFreezeSlow);
                                        for (int j = 0; j < buffCount; j++)
                                        {
                                            hurtBox.healthComponent.body.RemoveOldestTimedBuff(preFreezeSlow);
                                        }

                                        //setState.SetFrozen(duartion);
                                        hurtBox.healthComponent.isInFrozenState = true;
                                    //}
                                }

                                EffectData effectData2 = new EffectData
                                {
                                    origin = victimBody.corePosition
                                };
                                effectData2.SetNetworkedObjectReference(victimBody.gameObject);
                                EffectManager.SpawnEffect(EntityStates.Mage.Weapon.IceNova.impactEffectPrefab, effectData2, true);
                                //EffectManager.SpawnEffect(EntityStates.Mage.Weapon.IceNova.novaEffectPrefab, effectData2, true);

                                //EntityStates.Mage.Weapon.FireIceOrb.effectPrefab;
                                //GlobalEventManager.CommonAssets.bleedOnHitAndExplodeImpactEffect
                                //EffectManager.SpawnEffect(iceDeathAOEObjectLazy, effectData, true);
                                //effectData.SetNetworkedObjectReference(victimBody.gameObject);
                                ////EffectManager.SpawnEffect(iceDeathAOEObject, effectData, true);
                                //EffectManager.SpawnEffect(iceDeathAOEObjectLazy, effectData, true);
                                //GameObject gameObject = UnityEngine.Object.Instantiate(iceDeathAOEObject, victimBody.corePosition, Quaternion.identity);
                                //iceDeathObject.AddComponent<EffectComponent>();

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
                    cryoAOEHurtBoxBuffer.Clear();

                    //if (victimBody.GetBuffCount(preFreezeSlow) > requiredStacksForFreeze.Value)
                    //{
                    //}

                    if (victimBody.healthComponent.isInFrozenState)
                    {
                        EffectData effectData2 = new EffectData
                        {
                            origin = victimBody.corePosition
                        };
                        effectData2.SetNetworkedObjectReference(victimBody.gameObject);
                        EffectManager.SpawnEffect(EntityStates.Mage.Weapon.IceNova.impactEffectPrefab, effectData2, true);
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
