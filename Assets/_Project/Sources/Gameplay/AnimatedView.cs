using System;
using System.Collections;
using UnityEngine;

namespace Gameplay.Common
{
    public class AnimatedView : EntityView
    {
        [SerializeField] protected Animator Animator;

        public virtual void ResetAnimator() => Animator.speed = 1;

        protected void PauseAnimator() => Animator.speed = 0f;

        protected IEnumerator CallUponClipCompleted(Action<EntityView> onPerformed, string stateName)
        {
            const int layerIndex = 0;

            while (!Animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName))
                yield return null;

            while (Animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName) &&
                   Animator.GetCurrentAnimatorStateInfo(layerIndex).normalizedTime < 1f)
            {
                yield return null;
            }

            PauseAnimator();
            onPerformed?.Invoke(this);
        }
    }
}