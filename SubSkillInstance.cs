using Cysharp.Threading.Tasks;
using Masto.Character;
using Masto.Pooling;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Masto.Skill
{
    public class SubSkillInstance
    {
        SubSkill entity;
        public SubSkillInstance(SubSkill _entity)
        {
            entity = _entity;
        }

        public void CastSubSkill(Unit caster, SkillCollider hitbox, CancellationTokenSource token)
        {
            // return Tools.CoroutineUtil.Instance.StartCoroutine(CastSubSkillRoutine(caster, hitbox));
            if (entity.AttachHitboxToCaster)
                hitbox.transform.SetParent(caster.t);

            // hitbox.SetTelegraphVisibility(false);

            for (int i = 0; i < entity.FXs.Count; i++)
            {
                SpawnFX(entity.FXs[i], caster, hitbox, token);
            }

            for (int i = 0; i < entity.SFXs.Count; i++)
            {
                SpawnSFX(entity.SFXs[i], caster, hitbox, token);
            }

            for (int i = 0; i < entity.Effects.Count; i++)
            {
                CastSkillEffect(entity.Effects[i], caster, hitbox, token);
            }
        }

        public void ShowTelegraph(Unit caster, SkillCollider hitbox, float duration, CancellationTokenSource token)
        {
            //Do not show telegraph in dezoom mode
            if (!GameController.IsPossessing) return;

            for (int i = 0; i < entity.Telegraphs.Count; i++)
            {
                SpawnFX(entity.Telegraphs[i], caster, hitbox, token, true, duration);
            }
        }

        async void CastSkillEffect(SkillEffect effect, Unit caster, SkillCollider hitbox, CancellationTokenSource token)
        {
            //Delay
            if (effect.Delay > 0)
                await UniTask.Delay(Mathf.RoundToInt(effect.Delay * 1000), false, PlayerLoopTiming.Update, token.Token);

            //Get Targets
            List<Damageable> targets = new List<Damageable>();
            ESide side;
            switch (entity.TargetSide)
            {
                case ETargetingSide.CasterSide:
                    side = caster.side;
                    break;
                default:
                case ETargetingSide.OppositeSide:
                    side = caster.side.Opposite();
                    break;
                case ETargetingSide.Both:
                    side = ESide.Both;
                    break;
            }
            if (entity.TargetSide != ETargetingSide.None)
                targets = hitbox.GetDamageablesInCollider(side);

            if (entity.IncludeSelf && !targets.Contains(caster))
                targets.Add(caster);

            else if (!entity.IncludeSelf && targets.Contains(caster))
                targets.Remove(caster);

            // Cast effect
            effect.TriggerEffect(caster, targets, hitbox);

        }

        async void SpawnFX(SkillFX skillFX, Unit caster, SkillCollider hitbox, CancellationTokenSource token, bool telegraph = false, float telegraphDuration = 0)
        {
            Vector3 casterOGPos = caster.t.position;

            //Delay
            if (skillFX.Delay > 0)
                await UniTask.Delay(Mathf.RoundToInt(skillFX.Delay * 1000), false, PlayerLoopTiming.Update, token.Token);


            //SKILLS ARE POOLED BACK ON THE ANIMATOR USING SCRIPT AUTODESTROYVFX
            var fx = PoolsManager.GetFromPool<Transform>(skillFX.FxPrefab.name);
            var fxPivot = fx.GetChild(0);
            fxPivot.localScale = hitbox.transform.localScale; //Multiply by the scale of the effect
            fxPivot.localEulerAngles = Vector3.zero;

            if (skillFX.AttachFXToCaster)
            {
                fx.SetParent(caster.t);
            }

            switch (skillFX.SpawnPosition)
            {
                default:
                case SkillFX.EFXSpawnPosition.OnCaster:
                    switch (skillFX.YOffset)
                    {
                        default:
                        case SkillFX.EFXYOffset.OnFoot:
                            fx.position = caster.t.position;
                            break;
                        case SkillFX.EFXYOffset.OnBelly:
                            fx.position = caster.BellyTransform.position;
                            break;
                        case SkillFX.EFXYOffset.OnHead:
                            fx.position = caster.HeadTransform.position;
                            break;
                        case SkillFX.EFXYOffset.Custom:
                            caster.CustomTransform.localPosition = new Vector3(skillFX.CustomOffset.x * (caster.FacingRight ? 1 : -1), skillFX.CustomOffset.y, 0);
                            fx.position = caster.CustomTransform.position;
                            break;
                    }
                    break;

                case SkillFX.EFXSpawnPosition.OnHitbox:
                    fx.position = hitbox.transform.position;
                    break;

                case SkillFX.EFXSpawnPosition.OnMouse:
                    fx.position = CameraManager.Instance.GetMouseGroundPos();
                    break;
            }

            if (skillFX.FaceCamera)
                fxPivot.LookAt(fxPivot.position + CameraManager.Instance.Cam.transform.forward);

            if (skillFX.Rotation == SkillFX.EFXRotation.CasterHitboxAngle)
            {
                Vector3 perpendicular = casterOGPos - hitbox.transform.position;
                Quaternion rot = Quaternion.LookRotation(Vector3.forward, perpendicular);
                float newZ = rot.eulerAngles.z + 90;

                fxPivot.localEulerAngles += new Vector3(0, 0, newZ);
            }
            else if (skillFX.Rotation == SkillFX.EFXRotation.CopyHitbox)
            {
                fxPivot.localEulerAngles += new Vector3(0, 0, hitbox.transform.localEulerAngles.z + 180);
            }

            if (skillFX.MirrorFXBasedOnFacingDirection && caster.FacingRight)
            {
                if (skillFX.Rotation == SkillFX.EFXRotation.None)
                    fxPivot.localScale = new Vector3(-fxPivot.localScale.x, fxPivot.localScale.y, fxPivot.localScale.z);
                else
                    fxPivot.localScale = new Vector3(fxPivot.localScale.x, -fxPivot.localScale.y, fxPivot.localScale.z);
            }

            if (telegraph)
            {
                var t = fx.GetComponent<Telegraph>();
                if (t != null)
                {
                    t.Charge(telegraphDuration);
                }
                else
                {
                    Debug.LogErrorFormat("The telegraph prefab names '{0}' does not have a telegraph component", skillFX.FxPrefab.name);
                }

                await UniTask.Delay(Mathf.RoundToInt(telegraphDuration * 1000), false, PlayerLoopTiming.Update, token.Token);

                if (token.IsCancellationRequested)
                {
                    t.gameObject.SetActive(false);
                }
            }
        }

        async void SpawnSFX(SkillSFX skillSFX, Unit caster, SkillCollider hitbox, CancellationTokenSource token)
        {
            Transform posT;
            switch (skillSFX.SpawnPosition)
            {
                default:
                case SkillFX.EFXSpawnPosition.OnCaster:
                    posT = caster.t;
                    break;

                case SkillFX.EFXSpawnPosition.OnHitbox:
                    posT = hitbox.transform;
                    break;

                case SkillFX.EFXSpawnPosition.OnMouse:
                    posT = caster.t;
                    break;
            }

            if (!GameController.IsPossessing ||
                (GameController.IsPossessing && Vector2.Distance(posT.position, GameController.Instance.GetPossessedUnit().t.position) < GameManager.Instance.Settings.SfxPlayRangeWhilePossessing))
            {
                //Delay
                if (skillSFX.Delay > 0)
                    await UniTask.Delay(Mathf.RoundToInt(skillSFX.Delay * 1000), false, PlayerLoopTiming.Update, token.Token);

                skillSFX.ClipRef.RandomizePitch();
                var osas = GameManager.Instance.SfxHandler.PlayTempSFX(skillSFX.ClipRef, !caster.IsPossessed);
                if (osas == null) return;

                if (skillSFX.AttachFXToCaster)
                {
                    osas.transform.SetParent(caster.t);
                }

                osas.transform.position = posT.position;
            }
        }
    }
}