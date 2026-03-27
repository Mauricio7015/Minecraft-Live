using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ZombieIslandVR.World;
using ZombieIslandVR.Player;
using ZombieIslandVR.Weapons;
using ZombieIslandVR.Zombie;
using ZombieIslandVR.Systems;

namespace ZombieIslandVR.Editor
{
    /// <summary>
    /// One-click full project setup and APK build.
    /// Menu: ZombieIsland ▸ Full Auto Setup  → monta toda a cena automaticamente
    /// Menu: ZombieIsland ▸ Build APK         → gera o APK para Quest 3
    /// </summary>
    public static class FullAutoSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/ZombieIsland.unity";
        private const string ApkOutputPath = "Builds/ZombieIsland.apk";

        // ─── Menu Items ───────────────────────────────────────────────────────

        [MenuItem("ZombieIsland/Full Auto Setup", priority = 1)]
        public static void RunFullSetup()
        {
            // Abrir a cena correta
            if (!OpenScene()) return;

            EditorUtility.DisplayProgressBar("Zombie Island Setup", "Criando ambiente...", 0.1f);
            RuntimeSceneSetup.SetupScene();

            EditorUtility.DisplayProgressBar("Zombie Island Setup", "Criando XR Rig (player)...", 0.3f);
            CreateXRRig();

            EditorUtility.DisplayProgressBar("Zombie Island Setup", "Criando prefabs de zumbi...", 0.5f);
            CreateZombiePrefabs();

            EditorUtility.DisplayProgressBar("Zombie Island Setup", "Criando prefabs de armas e itens...", 0.6f);
            CreateWeaponAndItemPrefabs();

            EditorUtility.DisplayProgressBar("Zombie Island Setup", "Configurando Global Volume...", 0.7f);
            CreateGlobalVolume();

            EditorUtility.DisplayProgressBar("Zombie Island Setup", "Bakando NavMesh...", 0.8f);
            BakeNavMesh();

            EditorUtility.DisplayProgressBar("Zombie Island Setup", "Salvando cena...", 0.95f);
            EditorSceneManager.SaveOpenScenes();

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Zombie Island VR",
                "✅ Setup concluído!\n\nPróximo passo:\nZombieIsland → Build APK", "OK");

            Debug.Log("[FullAutoSetup] Setup completo. Pronto para build.");
        }

        [MenuItem("ZombieIsland/Build APK", priority = 2)]
        public static void BuildApk()
        {
            // Salvar cenas abertas
            EditorSceneManager.SaveOpenScenes();

            Directory.CreateDirectory("Builds");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkOutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            EditorUtility.DisplayProgressBar("Build APK", "Compilando para Android/Quest 3...", 0.1f);
            var report = BuildPipeline.BuildPlayer(options);
            EditorUtility.ClearProgressBar();

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Build Concluído!",
                    $"✅ APK gerado em:\n{Path.GetFullPath(ApkOutputPath)}\n\n" +
                    "Instale com:\nadb install -r ZombieIsland.apk", "OK");

                // Abrir a pasta no explorer
                EditorUtility.RevealInFinder(ApkOutputPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Build Falhou",
                    $"❌ Erro na build.\nVerifique o Console para detalhes.", "OK");
            }
        }

        // ─── XR Rig ───────────────────────────────────────────────────────────

        private static void CreateXRRig()
        {
            // Não criar se já existir
            var existing = GameObject.FindWithTag("Player");
            if (existing != null)
            {
                Debug.Log("[FullAutoSetup] Player já existe na cena, pulando criação do XR Rig.");
                return;
            }

            // ── Raiz do Player ──
            var player = new GameObject("XR Rig [Player]");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0f, 0f);

            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.height = 1.8f;
            cc.radius = 0.3f;

            // Componentes de gameplay no root
            var stats       = player.AddComponent<PlayerStats>();
            var inventory   = player.AddComponent<PlayerInventory>();
            var controller  = player.AddComponent<PlayerController>();
            var hands       = player.AddComponent<PlayerHands>();
            var foodSys     = player.AddComponent<FoodSystem>();
            var craftSys    = player.AddComponent<CraftingSystem>();
            var saveSystem  = player.AddComponent<SaveSystem>();

            // ── Camera Offset ──
            var camOffset = new GameObject("Camera Offset");
            camOffset.transform.SetParent(player.transform);
            camOffset.transform.localPosition = new Vector3(0f, 1.36f, 0f);

            // ── Main Camera ──
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(camOffset.transform);
            camGO.transform.localPosition = Vector3.zero;
            var camera = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();

            // TrackedPoseDriver para tracking de cabeça (OpenXR)
            // Usa reflexão para evitar erro de compilação se InputSystem.XR não estiver disponível
            var tpdType = System.Type.GetType(
                "UnityEngine.InputSystem.XR.TrackedPoseDriver, Unity.InputSystem");
            if (tpdType != null)
            {
                camGO.AddComponent(tpdType);
                Debug.Log("[FullAutoSetup] TrackedPoseDriver adicionado à câmera.");
            }
            else
            {
                // Fallback: SpatialTracking (legacy)
                var legacyType = System.Type.GetType(
                    "UnityEngine.SpatialTracking.TrackedPoseDriver, UnityEngine.SpatialTracking");
                if (legacyType != null) camGO.AddComponent(legacyType);
                Debug.LogWarning("[FullAutoSetup] InputSystem.XR não encontrado. Usando fallback.");
            }

            // ── Mãos (controladores) ──
            var leftHand = CreateHandAnchor("Left Hand Anchor", player.transform,
                                             new Vector3(-0.2f, 1.2f, 0.3f));
            var rightHand = CreateHandAnchor("Right Hand Anchor", player.transform,
                                              new Vector3( 0.2f, 1.2f, 0.3f));

            // ── Serialized Field Wiring ──
            controller.cameraTransform = camGO.transform;
            controller.characterController = cc;

            hands.leftHandAnchor  = leftHand;
            hands.rightHandAnchor = rightHand;

            foodSys.playerStats    = stats;
            foodSys.inventory      = inventory;
            foodSys.headTransform  = camGO.transform;
            foodSys.playerHands    = hands;

            saveSystem.playerStats     = stats;
            saveSystem.playerTransform = player.transform;
            saveSystem.dayNightCycle   = GameObject.FindObjectOfType<DayNightCycle>();

            Debug.Log("[FullAutoSetup] XR Rig criado com todos os componentes de player.");
        }

        private static Transform CreateHandAnchor(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        // ─── Zombie Prefabs ───────────────────────────────────────────────────

        private static void CreateZombiePrefabs()
        {
            Directory.CreateDirectory("Assets/Prefabs/Zombies");

            CreateZombiePrefab("Zombie_Common", 1.0f, new Color(0.3f, 0.15f, 0.3f),
                                100f, 1.5f, ZombieAI.ZombieType.Common);
            CreateZombiePrefab("Zombie_Runner", 0.85f, new Color(0.6f, 0.5f, 0.1f),
                                60f, 3.5f, ZombieAI.ZombieType.Runner);
            CreateZombiePrefab("Zombie_Brute", 1.5f, new Color(0.5f, 0.05f, 0.05f),
                                400f, 0.9f, ZombieAI.ZombieType.Brute);

            AssetDatabase.SaveAssets();
            Debug.Log("[FullAutoSetup] 3 prefabs de zumbi criados em Assets/Prefabs/Zombies/");
        }

        private static void CreateZombiePrefab(string prefabName, float scale, Color color,
                                                float hp, float speed,
                                                ZombieAI.ZombieType type)
        {
            string path = $"Assets/Prefabs/Zombies/{prefabName}.prefab";
            if (File.Exists(path)) return;

            var root = new GameObject(prefabName);
            root.transform.localScale = Vector3.one * scale;

            // Corpo (cápsula)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            SetColor(body, color);

            // Cabeça (esfera)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform);
            head.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            SetColor(head, color * 0.8f);

            // Braço esquerdo
            var lArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lArm.name = "Left Arm";
            lArm.transform.SetParent(root.transform);
            lArm.transform.localPosition = new Vector3(-0.45f, 1.2f, 0.1f);
            lArm.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
            lArm.transform.localRotation = Quaternion.Euler(30f, 0f, 20f);
            SetColor(lArm, color);

            // Braço direito
            var rArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rArm.name = "Right Arm";
            rArm.transform.SetParent(root.transform);
            rArm.transform.localPosition = new Vector3( 0.45f, 1.2f, 0.1f);
            rArm.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
            rArm.transform.localRotation = Quaternion.Euler(30f, 0f, -20f);
            SetColor(rArm, color);

            // Componentes
            var navAgent = root.AddComponent<NavMeshAgent>();
            navAgent.speed  = speed;
            navAgent.radius = 0.3f;
            navAgent.height = 1.8f;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1f, 0f);
            capsule.height = 2f;
            capsule.radius = 0.3f;

            var ai = root.AddComponent<ZombieAI>();
            ai.zombieType = type;

            var health = root.AddComponent<ZombieHealth>();
            health.maxHealth = hp;

            root.AddComponent<AudioSource>();

            // Atribuir head/arm refs ao ZombieHealth
            health.leftArmObject  = lArm;
            health.rightArmObject = rArm;

            // Layer "Zombie" (cria se não existir)
            root.layer = EnsureLayer("Zombie");

            // Salvar como prefab
            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }

        // ─── Weapon & Item Prefabs ────────────────────────────────────────────

        private static void CreateWeaponAndItemPrefabs()
        {
            Directory.CreateDirectory("Assets/Prefabs/Weapons");
            Directory.CreateDirectory("Assets/Prefabs/Items");

            CreatePistolPrefab();
            CreateAxePrefab();
            CreateAmmoPrefab();
            CreateMedkitPrefab();

            AssetDatabase.SaveAssets();
            Debug.Log("[FullAutoSetup] Prefabs de arma/item criados.");
        }

        private static void CreatePistolPrefab()
        {
            const string path = "Assets/Prefabs/Weapons/Pistol.prefab";
            if (File.Exists(path)) return;

            var root = new GameObject("Pistol");

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.05f, 0.12f, 0.18f);
            SetColor(body, new Color(0.2f, 0.2f, 0.2f));

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(root.transform);
            barrel.transform.localPosition = new Vector3(0f, 0.02f, 0.14f);
            barrel.transform.localScale = new Vector3(0.025f, 0.06f, 0.025f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            SetColor(barrel, new Color(0.15f, 0.15f, 0.15f));

            var muzzlePoint = new GameObject("MuzzlePoint");
            muzzlePoint.transform.SetParent(root.transform);
            muzzlePoint.transform.localPosition = new Vector3(0f, 0.02f, 0.22f);

            root.AddComponent<Rigidbody>();
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.05f, 0.12f, 0.18f);

            var firearm = root.AddComponent<FirearmWeapon>();
            firearm.muzzlePoint = muzzlePoint.transform;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }

        private static void CreateAxePrefab()
        {
            const string path = "Assets/Prefabs/Weapons/Axe.prefab";
            if (File.Exists(path)) return;

            var root = new GameObject("Axe");

            // Cabo
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.transform.SetParent(root.transform);
            handle.transform.localPosition = Vector3.zero;
            handle.transform.localScale = new Vector3(0.04f, 0.35f, 0.04f);
            SetColor(handle, new Color(0.35f, 0.2f, 0.1f));

            // Lâmina
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(root.transform);
            blade.transform.localPosition = new Vector3(0.06f, 0.33f, 0f);
            blade.transform.localScale = new Vector3(0.18f, 0.16f, 0.04f);
            SetColor(blade, new Color(0.6f, 0.6f, 0.65f));

            root.AddComponent<Rigidbody>();
            root.AddComponent<BoxCollider>();

            var melee = root.AddComponent<MeleeWeapon>();
            melee.canBlock = true;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }

        private static void CreateAmmoPrefab()
        {
            const string path = "Assets/Prefabs/Items/AmmoBox.prefab";
            if (File.Exists(path)) return;

            var root = new GameObject("AmmoBox");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform);
            body.transform.localScale = new Vector3(0.1f, 0.07f, 0.15f);
            SetColor(body, new Color(0.9f, 0.8f, 0.1f));

            root.AddComponent<BoxCollider>().isTrigger = true;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }

        private static void CreateMedkitPrefab()
        {
            const string path = "Assets/Prefabs/Items/Medkit.prefab";
            if (File.Exists(path)) return;

            var root = new GameObject("Medkit");

            var h = GameObject.CreatePrimitive(PrimitiveType.Cube);
            h.name = "Cross_H";
            h.transform.SetParent(root.transform);
            h.transform.localScale = new Vector3(0.18f, 0.06f, 0.06f);
            SetColor(h, new Color(0.9f, 0.1f, 0.1f));

            var v = GameObject.CreatePrimitive(PrimitiveType.Cube);
            v.name = "Cross_V";
            v.transform.SetParent(root.transform);
            v.transform.localScale = new Vector3(0.06f, 0.18f, 0.06f);
            SetColor(v, new Color(0.9f, 0.1f, 0.1f));

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.18f, 0.18f, 0.06f);
            col.isTrigger = true;

            var interactable = root.AddComponent<InteractableObject>();
            interactable.healthRestore = 50f;
            interactable.interactionPrompt = "Usar Medkit";

            PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
        }

        // ─── Global Volume ────────────────────────────────────────────────────

        private static void CreateGlobalVolume()
        {
            if (GameObject.FindObjectOfType<Volume>() != null) return;

            var go = new GameObject("Global Volume");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // ColorAdjustments
            var colorAdj = profile.Add<ColorAdjustments>(overrides: true);
            colorAdj.active = true;

            // Vignette
            var vignette = profile.Add<Vignette>(overrides: true);
            vignette.active = true;
            vignette.intensity.value = 0.25f;
            vignette.intensity.overrideState = true;

            // ChromaticAberration
            var chromatic = profile.Add<ChromaticAberration>(overrides: true);
            chromatic.active = true;
            chromatic.intensity.value = 0f;
            chromatic.intensity.overrideState = true;

            // Salvar profile como asset
            Directory.CreateDirectory("Assets/_Project/Settings");
            AssetDatabase.CreateAsset(profile, "Assets/_Project/Settings/GlobalVolumeProfile.asset");

            vol.profile = profile;
            Debug.Log("[FullAutoSetup] Global Volume criado com ColorAdjustments + Vignette.");
        }

        // ─── NavMesh ──────────────────────────────────────────────────────────

        private static void BakeNavMesh()
        {
            // Garante que o chão tem NavMeshSurface
            var floor = GameObject.Find("Island_Floor");
            if (floor != null && floor.GetComponent<NavMeshSurface>() == null)
                floor.AddComponent<NavMeshSurface>();

            // Bake todas as surfaces
            var surfaces = GameObject.FindObjectsOfType<NavMeshSurface>();
            foreach (var s in surfaces)
            {
                s.BuildNavMesh();
                Debug.Log($"[FullAutoSetup] NavMesh bakado: {s.gameObject.name}");
            }

            if (surfaces.Length == 0)
                Debug.LogWarning("[FullAutoSetup] Nenhuma NavMeshSurface encontrada. Crie o ambiente primeiro.");
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static bool OpenScene()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Erro", $"Cena não encontrada: {ScenePath}", "OK");
                return false;
            }
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);
            return true;
        }

        private static void SetColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            r.sharedMaterial = mat;
        }

        private static int EnsureLayer(string layerName)
        {
            int idx = LayerMask.NameToLayer(layerName);
            if (idx >= 0) return idx;

            // Adicionar layer via SerializedObject (apenas em Editor)
            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < 32; i++)
            {
                var layerProp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    layerProp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[FullAutoSetup] Layer '{layerName}' criada no índice {i}.");
                    return i;
                }
            }
            Debug.LogWarning($"[FullAutoSetup] Não foi possível criar layer '{layerName}' — todas ocupadas.");
            return 0;
        }
    }
}
