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
using EntityStates;

namespace vanillaVoid.Items
{
    public class DashQuill : ItemBase<DashQuill>
    {
        public ConfigEntry<float> dashVelocity;

        public ConfigEntry<float> shorthopVelocity;

        public ConfigEntry<int> dashesPerStack;

        public ConfigEntry<string> voidPair;

        public override string ItemName => "Quasitemporal Quill";

        public override string ItemLangTokenName => "DASHQUILL_ITEM";

        public override string ItemPickupDesc => "Gain an airdash. <style=cIsVoid>Corrupts all Hopoo Feathers</style>.";

        public override string ItemFullDescription => $"Jumping while midair performs an <style=cIsUtility>airdash</style>. Gain <style=cIsUtility>{dashesPerStack.Value}</style> <style=cStack>(+{dashesPerStack.Value} per stack)</style> maximum <style=cIsUtility>airdashes</style>. <style=cIsVoid>Corrupts all Hopoo Feathers</style>.";

        public override string ItemLore => $"<style=cSub>Order: Normal Ink and Quill \nTracking Number: 0372******* \nEstimated Delivery: 1/2/2056 \nShipping Method: High Priority/Fragile/Confidential \nShipping Address: [REDACTED] \nShipping Details: \n\n</style>" + 
            "Hey - hopefully a quick summary should do, since I don't have much time to set this up - first off, don't touch the ink. Keep it at a distance from yourself or have several layers of protection - corporate and HR demand it, and because of that, we still don't know what'll happen if someone touches it. However, we know what it does to inanimate objects, and as much as those corporate leeches can get on my nerves I think they've got a point here - it rapidly, yet briefly, ages the object it touches. The process can be altered a bit - it takes as long as the ink takes to dry into the substance. We say ink around here, though we're not sure where exactly it comes from nor its exact composition - and despite the name I wouldn't recommend trying to write with it. It'll rot right through the page and start gnawing at the table after. \n\nAnd remember - this is a secret. Almost no one else should know about this, but I need someone on the outside to do some quick and dirty research for me - I need a precident for this to work. Remember to stay safe. Oh, and sure - this isn't the most secure way to tell you all this, but don't worry, no one reads these anyways.";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override GameObject ItemModel => vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlQuillPickupReal.prefab");

        public override Sprite ItemIcon => vanillaVoidPlugin.MainAssets.LoadAsset<Sprite>("quillIcon512.png");


        public static GameObject ItemBodyModelPrefab;

        public override ItemTag[] ItemTags => new ItemTag[2] { ItemTag.Utility, ItemTag.AIBlacklist };

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            ItemDef.requiredExpansion = vanillaVoidPlugin.sotvDLC;
            VoidItemAPI.VoidTransformation.CreateTransformation(ItemDef, voidPair.Value);

            Hooks(); 
        }

        public override void CreateConfig(ConfigFile config)
        {
            dashVelocity = config.Bind<float>("Item: " + ItemName, "Dash Velocity", 21f, "Adjust how fast a player goes upon dashing.");
            shorthopVelocity = config.Bind<float>("Item: " + ItemName, "Shorthop Strength", .5f, "Adjust the strength of the shorthop upon dashing. This should be quite low or it'll basically just be better than Hopoo Feather.");
            dashesPerStack = config.Bind<int>("Item: " + ItemName, "Dash Count", 1, "Adjust how many dashes each stack gives.");
            voidPair = config.Bind<string>("Item: " + ItemName, "Item to Corrupt", "Feather", "Adjust which item this is the void pair of.");
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            
            ItemBodyModelPrefab = vanillaVoidPlugin.MainAssets.LoadAsset<GameObject>("mdlQuillSoloDisplay.prefab");
            //string orbTransp = "RoR2/DLC1/voidraid/matVoidRaidPlanetPurpleWave.mat"; 
            //string orbCore = "RoR2/DLC1/voidstage/matVoidCoralPlatformPurple.mat";

            //string orbTransp = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorLightning.mat";
            //string orbCore = "RoR2/DLC1/VoidSurvivor/matVoidSurvivorPod.mat";
            //
            //var adzeOrbsModelTransp = ItemModel.transform.Find("orbTransp").GetComponent<MeshRenderer>();
            //var adzeOrbsModelCore = ItemModel.transform.Find("orbCore").GetComponent<MeshRenderer>();
            //adzeOrbsModelTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            //adzeOrbsModelCore.material = Addressables.LoadAssetAsync<Material>(orbCore).WaitForCompletion();
            //
            //var adzeOrbsDisplayTransp = ItemBodyModelPrefab.transform.Find("orbTransp").GetComponent<MeshRenderer>();
            //var adzeOrbsDisplayCore = ItemBodyModelPrefab.transform.Find("orbCore").GetComponent<MeshRenderer>();
            //adzeOrbsDisplayTransp.material = Addressables.LoadAssetAsync<Material>(orbTransp).WaitForCompletion();
            //adzeOrbsDisplayCore.material = Addressables.LoadAssetAsync<Material>(orbCore).WaitForCompletion();

            var itemDisplay = ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            itemDisplay.rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);



            ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
            rules.Add("mdlCommandoDualies", new RoR2.ItemDisplayRule[]{
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.1353241f, 0.08783203f, 0.03012169f),
                    localAngles = new Vector3(307.3915f, 23.86982f, 217.4884f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.01994211f, 0.0001275018f, -0.1070882f),
                    localAngles = new Vector3(24.3717f, 268.774f, 111.6553f),
                    localScale = new Vector3(0.045f, 0.045f, 0.045f)
                }
            });
            rules.Add("mdlBandit2", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Hat",
                    localPos = new Vector3(-0.1191038f, 0.02249365f, 0.01268844f),
                    localAngles = new Vector3(3.652771f, 93.2954f, 250.8319f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-0.3592396f, 0.9387559f, 0.082003f),
                    localAngles = new Vector3(26.05843f, 3.707305f, 93.7645f),
                    localScale = new Vector3(0.5f, 0.5f, 0.5f)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.04101966f, 0.1495899f, 0.03951247f),
                    localAngles = new Vector3(19.68638f, 195.4905f, 116.7981f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlEngiTurret", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule //alt turret
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.01408422f, 0.8379388f, -1.267355f),
                    localAngles = new Vector3(13.6687f, 108.9054f, 322.5609f),
                    localScale = new Vector3(0.25f, 0.25f, 0.25f)

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
                    localPos = new Vector3(0.03000752f, 0.1098852f, -0.007914024f),
                    localAngles = new Vector3(345.2831f, 15.06405f, 259.0744f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }

            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.1353378f, 0.1148387f, 0.02999666f),
                    localAngles = new Vector3(18.60601f, 201.3888f, 107.481f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "FootFrontR",
                    localPos = new Vector3(0.1380639f, -0.04221976f, 0.00659878f),
                    localAngles = new Vector3(31.21218f, 185.5531f, 85.90443f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.02692784f, 0.1715988f, -0.07528451f),
                    localAngles = new Vector3(27.92293f, 233.8488f, 42.9845f),
                    localScale = new Vector3(0.06f, 0.06f, 0.06f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(-1.474291f, 0.4146849f, 1.184042f),
                    localAngles = new Vector3(9.905084f, 49.179f, 92.92774f),
                    localScale = new Vector3(0.55f, 0.55f, 0.55f)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.06930835f, 0.1518766f, -0.047157f),
                    localAngles = new Vector3(37.86217f, 251.961f, 115.654f),
                    localScale = new Vector3(0.0756f, 0.0756f, 0.0756f)
                }
            });
            rules.Add("mdlRailGunner", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Backpack",
                    localPos = new Vector3(-0.188871f, 0.3919613f, -0.07400557f),
                    localAngles = new Vector3(7.768537f, 292.3127f, 358.3477f),
                    localScale = new Vector3(0.05f, 0.05f, 0.05f)
                }
            });
            rules.Add("mdlVoidSurvivor", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighL",
                    localPos = new Vector3(-0.02819476f, 0.2904983f, -0.06415462f),
                    localAngles = new Vector3(10.21205f, 67.92503f, 195.5395f),
                    localScale = new Vector3(0.055f, 0.055f, 0.055f)
                }
            });
            rules.Add("mdlScav", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmL",
                    localPos = new Vector3(0.4837f, -0.8265978f, 2.033911f),
                    localAngles = new Vector3(17.15146f, 100.2759f, 119.889f),
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
                    childName =  "PauldronR",
                    localPos = new Vector3(0.5386145f, -0.2152944f, 0.5276036f),
                    localAngles = new Vector3(304.1717f, 100.8858f, 106.0395f),
                    localScale = new Vector3(0.12f, 0.12f, 0.12f)
                }
            });
            rules.Add("NemesisEnforcerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ElbowL",
                    localPos = new Vector3(0.002142102f, 0.005764513f, 0.00174125f),
                    localAngles = new Vector3(331.2649f, 1.659026f, 256.17f),
                    localScale = new Vector3(0.002f, 0.002f, 0.002f)
                }
            });
            rules.Add("mdlPaladin", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(0.04007983f, 0.2696055f, -0.1396203f),
                    localAngles = new Vector3(41.84266f, 32.5432f, 243.7065f),
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
                    childName = "ElbowL",
                    localPos = new Vector3(0.0003440298f, 0.001506613f, 0.00005643925f),
                    localAngles = new Vector3(53.94598f, 327.5067f, 235.5436f),
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
            //        localPos = new Vector3(0f, 0f, 0f),
            //        localAngles = new Vector3(0f, 0f, 0f),
            //        localScale = new Vector3(1f, 1f, 1f)
            //    }
            //});
            rules.Add("DancerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(0.00003724613f, 0.1874127f, -0.02244581f),
                    localAngles = new Vector3(46.24128f, 144.1943f, 328.1375f),
                    localScale = new Vector3(0.07f, 0.07f, 0.07f)
                }
            });
            rules.Add("JavangleMystBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Head",
                    localPos = new Vector3(-0.01266001f, 0.15735f, -0.1306189f),
                    localAngles = new Vector3(27.29047f, 60.5005f, 203.4501f),
                    localScale = new Vector3(0.075f, 0.075f, 0.075f)
                }
            });
            rules.Add("ExecutionerBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Pelvis",
                    localPos = new Vector3(-0.000860632f, 0.002425583f, -0.0005776987f),
                    localAngles = new Vector3(44.26307f, 350.5325f, 99.25278f),
                    localScale = new Vector3(0.00035f, 0.00035f, 0.00035f)
                }
            });
            rules.Add("NemmandoBody", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "ThighR",
                    localPos = new Vector3(-0.001144606f, 0.002062514f, -0.0005542804f),
                    localAngles = new Vector3(354.0225f, 169.6777f, 242.9034f),
                    localScale = new Vector3(0.00035f, 0.00035f, 0.00035f)
                }
            });

            return rules;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddAirdashToken;
        }

        private void AddAirdashToken(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (self.inventory)
            {
                int itemCount = self.inventory.GetItemCount(ItemBase<DashQuill>.instance.ItemDef);
                var token = self.gameObject.GetComponent<AirdashToken>();
                if (itemCount > 0)
                {
                    //var token = self.gameObject.GetComponent<AirdashToken>();
                    if (!token)
                    {
                        token = self.gameObject.AddComponent<AirdashToken>();
                        token.body = self;
                    }

                    token.dashMax = itemCount * dashesPerStack.Value; 
                    
                }else if (token)
                {
                    //Destroy(token.gameObject); it wont let me do this so uhhh
                    token.timeToDie = true;
                }
                
            }
        }

        public class AirdashToken : MonoBehaviour
        {
            public int dashMax;
            public int dashCurrent;
            int count = 0;
            int previousCount = 0;
            public float timer;
            public float lastJumpTime;
            public bool timeToDie;
            public CharacterBody body; //the player it's attached to

            void Awake()
            {
                timer = 0f;
                dashCurrent = dashMax;
                count = 0;
                timeToDie = false;
            }

            private void Update()
            {
                if (timeToDie)
                {
                    Destroy(this);
                }

                if (body.characterMotor.isGrounded)
                {
                    dashCurrent = dashMax;
                    count = 0;
                }
                
                if (body.inputBank.jump.justPressed && body.characterMotor.jumpCount == body.maxJumpCount && count >= body.maxJumpCount && dashCurrent != 0 && !body.HasBuff(RoR2Content.Buffs.Nullified) && !body.HasBuff(RoR2Content.Buffs.Entangle))
                {
                    Vector3 dir = body.inputBank.moveVector;
                    if(dir != Vector3.zero)
                    {
                        float dashVelo = ItemBase<DashQuill>.instance.dashVelocity.Value;
                        float vertStrength = ItemBase<DashQuill>.instance.shorthopVelocity.Value;

                        Quaternion quat = Quaternion.Euler(dir.x, dir.y, dir.z);
                        float num = body.acceleration * body.characterMotor.airControl;
                        float num2 = Mathf.Sqrt(dashVelo / num);
                        float num3 = body.moveSpeed / num;
                        float horizontalBonus = (num2 + num3) / num3;

                        GenericCharacterMain.ApplyJumpVelocity(body.characterMotor, body, horizontalBonus, vertStrength, false);

                        string effectName = "RoR2/DLC1/MoveSpeedOnKill/MoveSpeedOnKillActivate.prefab";
                        GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>(effectName).WaitForCompletion();
                        string effect2 = "RoR2/DLC1/VoidSuppressor/SuppressorDieEffect.prefab";
                        GameObject effect2Prefab = Addressables.LoadAssetAsync<GameObject>(effect2).WaitForCompletion();
                        Vector3 newScale = new Vector3(.5f, .5f, .5f);
                        effect2Prefab.transform.localScale = newScale;

                        EffectManager.SimpleEffect(effect2Prefab, body.transform.position, quat, true);
                        EffectManager.SimpleImpactEffect(effectPrefab, body.transform.position, dir, true);

                        dashCurrent--;

                    }

                }
                else if(body.inputBank.jump.justPressed)
                {
                    count++;
                }
            }

            public void detonateToken()
            {
                Destroy(this);
            }
        }

    }

}
