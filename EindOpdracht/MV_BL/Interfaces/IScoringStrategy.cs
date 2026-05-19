using MV_BL.Domain;

namespace MV_BL.Interfaces;

public enum ScoringMode
{
	SimplePercent
}

public interface IScoringStrategy
{
	ScoringMode Mode { get; }
    double Calculate(int correctCount, int totalCount);
}
