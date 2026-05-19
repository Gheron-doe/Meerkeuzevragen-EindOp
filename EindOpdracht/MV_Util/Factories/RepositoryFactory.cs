using MV_BL.Interfaces;
using MV_DL.Repositories;
using System.Text.Json;

namespace MV_Util.Factories
{
    public class RepositoryFactory
    {
        private readonly Dictionary<string, Func<string, IRepositoryFactory>> _providers
            = new(StringComparer.OrdinalIgnoreCase);

        public RepositoryFactory()
        {
            Register("SQL", connStr => new SqlRepositoryFactory(connStr));
        }

        public void Register(string dbType, Func<string, IRepositoryFactory> creator)
            => _providers[dbType] = creator;

        public IRepositoryFactory Create(string dbType, string connectionString)
        {
            if (_providers.TryGetValue(dbType, out var creator))
                return creator(connectionString);

            throw new InvalidOperationException(
                $"No repository provider registered for DatabaseType '{dbType}'. " +
                $"Registered types: {string.Join(", ", _providers.Keys)}. " +
                $"Call Register() before CreateFromSettings() to add a new provider.");
        }

        public IRepositoryFactory CreateFromSettings(string appsettingsPath)
        {
            var json = File.ReadAllText(appsettingsPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var dbType = root
                .GetProperty("AppSettings")
                .GetProperty("databaseType")
                .GetString()
                ?? throw new InvalidOperationException("'AppSettings.databaseType' is null in appsettings.json.");

            var connStrings = root.GetProperty("ConnectionStrings");
            var connStr = dbType.ToUpperInvariant() switch
            {
                "SQL" => connStrings.GetProperty("SQLServerConnection").GetString(),
                _ => throw new InvalidOperationException(
                        $"No ConnectionStrings entry mapped for DatabaseType '{dbType}'. " +
                        $"Add a case in RepositoryFactory.CreateFromSettings().")
            } ?? throw new InvalidOperationException($"Connection string for '{dbType}' is null in appsettings.json.");

            return Create(dbType, connStr);
        }
    }
}

