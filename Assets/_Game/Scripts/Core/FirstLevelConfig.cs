using UnityEngine;
namespace Starfall
{
    [CreateAssetMenu(menuName = "Starfall/First Level Config")]
    public sealed class FirstLevelConfig : ScriptableObject
    {
        public string timingVersion = "1", balanceVersion = "1";
        public float bossHealth = 700, bossBulletDamage = 7, bossChargeDamage = 14, bossWarning = .7f, bossRecovery = 1.2f;
        public int roomCoins = 8;
        public float roomEnergy = 25, roomHeal = 10, challengeSeconds = 35;
    }
}
