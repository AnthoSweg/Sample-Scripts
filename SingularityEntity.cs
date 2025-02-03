using UnityEngine;
namespace Masto.Bonus
{
    public class SingularityEntity : BonusEntity
    {
        public override BonusInstance CreateInstance()
        {
            return new SingularityInstance(this);
        }

        [SerializeField] float[] cooldown = new float[3];
        public float[] Cooldowns => cooldown;
    }
}