using System;
using UnityEngine;

namespace Starfall
{
    public enum ItemKind { Weapon, Active, Passive }
    [Serializable]
    public sealed class ItemDefinition
    {
        public string id, nameKey, descriptionKey, icon = "orb", rarity = "common", tags;
        public ItemKind kind;
        public bool implemented = true;
        public float damage, interval, energyCost, spread, value, cooldown;
        public int pellets = 1, maxCharges = 2, stackLimit = 2;
    }
    [CreateAssetMenu(menuName = "Starfall/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        public ItemDefinition[] items;
        public ItemDefinition Find(string id)
        {
            if (items != null) foreach (var item in items) if (item.id == id) return item;
            return null;
        }
        public string Describe(LocalizationService text, ItemDefinition item) => text.Get(item.descriptionKey,
            ("damage", item.damage.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)),
            ("interval", item.interval.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)),
            ("energy", item.energyCost.ToString("0.##")), ("value", item.value.ToString("0.##")),
            ("cooldown", item.cooldown.ToString("0.#")), ("charges", item.maxCharges.ToString()), ("pellets", item.pellets.ToString()));
        public static ItemCatalog Defaults()
        {
            var catalog = CreateInstance<ItemCatalog>();
            catalog.items = new[]
            {
                Weapon("pistol",20,.23f,0,1,0), Weapon("shotgun",9,.62f,12,6,20), Weapon("smg",8,.10f,3,1,0),
                Active("medkit",40,8), Active("shield",3,10),
                Passive("rapid",18), Passive("magnet",.65f), Passive("vitality",25), Passive("agile",15),
                Unavailable("crossbow",ItemKind.Weapon), Unavailable("launcher",ItemKind.Weapon), Unavailable("arc",ItemKind.Weapon),
                Unavailable("slow",ItemKind.Active), Unavailable("shock",ItemKind.Active), Unavailable("decoy",ItemKind.Active), Unavailable("grenade",ItemKind.Active),
                Unavailable("pierce",ItemKind.Passive), Unavailable("bounce",ItemKind.Passive), Unavailable("critical",ItemKind.Passive),
                Unavailable("recharge",ItemKind.Passive), Unavailable("lowhealth",ItemKind.Passive), Unavailable("blast",ItemKind.Passive),
                Unavailable("controlled",ItemKind.Passive), Unavailable("flawless",ItemKind.Passive)
            };
            return catalog;
        }
        static ItemDefinition Weapon(string id, float damage, float interval, float energy, int pellets, float spread) => new ItemDefinition
        { id = id, nameKey = "item." + id, descriptionKey = "desc." + id, kind = ItemKind.Weapon, damage = damage, interval = interval, energyCost = energy, pellets = pellets, spread = spread, icon = "arrow", tags = "projectile" };
        static ItemDefinition Active(string id, float value, float cooldown) => new ItemDefinition
        { id = id, nameKey = "item." + id, descriptionKey = "desc." + id, kind = ItemKind.Active, value = value, cooldown = cooldown, maxCharges = 2, icon = id == "medkit" ? "cross" : "orb", tags = "support" };
        static ItemDefinition Passive(string id, float value) => new ItemDefinition
        { id = id, nameKey = "item." + id, descriptionKey = "desc." + id, kind = ItemKind.Passive, value = value, stackLimit = 2, tags = "stats" };
        static ItemDefinition Unavailable(string id, ItemKind kind) => new ItemDefinition
        { id = id, kind = kind, implemented = false, nameKey = "item." + id, descriptionKey = "desc.unavailable" };
    }
}
