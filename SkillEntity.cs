using Sirenix.OdinInspector;
using UnityEngine;

namespace Masto.Skill
{
    [CreateAssetMenu(fileName = "SkillEntity", menuName = "Masto/Skills/SkillEntity", order = -1)]
    public class SkillEntity : ScriptableObject
    {
        public SkillInstance CreateInstance()
        {
            return new SkillInstance(this);
        }

        [HideLabel]
        [PreviewField(80, ObjectFieldAlignment.Left)]
        [HorizontalGroup("row", 80), VerticalGroup("row/left")]
        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [VerticalGroup("row/right"), HorizontalGroup("row/right/a"), LabelWidth(100)]
        [SerializeField] new string name;
        public string Name => name;

        [VerticalGroup("row/right"), HorizontalGroup("row/right/a"), LabelWidth(50)]
        [SerializeField] string desc;
        public string Desc => desc;

        [VerticalGroup("row/right"), HorizontalGroup("row/right/b"), LabelWidth(100)]
        [SerializeField, Unit(Units.Second)] float skillCooldown;
        public float SkillCooldown => skillCooldown;


        [VerticalGroup("row/right"), LabelWidth(100)]
        [SerializeField, MinValue(1), ProgressBar(0, 3, Segmented = true, DrawValueLabel = true)] int charges = 1;
        public int Charges => charges;

        [VerticalGroup("row/right"), HorizontalGroup("row/right/c", LabelWidth = 150, Gap = 10)]
        [SerializeField] bool stillWhileCasting;
        public bool StillWhileCasting => stillWhileCasting;

        [VerticalGroup("row/right"), HorizontalGroup("row/right/c", LabelWidth = 150)]
        [SerializeField, Unit(Units.Second), ShowIf("@stillWhileCasting==true")] float chargingDuration;
        public float ChargingDuration => chargingDuration;

        [VerticalGroup("row/right"), HorizontalGroup("row/right/d", LabelWidth = 150)]
        [SerializeField] bool blockLookingSide;
        public bool BlockLookingSide => blockLookingSide;

        [SerializeField] ConditionEntity[] conditions;
        public ConditionEntity[] Conditions => conditions;

        [SerializeField] SubSkill[] subSkills;
        public SubSkill[] SubSkills => subSkills;
    }

    public enum EFollowType
    {
        caster,
        mouse,
        mouseFixedRange,
        towardsMovementFixedRange
    }

    public enum ERotationType
    {
        None,
        TowardsMouse,
        AwayFromMouse,
        TowardsMovement
    }
}