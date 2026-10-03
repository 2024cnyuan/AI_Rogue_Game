using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Starfall
{
    public sealed class PrototypeRoom : MonoBehaviour
    {
        public readonly List<Rect> Walls = new List<Rect>();
        readonly bool[,] walkable = new bool[23, 15];
        readonly int[,] distance = new int[23, 15];
        readonly Vector2Int[] queue = new Vector2Int[23 * 15];
        static readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        BoxCollider2D exitBarrier;
        SpriteRenderer exitVisual;
        SpriteRenderer exitArt;
        SpriteRenderer pulse;
        public bool DoorOpen { get; private set; }
        public Vector2 Spawn => new Vector2(-8, -4);
        public Vector2 Exit => new Vector2(10.4f, 0);
        public void Build(GameMode mode = GameMode.Adventure, LevelRoomPlan plan = null)
        {
            var dark = new Color(.07f, .11f, .16f);
            PrototypeVisuals.Draw(transform, "Foundation", Vector2.zero, new Vector2(26, 18), dark, -30);
            for (int x = -11; x < 11; x++) for (int y = -7; y < 7; y++)
            {
                float tint = ((x + y) & 1) == 0 ? .015f : 0;
                PrototypeVisuals.Draw(transform, "Floor", new Vector2(x + .5f, y + .5f), Vector2.one * .97f,
                    new Color(.11f + tint, .18f + tint, .21f + tint), -20);
            }
            Wall(new Vector2(0, 7.5f), new Vector2(24, 1)); Wall(new Vector2(0, -7.5f), new Vector2(24, 1));
            Wall(new Vector2(-11.5f, 0), new Vector2(1, 14));
            Wall(new Vector2(11.5f, 4.5f), new Vector2(1, 5)); Wall(new Vector2(11.5f, -4.5f), new Vector2(1, 5));
            Wall(new Vector2(12, 0), new Vector2(1, 4));
            if (plan != null)
            {
                foreach (var cover in plan.Cover) Wall(cover.center, cover.size, plan.Stage == 4);
                Zone(Vector2.zero, new Vector2(20, 12), plan.Stage == 1 ? new Color(.13f, .24f, .2f) : plan.Stage == 2 ? new Color(.26f, .17f, .13f) : plan.Stage == 3 ? new Color(.13f, .23f, .35f) : plan.Stage == 4 ? new Color(.23f, .14f, .3f) : plan.Stage == 5 ? new Color(.1f, .17f, .28f) : new Color(.2f, .14f, .28f));
                if (plan.Stage == 5 || plan.Stage == 6) {
                    PrototypeVisuals.Draw(transform, "Horizontal aisle", Vector2.zero, new Vector2(19, 1.1f), plan.Stage == 5 ? new Color(.35f, .34f, .17f) : new Color(.47f, .43f, .38f), -16);
                    PrototypeVisuals.Draw(transform, "Vertical aisle", Vector2.zero, new Vector2(1.1f, 11), plan.Stage == 5 ? new Color(.35f, .34f, .17f) : new Color(.47f, .43f, .38f), -16);
                }
                if (plan.Stage == 3)
                {
                    // Raised walkway and island markings sit on the traversable floor.
                    PrototypeVisuals.Draw(transform, "North bridge", new Vector2(-1, 4), new Vector2(18, 1.2f), new Color(.3f, .4f, .48f), -16);
                    PrototypeVisuals.Draw(transform, "South bridge", new Vector2(1, -4), new Vector2(18, 1.2f), new Color(.3f, .4f, .48f), -16);
                    foreach (var at in new[] { new Vector2(-7, -4), new Vector2(0, 4), new Vector2(7, 0) })
                        PrototypeVisuals.Draw(transform, "Island platform", at, new Vector2(3, 2.5f), new Color(.35f, .47f, .56f), -15);
                    foreach (var cover in plan.Cover)
                        PrototypeVisuals.Draw(transform, "Reflective crystal", cover.center, new Vector2(Mathf.Min(cover.width, 1), Mathf.Min(cover.height, 1)), new Color(.6f, .84f, .96f), 3, "shooter");
                }
                if (plan.Stage == 1 && (plan.Kind == LevelRoomKind.Mechanism || plan.Id == "south" || plan.Id == "crossing"))
                {
                    pulse = PrototypeVisuals.Draw(transform, "Seal pulse warning", Vector2.zero, new Vector2(19, 1.3f), PrototypeVisuals.Gold, -10);
                    pulse.enabled = false;
                }
            }
            else if (mode == GameMode.Adventure || mode == GameMode.None)
            {
                Wall(new Vector2(-3.5f, 2.5f), new Vector2(3, 1)); Wall(new Vector2(3.5f, -2.5f), new Vector2(3, 1));
                Wall(new Vector2(-4, -2), new Vector2(1, 2)); Wall(new Vector2(4, 2), new Vector2(1, 2));
            }
            else if (mode == GameMode.Tutorial)
            {
                Wall(new Vector2(-4, 4), new Vector2(1, 3)); Wall(new Vector2(3, 1.5f), new Vector2(1, 4));
                Zone(new Vector2(-7, -4), new Vector2(6, 4), new Color(.23f, .24f, .2f));
                Zone(new Vector2(-7, 4), new Vector2(5, 4), new Color(.2f, .27f, .24f));
                Zone(new Vector2(-3, 0), new Vector2(6, 2), new Color(.25f, .2f, .17f));
                Zone(new Vector2(7, 1), new Vector2(6, 9), new Color(.2f, .2f, .27f));
            }
            else
            {
                Wall(new Vector2(-2, 4), new Vector2(1, 4)); Wall(new Vector2(3, -4), new Vector2(1, 4));
                Zone(new Vector2(-7, -4), new Vector2(6, 4), new Color(.18f, .28f, .26f));
                Zone(new Vector2(-7, 3), new Vector2(6, 5), new Color(.16f, .23f, .32f));
                Zone(new Vector2(4, 4), new Vector2(11, 4), new Color(.27f, .22f, .18f));
                Zone(new Vector2(7, -3), new Vector2(6, 6), new Color(.23f, .18f, .27f));
                Zone(new Vector2(-1, -4), new Vector2(4, 4), new Color(.16f, .25f, .3f));
            }
            for (int i = 0; i < 6; i++)
            {
                PrototypeVisuals.Draw(transform, "Route marker", new Vector2(-8 + i * 3.2f, 0), new Vector2(.3f, .3f),
                    new Color(.3f, .43f, .4f), -15, "arrow");
            }
            foreach (var p in new[] { new Vector2(-9, 5), new Vector2(-9, -5), new Vector2(9, 5), new Vector2(9, -5) })
            {
                PrototypeVisuals.Draw(transform, "Lamp base", p, new Vector2(.7f, .7f), dark, 0);
                PrototypeVisuals.Draw(transform, "Lamp", p, new Vector2(.32f, .32f), PrototypeVisuals.Gold, 1, "orb");
            }
            exitVisual = PrototypeVisuals.Draw(transform, "Exit gate", new Vector2(11.1f, 0), new Vector2(.25f, 4), PrototypeVisuals.Enemy, 3);
            exitBarrier = exitVisual.gameObject.AddComponent<BoxCollider2D>();
            Walls.Add(new Rect(11, -2, .3f, 4));
            for (int x = 0; x < 23; x++) for (int y = 0; y < 15; y++) walkable[x, y] = IsClear(new Vector2(x - 11, y - 7), .48f);
            PaintRoom(plan?.Stage ?? 1, plan);
        }
        void PaintRoom(int stage, LevelRoomPlan plan)
        {
            if (M5Art.Get("floor." + stage) == null) return;
            foreach (var r in GetComponentsInChildren<SpriteRenderer>()) {
                if (r.gameObject.name == "Floor" || r.gameObject.name == "Practice zone" || r.gameObject.name == "Route marker" || r.gameObject.name == "Wall cap" || r.gameObject.name == "Moss" || r.gameObject.name == "Lamp base" || r.gameObject.name == "Lamp") r.enabled = false;
                if (r.gameObject.name == "Stone wall") {
                    Vector3 size = r.transform.localScale; r.enabled=false;
                    var group=new GameObject("Painted cover"); group.transform.SetParent(r.transform,false); group.transform.localScale=new Vector3(1/size.x,1/size.y,1);
                    bool vertical=size.y>size.x; float length=vertical?size.y:size.x; float thickness=vertical?size.x:size.y;
                    thickness=Mathf.Max(.95f,thickness);
                    int count=Mathf.Max(1,Mathf.CeilToInt(length/2)); float segment=length/count;
                    for(int i=0;i<count;i++) {
                        Vector2 at=vertical?new Vector2(0,-length/2+segment*(i+.5f)):new Vector2(-length/2+segment*(i+.5f),0);
                        var tile=M5Art.Draw(group.transform,"wall."+stage,at,thickness,40-Mathf.RoundToInt((r.transform.position.y+at.y-thickness*.5f)*4));
                        tile.transform.localScale=new Vector3(segment/tile.sprite.bounds.size.x,thickness/tile.sprite.bounds.size.y,1);
                        if(vertical) { tile.transform.localScale=new Vector3(segment/tile.sprite.bounds.size.x,thickness/tile.sprite.bounds.size.y,1); tile.transform.localRotation=Quaternion.Euler(0,0,90); }
                        if(size.x<6 && size.y<6) { var shadow=tile.gameObject.AddComponent<ShadowCaster2D>(); shadow.selfShadows=false; shadow.castsShadows=true; }
                    }
                    PrototypeVisuals.Draw(group.transform,"Cover shadow",new Vector2(.08f,-.12f),new Vector2(size.x+.1f,size.y+.12f),new Color(0,0,0,.22f),-9);
                }
            }
            var floor=M5Art.Draw(transform,"floor."+stage,Vector2.zero,4,-19); floor.transform.localScale=Vector3.one; floor.drawMode=SpriteDrawMode.Tiled; floor.size=new Vector2(22,14);
            foreach(var at in new[]{new Vector2(-8,4),new Vector2(7,2),new Vector2(0,-5)}) M5Art.Draw(transform,"beacon",at,.65f,3);
            var landmark = M5Art.Draw(transform, "landmark." + stage, new Vector2(-9, 4.5f), 2.6f, 1);
            if (landmark != null) M5Art.Shadow(landmark.transform, 1);
            exitVisual.enabled=false; exitArt=M5Art.Draw(transform,"arch."+stage,new Vector2(11.1f,0),3.4f,46); exitArt.color=new Color(.5f,.58f,.57f);
            if (plan?.Stage == 1 && plan.Id == "courtyard") {
                M5Art.Draw(transform, "workstation", new Vector2(-9, -5.2f), 1.4f, 3);
                for(int i=0;i<7;i++) PrototypeVisuals.Draw(transform,"Courtyard guide",new Vector2(-6.3f,-5.3f+i*.4f),new Vector2(.14f,.14f),new Color(.58f,.79f,.67f,.35f),-10,"orb");
            }
        }
        void Zone(Vector2 at, Vector2 size, Color color) => PrototypeVisuals.Draw(transform, "Practice zone", at, size, color, -18);
        readonly List<DestructibleCover> mushrooms = new List<DestructibleCover>();
        public void AttachDestructibles(StarfallGame game) { foreach (var cover in mushrooms) cover.Initialize(game, this); }
        public void RemoveCover(Rect rect) { Walls.Remove(rect); RebuildNavigation(); }
        public void AddCover(Rect rect) { Walls.Add(rect); RebuildNavigation(); }
        void RebuildNavigation() { for (int x = 0; x < 23; x++) for (int y = 0; y < 15; y++) walkable[x,y] = IsClear(new Vector2(x - 11, y - 7), .48f); }
        void Wall(Vector2 at, Vector2 size, bool destructible = false)
        {
            var wall = PrototypeVisuals.Draw(transform, "Stone wall", at, size, new Color(.28f, .38f, .4f), 1);
            wall.gameObject.AddComponent<BoxCollider2D>(); Walls.Add(new Rect(at - size / 2, size));
            if (destructible) { wall.color = new Color(.55f, .42f, .7f); wall.sprite = PrototypeVisuals.Sprite("orb"); var cover = wall.gameObject.AddComponent<DestructibleCover>(); cover.Bounds = new Rect(at - size / 2, size); mushrooms.Add(cover); return; }
            PrototypeVisuals.Draw(transform, "Wall cap", at + new Vector2(0, size.y / 2 - .09f), new Vector2(size.x, .15f),
                new Color(.4f, .53f, .51f), 2);
            if (Mathf.Abs(at.x) < 6 && Mathf.Abs(at.y) < 6)
                PrototypeVisuals.Draw(transform, "Moss", at + new Vector2(0, .15f), size * .7f, new Color(.19f, .42f, .32f), 2);
        }
        public bool IsClear(Vector2 point, float radius)
        {
            foreach (var rect in Walls)
            {
                var expanded = new Rect(rect.xMin - radius, rect.yMin - radius, rect.width + 2 * radius, rect.height + 2 * radius);
                if (expanded.Contains(point)) return false;
            }
            return point.x > -10.6f && point.x < 10.6f && point.y > -6.6f && point.y < 6.6f;
        }
        public void OpenDoor() { DoorOpen = true; exitBarrier.enabled = false; exitVisual.color = PrototypeVisuals.Teal; if(exitArt!=null) exitArt.color=Color.white; }
        public void CloseDoor() { DoorOpen = false; exitBarrier.enabled = true; exitVisual.color = PrototypeVisuals.Enemy; if(exitArt!=null) exitArt.color=new Color(.5f,.58f,.57f); }
        public void SetPulse(bool visible, bool active) { if (pulse != null) { pulse.enabled = visible; pulse.color = active ? PrototypeVisuals.Enemy : PrototypeVisuals.Gold; } }
        public void RefreshNavigation(Vector2 target)
        {
            for (int x = 0; x < 23; x++) for (int y = 0; y < 15; y++) distance[x, y] = -1;
            var cell = NearestCell(target); int head = 0, tail = 0;
            queue[tail++] = cell; distance[cell.x, cell.y] = 0;
            while (head < tail)
            {
                var current = queue[head++];
                foreach (var d in directions)
                {
                    var next = current + d;
                    if (next.x < 0 || next.x >= 23 || next.y < 0 || next.y >= 15 || !walkable[next.x, next.y] || distance[next.x, next.y] >= 0) continue;
                    distance[next.x, next.y] = distance[current.x, current.y] + 1; queue[tail++] = next;
                }
            }
        }
        Vector2Int NearestCell(Vector2 point)
        {
            var best = new Vector2Int(3, 3); float score = float.MaxValue;
            for (int x = 0; x < 23; x++) for (int y = 0; y < 15; y++)
            {
                if (!walkable[x, y]) continue;
                float candidate = ((Vector2)new Vector2Int(x - 11, y - 7) - point).sqrMagnitude;
                if (candidate < score) { score = candidate; best = new Vector2Int(x, y); }
            }
            return best;
        }
        public Vector2 PathDirection(Vector2 from, Vector2 target)
        {
            if (!Physics2D.CircleCast(from, .35f, (target - from).normalized, Vector2.Distance(from, target), 1)) return (target - from).normalized;
            var current = NearestCell(from); var best = current; int score = distance[current.x, current.y];
            foreach (var d in directions)
            {
                var next = current + d;
                if (next.x < 0 || next.x >= 23 || next.y < 0 || next.y >= 15) continue;
                int value = distance[next.x, next.y];
                if (value >= 0 && (score < 0 || value < score)) { score = value; best = next; }
            }
            var waypoint = new Vector2(best.x - 11, best.y - 7);
            return (waypoint - from).sqrMagnitude < .08f ? Vector2.zero : (waypoint - from).normalized;
        }
    }
}
