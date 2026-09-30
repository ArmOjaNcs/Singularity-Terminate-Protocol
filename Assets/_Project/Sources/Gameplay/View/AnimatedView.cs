using System;
using System.Collections;
using UnityEngine;

namespace Gameplay.View
{
    public class AnimatedView : EntityView
    {
        [SerializeField] protected Animator Animator;

        protected Coroutine ViewRoutine;
        protected Coroutine PauseableRoutine;
        protected Action OnComplete;
        protected bool IsPaused;

        private protected virtual void OnDisable()
        {
            StopAllCoroutines();
        }

        public void Play()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            SetAnimatorSpeed(1f);
        }

        public void Pause()
        {
            if (IsPaused)
                return;

            IsPaused = true;
            SetAnimatorSpeed(0);
        }

        public virtual void ResetAnimator() { }

        protected void SetAnimatorSpeed(float speed)
        {
            speed = Mathf.Clamp01(speed);
            Animator.speed = speed;
        }

        protected void StartViewRoutine(string stateName, float clipTime)
        {
            ViewRoutine = StartCoroutineSafe(
                CallUponClipCompleted(stateName, clipTime), ref ViewRoutine);
        }

        protected void StartPauseableRoutine(float duration)
        {
            PauseableRoutine = StartCoroutineSafe(UpdateRoutine(duration), ref PauseableRoutine);
        }

        protected void StartPauseableRoutineFromCurrentClip()
        {
            const int layerIndex = 0;

            AnimatorStateInfo stateInfo =
                Animator.GetCurrentAnimatorStateInfo(layerIndex);

            if (stateInfo.length <= 0f)
                return;

            float normalizedTime =
                stateInfo.normalizedTime % 1f;

            float remainingTime =
                stateInfo.length * (1f - normalizedTime);

            StartPauseableRoutine(remainingTime);
        }

        protected virtual void OnRoutineStart() { }

        protected virtual void OnRoutineIteration(float elapsedTime, float cycleDuration) { }

        protected virtual void OnRoutineEnd() { }

        private Coroutine StartCoroutineSafe(IEnumerator routine, ref Coroutine coroutine)
        {
            if (!isActiveAndEnabled)
                return null;

            if (coroutine != null)
                StopCoroutine(coroutine);

            coroutine = StartCoroutine(routine);

            return coroutine;
        }

        private IEnumerator CallUponClipCompleted(string stateName, float clipTime)
        {
            const int layerIndex = 0;
            clipTime = Mathf.Clamp01(clipTime);

            while (!Animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName))
                yield return null;

            while (Animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName) &&
                   Animator.GetCurrentAnimatorStateInfo(layerIndex).normalizedTime < clipTime)
            {
                yield return null;
            }

            OnComplete();
        }

        private IEnumerator UpdateRoutine(float duration)
        {
            OnRoutineStart();
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                if (IsPaused)
                {
                    yield return null;
                    continue;
                }

                elapsedTime += Time.deltaTime;
                OnRoutineIteration(elapsedTime, duration);
                yield return null;
            }

            OnRoutineEnd();
        }
    }
}