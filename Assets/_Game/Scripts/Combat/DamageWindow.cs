using System.Collections.Generic;

namespace Starfall
{
    // Effective game time is supplied by the mode, so explicit pauses cannot age samples.
    public sealed class DamageWindow
    {
        readonly Queue<(double time, float damage)> samples = new Queue<(double, float)>();
        double started;
        public float Last { get; private set; }
        public float Total { get; private set; }
        public float Recent { get; private set; }
        public void Record(double now, float damage) { Expire(now); Last = damage; Total += damage; Recent += damage; samples.Enqueue((now, damage)); }
        public float Dps(double now)
        {
            Expire(now); double window = System.Math.Min(10, System.Math.Max(.001, now - started)); return (float)(Recent / window);
        }
        void Expire(double now) { while (samples.Count > 0 && now - samples.Peek().time >= 10) Recent -= samples.Dequeue().damage; }
        public void Reset(double now) { samples.Clear(); Last = Total = Recent = 0; started = now; }
    }
}
