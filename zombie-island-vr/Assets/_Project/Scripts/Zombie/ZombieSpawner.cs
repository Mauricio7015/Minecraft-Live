using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ZombieIslandVR.Zombie
{
    /// <summary>
    /// Object-pool based zombie spawner. Hard limit of 30 active zombies for Quest 3S.
    /// Scales spawn rate with day/night cycle.
    /// </summary>
    public class ZombieSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnEntry
        {
            public GameObject zombiePrefab;
            [Range(0f, 1f)] public float spawnWeight = 1f;
            public ZombieAI.ZombieType zombieType;
        }

        [Header("Spawn Config")]
        public SpawnEntry[] zombieTypes;
        public int maxActiveZombies = 30;
        public float spawnInterval = 15f;       // seconds between waves
        public float nightSpawnMultiplier = 2f;
        public float spawnRadius = 50f;
        public float minSpawnDistance = 15f;    // don't spawn too close to player

        [Header("Horde Event")]
        public int hordeSize = 20;
        public float hordeInterval = 480f;      // seconds (every 8 min)

        // ─── Pool ─────────────────────────────────────────────────────────────
        private List<GameObject> _pool = new List<GameObject>();
        private List<GameObject> _active = new List<GameObject>();
        private Transform _player;
        private float _nextHordeTime;
        private ZombieIslandVR.World.DayNightCycle _dayNightCycle;

        private void Start()
        {
            _player = GameObject.FindWithTag("Player")?.transform;
            _dayNightCycle = FindObjectOfType<ZombieIslandVR.World.DayNightCycle>();
            _nextHordeTime = Time.time + hordeInterval;
            StartCoroutine(SpawnLoop());
        }

        private void Update()
        {
            // Horde event
            if (Time.time >= _nextHordeTime)
            {
                _nextHordeTime = Time.time + hordeInterval;
                StartCoroutine(SpawnHorde());
            }

            // Clean dead zombies from active list
            _active.RemoveAll(z => z == null || z.GetComponent<ZombieHealth>()?.isDead == true);
        }

        // ─── Spawn Loop ───────────────────────────────────────────────────────

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                float interval = spawnInterval;
                if (IsNight()) interval /= nightSpawnMultiplier;

                yield return new WaitForSeconds(interval);

                int toSpawn = Mathf.Min(3, maxActiveZombies - _active.Count);
                for (int i = 0; i < toSpawn; i++)
                    SpawnOne();
            }
        }

        private IEnumerator SpawnHorde()
        {
            Debug.Log("[ZombieSpawner] HORDE EVENT!");
            int spawned = 0;
            while (spawned < hordeSize && _active.Count < maxActiveZombies)
            {
                SpawnOne();
                spawned++;
                yield return new WaitForSeconds(0.3f);
            }
        }

        private void SpawnOne()
        {
            if (_active.Count >= maxActiveZombies) return;
            if (_player == null) return;

            Vector3 spawnPos;
            if (!TryGetSpawnPosition(out spawnPos)) return;

            var prefab = SelectZombiePrefab();
            if (prefab == null) return;

            // Reuse pooled zombie or instantiate new
            GameObject zombie = GetFromPool(prefab);
            zombie.transform.position = spawnPos;
            zombie.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            zombie.SetActive(true);

            var health = zombie.GetComponent<ZombieHealth>();
            if (health != null) health.currentHealth = health.maxHealth;

            _active.Add(zombie);
        }

        // ─── Pool ─────────────────────────────────────────────────────────────

        private GameObject GetFromPool(GameObject prefab)
        {
            foreach (var z in _pool)
            {
                if (!z.activeSelf && z.name.StartsWith(prefab.name))
                {
                    _pool.Remove(z);
                    return z;
                }
            }
            var newZ = Instantiate(prefab);
            newZ.name = prefab.name + "_pooled";
            return newZ;
        }

        public void ReturnToPool(GameObject zombie)
        {
            zombie.SetActive(false);
            _active.Remove(zombie);
            _pool.Add(zombie);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private bool TryGetSpawnPosition(out Vector3 pos)
        {
            pos = Vector3.zero;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 offset2D = Random.insideUnitCircle.normalized * Random.Range(minSpawnDistance, spawnRadius);
                Vector3 candidate = _player.position + new Vector3(offset2D.x, 0f, offset2D.y);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                    return true;
                }
            }
            return false;
        }

        private GameObject SelectZombiePrefab()
        {
            float total = 0f;
            foreach (var e in zombieTypes) total += e.spawnWeight;
            float rng = Random.Range(0f, total);
            float cumul = 0f;
            foreach (var e in zombieTypes)
            {
                cumul += e.spawnWeight;
                if (rng <= cumul) return e.zombiePrefab;
            }
            return zombieTypes[0].zombiePrefab;
        }

        private bool IsNight()
        {
            return _dayNightCycle != null && _dayNightCycle.IsNight;
        }
    }
}
