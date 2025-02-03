namespace Masto.Bonus
{
    public class SingularityInstance : BonusInstance
    {
        public new SingularityEntity entity;
        public SingularityInstance(BonusEntity entity) : base(entity)
        {
            this.entity = entity as SingularityEntity;

            //This is called to refresh the cooldown reduction multiplier on this
            GameController.Instance.GetPossessedUnit()?.UpdateStat(Stats.EStat.Cooldown);
        }

        float cooldown;
        float cooldownReductionMultiplier;

        public override void OnInstanceLevelUp()
        {
            base.OnInstanceLevelUp();
            if (InstanceLevel == 0)
                cooldown = 1;
            else
                ResetCooldown();
        }

        public virtual void UpdateCooldown(float deltaTime)
        {
            cooldown -= deltaTime;
            if (cooldown <= 0)
            {
                ResetCooldown();
                TriggerEffect();
            }
        }

        void ResetCooldown()
        {
            cooldown = entity.Cooldowns[InstanceLevel] - (entity.Cooldowns[InstanceLevel] * cooldownReductionMultiplier);
        }

        public virtual void TriggerEffect()
        {

        }

        public void SetCooldownReductionMultiplier(float _multiplier)
        {
            cooldownReductionMultiplier = _multiplier;

            if (entity.Cooldowns[InstanceLevel] - (entity.Cooldowns[InstanceLevel] * cooldownReductionMultiplier) < cooldown)
                ResetCooldown();
        }

        public override string GetDetailDesc()
        {
            return FormatDescForLevel(base.GetDetailDesc(), InstanceLevel);
        }

        public override string GetNextLevelDetailDesc()
        {
            return FormatDescForLevel(base.GetNextLevelDetailDesc(), InstanceLevel + 1);
        }
    }
}