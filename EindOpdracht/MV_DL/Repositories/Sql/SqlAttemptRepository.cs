using Microsoft.Data.SqlClient;
using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_DL.Repositories.Sql
{
    public class SqlAttemptRepository : IAttemptRepository
    {
        private readonly DbConnectionFactory _dbfactory;
        public SqlAttemptRepository(DbConnectionFactory dbFactory)
        {
            _dbfactory = dbFactory;
        }

        public IReadOnlyList<TestAttempt> GetAll()
        => QueryList("SELECT Id, TestId, UserId, StartedAt, CompletedAt, Score, Feedback FROM TestAttempt ORDER BY StartedAt DESC");

        public IReadOnlyList<TestAttempt> GetByUser(int userId)
            => QueryList("SELECT Id, TestId, UserId, StartedAt, CompletedAt, Score, Feedback FROM TestAttempt WHERE UserId = @P ORDER BY StartedAt DESC", "@P", userId);

        public IReadOnlyList<TestAttempt> GetByTest(int testId)
            => QueryList("SELECT Id, TestId, UserId, StartedAt, CompletedAt, Score, Feedback FROM TestAttempt WHERE TestId = @P ORDER BY StartedAt DESC", "@P", testId);

        public TestAttempt? GetById(int id)
        {
            var list = QueryList("SELECT Id, TestId, UserId, StartedAt, CompletedAt, Score, Feedback FROM TestAttempt WHERE Id = @P", "@P", id);
            if (list.Count == 0) return null;

            var attempt = list[0];
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(
                "SELECT Id, AttemptId, TestQuestionId, SelectedAnswerId, IsCorrect FROM AttemptAnswer WHERE AttemptId = @Id ORDER BY TestQuestionId", conn);
            cmd.Parameters.AddWithValue("@Id", attempt.Id);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                attempt.Answers.Add(new AttemptAnswer
                {
                    Id = reader.GetInt32(0),
                    AttemptId = reader.GetInt32(1),
                    TestQuestionId = reader.GetInt32(2),
                    SelectedAnswerId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    IsCorrect = reader.GetBoolean(4)
                });
            }
            return attempt;
        }

        public int StartAttempt(TestAttempt attempt)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(
                "INSERT INTO TestAttempt (TestId, UserId, StartedAt) OUTPUT INSERTED.Id VALUES (@TestId, @UserId, @StartedAt)", conn);
            cmd.Parameters.AddWithValue("@TestId", attempt.TestId);
            cmd.Parameters.AddWithValue("@UserId", attempt.UserId);
            cmd.Parameters.AddWithValue("@StartedAt", attempt.StartedAt);
            return (int)cmd.ExecuteScalar();
        }

        public void Complete(int attemptId, DateTime completedAt, int correctCount, IEnumerable<AttemptAnswer> answers)
        {
            using var conn = _dbfactory.Create();
            using var tx = conn.BeginTransaction();
            try
            {
                using (var cmd = new SqlCommand(
                    "UPDATE TestAttempt SET CompletedAt = @CompletedAt, Score = @Score WHERE Id = @Id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@CompletedAt", completedAt);
                    cmd.Parameters.AddWithValue("@Score", correctCount);
                    cmd.Parameters.AddWithValue("@Id", attemptId);
                    cmd.ExecuteNonQuery();
                }

                foreach (var a in answers)
                {
                    using var ac = new SqlCommand(
                        "INSERT INTO AttemptAnswer (AttemptId, TestQuestionId, SelectedAnswerId, IsCorrect) " +
                        "VALUES (@AttemptId, @TestQuestionId, @SelectedAnswerId, @IsCorrect)", conn, tx);
                    ac.Parameters.AddWithValue("@AttemptId", attemptId);
                    ac.Parameters.AddWithValue("@TestQuestionId", a.TestQuestionId);
                    ac.Parameters.AddWithValue("@SelectedAnswerId", (object?)a.SelectedAnswerId ?? DBNull.Value);
                    ac.Parameters.AddWithValue("@IsCorrect", a.IsCorrect);
                    ac.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public void UpdateFeedback(int attemptId, string? feedback)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand("UPDATE TestAttempt SET Feedback = @Feedback WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Feedback", (object?)feedback ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", attemptId);
            cmd.ExecuteNonQuery();
        }


        private List<TestAttempt> QueryList(string sql, string? paramName = null, object? value = null)
        {
            var list = new List<TestAttempt>();
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(sql, conn);
            if (paramName is not null) cmd.Parameters.AddWithValue(paramName, value!);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TestAttempt
                {
                    Id = reader.GetInt32(0),
                    TestId = reader.GetInt32(1),
                    UserId = reader.GetInt32(2),
                    StartedAt = reader.GetDateTime(3),
                    CompletedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    Score = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    Feedback = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
            return list;
        }
    }
}
