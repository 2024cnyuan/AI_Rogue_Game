using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall
{
    public enum LevelRoomKind { Safe, Combat, Beacon, Mechanism, Challenge, Boss, Shop }
    public sealed class LevelRoomPlan
    {
        public string Id, Next, Back, Branch;
        public int Stage = 1;
        public LevelRoomKind Kind;
        public Rect[] Cover;
        public Vector2[] Enemies;
    }
    public sealed class FirstLevelPlan
    {
        public readonly int Seed;
        public readonly int Stage;
        public string StageId => new[] { "gardens", "workshop", "reservoir", "greenhouse", "hub", "sanctum" }[Stage - 1];
        public int RequiredTasks => Stage == 4 || Stage == 5 ? 3 : 2;
        public string RoomKey(string id) => Stage == 1 ? "room." + id : "room.s" + Stage + "." + id;
        public readonly List<LevelRoomPlan> Rooms = new List<LevelRoomPlan>();
        public FirstLevelPlan(int seed, int stage = 1)
        {
            if (stage < 1 || stage > 6) throw new ArgumentOutOfRangeException(nameof(stage));
            Seed = seed; Stage = stage;
            if (stage > 1) { BuildTheme(); return; }
            Add("entry", LevelRoomKind.Safe, 4, "courtyard", null);
            Add("courtyard", LevelRoomKind.Combat, 0, "north", "entry");
            Add("north", LevelRoomKind.Beacon, 1, "crossing", "courtyard");
            Add("crossing", LevelRoomKind.Combat, 2, "south", "north");
            Add("south", LevelRoomKind.Beacon, 3, "seal", "crossing");
            Add("seal", LevelRoomKind.Mechanism, 5, "supply", "south");
            Add("supply", LevelRoomKind.Safe, 4, "boss", "seal");
            Add("boss", LevelRoomKind.Boss, 6, null, "supply");
            Add("challenge", LevelRoomKind.Challenge, 2, null, "courtyard");
        }
        void BuildTheme()
        {
            Add("entry", LevelRoomKind.Safe, 4, "courtyard", null);
            Add("courtyard", LevelRoomKind.Combat, 0, "north", "entry");
            Add("north", LevelRoomKind.Beacon, 1, "crossing", "courtyard");
            Add("crossing", LevelRoomKind.Combat, 2, "south", "north");
            if (Stage == 4 || Stage == 5) { Find("crossing").Next = "middle"; Add("middle", LevelRoomKind.Beacon, 0, "south", "crossing"); }
            Add("south", LevelRoomKind.Beacon, 3, "seal", Stage == 2 ? "crossing" : "courtyard");
            Add("seal", LevelRoomKind.Mechanism, 5, "supply", "south");
            Add("supply", LevelRoomKind.Safe, 4, "boss", "seal");
            Add("boss", LevelRoomKind.Boss, 6, null, "supply");
            Add("shop", LevelRoomKind.Shop, 4, null, "north"); Find("north").Branch = "shop";
            if (Stage < 6) { Add("challenge", LevelRoomKind.Challenge, Stage == 2 ? 2 : 4, null, "crossing"); Find("crossing").Branch = "challenge"; }
            if (Stage == 2 || Stage == 5)
            {
                Add("bypass", LevelRoomKind.Combat, 3, Stage == 5 ? "north" : "crossing", "courtyard"); Find("courtyard").Branch = "bypass";
            }
            else Find("courtyard").Branch = "south";
            if (Stage == 6) { Add("prepare", LevelRoomKind.Safe, 4, null, "supply"); Find("supply").Branch = "prepare"; }
        }
        public LevelRoomPlan Find(string id) => Rooms.Find(room => room.Id == id);
        void Add(string id, LevelRoomKind kind, int layout, string next, string back)
        {
            Rect[][] templates =
            {
                new[] { R(-3, 2, 3, 1), R(3, -2, 3, 1), R(0, 0, 1, 2) },
                new[] { R(-3, 3, 1, 4), R(3, -3, 1, 4), R(2, 3, 2, 1) },
                new[] { R(-4, 1.5f, 2, 1), R(0, -2, 2, 1), R(4, 2, 2, 1), R(4, -3, 1, 2) },
                new[] { R(-3, 1, 1, 5), R(3, 1, 1, 5), R(0, 3, 2, 1) },
                new Rect[0], new[] { R(0, 0, 2, 2), R(-4, 3.5f, 2, 1), R(4, -3.5f, 2, 1) },
                new[] { R(-5, 3, 1, 2), R(5, -3, 1, 2) }
            };
            Rect[][] workshop = {
                new[] { R(-4, 2, 1, 4), R(1, -2, 4, 1), R(5, 3, 2, 1) },
                new[] { R(-3, -1, 1, 5), R(2, 3, 4, 1), R(4, -3, 1, 2) },
                new[] { R(-4, 3, 3, 1), R(0, 0, 1, 3), R(4, -3, 3, 1) },
                new[] { R(-4, -1, 3, 1), R(1, 2, 1, 4), R(5, -2, 1, 3) },
                new Rect[0], new[] { R(-2, 2, 2, 2), R(2, -2, 2, 2) }, new Rect[0]
            };
            Rect[][] reservoir = {
                new[] { R(-3, 1, 2, 3), R(3, -1, 2, 3) },
                new[] { R(-4, 3, 4, 1), R(1, -2, 1, 4), R(4, 3, 1, 2) },
                new[] { R(-3, -2, 1, 4), R(0, 3, 3, 1), R(4, -1, 2, 2) },
                new[] { R(-4, 2, 2, 2), R(0, -2, 3, 1), R(4, 1, 1, 4) },
                new Rect[0], new[] { R(0, 0, 2, 3), R(-4, 3, 2, 1) }, new[] { R(0, 3, 1, 1.5f), R(0, -3, 1, 1.5f) }
            };
            Rect[][] greenhouse = {
                new[] { R(-3, 2, 2, 2), R(3, -2, 2, 2), R(0, 0, 1, 1) },
                new[] { R(-3, 0, 1, 4), R(3, 0, 1, 4), R(0, 4, 2, 1) },
                new[] { R(-4, 2, 2, 1), R(0, -2, 2, 1), R(4, 2, 2, 1) },
                new[] { R(-3, -2, 2, 2), R(0, 2, 2, 2), R(4, -1, 1, 3) },
                new Rect[0], new[] { R(0, 0, 2, 2) }, new Rect[0] };
            Rect[][] rail = {
                new[] { R(-3, 3, 1, 3), R(3, -3, 1, 3) },
                new[] { R(-4, 2, 5, 1), R(3, -2, 5, 1) },
                new[] { R(0, 0, 1, 4), R(-4, 3, 2, 1), R(4, -3, 2, 1) },
                new[] { R(-4, 0, 1, 4), R(4, 0, 1, 4), R(0, 4, 4, 1) },
                new Rect[0], new[] { R(-3, 2, 2, 1), R(3, -2, 2, 1) }, new Rect[0] };
            Rect[][] sanctum = {
                new[] { R(-3, 2, 1, 2), R(3, 2, 1, 2), R(0, -2, 2, 1) },
                new[] { R(-4, -1, 1, 3), R(4, -1, 1, 3), R(0, 3, 3, 1) },
                new[] { R(-3, 0, 2, 1), R(3, 0, 2, 1), R(0, 4, 1, 2) },
                new[] { R(-3, -3, 1, 2), R(3, -3, 1, 2), R(0, 1, 2, 2) },
                new Rect[0], new[] { R(-3, 0, 1, 3), R(3, 0, 1, 3) }, new Rect[0] };
            var plan = new LevelRoomPlan { Id = id, Stage = Stage, Next = next, Back = back, Kind = kind, Cover = (Stage == 1 ? templates : Stage == 2 ? workshop : Stage == 3 ? reservoir : Stage == 4 ? greenhouse : Stage == 5 ? rail : sanctum)[layout], Enemies = new Vector2[0] };
            if (kind == LevelRoomKind.Combat || kind == LevelRoomKind.Beacon || kind == LevelRoomKind.Challenge)
            {
                // Combat stream is independent of future visual/reward random streams.
                var random = new System.Random(unchecked(Seed * 397 ^ layout * 7919));
                var candidates = new List<Vector2>();
                foreach (var p in new[] { new Vector2(-6, 4), new Vector2(-1, 5), new Vector2(2, 5), new Vector2(6, 4), new Vector2(8, 0), new Vector2(7, -4), new Vector2(0, -5), new Vector2(-5, 1) })
                    if (IsClear(plan, p, .55f)) candidates.Add(p);
                for (int i = candidates.Count - 1; i > 0; i--) { int j = random.Next(i + 1); (candidates[i], candidates[j]) = (candidates[j], candidates[i]); }
                int count = kind == LevelRoomKind.Challenge ? 6 : 4;
                if ((Stage == 3 || Stage == 4 || Stage == 6) && kind == LevelRoomKind.Challenge) count = 0;
                if (Stage == 6 && kind != LevelRoomKind.Challenge) count = kind == LevelRoomKind.Beacon ? 3 : 2;
                plan.Enemies = candidates.GetRange(0, Math.Min(count, candidates.Count)).ToArray();
            }
            Rooms.Add(plan);
        }
        public static Rect R(float x, float y, float width, float height) => new Rect(x - width / 2, y - height / 2, width, height);
        public static bool IsClear(LevelRoomPlan room, Vector2 point, float radius)
        {
            if (point.x <= -10.6f || point.x >= 10.6f || point.y <= -6.6f || point.y >= 6.6f) return false;
            foreach (var cover in room.Cover) if (new Rect(cover.xMin - radius, cover.yMin - radius, cover.width + radius * 2, cover.height + radius * 2).Contains(point)) return false;
            return true;
        }
    }
    public sealed class FirstLevelProgress
    {
        readonly int stage;
        public FirstLevelProgress(int theme = 1) { stage = theme; }
        public int Required => stage == 4 || stage == 5 ? 3 : 2;
        readonly HashSet<string> cleared = new HashSet<string>(), claimed = new HashSet<string>(), beacons = new HashSet<string>();
        public bool IsClear(string room) => cleared.Contains(room);
        public bool Clear(string room) => cleared.Add(room);
        public bool Claim(string id) => claimed.Add(id);
        public bool IsClaimed(string id) => claimed.Contains(id);
        public bool Activate(string room) {
            if (room != "north" && room != "south" && !(Required == 3 && room == "middle") || !IsClear(room)) return false;
            if (stage == 5 && (room == "middle" && !beacons.Contains("north") || room == "south" && !beacons.Contains("middle"))) return false;
            return beacons.Add(room);
        }
        public int Beacons => beacons.Count;
        public bool CanEnterBoss => Beacons == Required && IsClear("seal");
    }
}
