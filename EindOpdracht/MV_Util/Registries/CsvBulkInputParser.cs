using MV_BL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV_Util.Registries
{
    public class CsvBulkInputParser : IBulkInputParser
    {
        public string Format => "csv";

        public IEnumerable<BulkRow> Parse(string filePath)
        {
            bool first = true;
            foreach (var line in File.ReadLines(filePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                if (first)
                {
                    first = false;
                    // Skip header
                    if (trimmed.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                var parts = trimmed.Split(',', 2);
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0].Trim(), out int userId)) continue;
                yield return new BulkRow(userId, parts[1].Trim().ToUpperInvariant());
            }
        }
    }
}
