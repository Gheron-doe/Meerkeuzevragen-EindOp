using Microsoft.Data.SqlClient;
using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System.Data;

namespace MV_DL.Repositories.Sql;

public class SqlAppUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public SqlAppUserRepository(string connectionString) => _connectionString = connectionString;

    public List<AppUser> GetAll()
    {
        var users = new List<AppUser>();
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Username FROM AppUser ORDER BY Username";
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        users.Add(new AppUser((int)reader["Id"], (string)reader["Username"]));
                }
            }
        }
        return users;
    }

    public AppUser? GetById(int id)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Username FROM AppUser WHERE Id = @Id";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return new AppUser((int)reader["Id"], (string)reader["Username"]);
                }
            }
        }
        return null;
    }

    public AppUser? GetByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Username FROM AppUser WHERE Username = @Username";
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = username;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return new AppUser((int)reader["Id"], (string)reader["Username"]);
                }
            }
        }
        return null;
    }

    public void Add(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO AppUser (Username) OUTPUT INSERTED.Id VALUES (@Username)";
                cmd.Parameters.Add("@Username", SqlDbType.NVarChar, 100).Value = user.Username;
                user.Id = (int)cmd.ExecuteScalar();
            }
        }
    }
}
