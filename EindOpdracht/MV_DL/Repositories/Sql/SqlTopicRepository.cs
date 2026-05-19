using Microsoft.Data.SqlClient;
using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_DL.Repositories.Sql
{
    public class SqlTopicRepository : ITopicRepository
    {
        private readonly DbConnectionFactory _dbfactory;
        public SqlTopicRepository(DbConnectionFactory factory)
        {
            _dbfactory = factory;
        }
        public IReadOnlyList<Topic> GetAll(bool includeFlagged = false)
        {
            var list = new List<Topic>();
            using var conn = _dbfactory.Create();
            var sql = includeFlagged
                ? "SELECT Id, Name, Description, IsFlagged FROM Topic ORDER BY Name"
                : "SELECT Id, Name, Description, IsFlagged FROM Topic WHERE IsFlagged = 0 ORDER BY Name";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Topic
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                    IsFlagged = reader.GetBoolean(3)
                });
            }
            return list;
        }
        public Topic? GetById(int id)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(
                "SELECT Id, Name, Description, IsFlagged FROM Topic WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return new Topic
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                IsFlagged = reader.GetBoolean(3)
            };
        }

        public int Add(Topic topic)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(
                "INSERT INTO Topic (Name, Description, IsFlagged) " +
                "OUTPUT INSERTED.Id VALUES (@Name, @Description, @IsFlagged)", conn);
            cmd.Parameters.AddWithValue("@Name", topic.Name);
            cmd.Parameters.AddWithValue("@Description", (object?)topic.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsFlagged", topic.IsFlagged);
            return (int)cmd.ExecuteScalar();
        }

        public void Update(Topic topic)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(
                "UPDATE Topic SET Name = @Name, Description = @Description, IsFlagged = @IsFlagged WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Name", topic.Name);
            cmd.Parameters.AddWithValue("@Description", (object?)topic.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsFlagged", topic.IsFlagged);
            cmd.Parameters.AddWithValue("@Id", topic.Id);
            cmd.ExecuteNonQuery();
        }
    }
}
