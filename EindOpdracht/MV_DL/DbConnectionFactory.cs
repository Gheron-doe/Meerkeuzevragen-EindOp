using Microsoft.Data.SqlClient;

namespace MV_DL;

public class DbConnectionFactory
{
	private readonly DatabaseConfig _config;

	public DbConnectionFactory(DatabaseConfig config)
	{
		_config = config;
	}

	public SqlConnection Create()
	{
		var conn = new SqlConnection(_config.ConnectionString);
		conn.Open();
		return conn;
	}
}
