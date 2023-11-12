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
using RoR2.Orbs;
using System.Linq;
using RoR2.Projectile;

namespace vanillaVoid.Items
{
    public class RelentlessRounds : ItemBase<RelentlessRounds>
    {
        public ConfigEntry<float> baseDamage;

        public ConfigEntry<float> stackingDamage;

        public ConfigEntry<bool> hitAll;

        public ConfigEntry<bool> requireTeleporter;

//public ConfigEntry<string> voidPair;

        public override string ItemName => "Relentless Rounds";

        public override string ItemLangTokenName => "RELROUNDS_ITEM";

        public override string ItemPickupDesc => "Killing enemies" +
            (requireTeleporter.Value ? $" near the teleporter" : "") + $" damages active bosses. <style=cIsVoid>Corrupts all Armor-Piercing Rounds</style>.";

        public override string ItemFullDescription => $"Killing enemies" +
            (hitAll.Value ? $" near the teleporter" : "") + $" deals {baseDamage.Value * 100}%"+
            (stackingDamage.Value != 0 ? $" <style=cStack>(+{stackingDamage.Value * 100}% per stack)</style>" : "") + $" damage to " +
            (hitAll.Value ? $" all active bosses" : " a random active boss") + $". <style=cIsVoid>Corrupts all {"{CORRUPTION}"}</style>.";

        public override string ItemLore => $"Rounds Lore";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlAdzePickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("adzeIcon512.png");

        public static GameObject ItemBodyModelPrefab;

        public List<CharacterBody> activeBosses;

        public static DamageAPI.ModdedDamageType relentlessDamageType;

        public static GameObject rentlessProjectile => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("RentlessProjectile.prefab");

        public static GameObject rentlessProjectileGhost => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("RentlessProjectileGhost");

        public static GameObject rentlessEffect => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("RentlessEffect");

        public static GameObject rentlessOrbEffect => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("RelentlessOrbEffect.prefab");

        public static Material rentlessTrail => vanillaVoidPlugin.MainAssets.LoadAsset<Material>("matRentlessTrail");

        //public static Material

        public override ItemTag[] ItemTags => new ItemTag[1] { ItemTag.Damage };

        //public BuffDef relentlessDef { get; private set; }

        //public static DotController.DotIndex dotIndex;
        //public static DotController.DotDef dotDef;

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            //VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            //CreateDOT();
            activeBosses = new List<CharacterBody>();
            relentlessDamageType = DamageAPI.ReserveDamageType();

            SetupALot();
            Hooks(); 
        }

        public void SetupALot()
        {
            rentlessProjectile.AddComponent<NetworkIdentity>();
            rentlessProjectile.AddComponent<TeamFilter>();
            var contr = rentlessProjectile.AddComponent<ProjectileController>();
            contr.procCoefficient = 1;
            rentlessProjectile.AddComponent<ProjectileDamage>();
            rentlessProjectile.AddComponent<ProjectileTargetComponent>();
            var transf = rentlessProjectile.AddComponent<ProjectileNetworkTransform>();
            transf.positionTransmitInterval = .33333f;
            transf.interpolationFactor = 1;
            var steer = rentlessProjectile.AddComponent<ProjectileSteerTowardTarget>();
            steer.rotationSpeed = 720;
            rentlessProjectile.AddComponent<ProjectileSimple>();

            rentlessProjectileGhost.AddComponent<ProjectileGhostController>().enabled = false;
            var curve = rentlessProjectileGhost.AddComponent<MoveCurve>();
            curve.animateX = true;
            curve.animateY = true;
            curve.animateZ = true;
            curve.curveScale = 1;
            curve.moveCurve.keys = new Keyframe[] {
            new Keyframe(0, 0),
            new Keyframe(1, 1)
            };

            var rpgvfx = rentlessProjectileGhost.transform.Find("VFX");
            var rpgri = rpgvfx.gameObject.AddComponent<RotateItem>();
            rpgri.spinSpeed = 30;
            rpgri.bobHeight = .3f;
            rpgri.offsetVector = Vector3.zero;

            var effc = rentlessEffect.AddComponent<EffectComponent>();
            effc.applyScale = true;
            var vfxatr = rentlessEffect.AddComponent<VFXAttributes>();
            vfxatr.vfxPriority = VFXAttributes.VFXPriority.Low;
            vfxatr.vfxIntensity = VFXAttributes.VFXIntensity.Low;
            var timer = rentlessEffect.AddComponent<DestroyOnTimer>();
            timer.duration = .3f;

            var effc2 = rentlessOrbEffect.AddComponent<EffectComponent>();
            effc2.applyScale = true;

            var orb = rentlessOrbEffect.AddComponent<OrbEffect>();
            orb.startVelocity1 = new Vector3(-4, 3, -4);
            orb.startVelocity2 = new Vector3(4, 1, 4);
            orb.endVelocity1 = new Vector3(-4, 3, -4);
            orb.endVelocity2 = new Vector3(4, 1, 4);
            
            orb.movementCurve.keys = new Keyframe[] {
            new Keyframe(0, 0),
            new Keyframe(1, 1)
            };

            orb.startEffectScale = 1;
            orb.endEffect = rentlessEffect;
            orb.endEffectScale = 2.5f;

            var vfxatr2 = rentlessOrbEffect.AddComponent<VFXAttributes>();
            vfxatr2.vfxPriority = VFXAttributes.VFXPriority.Medium;
            vfxatr2.vfxIntensity = VFXAttributes.VFXIntensity.Medium;

            var bez = rentlessEffect.transform.Find("Bezier").gameObject;
            bez.GetComponent<LineRenderer>().material = rentlessTrail;

            orb.bezierCurveLine = bez.AddComponent<BezierCurveLine>();

            var asa = bez.AddComponent<AnimateShaderAlpha>();
            asa.alphaCurve.keys = new Keyframe[] {
            new Keyframe(0, 0),
            new Keyframe(.510f, 1.05f),
            new Keyframe(1, 1)
            };
            asa.destroyOnEnd = true;

        }

        public override void CreateConfig(ConfigFile config)
        {
            baseDamage = config.Bind<float>("Item: " + ItemName, "Percent Damage", 2.5f, "Adjust the percent of extra damage dealt on the first stack.");
            stackingDamage = config.Bind<float>("Item: " + ItemName, "Stacking Percent Damage", 2.5f, "Adjust the percent of extra damage dealt per stack.");
            hitAll = config.Bind<bool>("Item: " + ItemName, "Hit All Bosses", true, "Adjust if the item should hit all active bosses or only one at random.");
            requireTeleporter = config.Bind<bool>("Item: " + ItemName, "Require Teleporter", true, "Adjust if the item should only deal damage if the killed enemy.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "BossDamageBonus", "Adjust which item this is the void pair of.");
        }

        //public void CreateDOT()
        //{
        //    
        //    //relentlessDef = ScriptableObject.CreateInstance<BuffDef>();
        //    //relentlessDef.buffColor = Color.white;
        //    //relentlessDef.canStack = true;
        //    //relentlessDef.isDebuff = false;
        //    //relentlessDef.name = "ZnVV" + "shatterStatus";
        //    //relentlessDef.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("shatterStatus");
        //    //ContentAddition.AddBuffDef(relentlessDef);
        //    //
        //    ////Sprite DOTIcon = ItemIcon;
        //    //index = DotAPI.RegisterDotDef(0.25f, 0.25f, DamageColorIndex.SuperBleed, relentlessDef);
        //    ////DotAPI.RegisterDotDef()
        //    //
        //
        //    relentlessDef.name = "ZnVVrelentlessDOT";
        //    relentlessDef.buffColor = new Color32(96, 245, 250, 255);
        //    relentlessDef.canStack = false;
        //    relentlessDef.isDebuff = false;
        //    relentlessDef.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("shatterStatus");
        //
        //    //ashBurnEffectParams = new BurnEffectController.EffectParams
        //    //{
        //    //    startSound = "Play_item_proc_igniteOnKill_Loop",
        //    //    stopSound = "Stop_item_proc_igniteOnKill_Loop",
        //    //    overlayMaterial = Main.AssetBundle.LoadAsset<Material>("Assets/Items/Marwan's Ash/matMarwanAshBurnOverlay.mat"),
        //    //    fireEffectPrefab = null
        //    //};
        //    //ColorCatalog.ColorIndex.VoidCoin
        //    dotDef = new DotController.DotDef 
        //    {
        //        associatedBuff = relentlessDef,
        //        damageCoefficient = 1f,
        //        damageColorIndex = DamageColorIndex.Void,
        //        interval = .5f
        //    };
        //    dotIndex = DotAPI.RegisterDotDef(dotDef, (self, dotStack) =>
        //    {
        //        var damageMultiplier = 1f;
        //        var attackerDamage = 1f;
        //        if (dotStack.attackerObject)
        //        {
        //            var attackerBody = dotStack.attackerObject.GetComponent<CharacterBody>();
        //            if (attackerBody)
        //            {
        //                attackerDamage = attackerBody.damage;
        //                if (attackerDamage != 0f) damageMultiplier = dotStack.damage / attackerDamage;
        //            }
        //        }
        //        if (self.victimHealthComponent)
        //            dotStack.damage = self.victimHealthComponent.fullCombinedHealth * damageMultiplier; //Mathf.Min(self.victimHealthComponent.fullCombinedHealth * damageMultiplier, attackerDamage * 8);
        //        else
        //            dotStack.damage = 0;
        //        dotStack.damage *= dotDef.interval;
        //    });
        //}


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlAdzeDisplay.prefab");
            //string orbTransp = "RoR2/DLC1/voidraid/matVoidRaidPlanetPurpleWave.mat"; 
            //string orbCore = "RoR2/DLC1/voidstage/matVoidCoralPlatformPurple.mat";

            string orbTransp = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorLightning.mat";
            string orbCore = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorPod.mat";

            var adzeOrbsModelTransp = ItemModel.transform.Find("orbTransp").GetComponent<MeshRenderer>();
            var adzeOrbsModelCore = ItemModel.transform.Find("orbCore").GetComponent<MeshRenderer>();
            adzeOrbsModelTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            adzeOrbsModelCore.material = Addressables.LoadAssetAsync<Material>(orbCore).WaitForCompletion();

            var adzeOrbsDisplayTransp = ItemBodyModelPrefab.transform.Find("orbTransp").GetComponent<MeshRenderer>();
            var adzeOrbsDisplayCore = ItemBodyModelPrefab.transform.Find("orbCore").GetComponent<MeshRenderer>();
            adzeOrbsDisplayTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            adzeOrbsDisplayCore.material = Addressables.LoadAssetAsync<Material>(orbCore).WaitForCompletion();

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
            //On.RoR2.HealthComponent.TakeDamage += AdzeDamageBonus;
            CharacterBody.onBodyStartGlobal += CheckForBosses;
            GlobalEventManager.onCharacterDeathGlobal += RelentlessDeathDamage;

        }

        private void CheckForBosses(CharacterBody obj)
        {
            if (obj.isBoss)
            {
                activeBosses.Add(obj);
            }
        }

        private void RelentlessDeathDamage(DamageReport obj)
        {
            if (obj.victimIsBoss)
            {
                var success = activeBosses.Remove(obj.victimBody);
                //Debug.Log(success + " for removing " + obj.victimBody.name);
            }
            var teleInstance = TeleporterInteraction.instance;
            if (teleInstance)
            {
                var attacker = obj.attackerBody;
                if (attacker && !obj.victimIsBoss)
                {
                    //var token = attacker.GetComponent<RoundsToken>();
                    //if (token)
                    //{
                    
                    //}
                    if (attacker.inventory)
                    {
                        bool inTeleporter = teleInstance.holdoutZoneController.IsBodyInChargingRadius(obj.victimBody);
                        var count = attacker.inventory.GetItemCount(ItemDef);
                        if (count > 0 && (inTeleporter || !requireTeleporter.Value))
                        {
                            foreach (var boss in activeBosses)
                            {
                                DamageInfo damageInfo = new DamageInfo
                                {
                                    attacker = attacker.gameObject,
                                    crit = attacker.RollCrit(),
                                    damage = attacker.damage * 2.5f,
                                    position = attacker.transform.position,
                                    procCoefficient = 1,
                                    damageType = DamageType.Generic,
                                    damageColorIndex = DamageColorIndex.Item,
                                };
                                boss.healthComponent.TakeDamage(damageInfo);

                                RelentlessOrb relOrb = new RelentlessOrb();
                                relOrb.damageValue = attacker.damage * 2.5f * count;
                                relOrb.damageType = DamageType.Generic;
                                relOrb.isCrit = damageInfo.crit;
                                relOrb.damageColorIndex = DamageColorIndex.Void;
                                relOrb.procCoefficient = .5f;
                                relOrb.origin = obj.victimBody.corePosition;
                                relOrb.teamIndex = attacker.teamComponent.teamIndex;
                                relOrb.attacker = attacker.gameObject;
                                relOrb.procChainMask = damageInfo.procChainMask;
                                HurtBox hurtbox = boss.mainHurtBox;
                                if (hurtbox)
                                {
                                    relOrb.target = hurtbox;
                                    OrbManager.instance.AddOrb(relOrb);
                                }

                            }
                            //token = attacker.gameObject.AddComponent<RoundsToken>();

                        }
                    }
                }
            }
        }


        //private void AdzeDamageBonus(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo) {
        //    
        //    orig(self, damageInfo);
        //
        //    if (!damageInfo.rejected)
        //    {
        //        var body = self.body;
        //        if (body)
        //        {
        //            if (body.isBoss && body.GetBuffCount(relentlessDef) > 0)
        //            {
        //                var dotInfo = new InflictDotInfo()
        //                {
        //                    attackerObject = damageInfo.attacker,
        //                    victimObject = self.body.gameObject,
        //                    dotIndex = dotIndex,
        //                    duration = -1,
        //                    damageMultiplier = 1,
        //                };
        //                DotController.InflictDot(ref dotInfo);
        //            }
        //        }
        //    }
        //
        //}
        public class RelentlessOrb : Orb
        {
            public override void Begin()
            {
                base.duration = 0.2f;

                EffectData effectData = new EffectData
                {
                    origin = this.origin,
                    genericFloat = base.duration
                };
                effectData.SetHurtBoxReference(this.target);

                EffectManager.SpawnEffect(rentlessOrbEffect, effectData, true);
            }

            public override void OnArrival()
            {
                if (this.target)
                {
                    HealthComponent healthComponent = this.target.healthComponent;
                    if (healthComponent)
                    {
                        DamageInfo damageInfo = new DamageInfo();
                        damageInfo.damage = this.damageValue;
                        damageInfo.attacker = this.attacker;
                        damageInfo.inflictor = this.inflictor;
                        damageInfo.force = Vector3.zero;
                        damageInfo.crit = this.isCrit;
                        damageInfo.procChainMask = this.procChainMask;
                        damageInfo.procCoefficient = this.procCoefficient;
                        damageInfo.position = this.target.transform.position;
                        damageInfo.damageColorIndex = this.damageColorIndex;
                        damageInfo.damageType = this.damageType;
                        damageInfo.AddModdedDamageType(relentlessDamageType);
                        healthComponent.TakeDamage(damageInfo);
                        GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                        GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
                    }
                    //if (this.bouncesRemaining > 0)
                    //{
                    //    if (this.bouncedObjects != null)
                    //    {
                    //        this.bouncedObjects.Add(this.target.healthComponent);
                    //    }
                    //    HurtBox hurtBox = this.PickNextTarget(this.target.transform.position, healthComponent);
                    //    if (hurtBox)
                    //    {
                    //        RelentlessOrb maliceOrb = new RelentlessOrb();
                    //        maliceOrb.search = this.search;
                    //        maliceOrb.origin = this.target.transform.position;
                    //        maliceOrb.target = hurtBox;
                    //        maliceOrb.attacker = this.attacker;
                    //        maliceOrb.inflictor = this.inflictor;
                    //        maliceOrb.teamIndex = this.teamIndex;
                    //        maliceOrb.damageValue = this.damageValue * this.damageCoefficientPerBounce;
                    //        maliceOrb.bouncesRemaining = this.bouncesRemaining - 1;
                    //        maliceOrb.isCrit = this.isCrit;
                    //        maliceOrb.bouncedObjects = this.bouncedObjects;
                    //        maliceOrb.procChainMask = this.procChainMask;
                    //        maliceOrb.procCoefficient = this.procCoefficient;
                    //        maliceOrb.damageColorIndex = this.damageColorIndex;
                    //        maliceOrb.damageCoefficientPerBounce = this.damageCoefficientPerBounce;
                    //        maliceOrb.baseRange = this.baseRange;
                    //        maliceOrb.damageType = this.damageType;
                    //        OrbManager.instance.AddOrb(maliceOrb);
                    //    }
                    //}

                }
            }
            //public HurtBox PickNextTarget(Vector3 position, HealthComponent currentVictim)
            //{
            //    if (this.search == null)
            //    {
            //        this.search = new BullseyeSearch();
            //    }
            //    float range = baseRange;
            //    if (currentVictim && currentVictim.body)
            //    {
            //        range += currentVictim.body.radius;
            //    }
            //    this.search.searchOrigin = position;
            //    this.search.searchDirection = Vector3.zero;
            //    this.search.teamMaskFilter = TeamMask.allButNeutral;
            //    this.search.teamMaskFilter.RemoveTeam(this.teamIndex);
            //    this.search.filterByLoS = false;
            //    this.search.sortMode = BullseyeSearch.SortMode.Distance;
            //    this.search.maxDistanceFilter = range;
            //    this.search.RefreshCandidates();
            //    HurtBox hurtBox = (from v in this.search.GetResults()
            //                       where !this.bouncedObjects.Contains(v.healthComponent)
            //                       select v).FirstOrDefault<HurtBox>();
            //    if (hurtBox)
            //    {
            //        this.bouncedObjects.Add(hurtBox.healthComponent);
            //    }
            //    return hurtBox;
            //}

            public float damageValue;

            public GameObject attacker;

            public GameObject inflictor;

            public TeamIndex teamIndex;

            public bool isCrit;

            public ProcChainMask procChainMask;

            public float procCoefficient = 1f;

            public DamageColorIndex damageColorIndex;

            public float baseRange = 20f;

            public DamageType damageType;

        }
    }
    //public class RoundsToken : MonoBehaviour
    //{
    //    public List<CharacterBody> activeBosses;
    //
    //    void Awake()
    //    {
    //        activeBosses = new List<CharacterBody>();
    //    }
    //}

}
