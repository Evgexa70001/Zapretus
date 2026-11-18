using System.Linq;

namespace Zapretus {
    internal sealed class ProbeItem {
        public string Name, Detail;
        public bool Passed, Skipped;
        public int Weight;
        public long Bytes, Milliseconds;
    }
    internal sealed class ProbeResult {
        public ProbeItem[] Items;
        public int Score { get { return Items.Where(x => x.Passed && !x.Skipped).Sum(x => x.Weight); } }
        public bool AllPassed { get { return Items.Where(x => !x.Skipped).All(x => x.Passed); } }
        public bool Improves(ProbeResult baseline) {
            return Score > baseline.Score && Preserves(baseline);
        }
        public bool Preserves(ProbeResult baseline) {
            return baseline.Items.Where(x => x.Passed && !x.Skipped)
                .All(x => Items.Any(y => y.Name == x.Name && y.Passed && !y.Skipped));
        }
        public string Summary { get { return Items.Count(x => x.Passed && !x.Skipped) + "/" + Items.Count(x => !x.Skipped) + ", баллы " + Score; } }
    }
}
