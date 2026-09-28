using System;
using UnityEngine;

namespace Gameplay.Common
{
    public class AnimatedCreature : AnimatedView
    {
        private bool _isDying;
        
        public event Action<EntityView> OnDeathPerformed;
        public event Action<EntityView> OnWinPerformed;

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
            StartCoroutine(CallUponClipCompleted(OnDeathPerformed, "Death"));
        }

        public void SetWin()
        {
            if (_isDying)
                return;

            Animator.SetTrigger("Win");
            StartCoroutine(CallUponClipCompleted(OnWinPerformed, "Victory"));
        }

        public void ResetState()
        {
            _isDying = false;
        }
    }
}