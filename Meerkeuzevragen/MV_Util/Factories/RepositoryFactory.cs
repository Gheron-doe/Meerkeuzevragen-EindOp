using MV_BL.Interfaces;
using MV_DL.Repositories;
using System.Text.Json;

namespace MV_Util.Factories;

public static class RepositoryFactory
{
    public static IRepositoryFactory Create(string dbType, string connectionString)
        => (dbType ?? string.Empty).ToUpperInvariant() switch
        {
            "SQL" => new SqlRepositoryFactory(connectionString),
            _ => throw new InvalidOperationException($"Unknown DatabaseType '{dbType}'.")
        };
}
