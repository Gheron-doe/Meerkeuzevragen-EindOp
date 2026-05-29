using Microsoft.Data.SqlClient;
using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System.Data;

namespace MV_DL.Repositories.Sql;

public class SqlTopicRepository : ITopicRepository
{
    private readonly string _connectionString;

    public SqlTopicRepository(string connectionString) => _connectionString = connectionString;

    public List<Topic> GetAll()
    {
        var topics = new List<Topic>();
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name, IsFlagged FROM Topic WHERE IsFlagged = 0 ORDER BY Name";
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        topics.Add(new Topic((int)reader["Id"], (string)reader["Name"], (bool)reader["IsFlagged"]));
                }
            }
        }
        return topics;
    }

    public Topic? GetById(int id)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name, IsFlagged FROM Topic WHERE Id = @Id AND IsFlagged = 0";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return new Topic((int)reader["Id"], (string)reader["Name"], (bool)reader["IsFlagged"]);
                }
            }
        }
        return null;
    }

    public Topic? GetByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name, IsFlagged FROM Topic WHERE Name = @Name AND IsFlagged = 0";
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = name;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return new Topic((int)reader["Id"], (string)reader["Name"], (bool)reader["IsFlagged"]);
                }
            }
        }
        return null;
    }

    public void Add(Topic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Topic (Name, IsFlagged) OUTPUT INSERTED.Id VALUES (@Name, @IsFlagged)";
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = topic.Name;
                cmd.Parameters.Add("@IsFlagged", SqlDbType.Bit).Value = topic.IsFlagged;
                topic.Id = (int)cmd.ExecuteScalar();
            }
        }
    }

    public void Update(Topic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE Topic SET Name = @Name, IsFlagged = @IsFlagged WHERE Id = @Id";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = topic.Id;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = topic.Name;
                cmd.Parameters.Add("@IsFlagged", SqlDbType.Bit).Value = topic.IsFlagged;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
