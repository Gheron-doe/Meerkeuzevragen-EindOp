using MV_BL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV_Util.Registries
{
    public class SimplePercentScoring : IScoringStrategy
    {
        public ScoringMode Mode => ScoringMode.SimplePercent;

        public double Calculate(int correctCount, int totalCount)
        {
            if (totalCount == 0) return 0;
            return Math.Round(100.0 * correctCount / totalCount, 1);
        }
    }
}
