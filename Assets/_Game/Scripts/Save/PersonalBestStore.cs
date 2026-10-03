using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Starfall
{
    public enum ClearFeedback { First, Improved, Matched, Close, Cleared, Failed, Practice }
    [Serializable] public sealed class BestRecord
    {
        public string stageId, difficulty = "standard", timingVersion = "1", balanceVersion = "1", attemptId, date, mode;
        public long milliseconds;
        public int seed;
        public string[] equipment;
        public string Group => stageId + "/" + difficulty + "/" + timingVersion + "/" + balanceVersion + "/" + (string.IsNullOrEmpty(mode) ? "campaign" : mode);
    }
    [Serializable] public sealed class RecordBook
    {
        public int version = 1;
        public List<BestRecord> bests = new List<BestRecord>();
        public List<string> receipts = new List<string>();
    }
    public sealed class ClearResult
    {
        public BestRecord Attempt;
        public long? Previous;
        public ClearFeedback Feedback;
        public bool Saved, Eligible, Success;
        public long Delta => Previous.HasValue ? Math.Abs(Attempt.milliseconds - Previous.Value) : 0;
    }
    public sealed class PersonalBestStore
    {
        readonly string path;
        readonly Func<string, string, bool> writer;
        public RecordBook Book { get; private set; } = new RecordBook();
        public bool ReadProblem { get; private set; }
        public PersonalBestStore(string directory, Func<string, string, bool> save = null)
        {
            path = Path.Combine(directory, "starfall-records.json"); writer = save ?? AtomicWrite;
            if (!File.Exists(path)) return;
            Book = Read(path);
            if (Book != null) return;
            ReadProblem = true;
            try { File.Copy(path, path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            Book = Read(path + ".bak") ?? new RecordBook();
        }
        static RecordBook Read(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var book = JsonUtility.FromJson<RecordBook>(File.ReadAllText(file));
                if (book == null || book.version != 1 || book.bests == null || book.receipts == null) return null;
                var groups = new HashSet<string>();
                foreach (var best in book.bests)
                    if (best == null || string.IsNullOrEmpty(best.stageId) || string.IsNullOrEmpty(best.attemptId) || best.milliseconds < 0 || best.equipment == null || !groups.Add(best.Group)) return null;
                return book;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException) { return null; }
        }
        public BestRecord Find(string stage = "gardens", string timing = "1", string balance = "1", string mode = null)
        {
            foreach (var best in Book.bests) if (best.stageId == stage && best.difficulty == "standard" && best.timingVersion == timing && best.balanceVersion == balance && (string.IsNullOrEmpty(best.mode) ? "campaign" : best.mode) == (string.IsNullOrEmpty(mode) ? "campaign" : mode)) return best;
            return null;
        }
        public ClearResult Prepare(BestRecord attempt, bool success, bool eligible)
        {
            long? previous = Find(attempt.stageId, attempt.timingVersion, attempt.balanceVersion, attempt.mode)?.milliseconds;
            return new ClearResult { Attempt = attempt, Previous = previous, Eligible = eligible, Success = success,
                Feedback = !success ? ClearFeedback.Failed : !eligible ? ClearFeedback.Practice : Compare(attempt.milliseconds, previous) };
        }
        public static ClearFeedback Compare(long time, long? previous)
        {
            if (!previous.HasValue) return ClearFeedback.First;
            if (time < previous.Value) return ClearFeedback.Improved;
            if (time == previous.Value) return ClearFeedback.Matched;
            double threshold = Math.Max(1000, Math.Min(10000, previous.Value * .05));
            return time - previous.Value <= threshold ? ClearFeedback.Close : ClearFeedback.Cleared;
        }
        public bool Commit(ClearResult result)
        {
            if (result == null || !result.Success || !result.Eligible || result.Attempt.milliseconds < 0 || string.IsNullOrEmpty(result.Attempt.attemptId)) return false;
            if (result.Saved || Book.receipts.Contains(result.Attempt.attemptId)) { result.Saved = true; return true; }
            var next = JsonUtility.FromJson<RecordBook>(JsonUtility.ToJson(Book));
            var old = Find(result.Attempt.stageId, result.Attempt.timingVersion, result.Attempt.balanceVersion, result.Attempt.mode);
            if (old == null || result.Attempt.milliseconds < old.milliseconds)
            {
                next.bests.RemoveAll(best => best.Group == result.Attempt.Group); next.bests.Add(result.Attempt);
            }
            next.receipts.Add(result.Attempt.attemptId);
            if (!writer(path, JsonUtility.ToJson(next, true))) return false;
            Book = next; result.Saved = true; return true;
        }
        static bool AtomicWrite(string file, string content)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                using (var stream = new FileStream(file + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                using (var output = new StreamWriter(stream)) { output.Write(content); output.Flush(); stream.Flush(true); }
                if (File.Exists(file)) File.Replace(file + ".tmp", file, file + ".bak"); else File.Move(file + ".tmp", file);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException) { return false; }
        }
    }
}
