using Masto.Character;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Masto.Skill
{
    [Serializable]
    public class SubSkill
    {
        [PropertySpace(SpaceBefore = 20)]

        [SerializeField, EnumPaging, HorizontalGroup("sideGroup", MarginRight = .25f)] ETargetingSide targetSide;
        public ETargetingSide TargetSide => targetSide;
        [PropertySpace(SpaceBefore = 20)]
        [SerializeField, HorizontalGroup("sideGroup", Width = 300)] bool includeSelf;
        public bool IncludeSelf => includeSelf;

        [PropertySpace(20)]

        [SerializeField, Required] SkillCollider collider;
        public SkillCollider Collider => collider;

        [SerializeField, HideIf("targetSide", ETargetingSide.None), EnumToggleButtons] EFollowType followType;
        public EFollowType FollowType => followType;

        [SerializeField, HideIf("targetSide", ETargetingSide.None), EnumToggleButtons] ERotationType rotationType;
        public ERotationType RotationType => rotationType;

        [HideIf("FollowType", EFollowType.caster), HideIf("targetSide", ETargetingSide.None)]
        [SerializeField, Unit(Units.Meter)] float skillMaxRange;
        public float SkillMaxRange => skillMaxRange;

        [SerializeField, HideIf("targetSide", ETargetingSide.None)] bool attachHitboxToCaster;
        public bool AttachHitboxToCaster => attachHitboxToCaster;

        [PropertySpace(SpaceBefore = 20, SpaceAfter = 20)]

        [SerializeField] List<SkillFX> telegraphs = new List<SkillFX>();
        public List<SkillFX> Telegraphs => telegraphs;

        [SerializeReference] List<SkillEffect> effects = new List<SkillEffect>();
        public List<SkillEffect> Effects => effects;

        [PropertySpace(SpaceAfter = 20)]
        [SerializeField] List<SkillFX> fxs = new List<SkillFX>();
        public List<SkillFX> FXs => fxs;

        [PropertySpace(SpaceAfter = 20)]
        [SerializeField] List<SkillSFX> sfxs = new List<SkillSFX>();
        public List<SkillSFX> SFXs => sfxs;


    }
}