using Gameplay.View;
using System;

namespace Gameplay.Enemy
{
    public class EnemyView : AnimatedCreature
    {
        private bool _isAttack;

        public event Action<EnemyView> OnAttackPerformed;
        public event Action<EnemyView> OnAttackFinished;

        private void OnEnable()
        {
            _isAttack = false;
        }

        public void SetAttack()
        {
            if (_isAttack)
                return;

            _isAttack = true;
            Animator.SetTrigger("Attack");
            OnComplete = PerformAttack;
            StartViewRoutine("Attack", 0.5f);
        }

        protected void PerformAttack()
        {
            OnAttackPerformed?.Invoke(this);
            StartPauseableRoutineFromCurrentClip();
        }

        protected override void OnRoutineEnd()
        {
            _isAttack = false;
            OnAttackFinished?.Invoke(this);
        }
    }
}