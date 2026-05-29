using Microsoft.Data.SqlClient;
using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System.Data;

namespace MV_DL.Repositories.Sql;

public class SqlAttemptRepository : IAttemptRepository
{
    private readonly string _connectionString;

    public SqlAttemptRepository(string connectionString) => _connectionString = connectionString;

    public List<TestAttempt> GetAll()
        => QueryList("SELECT Id, TestId, UserId, StartedAt, CompletedAt, Feedback FROM TestAttempt ORDER BY StartedAt DESC");

    public List<TestAttempt> GetByUser(int userId)
        => QueryList("SELECT Id, TestId, UserId, StartedAt, CompletedAt, Feedback FROM TestAttempt WHERE UserId = @P ORDER BY StartedAt DESC", "@P", userId);

    public List<TestAttempt> GetByTest(int testId)
        => QueryList("SELECT Id, TestId, UserId, StartedAt, CompletedAt, Feedback FROM TestAttempt WHERE TestId = @P ORDER BY StartedAt DESC", "@P", testId);

    public TestAttempt? GetById(int id)
    {
        var list = QueryList("SELECT Id, TestId, UserId, StartedAt, CompletedAt, Feedback FROM TestAttempt WHERE Id = @P", "@P", id);
        return list.FirstOrDefault();
    }

    public void Save(TestAttempt attempt, List<AttemptAnswer> answers)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        attempt.CompletedAt = DateTime.Now;

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            if (!Exists(conn, "Test", attempt.TestId, "IsFlagged = 0"))
                throw new TestException($"Test #{attempt.TestId} does not exist.");
            if (!Exists(conn, "AppUser", attempt.UserId))
                throw new UserException($"AppUser #{attempt.UserId} does not exist.");

            using (SqlTransaction tx = conn.BeginTransaction())
            {
                try
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO TestAttempt (TestId, UserId, StartedAt, CompletedAt) " +
                                          "OUTPUT INSERTED.Id VALUES (@T, @U, @S, @C)";
                        cmd.Parameters.Add("@T", SqlDbType.Int).Value = attempt.TestId;
                        cmd.Parameters.Add("@U", SqlDbType.Int).Value = attempt.UserId;
                        cmd.Parameters.Add("@S", SqlDbType.DateTime).Value = attempt.StartedAt;
                        cmd.Parameters.Add("@C", SqlDbType.DateTime).Value = attempt.CompletedAt!.Value;
                        attempt.Id = (int)cmd.ExecuteScalar();
                    }
                    foreach (AttemptAnswer a in answers)
                        InsertAttemptAnswer(conn, tx, attempt.Id, a);
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }

    public void UpdateFeedback(int attemptId, string? feedback)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE TestAttempt SET Feedback = @F WHERE Id = @Id";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = attemptId;
                cmd.Parameters.Add("@F", SqlDbType.NVarChar, 500).Value = (object?)feedback ?? DBNull.Value;
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void BulkInsertAttempts(IEnumerable<(TestAttempt attempt, List<AttemptAnswer> answers)> batch)
    {
        var list = batch.ToList();
        if (list.Count == 0) return;

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlTransaction tx = conn.BeginTransaction())
            {
                try
                {
                    foreach ((TestAttempt attempt, List<AttemptAnswer> answers) in list)
                    {
                        if (!Exists(conn, tx, "Test", attempt.TestId, "IsFlagged = 0"))
                            throw new TestException($"Test #{attempt.TestId} does not exist.");
                        if (!Exists(conn, tx, "AppUser", attempt.UserId))
                            throw new UserException($"AppUser #{attempt.UserId} does not exist.");

                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "INSERT INTO TestAttempt (TestId, UserId, StartedAt, CompletedAt, Feedback) " +
                                              "OUTPUT INSERTED.Id VALUES (@T, @U, @S, @C, @F)";
                            cmd.Parameters.Add("@T", SqlDbType.Int).Value = attempt.TestId;
                            cmd.Parameters.Add("@U", SqlDbType.Int).Value = attempt.UserId;
                            cmd.Parameters.Add("@S", SqlDbType.DateTime).Value = attempt.StartedAt;
                            cmd.Parameters.Add("@C", SqlDbType.DateTime).Value = (object?)attempt.CompletedAt ?? DBNull.Value;
                            cmd.Parameters.Add("@F", SqlDbType.NVarChar, 500).Value = (object?)attempt.Feedback ?? DBNull.Value;
                            attempt.Id = (int)cmd.ExecuteScalar();
                        }
                        foreach (AttemptAnswer a in answers)
                            InsertAttemptAnswer(conn, tx, attempt.Id, a);
                    }
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }

    private static bool Exists(SqlConnection conn, string table, int id, string? extra = null)
        => Exists(conn, null, table, id, extra);

    private static bool Exists(SqlConnection conn, SqlTransaction? tx, string table, int id, string? extra = null)
    {
        string sql = $"SELECT 1 FROM {table} WHERE Id = @Id";
        if (!string.IsNullOrEmpty(extra)) sql += " AND " + extra;
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            return cmd.ExecuteScalar() is not null;
        }
    }

    private static void InsertAttemptAnswer(SqlConnection conn, SqlTransaction tx, int attemptId, AttemptAnswer a)
    {
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO AttemptAnswer (AttemptId, TestQuestionId, SelectedAnswerId) " +
                              "VALUES (@A, @TQ, @SA)";
            cmd.Parameters.Add("@A", SqlDbType.Int).Value = attemptId;
            cmd.Parameters.Add("@TQ", SqlDbType.Int).Value = a.TestQuestionId;
            cmd.Parameters.Add("@SA", SqlDbType.Int).Value = (object?)a.SelectedAnswerId ?? DBNull.Value;
            cmd.ExecuteNonQuery();
        }
    }

    private List<TestAttempt> QueryList(string sql, string? paramName = null, object? value = null)
    {
        var rows = new List<(int id, int testId, int userId, DateTime startedAt, DateTime? completedAt, string? feedback)>();
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                if (paramName is not null) cmd.Parameters.AddWithValue(paramName, value!);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        rows.Add((
                            (int)reader["Id"],
                            (int)reader["TestId"],
                            (int)reader["UserId"],
                            (DateTime)reader["StartedAt"],
                            reader["CompletedAt"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["CompletedAt"],
                            reader["Feedback"] == DBNull.Value ? null : (string)reader["Feedback"]
                        ));
                }
            }
        }

        var list = new List<TestAttempt>();
        foreach (var r in rows)
            list.Add(new TestAttempt(r.id, r.testId, r.userId, r.startedAt, r.completedAt, r.feedback,
                LoadAttemptAnswers(r.id)));
        return list;
    }

    private List<AttemptAnswer> LoadAttemptAnswers(int attemptId)
    {
        var list = new List<AttemptAnswer>();
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT AttemptId, TestQuestionId, SelectedAnswerId FROM AttemptAnswer " +
                                  "WHERE AttemptId = @A ORDER BY TestQuestionId";
                cmd.Parameters.Add("@A", SqlDbType.Int).Value = attemptId;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(new AttemptAnswer(
                            (int)reader["AttemptId"],
                            (int)reader["TestQuestionId"],
                            reader["SelectedAnswerId"] == DBNull.Value ? null : (int?)reader["SelectedAnswerId"]));
                }
            }
        }
        return list;
    }
}
