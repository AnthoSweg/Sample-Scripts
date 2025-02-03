using Cysharp.Threading.Tasks;
using DG.Tweening;
using Masto.Character;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Masto.Skill
{
    public class SkillInstance
    {
        public int id;
        public string trigger;

        List<SkillCollider> hitBoxes;

        float cooldown;
        float cooldownDuration;
        bool cooldownPending;

        int chargesLeft;
        public int ChargesLeft => chargesLeft;

        //int additionalCharge;
        public int MaxCharge => entity.Charges + (id == 1 ? GameManager.Instance.FightManager.additionalDashCharge : 0);

        float scaleMultiplier;
        float cooldownReductionMultiplier;

        CancellationTokenSource cancelToken;

        SkillEntity entity;
        public SkillEntity Entity => entity;
        public SkillInstance(SkillEntity _entity)
        {
            entity = _entity;
            chargesLeft = MaxCharge;
            UpdateCooldownDuration();

            GenerateHitboxes();
        }

        private void GenerateHitboxes()
        {
            hitBoxes = new List<SkillCollider>();
            for (int i = 0; i < entity.SubSkills.Length; i++)
            {
                SkillCollider hitbox = GameObject.Instantiate(entity.SubSkills[i].Collider);
                hitBoxes.Add(hitbox);
            }
            for (int i = 0; i < Entity.Conditions.Length; i++)
            {
                Entity.Conditions[i].Start();
            }
        }

        public void SelectSkill(Unit caster)
        {
            for (int i = 0; i < hitBoxes.Count; i++)
            {
                hitBoxes[i].multiplier = scaleMultiplier;
                hitBoxes[i].transform.localScale = Vector3.one * scaleMultiplier;
            }
        }

        public void DeselectSkill()
        {
        }

        public void PreviewSkill(Unit caster, Vector2 playerMovement)
        {
            var mousePos = CameraManager.Instance.GetMouseGroundPos();
            for (int i = 0; i < entity.SubSkills.Length; i++)
            {
                Vector3 newPos;
                //Position the hitbox
                switch (entity.SubSkills[i].FollowType)
                {
                    default:
                    case EFollowType.caster:
                        newPos = caster.t.position;
                        break;

                    case EFollowType.mouse:
                        if (Vector2.Distance(mousePos, caster.t.position) > entity.SubSkills[i].SkillMaxRange)
                        {
                            newPos = caster.t.position + ((mousePos - caster.t.position).normalized * entity.SubSkills[i].SkillMaxRange);
                        }
                        else
                        {
                            newPos = mousePos;
                        }
                        break;

                    case EFollowType.mouseFixedRange:
                        newPos = caster.t.position + ((mousePos - caster.t.position).normalized * entity.SubSkills[i].SkillMaxRange);
                        break;
                    case EFollowType.towardsMovementFixedRange:
                        newPos = caster.t.position + ((Vector3)playerMovement.normalized * entity.SubSkills[i].SkillMaxRange);
                        break;
                }
                hitBoxes[i].transform.position = newPos;

                //Rotate the hitbox
                if (entity.SubSkills[i].RotationType == ERotationType.TowardsMovement)
                    RotateHitbox(hitBoxes[i], caster, caster.t.position + (Vector3)playerMovement, entity.SubSkills[i].RotationType);
                else
                    RotateHitbox(hitBoxes[i], caster, mousePos, entity.SubSkills[i].RotationType);
            }
        }

        public async void UseSkill(Unit caster)
        {
            while (cancelToken != null)
                await UniTask.Yield();
            cancelToken = new CancellationTokenSource();

            List<SubSkillInstance> ssInstances = new List<SubSkillInstance>();
            for (int i = 0; i < entity.SubSkills.Length; i++)
            {
                ssInstances.Add(new SubSkillInstance(entity.SubSkills[i]));

                if (entity.SubSkills[i].AttachHitboxToCaster)
                    hitBoxes[i].transform.SetParent(caster.t);
            }

            if (entity.StillWhileCasting) caster.stillFromCasting = true;
            if (entity.BlockLookingSide) caster.blockLookingSide = true;

            //Only first skill aka auto attack have this charge/release behaviour
            if (id == 0)
            {
                //Start telegraph loading
                for (int i = 0; i < ssInstances.Count; i++)
                {
                    ssInstances[i].ShowTelegraph(caster, hitBoxes[i], entity.ChargingDuration, cancelToken);
                }

                //Wait for it to be over
                await UniTask.Delay(Mathf.RoundToInt(entity.ChargingDuration * 1000), false, PlayerLoopTiming.Update, cancelToken.Token);
                if (!Application.isPlaying || caster.Health.isDead) return;

                if (cancelToken.IsCancellationRequested)
                {
                    cancelToken = null;
                    return;
                }
                caster.Animate("release");
                caster.stillFromCasting = false;
            }
            //In case we want to freeze the unit when casting a skill or a dash
            //This does not affect animation timing with VFX and effects
            else if (entity.ChargingDuration > 0)
            {
                WaitAndReenableMovement(entity.ChargingDuration, caster, cancelToken);
            }

            for (int i = 0; i < ssInstances.Count; i++)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    cancelToken = null;
                    return;
                }
                if (hitBoxes[i] == null)
                {
                    Debug.LogError("Can't cast sub skill, hitbox is null");
                }
                else
                    ssInstances[i].CastSubSkill(caster, hitBoxes[i], cancelToken);
            }
            cancelToken = null;
        }
        async void WaitAndReenableMovement(float duration, Unit caster, CancellationTokenSource token)
        {
            await UniTask.Delay(Mathf.RoundToInt(duration * 1000), false, PlayerLoopTiming.Update, token.Token);

            if (!Application.isPlaying || caster.Health.isDead) return;
            caster.stillFromCasting = false;
        }

        public bool AITryCastSkill(Unit caster, float range)
        {
            Damageable target = null;
            for (int i = 0; i < entity.SubSkills.Length; i++)
            {
                //--------------------------------GET THE TARGET
                switch (entity.SubSkills[i].FollowType)
                {
                    default:
                    case EFollowType.caster:
                        if (caster.Enemy != null)
                        {
                            //If enemy not in range of auto attack, do not cast skill
                            if (Vector2.Distance(caster.t.position, caster.Enemy.t.position) <= range)
                                target = caster.Enemy;
                        }
                        break;

                    case EFollowType.mouse:
                    case EFollowType.mouseFixedRange:
                        //TODO Get the caster enemy by default ?
                        if (caster.Enemy != null && Vector2.Distance(caster.t.position, caster.Enemy.t.position) <= Mathf.Max(range, entity.SubSkills[i].SkillMaxRange))
                        {
                            target = caster.Enemy;
                            break;
                        }
                        //Get all enemies in range of skill
                        Collider2D[] cols = Physics2D.OverlapCircleAll(caster.t.position, Mathf.Max(range, entity.SubSkills[i].SkillMaxRange));
                        float maxDistance = 0;
                        //get the farthest one
                        for (int j = 0; j < cols.Length; j++)
                        {
                            var co = cols[j].GetComponent<Damageable>();
                            if (co != null &&
                                !co.side.IsSameSide(caster.side))
                            {
                                //Allies can't auto target objects
                                if (caster.side == ESide.Ally && co is not Unit) continue;

                                float distance = Vector2.Distance(caster.t.position, co.t.position);
                                if (target == null || distance > maxDistance)
                                {
                                    target = co;
                                    maxDistance = distance;
                                }
                            }
                        }
                        break;
                }

                if (target == null) return false;

                //--------------------------------SELECT SKILL
                SelectSkill(caster);

                //--------------------------------POSITION SKILL
                switch (entity.SubSkills[i].FollowType)
                {
                    default:
                    case EFollowType.caster:
                        hitBoxes[i].transform.position = caster.t.position;
                        break;
                    case EFollowType.mouse:
                        hitBoxes[i].transform.position = target.t.position;
                        break;
                    case EFollowType.mouseFixedRange:
                        // hitBoxes[i].transform.position = target.t.position;
                        hitBoxes[i].transform.position = caster.t.position + ((target.t.position - caster.t.position).normalized * entity.SubSkills[i].SkillMaxRange);
                        break;
                }

                //--------------------------------ROTATE SKILL
                RotateHitbox(hitBoxes[i], caster, target.t.position, entity.SubSkills[i].RotationType);
            }
            //Returning true casts skill
            return true;
        }

        public async void StartCooldown()
        {
            if (!GameManager.Instance.GODMODE)
            {
                chargesLeft--;

                if (cooldownPending) return;

                cooldownPending = true;

                //Start cooldown and wait for it to finish
                //If there are more charges to regain, start cooldown again
                while (chargesLeft < MaxCharge)
                {
                    cooldown = cooldownDuration;
                    DOTween.To(() => cooldown, x => cooldown = x, 0, cooldownDuration).SetEase(Ease.Linear);
                    await UniTask.Delay(Mathf.RoundToInt(cooldownDuration * 1000));
                    chargesLeft++;
                }
                cooldownPending = false;
            }
        }

        void RotateHitbox(SkillCollider hitbox, Unit caster, Vector3 lookPos, ERotationType rotationType)
        {
            if (rotationType == ERotationType.None) return;

            float angle = Mathf.Atan2(lookPos.y - caster.t.position.y, lookPos.x - caster.t.position.x) * Mathf.Rad2Deg;
            angle += rotationType == ERotationType.AwayFromMouse ? -180 : 0;
            hitbox.angleOffset = angle;
            hitbox.transform.eulerAngles = new Vector3(0, 0, angle);
        }

        public float GetCooldownPercentage()
        {
            if (chargesLeft == MaxCharge) return 0;

            return cooldown / cooldownDuration;
        }

        void UpdateCooldownDuration()
        {
            cooldownDuration = entity.SkillCooldown - (entity.SkillCooldown * cooldownReductionMultiplier);
        }

        public bool IsSkillAvailable()
        {
            return chargesLeft > 0;
        }

        public Sprite GetIcon()
        {
            return entity.Icon;
        }

        public float GetAutoAttackRange()
        {
            if (entity.SubSkills[0].FollowType == EFollowType.caster)
                return entity.SubSkills[0].Collider.Range;
            else
                return entity.SubSkills[0].SkillMaxRange + entity.SubSkills[0].Collider.Range;
        }

        public bool CanBeAutoCasted()
        {
            if (entity.SubSkills[0].FollowType == EFollowType.mouse
                || entity.SubSkills[0].RotationType == ERotationType.TowardsMouse
                || entity.SubSkills[0].RotationType == ERotationType.AwayFromMouse)
                return false;

            return true;
        }

        public void SetRangeMultiplier(float _multiplier)
        {
            scaleMultiplier = _multiplier;
            for (int i = 0; i < hitBoxes.Count; i++)
            {
                hitBoxes[i].multiplier = scaleMultiplier;
                hitBoxes[i].transform.localScale = Vector3.one * scaleMultiplier;
            }
        }

        public void SetCooldownReductionMultiplier(float _multiplier)
        {
            cooldownReductionMultiplier = _multiplier;
            UpdateCooldownDuration();
        }

        public bool CanCast(Unit caster)
        {
            for (int i = 0; i < Entity.Conditions.Length; i++)
            {
                if (Entity.Conditions[i].CanCast(caster) == false) return false;
            }

            for (int i = 0; i < Entity.SubSkills.Length; i++)
            {
                for (int j = 0; j < Entity.SubSkills[i].Effects.Count; j++)
                {
                    if (Entity.SubSkills[i].Effects[j].CanCast(caster) == false) return false;
                }
            }

            return true;
        }


        public void CancelSkill()
        {
            cancelToken?.Cancel();
        }

        public void ForceChangeChargesLeft(int value)
        {
            chargesLeft += value;
        }
    }
}