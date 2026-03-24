using UnityEngine;

namespace ZombieIslandVR.World
{
    /// <summary>
    /// Configures the island biomes and zone data.
    /// Uses Unity Terrain. Procedural heightmap applied at start.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class BiomeZone
        {
            public string zoneName;
            public Vector3 center;
            public float radius;
            public Color gizmoColor = Color.green;
            [Range(0, 100)] public int zombieDensity = 10;
            [Range(0, 100)] public int lootDensity = 10;
            public GameObject[] propPrefabs;
        }

        [Header("Island Config")]
        public int islandRadius = 500;           // meters (500 = ~1km playable)
        public float waterRadioactiveEdge = 50f;
        public Terrain mainTerrain;

        [Header("Zones")]
        public BiomeZone beach;
        public BiomeZone tropicalForest;
        public BiomeZone abandonedVillage;
        public BiomeZone destroyedPort;
        public BiomeZone militaryBase;

        [Header("Terrain Generation")]
        [Range(1, 8)] public int octaves = 5;
        [Range(0.1f, 10f)] public float frequency = 2f;
        [Range(0f, 1f)] public float persistence = 0.5f;
        public float heightScale = 80f;
        public AnimationCurve islandShapeCurve;
        public int seed = 42;

        private void Start()
        {
            if (mainTerrain != null)
                GenerateTerrain();
        }

        // ─── Terrain Generation ───────────────────────────────────────────────

        public void GenerateTerrain()
        {
            TerrainData td = mainTerrain.terrainData;
            int res = td.heightmapResolution;
            float[,] heights = new float[res, res];

            Random.InitState(seed);

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float nx = (float)x / res - 0.5f;
                    float nz = (float)z / res - 0.5f;

                    float noiseVal = FractalNoise(nx * frequency, nz * frequency);

                    // Island shape: height falls to zero at edges
                    float distFromCenter = Mathf.Sqrt(nx * nx + nz * nz) * 2f;
                    float islandMask = islandShapeCurve != null
                        ? islandShapeCurve.Evaluate(1f - distFromCenter)
                        : Mathf.Max(0f, 1f - distFromCenter * 1.5f);

                    heights[z, x] = Mathf.Clamp01(noiseVal * islandMask) * heightScale / td.size.y;
                }
            }

            td.SetHeights(0, 0, heights);
            Debug.Log("[WorldGenerator] Terrain generated.");
        }

        private float FractalNoise(float x, float z)
        {
            float value = 0f;
            float amplitude = 1f;
            float freq = 1f;
            float maxVal = 0f;

            for (int i = 0; i < octaves; i++)
            {
                value += Mathf.PerlinNoise(x * freq + seed, z * freq + seed) * amplitude;
                maxVal += amplitude;
                amplitude *= persistence;
                freq *= 2f;
            }

            return value / maxVal;
        }

        // ─── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            DrawZoneGizmo(beach, "Beach");
            DrawZoneGizmo(tropicalForest, "Forest");
            DrawZoneGizmo(abandonedVillage, "Village");
            DrawZoneGizmo(destroyedPort, "Port");
            DrawZoneGizmo(militaryBase, "Base");

            // Island border
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, islandRadius);

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, islandRadius + waterRadioactiveEdge);
        }

        private void DrawZoneGizmo(BiomeZone zone, string label)
        {
            if (zone == null) return;
            Gizmos.color = zone.gizmoColor;
            Gizmos.DrawWireSphere(zone.center, zone.radius);
        }
    }
}
