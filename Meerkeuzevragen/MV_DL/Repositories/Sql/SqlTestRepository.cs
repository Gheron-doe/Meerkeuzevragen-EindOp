using Microsoft.Data.SqlClient;
using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System.Data;

namespace MV_DL.Repositories.Sql;

public class SqlTestRepository : ITestRepository
{
    private readonly string _connectionString;
    private readonly IQuestionRepository _questionRepo;

    public SqlTestRepository(string connectionString, IQuestionRepository questionRepo)
    {
        _connectionString = connectionString;
        _questionRepo = questionRepo;
    }

    public List<Test> GetAll()
    {
        var rows = new List<(int id, string title, DateTime created, bool flagged)>();
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Title, CreatedAt, IsFlagged FROM Test WHERE IsFlagged = 0 ORDER BY CreatedAt DESC";
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        rows.Add(((int)reader["Id"], (string)reader["Title"],
                            (DateTime)reader["CreatedAt"], (bool)reader["IsFlagged"]));
                }
            }

            var tests = new List<Test>();
            foreach (var r in rows)
                tests.Add(new Test(r.id, r.title, r.created, r.flagged,
                    LoadTestQuestions(conn, r.id), LoadTopicsForTest(conn, r.id)));
            return tests;
        }
    }

    public Test? GetById(int id)
    {
        (int id, string title, DateTime created, bool flagged)? row = null;
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Title, CreatedAt, IsFlagged FROM Test WHERE Id = @Id";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        row = ((int)reader["Id"], (string)reader["Title"],
                            (DateTime)reader["CreatedAt"], (bool)reader["IsFlagged"]);
                }
            }

            if (row is null) return null;
            var r = row.Value;
            return new Test(r.id, r.title, r.created, r.flagged,
                LoadTestQuestions(conn, r.id), LoadTopicsForTest(conn, r.id));
        }
    }

    public List<int> GetTestDifficulties(int testId)
    {
        var list = new List<int>();
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DISTINCT q.DifficultyLevel FROM Question q " +
                                  "INNER JOIN TestQuestion tq ON tq.QuestionId = q.Id " +
                                  "WHERE tq.TestId = @T ORDER BY q.DifficultyLevel";
                cmd.Parameters.Add("@T", SqlDbType.Int).Value = testId;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) list.Add((int)reader["DifficultyLevel"]);
                }
            }
        }
        return list;
    }

    public void Add(Test test)
    {
        ArgumentNullException.ThrowIfNull(test);

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlTransaction tx = conn.BeginTransaction())
            {
                try
                {
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO Test (Title, CreatedAt, IsFlagged) " +
                                          "OUTPUT INSERTED.Id VALUES (@Title, @Created, @Flagged)";
                        cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = test.Title;
                        cmd.Parameters.Add("@Created", SqlDbType.DateTime).Value = test.CreatedAt;
                        cmd.Parameters.Add("@Flagged", SqlDbType.Bit).Value = test.IsFlagged;
                        test.Id = (int)cmd.ExecuteScalar();
                    }

                    foreach (TestQuestion tq in test.Questions)
                    {
                        if (!QuestionExists(conn, tx, tq.QuestionId))
                            throw new QuestionException($"Question #{tq.QuestionId} does not exist.");

                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "INSERT INTO TestQuestion (TestId, QuestionId, SortOrder) " +
                                              "OUTPUT INSERTED.Id VALUES (@T, @Q, @S)";
                            cmd.Parameters.Add("@T", SqlDbType.Int).Value = test.Id;
                            cmd.Parameters.Add("@Q", SqlDbType.Int).Value = tq.QuestionId;
                            cmd.Parameters.Add("@S", SqlDbType.Int).Value = tq.SortOrder;
                            tq.Id = (int)cmd.ExecuteScalar();
                            tq.TestId = test.Id;
                        }
                    }

                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }

    public void Flag(int id)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE Test SET IsFlagged = 1 WHERE Id = @Id";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static bool QuestionExists(SqlConnection conn, SqlTransaction tx, int questionId)
    {
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT 1 FROM Question WHERE Id = @Id AND IsFlagged = 0";
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = questionId;
            return cmd.ExecuteScalar() is not null;
        }
    }

    private List<TestQuestion> LoadTestQuestions(SqlConnection conn, int testId)
    {
        var rows = new List<(int id, int testId, int qId, int sort)>();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, TestId, QuestionId, SortOrder FROM TestQuestion WHERE TestId = @T ORDER BY SortOrder";
            cmd.Parameters.Add("@T", SqlDbType.Int).Value = testId;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    rows.Add(((int)reader["Id"], (int)reader["TestId"],
                        (int)reader["QuestionId"], (int)reader["SortOrder"]));
            }
        }
        var list = new List<TestQuestion>();
        foreach (var r in rows)
            list.Add(new TestQuestion(r.id, r.testId, r.qId, r.sort, _questionRepo.GetById(r.qId)));
        return list;
    }

    private static List<Topic> LoadTopicsForTest(SqlConnection conn, int testId)
    {
        var list = new List<Topic>();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT DISTINCT tp.Id, tp.Name, tp.IsFlagged FROM Topic tp " +
                              "INNER JOIN QuestionTopic qt ON tp.Id = qt.TopicId " +
                              "INNER JOIN TestQuestion tq ON qt.QuestionId = tq.QuestionId " +
                              "WHERE tq.TestId = @T AND tp.IsFlagged = 0";
            cmd.Parameters.Add("@T", SqlDbType.Int).Value = testId;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    list.Add(new Topic((int)reader["Id"], (string)reader["Name"], (bool)reader["IsFlagged"]));
            }
        }
        return list;
    }
}
