using UnityEngine;
namespace Masto.Bonus
{
    [CreateAssetMenu(fileName = "OrbitalRayEntity", menuName = "Masto/Bonuses/OrbitalRayEntity", order = 0)]
    public class OrbitalRayEntity : SingularityEntity
    {
        [SerializeField] GameObject prefab;
        public GameObject Prefab => prefab;

        [SerializeField] GameObject impactPrefab;
        public GameObject ImpactPrefab => impactPrefab;

        [SerializeField] float range;
        public float Range => range;

        [SerializeField] int[] dmg = new int[3];
        public int[] Dmg => dmg;

        [SerializeField] float[] zoneSize;
        public float[] ZoneSize => zoneSize;

        public override BonusInstance CreateInstance()
        {
            return new OrbitalRayInstance(this);
        }
    }
}