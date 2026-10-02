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
        public float damage, interval, energyCost, spread, value, cooldown, duration, radius, delay;
        public int pellets = 1, maxCharges = 2, stackLimit = 2, piercing, chains;
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
            ("cooldown", item.cooldown.ToString("0.#")), ("charges", item.maxCharges.ToString()), ("pellets", item.pellets.ToString()),
            ("radius", item.radius.ToString("0.#")), ("duration", item.duration.ToString("0.#")), ("delay", item.delay.ToString("0.##")),
            ("piercing", item.piercing.ToString()), ("chains", item.chains.ToString()));
        public static ItemCatalog Defaults()
        {
            var catalog = CreateInstance<ItemCatalog>();
            catalog.items = new[]
            {
                Weapon("pistol",20,.23f,0,1,0), Weapon("shotgun",9,.62f,12,6,20), Weapon("smg",8,.10f,3,1,0),
                Active("medkit",40,8), Active("shield",3,10),
                Passive("rapid",18), Passive("magnet",.65f), Passive("vitality",25), Passive("agile",15),
                Weapon("crossbow",65,1.05f,10,1,0), Weapon("launcher",55,.9f,15,1,0), Weapon("arc",26,.48f,8,1,0),
                Active("slow",50,9), Active("shock",22,8), Active("decoy",60,11), Active("grenade",60,8),
                Passive("pierce",1), Passive("bounce",1), Passive("critical",15),
                Passive("recharge",7), Passive("lowhealth",20), Passive("blast",25),
                Passive("controlled",30), Passive("flawless",8)
            };
            catalog.Find("crossbow").delay = .55f; catalog.Find("crossbow").piercing = 2;
            catalog.Find("launcher").radius = 2.4f; catalog.Find("launcher").delay = .6f;
            catalog.Find("arc").radius = 4; catalog.Find("arc").chains = 3;
            catalog.Find("slow").radius = 3.2f; catalog.Find("slow").duration = 4;
            catalog.Find("shock").radius = 3; catalog.Find("shock").duration = 1;
            catalog.Find("decoy").duration = 7;
            catalog.Find("grenade").radius = 2.6f; catalog.Find("grenade").delay = .9f;
            foreach (var item in catalog.items) {
                item.tags = item.kind == ItemKind.Weapon ? "projectile," + item.id : item.kind == ItemKind.Active ? "active," + item.id : "passive," + item.id;
                if (item.id == "crossbow" || item.id == "arc" || item.id == "controlled") item.rarity = "rare";
            }
            return catalog;
        }
        public static ItemDefinition WorkshopWeapon() => new ItemDefinition { id = "workshop_smg", nameKey = "item.workshop_smg", descriptionKey = "desc.smg", kind = ItemKind.Weapon,
            damage = 10, interval = .1f, energyCost = 3, icon = "arrow", rarity = "rare", tags = "projectile,smg,variant" };
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
