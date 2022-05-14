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
    public class ClockworkMechanism : ItemBase<ClockworkMechanism>
    {
        public ConfigEntry<float> directorBuff;

        public ConfigEntry<float> stackingBuff;

        public ConfigEntry<float> breakCooldown;

        public ConfigEntry<bool> alwaysHappen;

        public ConfigEntry<string> voidPair;

        private Xoroshiro128Plus watchVoidRng;
        public override string ItemName => "Clockwork Mechanism";

        public override string ItemLangTokenName => "CLOCKWORK_ITEM";

        public override string ItemPickupDesc => "Increase the number of interactables per stage. Breaks a random item at low health. <style=cIsVoid>Corrupts all Delicate Watches</style>.";

        public override string ItemFullDescription => $"Increase the number of <style=cIsUtility>interactables</style> per stage by an amount equal to <style=cIsUtility>{Math.Round(directorBuff.Value / 15, 1)}</style> <style=cStack>(+{Math.Round(stackingBuff.Value / 15, 1)} per stack)</style> chests. Taking damage to below <style=cIsHealth>25% health</style> breaks <style=cDeath>a random item</style>, with a cooldown of <style=cIsUtility>{breakCooldown.Value} seconds</style>. <style=cIsVoid>Corrupts all Delicate Watches</style>.";

        public override string ItemLore => $"\"The clock is always ticking. The hands of time move independently of your desire for them to still - the sands flow eternally and will never pause. Use what little time you have efficiently - once you've lost that time, it's quite hard to find more.\"" +
            "\n\n- Lost Journal, recovered from Petrichor V";

        public override ItemTier Tier => ItemTier.VoidTier1;
        

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlClockworkPickup.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("clockworkIcon512.png");

        public override ItemTag[] ItemTags => new ItemTag[3] { ItemTag.Utility, ItemTag.LowHealth, ItemTag.AIBlacklist };

        public static GameObject ItemBodyModelPrefab;

        public BuffDef recentBreak { get; private set; }

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);
            CreateBuff();
            Hooks();
            
        }

        public override void CreateConfig(ConfigFile config)
        {
            directorBuff = config.Bind<float>("Item: " + ItemName, "Increased Credits", 22.5f, "Adjust how many credits the first stack gives the director. 15 credits is one chest.");
            stackingBuff = config.Bind<float>("Item: " + ItemName, "Percent Increase per Stack", 22.5f, "Adjust the increase gained per stack."); //22.5f is 1.5 chests
            breakCooldown = config.Bind<float>("Item: " + ItemName, "Cooldown Between Breaking Items", 3.0f, "Adjust how long the cooldown is between the item breaking other items.");
            alwaysHappen = config.Bind<bool>("Item: " + ItemName, "Function in Special Stages", false, "Adjust whether or not the item should increase the number of credits in stages where the director doesn't get any credits (ex Bazaar, Void Fields).");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "FragileDamageBonus", "Adjust which item this is the void pair of.");
        }

        public void CreateBuff()
        {
            recentBreak = ScriptableObject.CreateInstance<BuffDef>();
            recentBreak.buffColor = Color.white;
            recentBreak.canStack = false;
            recentBreak.isDebuff = false;
            recentBreak.name = "ZnVV" + "shatterStatus";
            recentBreak.iconSprite = vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("shatterStatus");
            ContentAddition.AddBuffDef(recentBreak);
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlClockworkDisplay.prefab");

            string transpMat = "RoR2/DLC1/voidraid/matVoidRaidPlanetAcidRing.mat";

            var transpBit = ItemBodyModelPrefab.transform.Find("CaseTopTransp").GetComponent<MeshRenderer>(); //CaseTopTransp 
            transpBit.material = Addressables.LoadAssetAsync<Material>(transpMat).WaitForCompletion();

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);

            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(-0.01480415f, 0.02261418f, 0.05049226f),
                    localAngles = new Vector3(76.72475f, 298.479f, 269.3308f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(0.02170372f, 0.02288273f, 0.03459106f),
                    localAngles = new Vector3(40.77299f, 59.15304f, 45.32623f),
                    localScale = new Vector3(0.055f, 0.055f, 0.055f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MainWeapon",
                    localPos = new Vector3(-0.05870793f, 0.4463752f, -0.03718469f),
                    localAngles = new Vector3(272.7316f, 270.5243f, 60.82039f),
                    localScale = new Vector3(0.055f, 0.055f, 0.055f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(-0.009995257f, 3.125906f, 0.3110375f),
                    localAngles = new Vector3(321.4942f, 87.76231f, 68.65869f),
                    localScale = new Vector3(.5f, .5f, .5f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.008402169f, 0.317406f, -0.02993058f),
                    localAngles = new Vector3(16.07273f, 266.9244f, 56.10647f),
                    localScale = new Vector3(.05f, .05f, .05f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.56402f, 0.2794803f, -0.6070232f),
                    localAngles = new Vector3(0F, 270F, 256.4133f),
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
                    childName = "UpperArmL",
                    localPos = new Vector3(0.02184698f, 0.2957828f, 0.01117276f),
                    localAngles = new Vector3(40.07924f, 169.024f, 58.42667f),
                    localScale = new Vector3(0.04f, 0.04f, 0.04f)
                }
                
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandR",
                    localPos = new Vector3(-0.005859678f, 0.04901259f, 0.08395796f),
                    localAngles = new Vector3(312.638f, 68.74719f, 76.17706f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "HandL",
                    localPos = new Vector3(-0.001769217f, 0.8750249f, 0.07755471f),
                    localAngles = new Vector3(86.13224f, 306.094f, 282.6791f),
                    localScale = new Vector3(.08f, .08f, .08f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.0394742f, 0.2271337f, -0.01859298f),
                    localAngles = new Vector3(284.3109f, 327.4875f, 323.4077f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(0.5622984f, 1.865077f, -0.6787693f),
                    localAngles = new Vector3(66.2142f, 74.12748f, 274.7035f),
                    localScale = new Vector3(0.7f, 0.7f, 0.7f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.01683217f, 0.1543664f, -0.03668313f),
                    localAngles = new Vector3(64.64587f, 102.4221f, 240.8662f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(-0.007638952f, 0.2209761f, -0.04840721f),
                    localAngles = new Vector3(278.3271f, 191.7697f, 152.4978f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ForeArmL",
                    localPos = new Vector3(0.04742341f, 0.3199588f, 0.01808804f),
                    localAngles = new Vector3(73.18906f, 38.83948f, 291.2487f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            return rules;

        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.UpdateLastHitTime += BreakItem;
            RoR2.SceneDirector.onPrePopulateSceneServer += HelpDirector;
        }

        private void HelpDirector(SceneDirector obj)
        {
            if (alwaysHappen.Value || obj.interactableCredit != 0) {
                //Debug.Log("function starting, interactable credits: " + obj.interactableCredit);
                int itemCount = 0;
                foreach (var player in PlayerCharacterMasterController.instances)
                {
                    itemCount += player.master.inventory.GetItemCount(ItemBase<ClockworkMechanism>.instance.ItemDef);
                }
                obj.interactableCredit += (int)(directorBuff.Value + (stackingBuff.Value * (itemCount - 1)));
            }
            //Debug.Log("function ending, interactable credits after: " + obj.interactableCredit);
        }

        private void BreakItem(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, HealthComponent self, float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker)
        {
            orig.Invoke(self, damageValue, damagePosition, damageIsSilent, attacker);
            if (NetworkServer.active && (bool)attacker && (bool)self && (bool)self.body && ItemBase<ClockworkMechanism>.instance.GetCount(self.body) > 0 && self.isHealthLow && !(self.GetComponent<CharacterBody>().GetBuffCount(recentBreak) > 0) )
            {
                self.GetComponent<CharacterBody>().AddTimedBuff(recentBreak, breakCooldown.Value);
                if (watchVoidRng == null)
                {
                    watchVoidRng = new Xoroshiro128Plus(Run.instance.seed);
                }

                List<ItemIndex> list = new List<ItemIndex>(self.body.inventory.itemAcquisitionOrder);
                ItemIndex itemIndex = ItemIndex.None;
                Util.ShuffleList(list, watchVoidRng);
                foreach (ItemIndex item in list)
                {
                    
                    ItemDef itemDef = ItemCatalog.GetItemDef(item);
                    if ((bool)itemDef && itemDef.tier != ItemTier.NoTier)
                    {
                        itemIndex = item;
                        break;
                    }
                    
                }
                if (itemIndex != ItemIndex.None)
                {
                    self.body.inventory.RemoveItem(itemIndex);
                    self.body.inventory.GiveItem(ItemBase<BrokenClockworkMechanism>.instance.ItemDef);
                    CharacterMasterNotificationQueue.PushItemTransformNotification(self.body.master, itemIndex, ItemBase<BrokenClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                }

                //List<ItemIndex> itemList = new List<ItemIndex>(self.body.inventory.itemAcquisitionOrder);
                //Util.ShuffleList(itemList, watchVoidRng);

                //self.body.inventory.GiveItem(ItemBase<BrokenClockworkMechanism>.instance.ItemDef, 1);
                //self.body.inventory.RemoveItem(ItemCatalog.GetItemDef(itemList[0]), 1);
                //CharacterMasterNotificationQueue.PushItemTransformNotification(self.body.master, ItemCatalog.GetItemDef(itemList[0]).itemIndex, ItemBase<BrokenClockworkMechanism>.instance.ItemDef.itemIndex, CharacterMasterNotificationQueue.TransformationType.Default);
                EffectData effectData = new EffectData
                {
                    origin = self.transform.position
                };
                effectData.SetNetworkedObjectReference(self.gameObject);
                EffectManager.SpawnEffect(HealthComponent.AssetReferences.fragileDamageBonusBreakEffectPrefab, effectData, transmit: true);
            }
            orig(self, damageValue, damagePosition, damageIsSilent, attacker);
        }
    }
}
