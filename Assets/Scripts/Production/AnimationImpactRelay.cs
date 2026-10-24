using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Put Animation Events named AscendanceImpact and AscendanceAttackFinished on production clips.
    /// If clips have no events, CharacterPresentationController uses safe timing fallbacks.
    /// </summary>
    public sealed class AnimationImpactRelay : MonoBehaviour
    {
        private CharacterPresentationController owner;

        public void Bind(CharacterPresentationController controller) => owner = controller;

        public void AscendanceImpact() => owner?.NotifyAnimationImpact();
        public void AscendanceAttackFinished() => owner?.NotifyAnimationAttackFinished();
    }
}
