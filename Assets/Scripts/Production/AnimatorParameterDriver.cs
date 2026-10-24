using System.Collections.Generic;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Safe adapter for production Animator Controllers. Parameters are optional;
    /// missing parameters are ignored so an imported rig can be integrated incrementally.
    /// Recommended parameters: Speed(float), Combat(bool), AttackType(int),
    /// Attack(trigger), Hit(trigger), Death(trigger), Revive(trigger), Downed(bool).
    /// </summary>
    public sealed class AnimatorParameterDriver
    {
        private readonly Animator animator;
        private readonly HashSet<int> parameters = new();

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Combat = Animator.StringToHash("Combat");
        private static readonly int AttackType = Animator.StringToHash("AttackType");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Death = Animator.StringToHash("Death");
        private static readonly int Revive = Animator.StringToHash("Revive");
        private static readonly int Downed = Animator.StringToHash("Downed");

        public bool IsValid => animator != null && animator.runtimeAnimatorController != null;

        public AnimatorParameterDriver(Animator source)
        {
            animator = source;
            if (animator == null) return;
            foreach (var parameter in animator.parameters) parameters.Add(parameter.nameHash);
        }

        public void SetLocomotion(float speed)
        {
            if (Has(Speed)) animator.SetFloat(Speed, Mathf.Max(0f, speed), 0.08f, Time.deltaTime);
            if (Has(Combat)) animator.SetBool(Combat, true);
        }

        public void TriggerAttack(string style)
        {
            if (Has(AttackType)) animator.SetInteger(AttackType, AttackStyleCode(style));
            if (Has(Attack)) animator.SetTrigger(Attack);
        }

        public void TriggerHit()
        {
            if (Has(Hit)) animator.SetTrigger(Hit);
        }

        public void TriggerDeath()
        {
            if (Has(Downed)) animator.SetBool(Downed, true);
            if (Has(Death)) animator.SetTrigger(Death);
        }

        public void TriggerRevive()
        {
            if (Has(Downed)) animator.SetBool(Downed, false);
            if (Has(Revive)) animator.SetTrigger(Revive);
        }

        private bool Has(int hash) => animator != null && parameters.Contains(hash);

        private static int AttackStyleCode(string style)
        {
            if (string.IsNullOrEmpty(style)) return 0;
            return style.ToLowerInvariant() switch
            {
                "heavy" => 1,
                "ranged" => 2,
                "spell" => 3,
                "shield" => 4,
                _ => 0
            };
        }
    }
}
