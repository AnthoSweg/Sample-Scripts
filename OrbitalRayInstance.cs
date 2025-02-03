using Cysharp.Threading.Tasks;
using Masto.Character;
using Masto.Pooling;
using System.Collections.Generic;
using UnityEngine;
namespace Masto.Bonus
{
    public class OrbitalRayInstance : SingularityInstance
    {
        new OrbitalRayEntity entity;
        public OrbitalRayInstance(SingularityEntity entity) : base(entity)
        {
            this.entity = entity as OrbitalRayEntity;
        }

        async void InstantiateRay(Vector2 origin, int occurence = 0)
        {
            Unit caster = GameController.Instance.GetPossessedUnit();
            if (caster == null) return;

            float occurenceMultiplier = 1 - (occurence * .25f);
            float sizeMultiplier = caster.SZoneScaleMulti * occurenceMultiplier;

            //Get a random point inside range
            Vector2 impactPoint = GetRandomUnitPositionInRange(origin, entity.Range * occurenceMultiplier);

            //Spawn ray, change its size and wait a bit
            GameObject ray = PoolsManager.GetFromPool<Transform>(entity.Prefab.name).gameObject;
            ray.transform.localPosition = impactPoint;
            ray.transform.localScale = Vector3.one * sizeMultiplier;

            await UniTask.Delay(500);

            //Instantiate impact FX
            GameObject impact = PoolsManager.GetFromPool<Transform>(entity.ImpactPrefab.name).gameObject;
            impact.transform.localPosition = impactPoint;
            impact.transform.localScale = Vector3.one * sizeMultiplier;

            if (caster == null) return;

            //deal damage
            bool hasCrit = false;
            var touchedEnemies = GetDamageablesInCollider(impactPoint, entity.ZoneSize[InstanceLevel] * sizeMultiplier);
            for (int i = 0; i < touchedEnemies.Count; i++)
            {
                var hit = new HitInfo(entity.Dmg[InstanceLevel]);
                caster.DealDamage(touchedEnemies[i], hit);
                if (hit.isCrit)
                    hasCrit = true;
            }

            //If crit, relaunch a smaller ray
            if (hasCrit && occurence < 4)
                InstantiateRay(impactPoint, occurence + 1);
        }

        List<Damageable> GetDamageablesInCollider(Vector2 pos, float range)
        {
            List<Damageable> touchedDamageables = new List<Damageable>();

            var cols = Physics2D.OverlapCircleAll(pos, range);
            for (int c = 0; c < cols.Length; c++)
            {
                var co = cols[c].GetComponent<Damageable>();
                if (co != null &&
                    co.side.IsSameSide(ESide.Enemy) &&
                    !touchedDamageables.Contains(co))
                {
                    touchedDamageables.Add(co);
                }
            }

            return touchedDamageables;
        }

        Vector3 GetRandomUnitPositionInRange(Vector2 pos, float range)
        {
            var cols = Physics2D.OverlapCircleAll(pos, range);
            for (int c = 0; c < cols.Length; c++)
            {
                var co = cols[c].GetComponent<Damageable>();
                if (co != null &&
                    co.side.IsSameSide(ESide.Enemy) &&
                    co is Unit)
                {
                    return co.t.position;
                }
            }

            return (Random.insideUnitCircle * range) + pos;
        }

        public override void TriggerEffect()
        {
            base.TriggerEffect();
            InstantiateRay(GameController.Instance.GetPossessedUnit().t.position);
        }

        public override void OnBonusAdded()
        {
            base.OnBonusAdded();
            var parent = new GameObject(entity.Prefab.name + " parent");
            PoolsManager.CreatePool(new PoolData(entity.Prefab.name, entity.Prefab.transform), parent.transform);
            PoolsManager.CreatePool(new PoolData(entity.ImpactPrefab.name, entity.ImpactPrefab.transform), parent.transform);
        }

        public override string FormatDescForLevel(string desc, int index)
        {
            desc = desc.Replace("_VALUE1", entity.Dmg[index].ToString());
            desc = desc.Replace("_VALUE2", entity.ZoneSize[index].ToString());
            desc = desc.Replace("_VALUE3", entity.Cooldowns[index].ToString());
            return desc;
        }
    }
}