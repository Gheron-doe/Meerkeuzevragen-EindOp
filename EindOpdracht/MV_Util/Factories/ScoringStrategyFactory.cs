using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_Util.Registries;

namespace MV_Util.Factories;

public class ScoringStrategyFactory
{
    private readonly Dictionary<ScoringMode, IScoringStrategy> _strategies = new();

    public ScoringStrategyFactory()
    {
        Register(new SimplePercentScoring());
    }

    public void Register(IScoringStrategy strategy) => _strategies[strategy.Mode] = strategy;

    public IEnumerable<ScoringMode> AvailableModes => _strategies.Keys.OrderBy(m => (int)m);

    public IScoringStrategy Create(ScoringMode mode)
    {
        if (_strategies.TryGetValue(mode, out var s)) return s;
        throw new UnsupportedFormatException(mode.ToString(), nameof(ScoringStrategyFactory));
    }

    public IScoringStrategy Default => _strategies[ScoringMode.SimplePercent];
}
