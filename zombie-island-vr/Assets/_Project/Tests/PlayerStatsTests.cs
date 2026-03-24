using NUnit.Framework;
using UnityEngine;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.Tests
{
    /// <summary>
    /// Play Mode tests for PlayerStats.
    /// Run via: Window ▸ General ▸ Test Runner ▸ PlayMode
    /// </summary>
    public class PlayerStatsTests
    {
        private GameObject _go;
        private PlayerStats _stats;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestPlayer");
            _stats = _go.AddComponent<PlayerStats>();
            // Awake() is called automatically by Unity when AddComponent runs in PlayMode
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        // ─── Health ───────────────────────────────────────────────────────────

        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            _stats.TakeDamage(25f);
            Assert.AreEqual(75f, _stats.currentHealth, 0.01f,
                "HP should drop from 100 to 75 after 25 damage.");
        }

        [Test]
        public void TakeDamage_ClampsToZero()
        {
            _stats.TakeDamage(200f);
            Assert.AreEqual(0f, _stats.currentHealth, 0.01f,
                "HP should not go below 0.");
        }

        [Test]
        public void Die_SetIsDeadTrue_WhenHealthReachesZero()
        {
            _stats.TakeDamage(_stats.maxHealth);
            Assert.IsTrue(_stats.IsDead, "Player should be dead after lethal damage.");
        }

        [Test]
        public void Heal_RestoresHealth()
        {
            _stats.TakeDamage(50f);
            _stats.Heal(20f);
            Assert.AreEqual(70f, _stats.currentHealth, 0.01f,
                "HP should be 70 after taking 50 damage and healing 20.");
        }

        [Test]
        public void Heal_ClampsToMaxHealth()
        {
            _stats.Heal(999f);
            Assert.AreEqual(_stats.maxHealth, _stats.currentHealth, 0.01f,
                "HP should not exceed maxHealth.");
        }

        // ─── Hunger ───────────────────────────────────────────────────────────

        [Test]
        public void Eat_IncreasesHunger()
        {
            _stats.currentHunger = 50f;
            _stats.Eat(30f);
            Assert.AreEqual(80f, _stats.currentHunger, 0.01f,
                "Hunger should go from 50 to 80 after eating 30.");
        }

        [Test]
        public void Eat_ClampsToMaxHunger()
        {
            _stats.currentHunger = 0f;
            _stats.Eat(999f);
            Assert.AreEqual(_stats.maxHunger, _stats.currentHunger, 0.01f,
                "Hunger should not exceed maxHunger.");
        }

        // ─── Thirst ───────────────────────────────────────────────────────────

        [Test]
        public void Drink_IncreasesThirst()
        {
            _stats.currentThirst = 30f;
            _stats.Drink(40f);
            Assert.AreEqual(70f, _stats.currentThirst, 0.01f,
                "Thirst should go from 30 to 70 after drinking 40.");
        }

        // ─── Critical State ───────────────────────────────────────────────────

        [Test]
        public void CriticalHealth_EventFired_WhenBelowThreshold()
        {
            bool eventFired = false;
            _stats.onCriticalHealth.AddListener(() => eventFired = true);

            // Deal damage enough to go below criticalHealthThreshold (default 25)
            _stats.TakeDamage(_stats.maxHealth - _stats.criticalHealthThreshold + 1f);

            Assert.IsTrue(eventFired, "onCriticalHealth should fire when HP drops below threshold.");
        }

        // ─── IsWellFed ────────────────────────────────────────────────────────

        [Test]
        public void IsWellFed_TrueWhenBothAbove40()
        {
            _stats.currentHunger = 50f;
            _stats.currentThirst = 50f;
            Assert.IsTrue(_stats.IsWellFed, "Should be well fed when both hunger and thirst > 40.");
        }

        [Test]
        public void IsWellFed_FalseWhenHungerLow()
        {
            _stats.currentHunger = 30f;
            _stats.currentThirst = 80f;
            Assert.IsFalse(_stats.IsWellFed, "Should not be well fed when hunger <= 40.");
        }
    }
}
