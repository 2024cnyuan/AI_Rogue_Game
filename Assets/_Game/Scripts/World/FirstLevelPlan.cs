using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfall
{
    public enum LevelRoomKind { Safe, Combat, Beacon, Mechanism, Challenge, Boss }
    public sealed class LevelRoomPlan
    {
        public string Id, Next, Back;
        public LevelRoomKind Kind;
        public Rect[] Cover;
        public Vector2[] Enemies;
    }
    public sealed class FirstLevelPlan
    {
        public readonly int Seed;
        public readonly List<LevelRoomPlan> Rooms = new List<LevelRoomPlan>();
        public FirstLevelPlan(int seed)
        {
            Seed = seed;
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
            var plan = new LevelRoomPlan { Id = id, Next = next, Back = back, Kind = kind, Cover = templates[layout], Enemies = new Vector2[0] };
            if (kind == LevelRoomKind.Combat || kind == LevelRoomKind.Beacon || kind == LevelRoomKind.Challenge)
            {
                // Combat stream is independent of future visual/reward random streams.
                var random = new System.Random(unchecked(Seed * 397 ^ layout * 7919));
                var candidates = new List<Vector2>();
                foreach (var p in new[] { new Vector2(-6, 4), new Vector2(-1, 5), new Vector2(2, 5), new Vector2(6, 4), new Vector2(8, 0), new Vector2(7, -4), new Vector2(0, -5), new Vector2(-5, 1) })
                    if (IsClear(plan, p, .55f)) candidates.Add(p);
                for (int i = candidates.Count - 1; i > 0; i--) { int j = random.Next(i + 1); (candidates[i], candidates[j]) = (candidates[j], candidates[i]); }
                plan.Enemies = candidates.GetRange(0, kind == LevelRoomKind.Challenge ? 6 : 4).ToArray();
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
        readonly HashSet<string> cleared = new HashSet<string>(), claimed = new HashSet<string>(), beacons = new HashSet<string>();
        public bool IsClear(string room) => cleared.Contains(room);
        public bool Clear(string room) => cleared.Add(room);
        public bool Claim(string id) => claimed.Add(id);
        public bool IsClaimed(string id) => claimed.Contains(id);
        public bool Activate(string room) => (room == "north" || room == "south") && IsClear(room) && beacons.Add(room);
        public int Beacons => beacons.Count;
        public bool CanEnterBoss => Beacons == 2 && IsClear("seal");
    }
}
