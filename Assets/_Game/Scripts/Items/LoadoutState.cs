using System.Collections.Generic;
using UnityEngine;

namespace Starfall
{
    public sealed class LoadoutState
    {
        readonly ItemCatalog catalog;
        readonly Dictionary<string, int> passives = new Dictionary<string, int>();
        public string SpecialWeapon { get; private set; }
        public string Weapon { get; private set; } = "pistol";
        public string Active { get; private set; }
        public float Energy { get; private set; } = 100;
        public int Charges { get; private set; }
        public float ActiveCooldown { get; private set; }
        public float ShieldLeft { get; private set; }
        public bool InfiniteEnergy { get; set; }
        public bool InfiniteCharges { get; set; }
        public bool Invincible { get; set; }
        public IReadOnlyDictionary<string, int> Passives => passives;
        public LoadoutSnapshot Snapshot()
        {
            var value = new LoadoutSnapshot { special = SpecialWeapon, weapon = Weapon, active = Active, energy = Energy, charges = Charges, cooldown = ActiveCooldown };
            foreach (var pair in passives) value.passives.Add(new PassiveSnapshot { id = pair.Key, layers = pair.Value });
            return value;
        }
        public bool RestoreSnapshot(LoadoutSnapshot value)
        {
            if (value == null || value.passives == null) return false;
            var ids = new HashSet<string>();
            if(value.passives.Count>6 || float.IsNaN(value.energy) || value.energy<0 || value.energy>100 || float.IsNaN(value.cooldown) || value.cooldown<0 || value.cooldown>300 || value.charges<0) return false;
            foreach(var pair in value.passives) if(pair==null || pair.layers<1 || pair.layers>2 || !ids.Add(pair.id)) return false;
            var copy = new LoadoutState(catalog);
            if (!string.IsNullOrEmpty(value.special) && (value.special == "pistol" || !copy.Equip(value.special) || catalog.Find(value.special).kind != ItemKind.Weapon)) return false;
            if (!string.IsNullOrEmpty(value.active) && (!copy.Equip(value.active) || catalog.Find(value.active).kind != ItemKind.Active)) return false;
            foreach (var pair in value.passives) for (int i = 0; i < pair.layers; i++)
                if (catalog.Find(pair.id)?.kind != ItemKind.Passive || !copy.Equip(pair.id)) return false;
            if (value.weapon != "pistol" && value.weapon != copy.SpecialWeapon) return false;
            if (value.charges > (copy.Active == null ? 0 : catalog.Find(copy.Active).maxCharges)) return false;
            SpecialWeapon = copy.SpecialWeapon; Weapon = value.weapon; Active = copy.Active; passives.Clear();
            foreach (var pair in copy.passives) passives.Add(pair.Key, pair.Value);
            Energy = value.energy; Charges = value.charges; ActiveCooldown = value.cooldown; ShieldLeft = 0;
            InfiniteEnergy = InfiniteCharges = Invincible = false; return true;
        }
        public LoadoutState(ItemCatalog definitions) { catalog = definitions; }
        public int Layers(string id) => passives.TryGetValue(id, out int layers) ? layers : 0;
        public float PassiveValue(string id) => (catalog.Find(id)?.value ?? 0) * Layers(id);
        public int Pierce => Mathf.Clamp(Mathf.RoundToInt(PassiveValue("pierce")), 0, 2);
        public int Bounces => Mathf.Clamp(Mathf.RoundToInt(PassiveValue("bounce")), 0, 2);
        public float CritChance => Mathf.Clamp(PassiveValue("critical") / 100, 0, .4f);
        public float BlastMultiplier => Mathf.Clamp(1 + PassiveValue("blast") / 100, 1, 1.5f);
        public float IncomingMultiplier(VitalState health) => health.Health <= health.Maximum * .35f + .0001f ? Mathf.Clamp(1 - PassiveValue("lowhealth") / 100, .5f, 1) : 1;
        public bool ReplacePassive(string old, string replacement)
        {
            if (Layers(old) == 0 || Layers(replacement) != 0 || catalog.Find(replacement)?.kind != ItemKind.Passive) return false;
            passives.Remove(old); return Equip(replacement);
        }
        public float FireRateMultiplier => Mathf.Clamp(1 / (1 + PassiveValue("rapid") / 100), .45f, 1);
        public float DodgeMultiplier => Mathf.Clamp(1 - PassiveValue("agile") / 100, .35f, 1);
        public float ExtraHealth => PassiveValue("vitality");
        public float PickupRange => 1.5f + PassiveValue("magnet");
        public bool Equip(string id)
        {
            var item = catalog.Find(id); if (item == null || !item.implemented) return false;
            if (item.kind == ItemKind.Weapon)
            { if (id == "pistol") Weapon = "pistol"; else { SpecialWeapon = id; Weapon = id; } return true; }
            if (item.kind == ItemKind.Active) { Active = id; Charges = item.maxCharges; ActiveCooldown = ShieldLeft = 0; return true; }
            if (Layers(id) >= item.stackLimit || Layers(id) == 0 && passives.Count >= 6) return false;
            passives[id] = Layers(id) + 1; return true;
        }
        public bool RemovePassive(string id)
        {
            int layers = Layers(id); if (layers == 0) return false;
            if (layers == 1) passives.Remove(id); else passives[id] = layers - 1; return true;
        }
        public void Switch(bool special) { Weapon = special && SpecialWeapon != null ? SpecialWeapon : "pistol"; }
        public void Unequip(ItemKind kind) { if (kind == ItemKind.Weapon) { SpecialWeapon = null; Weapon = "pistol"; } else if (kind == ItemKind.Active) { Active = null; Charges = 0; ActiveCooldown = ShieldLeft = 0; } }
        public bool SpendEnergy(float cost)
        {
            if (InfiniteEnergy || cost <= 0) return true;
            if (Energy < cost) return false; Energy = Mathf.Max(0, Energy - cost); return true;
        }
        public bool AddEnergy(float amount) { if (Energy >= 100) return false; Energy = Mathf.Clamp(Energy + amount, 0, 100); return true; }
        public string TryUse(VitalState health, bool dodging, System.Func<bool> activate = null)
        {
            if (!health.Alive) return "active.dead";
            if (dodging) return "active.dodging";
            if (Active == null) return "active.none";
            if (ActiveCooldown > 0) return "active.cooldown";
            if (!InfiniteCharges && Charges <= 0) return "active.empty";
            var item = catalog.Find(Active);
            if (Active == "medkit" && health.Health >= health.Maximum) return "active.full";
            if (Active != "medkit" && Active != "shield" && (activate == null || !activate())) return "active.blocked";
            if (Active == "medkit") health.Heal(item.value); else if (Active == "shield") ShieldLeft = item.value;
            if (!InfiniteCharges) Charges--; ActiveCooldown = item.cooldown; return null;
        }
        public void Tick(float delta) { ActiveCooldown = Mathf.Max(0, ActiveCooldown - delta); ShieldLeft = Mathf.Max(0, ShieldLeft - delta); }
        public void Restore() { Energy = 100; Charges = Active != null ? catalog.Find(Active).maxCharges : 0; ActiveCooldown = ShieldLeft = 0; }
        public bool RefillActive(string id) { if(id!=Active || Charges>=catalog.Find(id).maxCharges) return false; Charges=catalog.Find(id).maxCharges; return true; }
        public void ClearEffects() { ShieldLeft = 0; ActiveCooldown = 0; }
        public void EndRoom() { ShieldLeft = 0; }
        public void RefillCharge() { if (Active != null) Charges = Mathf.Min(catalog.Find(Active).maxCharges, Charges + 1); }
        public void DefaultEquipment() { SpecialWeapon = Active = null; Weapon = "pistol"; passives.Clear(); Restore(); }
        public LoadoutState Copy()
        {
            var copy = new LoadoutState(catalog) { SpecialWeapon = SpecialWeapon, Weapon = Weapon, Active = Active, Energy = Energy, Charges = Charges,
                ActiveCooldown = ActiveCooldown, ShieldLeft = 0, InfiniteEnergy = InfiniteEnergy, InfiniteCharges = InfiniteCharges, Invincible = Invincible };
            foreach (var pair in passives) copy.passives.Add(pair.Key, pair.Value); return copy;
        }
    }
}
