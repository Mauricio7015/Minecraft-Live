using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZombieIslandVR.World
{
    /// <summary>
    /// Programmatically builds the ZombieIsland scene from Unity primitives.
    /// Run from the menu: ZombieIsland ▸ Setup Scene
    ///
    /// After running, manually:
    ///   1. Window ▸ AI ▸ Navigation ▸ Bake (NavMesh)
    ///   2. Assign zombie prefabs to ZombieSpawner component
    ///   3. Set up XR Rig (Meta XR SDK OVRCameraRig or XR Origin)
    /// </summary>
    public class RuntimeSceneSetup : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("ZombieIsland/Setup Scene")]
        public static void SetupScene()
        {
            Debug.Log("[RuntimeSceneSetup] Building scene...");

            CreateFloor();
            CreateSkyAndFog();
            CreateLighting();
            CreateObstacles();
            CreateBuildings();
            CreateContainers();
            SetupZombieSpawner();
            SetupDayNightCycle();
            SetupGunShotSystem();

            Debug.Log("[RuntimeSceneSetup] Done. Remember to bake NavMesh and assign XR Rig.");
        }

        // ─── Floor ────────────────────────────────────────────────────────────

        private static void CreateFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Island_Floor";
            floor.transform.localScale = new Vector3(50f, 1f, 50f); // 500×500 m
            floor.transform.position = Vector3.zero;
            SetMaterialColor(floor, new Color(0.2f, 0.35f, 0.15f));  // dark grass

            // Mark as NavMesh static
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic);
        }

        // ─── Sky & Fog ────────────────────────────────────────────────────────

        private static void CreateSkyAndFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.fogColor = new Color(0.3f, 0.25f, 0.2f);
            RenderSettings.ambientLight = new Color(0.15f, 0.12f, 0.1f);
        }

        // ─── Lighting ─────────────────────────────────────────────────────────

        private static void CreateLighting()
        {
            // Main directional (sun) — DayNightCycle will control it
            var sunGO = new GameObject("Sun");
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.7f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Fire point lights at corners for atmosphere
            Vector3[] firePositions = {
                new Vector3(-80f, 3f, -80f),
                new Vector3( 80f, 3f, -80f),
                new Vector3(-80f, 3f,  80f),
                new Vector3( 80f, 3f,  80f),
            };
            foreach (var pos in firePositions)
            {
                var fireGO = new GameObject("FireLight");
                var fireLight = fireGO.AddComponent<Light>();
                fireLight.type = LightType.Point;
                fireLight.color = new Color(1f, 0.4f, 0.05f);
                fireLight.intensity = 3f;
                fireLight.range = 20f;
                fireGO.transform.position = pos;
            }
        }

        // ─── Obstacles ────────────────────────────────────────────────────────

        private static void CreateObstacles()
        {
            var parent = new GameObject("Obstacles");

            // Crates
            PlaceBoxes(parent.transform, "Crate", 20,
                new Color(0.45f, 0.3f, 0.15f),
                new Vector3(1.2f, 1.2f, 1.2f),
                radius: 60f);

            // Barrels
            PlaceCylinders(parent.transform, "Barrel", 10,
                new Color(0.2f, 0.2f, 0.2f),
                new Vector3(0.7f, 1.0f, 0.7f),
                radius: 55f);

            // Long cover walls
            PlaceWalls(parent.transform);

            // Destroyed cars
            PlaceCars(parent.transform);
        }

        private static void PlaceBoxes(Transform parent, string prefix, int count,
            Color color, Vector3 scale, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count) + Random.Range(-10f, 10f);
                float dist = Random.Range(radius * 0.4f, radius);
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * dist,
                    scale.y * 0.5f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * dist);

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"{prefix}_{i}";
                go.transform.SetParent(parent);
                go.transform.position = pos;
                go.transform.localScale = scale;
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 90f), 0f);
                SetMaterialColor(go, color);
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic);
            }
        }

        private static void PlaceCylinders(Transform parent, string prefix, int count,
            Color color, Vector3 scale, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count) + Random.Range(-15f, 15f);
                float dist = Random.Range(radius * 0.5f, radius);
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * dist,
                    scale.y * 0.5f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * dist);

                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = $"{prefix}_{i}";
                go.transform.SetParent(parent);
                go.transform.position = pos;
                go.transform.localScale = scale;
                SetMaterialColor(go, color);
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic);
            }
        }

        private static void PlaceWalls(Transform parent)
        {
            var wallData = new (Vector3 pos, Vector3 scale, float rotY)[]
            {
                (new Vector3(  0f, 1f, -50f), new Vector3(30f, 2f, 0.5f),  0f),
                (new Vector3(  0f, 1f,  50f), new Vector3(30f, 2f, 0.5f),  0f),
                (new Vector3(-50f, 1f,   0f), new Vector3(0.5f, 2f, 30f),  0f),
                (new Vector3( 50f, 1f,   0f), new Vector3(0.5f, 2f, 30f),  0f),
                (new Vector3(-25f, 1f, -25f), new Vector3(20f, 2f, 0.5f), 30f),
                (new Vector3( 25f, 1f,  25f), new Vector3(20f, 2f, 0.5f), 30f),
            };
            foreach (var (pos, scale, rotY) in wallData)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "CoverWall";
                go.transform.SetParent(parent);
                go.transform.position = pos;
                go.transform.localScale = scale;
                go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
                SetMaterialColor(go, new Color(0.5f, 0.45f, 0.4f));
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic);
            }
        }

        private static void PlaceCars(Transform parent)
        {
            var carPositions = new Vector3[]
            {
                new Vector3(-40f, 0.5f, -15f),
                new Vector3( 35f, 0.5f,  20f),
                new Vector3( -5f, 0.5f,  45f),
            };
            foreach (var pos in carPositions)
            {
                var car = new GameObject("DestroyedCar");
                car.transform.SetParent(parent);
                car.transform.position = pos;
                car.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                // Body
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.SetParent(car.transform);
                body.transform.localPosition = Vector3.zero;
                body.transform.localScale = new Vector3(4f, 1.4f, 2f);
                SetMaterialColor(body, new Color(0.3f, 0.1f, 0.1f));

                // Roof
                var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.transform.SetParent(car.transform);
                roof.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                roof.transform.localScale = new Vector3(2f, 0.7f, 1.8f);
                SetMaterialColor(roof, new Color(0.25f, 0.08f, 0.08f));

                GameObjectUtility.SetStaticEditorFlags(car, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic);
            }
        }

        // ─── Buildings ────────────────────────────────────────────────────────

        private static void CreateBuildings()
        {
            var parent = new GameObject("Buildings");
            var zones = new (Vector3 center, string label)[]
            {
                (new Vector3(-70f, 0f, -70f), "Base_Militar"),
                (new Vector3( 70f, 0f, -70f), "Vila"),
                (new Vector3(-70f, 0f,  70f), "Floresta_Ruins"),
                (new Vector3( 70f, 0f,  70f), "Porto"),
            };

            foreach (var (center, label) in zones)
            {
                var zoneGO = new GameObject(label);
                zoneGO.transform.SetParent(parent.transform);
                zoneGO.transform.position = center;

                for (int i = 0; i < 3; i++)
                {
                    float bx = Random.Range(-15f, 15f);
                    float bz = Random.Range(-15f, 15f);
                    float bh = Random.Range(4f, 9f);
                    float bw = Random.Range(5f, 10f);
                    float bd = Random.Range(5f, 10f);

                    var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    building.name = $"Building_{label}_{i}";
                    building.transform.SetParent(zoneGO.transform);
                    building.transform.localPosition = new Vector3(bx, bh * 0.5f, bz);
                    building.transform.localScale = new Vector3(bw, bh, bd);
                    SetMaterialColor(building, new Color(0.35f, 0.3f, 0.25f));
                    GameObjectUtility.SetStaticEditorFlags(building, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic);
                }
            }
        }

        // ─── Containers ───────────────────────────────────────────────────────

        private static void CreateContainers()
        {
            var parent = new GameObject("Containers");
            var positions = new Vector3[]
            {
                new Vector3(-15f, 0.6f, -20f),
                new Vector3( 18f, 0.6f, -15f),
                new Vector3(-22f, 0.6f,  12f),
                new Vector3( 10f, 0.6f,  20f),
                new Vector3(-35f, 0.6f,  -5f),
                new Vector3( 30f, 0.6f,  10f),
                new Vector3(  5f, 0.6f, -30f),
                new Vector3(-10f, 0.6f,  30f),
            };

            foreach (var pos in positions)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Container_Locker";
                go.transform.SetParent(parent.transform);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(1f, 1.2f, 0.6f);
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                SetMaterialColor(go, new Color(0.4f, 0.35f, 0.2f));

                go.AddComponent<InteractableContainer>();
                // LootTable and AudioSource need to be assigned manually in Inspector
            }

            Debug.Log($"[RuntimeSceneSetup] Created {positions.Length} containers.");
        }

        // ─── ZombieSpawner ────────────────────────────────────────────────────

        private static void SetupZombieSpawner()
        {
            var existing = Object.FindFirstObjectByType<ZombieIslandVR.Zombie.ZombieSpawner>();
            if (existing != null) return;

            var go = new GameObject("ZombieSpawner");
            go.AddComponent<ZombieIslandVR.Zombie.ZombieSpawner>();
            Debug.Log("[RuntimeSceneSetup] ZombieSpawner created — assign zombie prefabs in Inspector.");
        }

        // ─── DayNightCycle ────────────────────────────────────────────────────

        private static void SetupDayNightCycle()
        {
            var existing = Object.FindFirstObjectByType<DayNightCycle>();
            if (existing != null) return;

            var sunGO = GameObject.Find("Sun") ?? new GameObject("Sun");
            var cycle = sunGO.AddComponent<DayNightCycle>();

            var sun = sunGO.GetComponent<Light>();
            if (sun == null) sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;

            cycle.sunLight = sun;
            cycle.timeOfDay = 0.25f;  // start at 6:00 AM
            Debug.Log("[RuntimeSceneSetup] DayNightCycle attached to Sun.");
        }

        // ─── GunShotEventSystem ───────────────────────────────────────────────

        private static void SetupGunShotSystem()
        {
            var existing = Object.FindFirstObjectByType<ZombieIslandVR.Systems.GunShotEventSystem>();
            if (existing != null) return;

            var go = new GameObject("GunShotEventSystem");
            go.AddComponent<ZombieIslandVR.Systems.GunShotEventSystem>();
            Debug.Log("[RuntimeSceneSetup] GunShotEventSystem created.");
        }

        // ─── Utility ──────────────────────────────────────────────────────────

        private static void SetMaterialColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ??
                                   Shader.Find("Standard"));
            mat.color = color;
            renderer.material = mat;
        }
#endif
    }
}
