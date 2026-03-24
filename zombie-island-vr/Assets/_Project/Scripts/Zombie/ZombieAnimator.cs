using UnityEngine;

namespace ZombieIslandVR.Zombie
{
    /// <summary>
    /// Bridge between ZombieAI state machine and Unity Animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class ZombieAnimator : MonoBehaviour
    {
        private Animator _animator;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int StateHash = Animator.StringToHash("State");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void SetState(ZombieAI.ZombieState state)
        {
            _animator.SetInteger(StateHash, (int)state);

            switch (state)
            {
                case ZombieAI.ZombieState.Idle:
                    _animator.SetFloat(SpeedHash, 0f);
                    break;
                case ZombieAI.ZombieState.Wandering:
                    _animator.SetFloat(SpeedHash, 0.5f);
                    break;
                case ZombieAI.ZombieState.Chasing:
                    _animator.SetFloat(SpeedHash, 1f);
                    break;
                case ZombieAI.ZombieState.Attacking:
                    _animator.SetFloat(SpeedHash, 0f);
                    break;
                case ZombieAI.ZombieState.Dead:
                    _animator.SetBool(IsDeadHash, true);
                    break;
            }
        }

        public void TriggerAttack()
        {
            _animator.SetTrigger(AttackHash);
        }

        public void SetSpeed(float normalizedSpeed)
        {
            _animator.SetFloat(SpeedHash, normalizedSpeed, 0.1f, Time.deltaTime);
        }
    }
}
