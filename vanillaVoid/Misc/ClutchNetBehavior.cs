using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using vanillaVoid.Items;

namespace vanillaVoid.Misc
{
    public class ClutchNetBehavior : NetworkBehaviour //note for self; not entirely needed to be separate, weaver just needs it to be top level
    {
        public int jumpMax;
        //public int jumpCurrent;
        int count = 0;
        int previousCount = 0;
        public float timer;
        public float timer2;
        public float lastJumpTime;
        public CharacterBody body; //the player it's attached to

        public static GameObject effect2;
        public HurtBox? nearest;
        public List<HurtBox> jumpBoxList;

        public bool activateInDelegate = false;

        //[SyncVar]
        public bool jumpInput;

        [SyncVar]
        public bool clutchValid;

        [SyncVar]
        public bool jumpOverride = false;

        public int jumpCurrent;

        public NetworkIdentity networkID;
        public bool alreadySetNetID = false;

        [Command]
        public void CmdSetClutchValid(bool value)
        {
            clutchValid = value;
        }

        public void StupidIntermediate()
        {
           // Debug.Log("has auth (intermediate): " + hasAuthority); //no authority here. why? client is calling a function on their own attachment. which has LocalPlayerAuthority

            ClientClutchEffects();

            if (hasAuthority)
            {
                CmdDoClutch2();
            }
        }

        [Command]
        public void CmdDoClutch2()
        {
            //Debug.Log("COMMAND Clutch2");
            ServerClutchEffects();
        }

        public bool canEnemyJump;

        public bool TestEnemyJump()
        {
            SphereSearch jumpSearch = new SphereSearch();
            jumpBoxList = new List<HurtBox>();

            jumpSearch.origin = body.transform.position;
            jumpSearch.mask = LayerIndex.entityPrecise.mask;
            jumpSearch.radius = body.radius + VoidFin.kickRange.Value;
            jumpSearch.RefreshCandidates();
            jumpSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(body.teamComponent.teamIndex));
            jumpSearch.FilterCandidatesByDistinctHurtBoxEntities();
            jumpSearch.OrderCandidatesByDistance();
            jumpSearch.GetHurtBoxes(jumpBoxList);
            jumpSearch.ClearCandidates();

            if (jumpBoxList.Count > 0)
            {
                nearest = jumpBoxList[0];
                return true;
            }

            return false;
        }

        void Awake()
        {
            try
            {
                body = gameObject.transform.parent.GetComponent<CharacterBody>();
                int stack = body.inventory.GetItemCountEffective(VoidFin.instance?.ItemDef);
                jumpMax = VoidFin.baseKicks.Value + (VoidFin.stackingKicks.Value * (stack - 1));
            }
            catch (Exception e){ }

            timer = 0f;
            //if (hasAuthority)
            //{
            //    CmdSetClutchJumpCurrent(jumpMax);
            //}
            jumpCurrent = jumpMax;
            count = 0;
        }
        
        private void OnEnable()
        {
            //Debug.Log("AWWAA");
            //body.onJump = (CharacterBody.JumpDelegate)Delegate.Combine(body.onJump, new CharacterBody.JumpDelegate(this.AttemptClutch));
        }

        private void OnDisable()
        {
            //Debug.Log("AWEAWEWE@");
            //body.onJump = (CharacterBody.JumpDelegate)Delegate.Remove(body.onJump, new CharacterBody.JumpDelegate(this.AttemptClutch));
        }

        public void AttemptClutch()
        {
            //FastDebug.Log("1 " + hasAuthority);
            if (body.inputBank.jump.justPressed)
            {
                //FastDebug.Log("PLEASE " + hasAuthority);
            }
        }

        void TryClutchBehavior(On.EntityStates.GenericCharacterMain.orig_ProcessJump orig, GenericCharacterMain self)
        {
            bool eatInput = false;
            if (body == self.characterBody && self.hasCharacterMotor)//&& VoidFin.allowJumpOverrides.Value)
            {
                bool flag3 = self.characterMotor.jumpCount < self.characterBody.maxJumpCount; //could this be a normal jump

                if (self.characterBody && flag3)
                {
                    int clutchCount = self.characterBody.inventory.GetItemCountEffective(VoidFin.instance.ItemDef);
                    if (clutchCount > 0)
                    {
                        Debug.Log("clutchCount: " + clutchCount + " | " + this);
                        //if (NetworkServer.active && behv && behv.token && behv.token.alreadySetNetID)
                        //{
                        //    var token = behv.token;
                        //    if (token.networkID == null)
                        //    {
                        //        token.networkID = token.gameObject.GetComponent<NetworkIdentity>();
                        //    }
                        //    token.networkID.RemoveClientAuthority(token.networkID.connectionToClient);
                        //    token.networkID.AssignClientAuthority(self.characterBody.netIdentity.connectionToClient);
                        //    token.alreadySetNetID = true;
                        //}
                        if (this && self.characterMotor.jumpCount != 0 && self.jumpInputReceived)
                        {

                            bool other = self.inputBank.jump.justPressed && this.jumpCurrent > 0 && self.characterBody.moveSpeed != 0 && this.canEnemyJump;
                            Debug.Log("other : " + other + " just pressed: " + self.inputBank.jump.justPressed + " | (> 0?) jumpcurrent: " + this.jumpCurrent + " | moveSpeed: " + self.characterBody.moveSpeed + " | canEnemyJump: " + this.canEnemyJump);

                            //if (self.isAuthority && self.localPlayerAuthority){
                            //    //behv.token.CmdSetClutchOverrideCalc();
                            //    other = self.inputBank.jump.justPressed && behv.token.jumpCurrent > 0 && self.characterBody.moveSpeed != 0 && behv.token.canEnemyJump;
                            //    Debug.Log("other : " + other + " just pressed: " + self.inputBank.jump.justPressed + " | (> 0?) jumpcurrent: " + behv.token.jumpCurrent + " | moveSpeed: " + self.characterBody.moveSpeed + " | canEnemyJump: " + behv.token.canEnemyJump);
                            //} 
                            //
                            if (this.jumpOverride || other)
                            {
                                eatInput = true;                          //false                              //true                        //true                              //false, for client intending to use this item
                                Debug.Log("eat : " + eatInput + " | " + this.hasAuthority + " | " + self.isAuthority + " | " + self.localPlayerAuthority + " | " + NetworkServer.active);

                                Debug.Log("calling stupid intermediate");
                                this.StupidIntermediate();
                                


                                //if (!NetworkServer.active){
                                //    behv.token.ActivateClutch();
                                //}
                            }
                            Debug.Log("eatInput " + eatInput + " | behv.token.jumpOverride: " + this.jumpOverride + " | " + self.isAuthority + " | " + self.localPlayerAuthority + " | ");
                        }
                    }
                }
            }
            if (!eatInput)
            {
                orig(self);
            }

        }


        private void FixedUpdate()
        {
            if (!body)
            {
                body = gameObject.transform.parent.GetComponent<CharacterBody>();
                if (!body)
                {
                    return;
                }
            }
            if (!body.characterMotor)
            {
                return;
            }

            if (body.characterMotor.isGrounded)
            {
                int stack = body.inventory.GetItemCountEffective(VoidFin.instance?.ItemDef);
                jumpMax = VoidFin.baseKicks.Value + (VoidFin.stackingKicks.Value * (stack - 1));

                jumpCurrent = jumpMax;
                count = 0;
                timer = 0;
            }

            canEnemyJump = TestEnemyJump();

            if (body.hasEffectiveAuthority && hasAuthority)
            {
                jumpInput = body.inputBank.jump.justPressed;
                CmdSetClutchValid(body.inputBank.jump.justPressed && body.characterMotor.jumpCount == body.maxJumpCount && count >= body.maxJumpCount && jumpCurrent > 0 && body.moveSpeed != 0 && canEnemyJump && body.characterMotor.jumpCount != 0);
            }

            if (!jumpInput)
            {
                if (body.characterMotor.jumpCount != previousCount)
                {
                    count++;
                    previousCount = body.characterMotor.jumpCount;
                }
            }
            
            timer -= Time.fixedDeltaTime;
            if(timer <= 0 && clutchValid)
            {
                timer = .15f; //.1375f;
                Vector3 dir = body.inputBank.moveVector;
                float vertStrength = .2075f; //.225f
                if (dir == Vector3.zero)
                {
                    dir.y = .5f;
                    vertStrength += 0.0925f;
                }

                if (dir != Vector3.zero) //checck this
                {
                    //float dashVelo = 21;
                     //.225f
                    float num = body.acceleration * body.characterMotor.airControl;
                    float num2 = Mathf.Sqrt(vertStrength / num);
                    float num3 = body.moveSpeed / num;
                    float jumpStrength = (num2 + num3) / num3;

                    GenericCharacterMain.ApplyJumpVelocity(body.characterMotor, body, 2f, jumpStrength, false);

                    if (body.modelLocator && body.modelLocator.modelTransform)
                    {
                        var anim = body.modelLocator.modelTransform.GetComponent<Animator>();
                        if (anim)
                        {
                            int layerIndex = anim.GetLayerIndex("Body");
                            if (layerIndex >= 0)
                            {
                                anim.CrossFadeInFixedTime("Jump", .05f, layerIndex);
                            }
                        }
                    }

                    var mult = VoidFin.baseDamage.Value + (VoidFin.stackingDamage.Value * (jumpMax - 1));
                    var multAOE = VoidFin.baseAOEDamage.Value + (VoidFin.stackingAOEDamage.Value * (jumpMax - 1));

                    DamageInfo damageInfo = new DamageInfo
                    {
                        attacker = body.gameObject,
                        crit = body.RollCrit(),
                        damage = body.damage * mult,
                        position = body.transform.position,
                        procCoefficient = 1,
                        damageType = DamageType.Generic,
                        damageColorIndex = DamageColorIndex.Item,
                        force = Vector3.down * 25
                    };

                    var nearbody = nearest.healthComponent.body;
                    var massnear = 1f;
                    if (nearbody.characterMotor)
                    {
                        massnear = nearest.healthComponent.body.characterMotor.mass;
                    }

                    var dirmodified = dir;
                    dirmodified.y = .25f;
                    int polarity = VoidFin.invertForce.Value ? -1 : 1; //28
                    float force = 20 / (1 + (1.5f * nearbody.GetBuffCount(VoidFin.hiddenClutchResist)));

                    Vector3 sourcePosition;
                    if(nearbody.transform != null && nearbody.transform.position != null)
                    {
                        sourcePosition = nearbody.transform.position;
                    }
                    else
                    {
                        sourcePosition = body.corePosition;
                    }

                    EffectData jumpVFXdata = new EffectData
                    {
                        origin = sourcePosition,
                        rotation = Util.QuaternionSafeLookRotation(dirmodified.normalized * polarity)
                    };
                    EffectManager.SpawnEffect(VoidFin.jumpVFX, jumpVFXdata, true);

                    if (NetworkServer.active)
                    {
                        nearest.healthComponent.TakeDamage(damageInfo);
                        nearest.healthComponent.TakeDamageForce((dirmodified * polarity) * force * Mathf.Pow(massnear, .9675f), true, true);
                        nearbody.AddTimedBuffAuthority(VoidFin.hiddenClutchResist.buffIndex, 1f);

                    }

                    SphereSearch jumpSearch = new SphereSearch();
                    var jumpBoxListLarger = new List<HurtBox>();
                    jumpSearch.origin = body.corePosition;
                    jumpSearch.mask = LayerIndex.entityPrecise.mask;
                    jumpSearch.radius = body.radius + 6.5f;
                    jumpSearch.RefreshCandidates();
                    jumpSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(body.teamComponent.teamIndex));
                    jumpSearch.FilterCandidatesByDistinctHurtBoxEntities();
                    jumpSearch.OrderCandidatesByDistance();
                    jumpSearch.GetHurtBoxes(jumpBoxListLarger);
                    jumpSearch.ClearCandidates();

                    if (jumpBoxListLarger.Count > 1)
                    {
                        for (int i = 0; i < jumpBoxListLarger.Count; ++i)
                        {
                            var hc = jumpBoxListLarger[i].healthComponent;
                            if (hc == nearest)
                            {
                                continue;
                            }

                            if (multAOE > 0)
                            {
                                DamageInfo damageInfoTemp = new DamageInfo
                                {
                                    attacker = body.gameObject,
                                    crit = body.RollCrit(),
                                    damage = body.damage * multAOE,
                                    position = hc.body.transform.position,
                                    procCoefficient = .25f,
                                    damageType = DamageType.Generic,
                                    damageColorIndex = DamageColorIndex.Item,
                                };
                                if (NetworkServer.active)
                                {
                                    hc.TakeDamage(damageInfoTemp);
                                }
                            }

                            var mass = 1f;
                            if (hc.body.characterMotor)
                            {
                                mass = hc.body.characterMotor.mass;
                            }
                            Vector3 aoeDir = (hc.body.transform.position - body.transform.position);
                            aoeDir.y += .1f;
                            if (NetworkServer.active)
                            {
                                hc.TakeDamageForce(aoeDir.normalized * Mathf.Sqrt(mass) * 200, true, true);
                            }
                        }
                    }

                    if (nearest.healthComponent.body.characterMotor && nearest.healthComponent.body.characterMotor.isGrounded)
                    {
                        var model = nearest.healthComponent.body.modelLocator.modelTransform;
                        var token = model.gameObject.GetComponent<SquashedComponent>();
                        if (token)
                        {
                            model.transform.localScale = token.originalScale;
                            Destroy(token);
                        }
                        model.gameObject.AddComponent<SquashedComponent>();
                    }
                    jumpCurrent--;
                }
            }
            else if (body.inputBank.jump.justPressed)
            {
                count++;
            }

            previousCount = body.characterMotor.jumpCount;
        }


        public void ClientClutchEffects()
        {
            Debug.Log("ClutchClientEffects");
            Vector3 dir = body.inputBank.moveVector;
            float vertStrength = .2075f; //.225f
            if (dir == Vector3.zero)
            {
                dir.y += .2f;
                vertStrength += 0.0925f;
            }

            if (dir != Vector3.zero) //checck this
            {
                //float dashVelo = 21;
                //.225f

                float num = body.acceleration * body.characterMotor.airControl;
                float num2 = Mathf.Sqrt(vertStrength / num);
                float num3 = body.moveSpeed / num;
                float jumpStrength = (num2 + num3) / num3;

                GenericCharacterMain.ApplyJumpVelocity(body.characterMotor, body, 2f, jumpStrength, false);

                if (body.modelLocator && body.modelLocator.modelTransform)
                {
                    var anim = body.modelLocator.modelTransform.GetComponent<Animator>();
                    if (anim)
                    {
                        int layerIndex = anim.GetLayerIndex("Body");
                        if (layerIndex >= 0)
                        {
                            anim.CrossFadeInFixedTime("Jump", .05f, layerIndex);
                        }
                    }
                }

                var nearbody = nearest.healthComponent.body;
                var massnear = 1f;
                if (nearbody.characterMotor)
                {
                    massnear = nearest.healthComponent.body.characterMotor.mass;
                }

                var dirmodified = dir;
                dirmodified.y += .2f;
                int polarity = VoidFin.invertForce.Value ? -1 : 1; //28

                //EffectData jumpVFXdata = new EffectData
                //{
                //    origin = nearbody.corePosition,
                //    rotation = Util.QuaternionSafeLookRotation(dirmodified.normalized * polarity),
                //    forceUnpooled = true
                //
                //};
                //EffectManager.SpawnEffect(VoidFin.jumpVFX, jumpVFXdata, true);

                if (nearest.healthComponent.body.characterMotor && nearest.healthComponent.body.characterMotor.isGrounded)
                {
                    var model = nearest.healthComponent.body.modelLocator.modelTransform;
                    var token = model.gameObject.GetComponent<SquashedComponent>();
                    if (token)
                    {
                        model.transform.localScale = token.originalScale;
                        Destroy(token);
                    }
                    model.gameObject.AddComponent<SquashedComponent>();
                }
            }
        }

        public void ActivateClutch()
        {
            Debug.Log("ActivateClutch");
            timer = .1375f;
            //Debug.Log("attempting to kick");
            Vector3 dir = body.inputBank.moveVector;
            float vertStrength = .2075f; //.225f
            if (dir == Vector3.zero)
            {
                dir.y += .2f;
                vertStrength += 0.0925f;
            }

            if (dir != Vector3.zero) //checck this
            {
                //float dashVelo = 21;
                //.225f

                float num = body.acceleration * body.characterMotor.airControl;
                float num2 = Mathf.Sqrt(vertStrength / num);
                float num3 = body.moveSpeed / num;
                float jumpStrength = (num2 + num3) / num3;

                GenericCharacterMain.ApplyJumpVelocity(body.characterMotor, body, 2f, jumpStrength, false);

                if (body.modelLocator && body.modelLocator.modelTransform)
                {
                    var anim = body.modelLocator.modelTransform.GetComponent<Animator>();
                    if (anim)
                    {
                        int layerIndex = anim.GetLayerIndex("Body");
                        if (layerIndex >= 0)
                        {
                            anim.CrossFadeInFixedTime("Jump", .05f, layerIndex);
                        }
                    }
                }

                var mult = VoidFin.baseDamage.Value + (VoidFin.stackingDamage.Value * (jumpMax - 1));
                var multAOE = VoidFin.baseAOEDamage.Value + (VoidFin.stackingAOEDamage.Value * (jumpMax - 1));

                DamageInfo damageInfo = new DamageInfo
                {
                    attacker = body.gameObject,
                    crit = body.RollCrit(),
                    damage = body.damage * mult,
                    position = body.transform.position,
                    procCoefficient = 1,
                    damageType = DamageType.Generic,
                    damageColorIndex = DamageColorIndex.Item,
                    force = Vector3.down * 25
                };

                nearest.healthComponent.TakeDamage(damageInfo);

                var nearbody = nearest.healthComponent.body;
                var massnear = 1f;
                if (nearbody.characterMotor)
                {
                    massnear = nearest.healthComponent.body.characterMotor.mass;
                }

                var dirmodified = dir;
                dirmodified.y += .2f;
                int polarity = VoidFin.invertForce.Value ? -1 : 1; //28
                Debug.Log("Activate Clutch");
                nearest.healthComponent.TakeDamageForce((dirmodified * polarity) * 20 * Mathf.Pow(massnear, .9675f), true, true);
                

                EffectData jumpVFXdata = new EffectData
                {
                    origin = nearbody.corePosition,
                    rotation = Util.QuaternionSafeLookRotation(dirmodified.normalized * polarity),
                    forceUnpooled = true

                };
                EffectManager.SpawnEffect(VoidFin.jumpVFX, jumpVFXdata, true);

                SphereSearch jumpSearch = new SphereSearch();
                var jumpBoxListLarger = new List<HurtBox>();
                jumpSearch.origin = body.transform.position;
                jumpSearch.mask = LayerIndex.entityPrecise.mask;
                jumpSearch.radius = body.radius + 6.5f;
                jumpSearch.RefreshCandidates();
                jumpSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(body.teamComponent.teamIndex));
                jumpSearch.FilterCandidatesByDistinctHurtBoxEntities();
                jumpSearch.OrderCandidatesByDistance();
                jumpSearch.GetHurtBoxes(jumpBoxListLarger);
                jumpSearch.ClearCandidates();

                if (jumpBoxListLarger.Count > 1)
                {
                    for (int i = 0; i < jumpBoxListLarger.Count; ++i)
                    {
                        var hc = jumpBoxListLarger[i].healthComponent;
                        if (hc == nearest)
                        {
                            continue;
                        }

                        if (multAOE > 0)
                        {
                            DamageInfo damageInfoTemp = new DamageInfo
                            {
                                attacker = body.gameObject,
                                crit = body.RollCrit(),
                                damage = body.damage * multAOE,
                                position = hc.body.corePosition,
                                procCoefficient = .25f,
                                damageType = DamageType.Generic,
                                damageColorIndex = DamageColorIndex.Item,
                            };
   
                            hc.TakeDamage(damageInfoTemp);
                        }

                        var mass = 1f;
                        if (hc.body.characterMotor)
                        {
                            mass = hc.body.characterMotor.mass;
                        }
                        Vector3 aoeDir = (hc.body.corePosition - body.corePosition);
                        aoeDir.y += .1f;

                        hc.TakeDamageForce(aoeDir.normalized * Mathf.Sqrt(mass) * 200, true, true);

                    }
                }

                if (nearest.healthComponent.body.characterMotor && nearest.healthComponent.body.characterMotor.isGrounded)
                {
                    var model = nearest.healthComponent.body.modelLocator.modelTransform;
                    var token = model.gameObject.GetComponent<SquashedComponent>();
                    if (token)
                    {
                        model.transform.localScale = token.originalScale;
                        Destroy(token);
                    }
                    model.gameObject.AddComponent<SquashedComponent>();
                }

                jumpCurrent--;
            }
        }

        public void ServerClutchEffects() //make voidfin call this
        {
            //Debug.Log("ServerClutchEffects");
            timer = .1375f;
            //Debug.Log("attempting to kick");
            Vector3 dir = body.inputBank.moveVector;
            float vertStrength = .2075f; //.225f
            if (dir == Vector3.zero)
            {
                dir.y += .2f;
                vertStrength += 0.0925f;
            }

            if (dir != Vector3.zero) //checck this
            {
                //float dashVelo = 21;
                //.225f

                if (body.modelLocator && body.modelLocator.modelTransform)
                {
                    var anim = body.modelLocator.modelTransform.GetComponent<Animator>();
                    if (anim)
                    {
                        int layerIndex = anim.GetLayerIndex("Body");
                        if (layerIndex >= 0)
                        {
                            anim.CrossFadeInFixedTime("Jump", .05f, layerIndex);
                        }
                    }
                }

                var mult = VoidFin.baseDamage.Value + (VoidFin.stackingDamage.Value * (jumpMax - 1));
                var multAOE = VoidFin.baseAOEDamage.Value + (VoidFin.stackingAOEDamage.Value * (jumpMax - 1));

                DamageInfo damageInfo = new DamageInfo
                {
                    attacker = body.gameObject,
                    crit = body.RollCrit(),
                    damage = body.damage * mult,
                    position = body.transform.position,
                    procCoefficient = 1,
                    damageType = DamageType.Generic,
                    damageColorIndex = DamageColorIndex.Item,
                    force = Vector3.down * 25
                };

                nearest.healthComponent.TakeDamage(damageInfo);

                var nearbody = nearest.healthComponent.body;
                var massnear = 1f;
                if (nearbody.characterMotor)
                {
                    massnear = nearest.healthComponent.body.characterMotor.mass;
                }

                var dirmodified = dir;
                dirmodified.y += .2f;
                int polarity = VoidFin.invertForce.Value ? -1 : 1; //28
                //Debug.Log("Server Clutch Effects");
                nearest.healthComponent.TakeDamageForce((dirmodified * polarity) * 20 * Mathf.Pow(massnear, .9675f), true, true);


                EffectData jumpVFXdata = new EffectData
                {
                    origin = nearbody.corePosition,
                    rotation = Util.QuaternionSafeLookRotation(dirmodified.normalized * polarity),
                    forceUnpooled = true

                };
                EffectManager.SpawnEffect(VoidFin.jumpVFX, jumpVFXdata, true);

                SphereSearch jumpSearch = new SphereSearch();
                var jumpBoxListLarger = new List<HurtBox>();
                jumpSearch.origin = body.transform.position;
                jumpSearch.mask = LayerIndex.entityPrecise.mask;
                jumpSearch.radius = body.radius + 6.5f;
                jumpSearch.RefreshCandidates();
                jumpSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(body.teamComponent.teamIndex));
                jumpSearch.FilterCandidatesByDistinctHurtBoxEntities();
                jumpSearch.OrderCandidatesByDistance();
                jumpSearch.GetHurtBoxes(jumpBoxListLarger);
                jumpSearch.ClearCandidates();

                if (jumpBoxListLarger.Count > 1)
                {
                    for (int i = 0; i < jumpBoxListLarger.Count; ++i)
                    {
                        var hc = jumpBoxListLarger[i].healthComponent;
                        if (hc == nearest)
                        {
                            continue;
                        }

                        if (multAOE > 0)
                        {
                            DamageInfo damageInfoTemp = new DamageInfo
                            {
                                attacker = body.gameObject,
                                crit = body.RollCrit(),
                                damage = body.damage * multAOE,
                                position = hc.body.corePosition,
                                procCoefficient = .25f,
                                damageType = DamageType.Generic,
                                damageColorIndex = DamageColorIndex.Item,
                            };

                            hc.TakeDamage(damageInfoTemp);
                            //hc.netId
                        }

                        var mass = 1f;
                        if (hc.body.characterMotor)
                        {
                            mass = hc.body.characterMotor.mass;
                        }
                        Vector3 aoeDir = (hc.body.corePosition - body.corePosition);
                        aoeDir.y += .1f;

                        hc.TakeDamageForce(aoeDir.normalized * Mathf.Sqrt(mass) * 200, true, true);

                    }
                }

                if (nearest.healthComponent.body.characterMotor && nearest.healthComponent.body.characterMotor.isGrounded)
                {
                    var model = nearest.healthComponent.body.modelLocator.modelTransform;
                    var token = model.gameObject.GetComponent<SquashedComponent>();
                    if (token)
                    {
                        model.transform.localScale = token.originalScale;
                        Destroy(token);
                    }
                    model.gameObject.AddComponent<SquashedComponent>();
                }
                jumpCurrent--;
            }
        }

    }
}
