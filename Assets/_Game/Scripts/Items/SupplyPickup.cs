using UnityEngine;

namespace Starfall
{
    public enum SupplyKind { Coin, Health, Energy }
    public sealed class SupplyPickup : MonoBehaviour
    {
        StarfallGame game;
        SupplyKind kind;
        SpriteRenderer view;
        Vector2 home;
        bool consumed;
        public void Initialize(StarfallGame owner, SupplyKind type)
        {
            game = owner; kind = type; home = transform.position;
            view = PrototypeVisuals.Draw(transform, "Supply", Vector2.zero, Vector2.one * .45f,
                kind == SupplyKind.Coin ? PrototypeVisuals.Gold : PrototypeVisuals.Teal, 6, kind == SupplyKind.Health ? "cross" : "coin");
            M5Art.Apply(view, kind == SupplyKind.Coin ? "coins" : kind == SupplyKind.Health ? "health" : "energy", .6f);
        }
        void Update()
        {
            if (consumed || game == null || !game.CanAct || !game.Player.Health.State.Alive) return;
            Vector2 target = game.Player.Body.position;
            if (kind == SupplyKind.Health && game.Player.Health.State.Health >= game.Player.Health.State.Maximum)
            { view.transform.localPosition = new Vector2(0, Mathf.Sin(Time.time * 3) * .06f); return; }
            float distance = Vector2.Distance(transform.position, target);
            if (kind == SupplyKind.Energy && game.Loadout.Energy >= 100) return;
            if (distance > game.Loadout.PickupRange || Physics2D.Raycast(transform.position, (target - (Vector2)transform.position).normalized, distance, 1))
            { view.transform.localPosition = new Vector2(0, Mathf.Sin(Time.time * 3) * .06f); return; }
            transform.position = Vector2.MoveTowards(transform.position, target, 7 * Time.deltaTime);
            if (Vector2.Distance(transform.position, target) > .42f) return;
            float healthBefore = game.Player.Health.State.Health, energyBefore = game.Loadout.Energy;
            if (kind == SupplyKind.Health && !game.Player.Health.Heal(25)) return;
            if (kind == SupplyKind.Energy && !game.Loadout.AddEnergy(35)) return;
            consumed = true;
            if (kind == SupplyKind.Coin) { game.Context.AddCoins(3); game.Notify("pickup.coin"); }
            else game.Notify(kind == SupplyKind.Health ? "pickup.heal" : "pickup.energy");
            game.Feedback?.Resource(kind == SupplyKind.Coin ? "coins" : kind == SupplyKind.Health ? "health" : "energy", kind == SupplyKind.Coin ? 3 : kind == SupplyKind.Health ? game.Player.Health.State.Health - healthBefore : game.Loadout.Energy - energyBefore);
            game.Tutorial?.SupplyCollected();
            game.Audio?.Play(GameSound.Pickup);
            Destroy(gameObject);
        }
    }
}
