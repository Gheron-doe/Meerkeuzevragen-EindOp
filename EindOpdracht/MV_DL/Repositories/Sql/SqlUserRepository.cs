using MV_BL.Domain;
using MV_BL.Interfaces;
using Microsoft.Data.SqlClient;

namespace MV_DL.Repositories.Sql
{
    public class SqlUserRepository : IUserRepository
    {
        private readonly DbConnectionFactory _dbfactory;
        public SqlUserRepository(DbConnectionFactory factory)
        {
            _dbfactory = factory;
        }
        public IReadOnlyList<User> GetAll()
        {
            var list = new List<User>();
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand("SELECT Id, Username FROM [User] ORDER BY Username", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(Read(reader));
            return list;
        }
        public User? GetById(int id) => 
            Single("SELECT Id, Username FROM [User] WHERE Id = @P", "@P", id);

        public User? GetByUsername(string username) =>
            Single("SELECT Id, Username FROM [User] WHERE Username = @P", "@P", username);
        public int Add(User user)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(
                "INSERT INTO [User] (Username) OUTPUT INSERTED.Id VALUES (@U)", conn);
            cmd.Parameters.AddWithValue("@U", user.Username);
            return (int)cmd.ExecuteScalar();
        }

        private static User Read(SqlDataReader reader) => new()
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1)
        };
        public User? Single(string sql, string paramName, object value)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue(paramName, value);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        }
    }
}
