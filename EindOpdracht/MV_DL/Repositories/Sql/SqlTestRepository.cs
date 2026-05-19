using Microsoft.Data.SqlClient;
using MV_BL.Domain;
using MV_BL.Helpers;
using MV_BL.Interfaces;

namespace MV_DL.Repositories
{
    public class SqlTestRepository : ITestRepository
    {
        private readonly DbConnectionFactory _dbfactory;
        private readonly IQuestionRepository _questionRepo;
        public SqlTestRepository(DbConnectionFactory factory, IQuestionRepository questionRepo)
        {
            _dbfactory = factory;
            _questionRepo = questionRepo;
        }
        public IReadOnlyList<Test> GetAll()
        {
            var list = new List<Test>();
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(
                "SELECT Id, TopicId, Title, CreatedAt, Difficulty, ScoringStrategy, IsFlagged " +
                "FROM Test WHERE IsFlagged = 0 ORDER BY CreatedAt DESC", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(ReadTest(reader));
            return list;
        }

        public Test? GetById(int id)
        {
            using var conn = _dbfactory.Create();
            Test? test = null;

            using (var cmd = new SqlCommand(
                "SELECT Id, TopicId, Title, CreatedAt, Difficulty, ScoringStrategy, IsFlagged " +
                "FROM Test WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read()) test = ReadTest(reader);
            }
            if (test is null) return null;

            using (var cmd = new SqlCommand(
                "SELECT Id, TestId, QuestionId, QuestionOrder, AnswerDisplayOrder " +
                "FROM TestQuestion WHERE TestId = @TestId ORDER BY QuestionOrder", conn))
            {
                cmd.Parameters.AddWithValue("@TestId", id);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    test.Questions.Add(new TestQuestion
                    {
                        Id = reader.GetInt32(0),
                        TestId = reader.GetInt32(1),
                        QuestionId = reader.GetInt32(2),
                        QuestionOrder = reader.GetInt32(3),
                        AnswerDisplayOrder = AnswerOrderSerializer.Deserialize(reader.GetString(4))
                    });
                }
            }

            foreach (var tq in test.Questions)
                tq.Question = _questionRepo.GetById(tq.QuestionId);

            return test;
        }

        public int Add(Test test)
        {
            using var conn = _dbfactory.Create();
            using var tx = conn.BeginTransaction();
            try
            {
                int newId;
                using (var cmd = new SqlCommand(
                    "INSERT INTO Test (TopicId, Title, CreatedAt, Difficulty, ScoringStrategy, IsFlagged) " +
                    "OUTPUT INSERTED.Id VALUES (@TopicId, @Title, @CreatedAt, @Difficulty, @ScoringStrategy, @IsFlagged)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@TopicId", test.TopicId);
                    cmd.Parameters.AddWithValue("@Title", test.Title);
                    cmd.Parameters.AddWithValue("@CreatedAt", test.CreatedAt);
                    cmd.Parameters.AddWithValue("@Difficulty", (object?)test.Difficulty ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ScoringStrategy", (int)test.ScoringStrategy);
                    cmd.Parameters.AddWithValue("@IsFlagged", test.IsFlagged);
                    newId = (int)cmd.ExecuteScalar();
                }

                foreach (var tq in test.Questions)
                {
                    using var cmd = new SqlCommand(
                        "INSERT INTO TestQuestion (TestId, QuestionId, QuestionOrder, AnswerDisplayOrder) " +
                        "OUTPUT INSERTED.Id VALUES (@TestId, @QuestionId, @QuestionOrder, @AnswerDisplayOrder)", conn, tx);
                    cmd.Parameters.AddWithValue("@TestId", newId);
                    cmd.Parameters.AddWithValue("@QuestionId", tq.QuestionId);
                    cmd.Parameters.AddWithValue("@QuestionOrder", tq.QuestionOrder);
                    cmd.Parameters.AddWithValue("@AnswerDisplayOrder", AnswerOrderSerializer.Serialize(tq.AnswerDisplayOrder));
                    tq.Id = (int)cmd.ExecuteScalar();
                    tq.TestId = newId;
                }

                tx.Commit();
                return newId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public void Deactivate(int id)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand("UPDATE Test SET IsFlagged = 1 WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

 

        private static Test ReadTest(SqlDataReader r) => new Test
        {
            Id = r.GetInt32(0),
            TopicId = r.GetInt32(1),
            Title = r.GetString(2),
            CreatedAt = r.GetDateTime(3),
            Difficulty = r.IsDBNull(4) ? null : r.GetInt32(4),
            ScoringStrategy = (ScoringMode)r.GetInt32(5),
            IsFlagged = r.GetBoolean(6)
        };
    }
}
