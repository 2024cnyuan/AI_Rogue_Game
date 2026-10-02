using UnityEngine;

namespace Starfall
{
    [CreateAssetMenu(menuName = "Starfall/Prototype Config")]
    public sealed class PrototypeConfig : ScriptableObject
    {
        [Header("Player")]
        public float health = 100;
        public float moveSpeed = 5;
        public float dodgeDuration = .18f;
        public float dodgeDistance = 2.2f;
        public float dodgeInvulnerability = .12f;
        public float dodgeCooldown = 1;
        public float hitProtection = .6f;
        [Header("Pistol")]
        public float shotInterval = .23f;
        public float bulletSpeed = 18;
        public float bulletDamage = 20;
        [Header("Enemies")]
        public float chaserHealth = 60;
        public float shooterHealth = 40;
        public float chaserSpeed = 2.6f;
        public float shooterSpeed = 1.7f;
        public float contactDamage = 12;
        public float enemyBulletDamage = 10;
        public float enemyBulletSpeed = 6;
        public float shooterWarning = .65f;
        public float shooterRecovery = 1.3f;
    }
}
