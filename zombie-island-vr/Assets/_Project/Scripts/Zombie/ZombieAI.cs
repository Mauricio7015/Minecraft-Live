using UnityEngine;
using UnityEngine.AI;

namespace ZombieIslandVR.Zombie
{
    /// <summary>
    /// Zombie AI state machine: Idle → Wander → Chase → Attack → Dead.
    /// Uses Unity NavMesh for pathfinding. Performance-tuned for Quest 3S.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(ZombieHealth))]
    public class ZombieAI : MonoBehaviour
    {
        public enum ZombieType { Common, Runner, Brute, Swarm, Nocturnal }
        public enum ZombieState { Idle, Wandering, Chasing, Attacking, Dead }

        // ─── Config ───────────────────────────────────────────────────────────
        [Header("Type")]
        public ZombieType zombieType = ZombieType.Common;

        [Header("Detection")]
        public float detectionRange = 15f;
        public float hearingRange = 25f;
        public float loseTargetRange = 30f;
        public float attackRange = 1.8f;
        public LayerMask playerLayer;
        public LayerMask obstacleMask;

        [Header("Movement")]
        public float walkSpeed = 1.2f;
        public float chaseSpeed = 3.5f;
        public float wanderRadius = 10f;
        public float wanderInterval = 4f;

        [Header("Combat")]
        public float attackDamage = 15f;
        public float attackCooldown = 1.5f;
        public float attackAnimDuration = 0.8f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip[] groanSounds;
        public AudioClip[] alertSounds;
        public float groanInterval = 5f;

        // ─── State ────────────────────────────────────────────────────────────
        public ZombieState CurrentState { get; private set; } = ZombieState.Idle;

        private NavMeshAgent _agent;
        private ZombieAnimator _animator;
        private Transform _player;
        private float _nextWanderTime;
        private float _nextAttackTime;
        private float _nextGroanTime;
        private bool _playerInSight;

        // ─── Init ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<ZombieAnimator>();
            ApplyTypeSettings();
        }

        private void Start()
        {
            _player = GameObject.FindWithTag("Player")?.transform;
        }

        private void Update()
        {
            if (CurrentState == ZombieState.Dead) return;
            if (_player == null) return;

            UpdateDetection();
            RunStateMachine();
            UpdateGroans();
        }

        // ─── State Machine ────────────────────────────────────────────────────

        private void RunStateMachine()
        {
            switch (CurrentState)
            {
                case ZombieState.Idle:
                    if (_playerInSight) TransitionTo(ZombieState.Chasing);
                    else if (Time.time > _nextWanderTime) TransitionTo(ZombieState.Wandering);
                    break;

                case ZombieState.Wandering:
                    if (_playerInSight) { TransitionTo(ZombieState.Chasing); break; }
                    if (_agent.remainingDistance < 0.5f) TransitionTo(ZombieState.Idle);
                    break;

                case ZombieState.Chasing:
                    if (!_playerInSight && Vector3.Distance(transform.position, _player.position) > loseTargetRange)
                    {
                        TransitionTo(ZombieState.Wandering);
                        break;
                    }
                    _agent.SetDestination(_player.position);
                    if (Vector3.Distance(transform.position, _player.position) <= attackRange)
                        TransitionTo(ZombieState.Attacking);
                    break;

                case ZombieState.Attacking:
                    // Face player
                    FaceTarget(_player.position);

                    if (Vector3.Distance(transform.position, _player.position) > attackRange + 0.5f)
                    {
                        TransitionTo(ZombieState.Chasing);
                        break;
                    }

                    if (Time.time >= _nextAttackTime)
                        PerformAttack();
                    break;
            }
        }

        private void TransitionTo(ZombieState newState)
        {
            CurrentState = newState;
            _animator?.SetState(newState);

            switch (newState)
            {
                case ZombieState.Idle:
                    _agent.isStopped = true;
                    _nextWanderTime = Time.time + Random.Range(1f, wanderInterval);
                    break;

                case ZombieState.Wandering:
                    _agent.isStopped = false;
                    _agent.speed = walkSpeed;
                    Vector3 wanderTarget = RandomNavPoint(wanderRadius);
                    _agent.SetDestination(wanderTarget);
                    _nextWanderTime = Time.time + wanderInterval;
                    break;

                case ZombieState.Chasing:
                    _agent.isStopped = false;
                    _agent.speed = chaseSpeed;
                    PlayRandomSound(alertSounds);
                    break;

                case ZombieState.Attacking:
                    _agent.isStopped = true;
                    break;

                case ZombieState.Dead:
                    _agent.enabled = false;
                    break;
            }
        }

        // ─── Detection ────────────────────────────────────────────────────────

        private void UpdateDetection()
        {
            float dist = Vector3.Distance(transform.position, _player.position);
            _playerInSight = false;

            if (dist > detectionRange) return;

            // Raycast to check visibility
            Vector3 dir = (_player.position - transform.position).normalized;
            if (!Physics.Raycast(transform.position + Vector3.up * 1.6f, dir, dist, obstacleMask))
                _playerInSight = true;

            // Nocturnal: only sees within 3m during day
            if (zombieType == ZombieType.Nocturnal)
            {
                bool isNight = IsNight();
                if (!isNight && dist > 3f) _playerInSight = false;
            }
        }

        // ─── Attack ───────────────────────────────────────────────────────────

        private void PerformAttack()
        {
            _nextAttackTime = Time.time + attackCooldown;
            _animator?.TriggerAttack();

            // Delayed damage (matches animation)
            Invoke(nameof(DealAttackDamage), attackAnimDuration * 0.5f);
        }

        private void DealAttackDamage()
        {
            if (CurrentState != ZombieState.Attacking) return;
            if (_player == null) return;

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= attackRange + 0.3f)
            {
                var stats = _player.GetComponent<ZombieIslandVR.Player.PlayerStats>();
                stats?.TakeDamage(attackDamage);
            }
        }

        // ─── Public ───────────────────────────────────────────────────────────

        /// <summary>Called when zombie is hit — immediately alerts and chases.</summary>
        public void OnHit(Vector3 hitPoint)
        {
            if (CurrentState == ZombieState.Dead) return;
            TransitionTo(ZombieState.Chasing);
        }

        /// <summary>Called by gunshot sound event. Hear and investigate.</summary>
        public void OnGunshotHeard(Vector3 soundOrigin)
        {
            float dist = Vector3.Distance(transform.position, soundOrigin);
            if (dist <= hearingRange)
                TransitionTo(ZombieState.Chasing);
        }

        // ─── Type Presets ─────────────────────────────────────────────────────

        private void ApplyTypeSettings()
        {
            switch (zombieType)
            {
                case ZombieType.Common:
                    break;
                case ZombieType.Runner:
                    walkSpeed = 2f; chaseSpeed = 5.5f; attackDamage = 10f;
                    break;
                case ZombieType.Brute:
                    walkSpeed = 0.8f; chaseSpeed = 2f; attackDamage = 35f; attackCooldown = 2f;
                    GetComponent<ZombieHealth>().maxHealth *= 3f;
                    break;
                case ZombieType.Swarm:
                    walkSpeed = 1.5f; chaseSpeed = 3.8f; attackDamage = 8f;
                    detectionRange = 20f;
                    break;
                case ZombieType.Nocturnal:
                    walkSpeed = 1f; chaseSpeed = 4f; attackDamage = 20f;
                    break;
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private void FaceTarget(Vector3 target)
        {
            Vector3 dir = (target - transform.position);
            dir.y = 0f;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }

        private Vector3 RandomNavPoint(float radius)
        {
            Vector3 randomDir = Random.insideUnitSphere * radius + transform.position;
            NavMesh.SamplePosition(randomDir, out NavMeshHit hit, radius, NavMesh.AllAreas);
            return hit.position;
        }

        private void UpdateGroans()
        {
            if (Time.time < _nextGroanTime) return;
            _nextGroanTime = Time.time + Random.Range(groanInterval * 0.5f, groanInterval * 1.5f);
            PlayRandomSound(groanSounds);
        }

        private void PlayRandomSound(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0 || audioSource == null) return;
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }

        private bool IsNight()
        {
            var cycle = FindObjectOfType<ZombieIslandVR.World.DayNightCycle>();
            return cycle != null && cycle.IsNight;
        }
    }
}
