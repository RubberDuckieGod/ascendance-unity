using UnityEngine;

namespace Ascendance
{
    [CreateAssetMenu(menuName = "Ascendance/Character Animation Profile", fileName = "CharacterAnimationProfile")]
    public sealed class CharacterAnimationProfile : ScriptableObject
    {
        [Header("Fallback timing used when imported clips have no Animation Events")]
        [Min(0.05f)] public float MeleeImpact = 0.27f;
        [Min(0.05f)] public float HeavyImpact = 0.36f;
        [Min(0.05f)] public float RangedImpact = 0.40f;
        [Min(0.05f)] public float SpellImpact = 0.44f;
        [Min(0.10f)] public float AttackDuration = 0.66f;

        [Header("Locomotion")]
        [Min(0.1f)] public float SpeedBlendScale = 1f;

        public float ImpactFor(string style)
        {
            return style?.ToLowerInvariant() switch
            {
                "heavy" => HeavyImpact,
                "ranged" => RangedImpact,
                "spell" => SpellImpact,
                _ => MeleeImpact
            };
        }
    }
}
