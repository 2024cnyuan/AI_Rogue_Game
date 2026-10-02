using System;
using System.Diagnostics;
using System.Globalization;

namespace Starfall
{
    public sealed class LevelTimer
    {
        readonly Func<double> now;
        double anchor, accumulated;
        bool started, stopped, excluded;
        public LevelTimer(Func<double> clock = null) { now = clock ?? (() => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency); }
        public double Seconds => accumulated + (started && !stopped && !excluded ? Math.Max(0, now() - anchor) : 0);
        public long Milliseconds => (long)Math.Floor(Seconds * 1000);
        public void Start() { if (started) return; started = true; anchor = now(); }
        public void Exclude(bool value)
        {
            if (value == excluded || stopped) return;
            if (started && !excluded) accumulated += Math.Max(0, now() - anchor);
            excluded = value; anchor = now();
        }
        public long Stop()
        {
            if (!stopped) { accumulated = Seconds; stopped = true; }
            return Milliseconds;
        }
        public static string Format(long milliseconds)
        {
            milliseconds = Math.Max(0, milliseconds);
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}.{2:000}", milliseconds / 60000, milliseconds / 1000 % 60, milliseconds % 1000);
        }
    }
}
