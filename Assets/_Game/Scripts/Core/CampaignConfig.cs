using UnityEngine;

namespace Starfall
{
    [CreateAssetMenu(menuName = "Starfall/Campaign Config")]
    public sealed class CampaignConfig : ScriptableObject
    {
        public float furnaceHealth = 620, mirrorHealth = 650;
        public float warning = .75f, recovery = 1.25f, bossDamage = 9;
        public float conveyorSpeed = 1.35f, iceResponse = .12f, escortSeconds = 45;
        public int healPrice = 18, energyPrice = 15, passivePrice = 32, weaponPrice = 42;
        public float shopHeal = 35, shopEnergy = 50;
        public float sporeHealth = 680, railHealth = 720, starcoreHealth = 1050, gridSurvivalSeconds = 18;
        public int sporeRounds = 3, summonLimit = 4;
    }
    public sealed class ShopLedger
    {
        readonly System.Collections.Generic.HashSet<string> sold = new System.Collections.Generic.HashSet<string>();
        bool buying;
        public bool Sold(string id) => sold.Contains(id);
        public bool Buy(string id, int price, RunContext context, System.Func<bool> apply)
        {
            if (buying || price <= 0 || context == null || context.Phase != RunPhase.Combat || context.Coins < price || Sold(id)) return false;
            buying = true;
            try
            {
                if (!apply()) return false;
                context.SpendCoins(price); sold.Add(id); return true;
            }
            finally { buying = false; }
        }
    }
}
