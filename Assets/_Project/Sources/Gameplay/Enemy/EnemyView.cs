using Gameplay.Common;

namespace Gameplay.Enemy
{
    public class EnemyView : AnimatedCreature
    {
        public void SetAttack()
        {
            Animator.SetTrigger("Attack");
        }
    }
}