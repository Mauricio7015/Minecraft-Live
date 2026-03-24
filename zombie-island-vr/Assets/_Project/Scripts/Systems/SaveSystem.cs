using System;
using System.IO;
using UnityEngine;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.Systems
{
    [Serializable]
    public class SaveData
    {
        public float health;
        public float hunger;
        public float thirst;
        public float stamina;
        public float posX, posY, posZ;
        public float rotY;
        public float timeOfDay;
        public int dayCount;
        public string timestamp;
    }

    /// <summary>
    /// JSON-based save system. Saves to persistent data path (works on Quest).
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "zombie_island_save.json");

        public PlayerStats playerStats;
        public Transform playerTransform;
        public ZombieIslandVR.World.DayNightCycle dayNightCycle;

        private int _dayCount = 1;

        // ─── Public API ───────────────────────────────────────────────────────

        public void Save()
        {
            var data = new SaveData
            {
                health = playerStats.currentHealth,
                hunger = playerStats.currentHunger,
                thirst = playerStats.currentThirst,
                stamina = playerStats.currentStamina,
                posX = playerTransform.position.x,
                posY = playerTransform.position.y,
                posZ = playerTransform.position.z,
                rotY = playerTransform.eulerAngles.y,
                timeOfDay = dayNightCycle?.timeOfDay ?? 0.25f,
                dayCount = _dayCount,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Saved to {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
            }
        }

        public bool Load()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("[SaveSystem] No save found.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(json);

                playerStats.currentHealth = data.health;
                playerStats.currentHunger = data.hunger;
                playerStats.currentThirst = data.thirst;
                playerStats.currentStamina = data.stamina;
                playerTransform.position = new Vector3(data.posX, data.posY, data.posZ);
                playerTransform.rotation = Quaternion.Euler(0f, data.rotY, 0f);
                if (dayNightCycle != null) dayNightCycle.timeOfDay = data.timeOfDay;
                _dayCount = data.dayCount;

                Debug.Log($"[SaveSystem] Loaded save from {data.timestamp} (Day {data.dayCount})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
                return false;
            }
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }

        public bool HasSave() => File.Exists(SavePath);

        // ─── Auto-save ────────────────────────────────────────────────────────
        private float _autoSaveTimer;
        public float autoSaveInterval = 300f;  // 5 minutes

        private void Update()
        {
            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= autoSaveInterval)
            {
                _autoSaveTimer = 0f;
                Save();
            }
        }
    }
}
