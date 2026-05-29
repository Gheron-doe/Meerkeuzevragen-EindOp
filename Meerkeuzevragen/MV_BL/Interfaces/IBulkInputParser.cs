using MV_BL.Domain.ViewModels;

namespace MV_BL.Interfaces;

public interface IBulkInputParser
{
    string Format { get; }

    IEnumerable<BulkRow> Parse(string filePath);
}
