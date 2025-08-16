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
        public int jumpCurrent;
        int count = 0;
        int previousCount = 0;
        public float timer;
        public float timer2;
        public float lastJumpTime;
        public CharacterBody body; //the player it's attached to

        public static GameObject effect2;
        public HurtBox? nearest;
        public List<HurtBox> jumpBoxList;

        [SyncVar]
        public bool jumpInput;

        [SyncVar]
        public bool clutchValid;

        [Command]
        public void CmdSetJumpInput(bool value)
        {
            if (value)
            {
                Debug.Log("cmdsetjumpinput : " + jumpInput);
            }
            jumpInput = value;
        }

        [Command]
        public void CmdSetClutchValid(bool value)
        {
            if (value)
            {
                Debug.Log("test : " + timer + " | " + jumpInput + " | " + (body.characterMotor.jumpCount == body.maxJumpCount) + " | " + (count >= body.maxJumpCount) + " | " + (jumpCurrent != 0) + " | " + (body.moveSpeed != 0) + " | " + canEnemyJump);
            }
            clutchValid = value;
            Debug.Log("CLUTCH VALID");
        }
        //[Command]
        //public void CmdClutchDamageTarget(HealthComponent hc, DamageInfo info)
        //{
        //    hc.TakeDamage(info);
        //}
        //
        //[Command]
        //public void CmdClutchDamageTargetForce(HealthComponent hc, Vector3 dir)
        //{
        //    hc.TakeDamageForce(dir, true, true);
        //}

        public bool canEnemyJump;

        //{
        //    get
        //    {
        //        return TestEnemyJump();
        //    }
        //    set
        //}

        public bool TestEnemyJump()
        {
            //Debug.Log("testing");
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
                int stack = body.inventory.GetItemCount(VoidFin.instance?.ItemDef);
                jumpMax = VoidFin.baseKicks.Value + (VoidFin.stackingKicks.Value * (stack - 1));

            }
            catch (Exception e)
            {

            }

            timer = 0f;
            jumpCurrent = jumpMax;
            count = 0;
            Debug.Log("bugthlhlgh");
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
                int stack = body.inventory.GetItemCount(VoidFin.instance?.ItemDef);
                jumpMax = VoidFin.baseKicks.Value + (VoidFin.stackingKicks.Value * (stack - 1));

                jumpCurrent = jumpMax;
                count = 0;
            }

            canEnemyJump = TestEnemyJump();

            if (body.hasEffectiveAuthority)
            {
                CmdSetJumpInput(body.inputBank.jump.justPressed);
                CmdSetClutchValid(jumpInput && body.characterMotor.jumpCount == body.maxJumpCount && count >= body.maxJumpCount && jumpCurrent != 0 && body.moveSpeed != 0 && canEnemyJump);
                //canEnemyJump = TestEnemyJump();
            }

            if (!jumpInput)
            {
                if (body.characterMotor.jumpCount != previousCount)
                {
                    count++;
                    previousCount = body.characterMotor.jumpCount;
                }
            }
            timer2 -= Time.fixedDeltaTime;
            if (timer2 < 0)
            {
                timer2 = 1;
                Debug.Log("jumpcount: " + body.characterMotor.jumpCount + " | jump input: " + jumpInput + " | canenemyjump" + canEnemyJump + " | " + body.maxJumpCount + " | " + jumpCurrent + " | " + jumpMax); //count >= body.maxJumpCount
            }
            timer -= Time.fixedDeltaTime;
            //Debug.Log("jumpcount: " + body.characterMotor.jumpCount + " | " + canEnemyJump + " | " + body.maxJumpCount + " | " + jumpCurrent + " | " + jumpMax); //count >= body.maxJumpCount
            if (timer <= 0 && jumpInput && body.characterMotor.jumpCount == body.maxJumpCount && count >= body.maxJumpCount && jumpCurrent != 0 && body.moveSpeed != 0 && canEnemyJump)
            {
                timer = .1375f;
                Debug.Log("attempting to kick");
                Vector3 dir = body.inputBank.moveVector;
                if (dir != Vector3.zero)
                {
                    //float dashVelo = 21;
                    float vertStrength = .2075f; //.225f

                    //Quaternion quat = Quaternion.Euler(dir.x, dir.y, dir.z);
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
                    dirmodified.y = .25f;
                    int polarity = VoidFin.invertForce.Value ? -1 : 1;
                    nearest.healthComponent.TakeDamageForce((dirmodified * polarity) * 28f * Mathf.Pow(massnear, .975f), true, true);

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
                                    damage = body.damage * multAOE, // make it stack laqter i dont want to 
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
                                //if (hc.body.characterMotor.Motor)
                                //{
                                //    hc.body.characterMotor.Motor.ForceUnground(0.075f);
                                //}
                            }
                            hc.TakeDamageForce((hc.body.corePosition - body.corePosition).normalized * Mathf.Sqrt(mass) * 225, true, true);
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
    }
}
