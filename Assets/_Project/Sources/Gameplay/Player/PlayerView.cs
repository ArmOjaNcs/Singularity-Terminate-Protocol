using System;
using Gameplay.Common;
using UnityEngine;

namespace Gameplay.Player
{
    public sealed class PlayerView : EntityView
    {
        [SerializeField] private Animator _animator;

        public void SetMoving(bool value)
        {
            _animator.SetBool("IsMoving", value);
        }

        public void SetDeath()
        {
            _animator.SetTrigger("Death");
        }

        public void SetWin()
        {
            _animator.SetTrigger("Win");
        }
    }
}