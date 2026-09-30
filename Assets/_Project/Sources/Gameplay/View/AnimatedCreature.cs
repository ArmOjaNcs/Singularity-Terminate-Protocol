using System;
using UnityEngine;

namespace Gameplay.View
{
    public class AnimatedCreature : AnimatedView
    {
        private bool _isDying;
        
        public event Action<AnimatedCreature> OnDeathPerformed;
        public event Action<AnimatedCreature> OnWinPerformed;

        public override void ResetAnimator()
        {
            Animator.Rebind();
            Animator.Update(0f);
        }

        public void SetMoving(bool value)
        {
            if (_isDying)
                return;

            Animator.SetBool("IsMoving", value);
        }

        public void SetDeath()
        {
            if (_isDying)
                return;

            _isDying = true;
            Animator.SetTrigger("Death");
            OnComplete = InvokeDeathEvent;
            StartViewRoutine("Death", 1);
        }

        public void SetWin()
        {
            if (_isDying)
                return;

            Animator.SetTrigger("Win");
            OnComplete = InvokeVictoryEvent;
            StartViewRoutine("Victory", 1);
        }

        public void ResetState()
        {
            _isDying = false;
        }

        private void InvokeDeathEvent() => OnDeathPerformed?.Invoke(this);

        private void InvokeVictoryEvent() => OnWinPerformed?.Invoke(this);
    }
}