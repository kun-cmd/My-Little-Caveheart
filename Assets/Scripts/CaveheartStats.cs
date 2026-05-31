using System;

namespace MyLittleCaveheart
{
    [Serializable]
    public struct CaveheartStats
    {
        public int awake;
        public int trust;
        public int stress;

        public CaveheartStats(int awake, int trust, int stress)
        {
            this.awake = awake;
            this.trust = trust;
            this.stress = stress;
        }

        public static CaveheartStats Starting => new CaveheartStats(0, 1, 1);

        public CaveheartStats Clamped()
        {
            return new CaveheartStats(
                Math.Max(0, Math.Min(8, awake)),
                Math.Max(0, Math.Min(8, trust)),
                Math.Max(0, Math.Min(8, stress)));
        }
    }
}
