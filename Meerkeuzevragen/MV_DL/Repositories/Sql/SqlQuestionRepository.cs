using Microsoft.Data.SqlClient;
using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System.Data;

namespace MV_DL.Repositories.Sql;

public class SqlQuestionRepository : IQuestionRepository
{
    private readonly string _connectionString;

    public SqlQuestionRepository(string connectionString) => _connectionString = connectionString;

    public List<Question> GetAll() => Query(null, null);

    public List<Question> GetByTopicsAndDifficulties(List<int>? topicIds, List<int>? difficulties)
        => Query(topicIds, difficulties);

    private List<Question> Query(List<int>? topicIds, List<int>? difficulties)
    {
        var rows = new List<(int id, string text, int diff, bool flagged, DateTime created, string? fb)>();

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            string sql = "SELECT DISTINCT q.Id, q.QuestionText, q.DifficultyLevel, q.IsFlagged, q.CreatedAt, q.Feedback FROM Question q";
            if (topicIds is { Count: > 0 })
                sql += " INNER JOIN QuestionTopic qt ON q.Id = qt.QuestionId";
            sql += " WHERE q.IsFlagged = 0";

            using (SqlCommand cmd = conn.CreateCommand())
            {
                if (topicIds is { Count: > 0 })
                {
                    var pNames = new List<string>();
                    for (int i = 0; i < topicIds.Count; i++)
                    {
                        string p = $"@T{i}";
                        pNames.Add(p);
                        cmd.Parameters.Add(p, SqlDbType.Int).Value = topicIds[i];
                    }
                    sql += $" AND qt.TopicId IN ({string.Join(",", pNames)})";
                }
                if (difficulties is { Count: > 0 })
                {
                    var pNames = new List<string>();
                    for (int i = 0; i < difficulties.Count; i++)
                    {
                        string p = $"@D{i}";
                        pNames.Add(p);
                        cmd.Parameters.Add(p, SqlDbType.Int).Value = difficulties[i];
                    }
                    sql += $" AND q.DifficultyLevel IN ({string.Join(",", pNames)})";
                }
                sql += " ORDER BY q.Id";
                cmd.CommandText = sql;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        rows.Add((
                            (int)reader["Id"],
                            (string)reader["QuestionText"],
                            (int)reader["DifficultyLevel"],
                            (bool)reader["IsFlagged"],
                            (DateTime)reader["CreatedAt"],
                            reader["Feedback"] == DBNull.Value ? null : (string)reader["Feedback"]
                        ));
                }
            }

            var questions = new List<Question>();
            foreach (var r in rows)
            {
                var q = new Question(r.id, r.text, r.diff, r.flagged, r.created, r.fb,
                    LoadAnswers(conn, r.id, null),
                    LoadTopicsForQuestion(conn, r.id, null));
                questions.Add(q);
            }
            return questions;
        }
    }

    public Question? GetById(int id)
    {
        (int id, string text, int diff, bool flagged, DateTime created, string? fb)? row = null;

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, QuestionText, DifficultyLevel, IsFlagged, CreatedAt, Feedback FROM Question WHERE Id = @Id AND IsFlagged = 0";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        row = ((int)reader["Id"], (string)reader["QuestionText"],
                            (int)reader["DifficultyLevel"], (bool)reader["IsFlagged"],
                            (DateTime)reader["CreatedAt"],
                            reader["Feedback"] == DBNull.Value ? null : (string)reader["Feedback"]);
                }
            }

            if (row is null) return null;
            var r = row.Value;
            return new Question(r.id, r.text, r.diff, r.flagged, r.created, r.fb,
                LoadAnswers(conn, r.id, null),
                LoadTopicsForQuestion(conn, r.id, null));
        }
    }

    public List<Topic> GetTopicsForQuestion(int questionId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            return LoadTopicsForQuestion(conn, questionId, null);
        }
    }

    public void Add(Question question)
    {
        ArgumentNullException.ThrowIfNull(question);

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
                        cmd.CommandText = "INSERT INTO Question (QuestionText, DifficultyLevel, IsFlagged, CreatedAt, Feedback) " +
                                          "OUTPUT INSERTED.Id VALUES (@Text, @Diff, @Flagged, @Created, @Fb)";
                        cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 1000).Value = question.QuestionText;
                        cmd.Parameters.Add("@Diff", SqlDbType.Int).Value = question.DifficultyLevel;
                        cmd.Parameters.Add("@Flagged", SqlDbType.Bit).Value = question.IsFlagged;
                        cmd.Parameters.Add("@Created", SqlDbType.DateTime).Value = question.CreatedAt ?? DateTime.Now;
                        cmd.Parameters.Add("@Fb", SqlDbType.NVarChar, 200).Value = (object?)question.Feedback ?? DBNull.Value;
                        question.Id = (int)cmd.ExecuteScalar();
                    }

                    foreach (Answer a in question.Answers)
                    {
                        a.QuestionId = question.Id;
                        InsertAnswer(conn, tx, a);
                    }

                    foreach (Topic t in question.Topics)
                    {
                        if (!TopicExists(conn, tx, t.Id))
                            throw new TopicException($"Topic #{t.Id} does not exist or is flagged.");
                        InsertQuestionTopic(conn, tx, question.Id, t.Id);
                    }

                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }

    public void UpdateWithAnswersAndTopics(Question question, List<int> newTopicIds)
    {
        ArgumentNullException.ThrowIfNull(question);

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
                        cmd.CommandText = "UPDATE Question SET QuestionText = @Text, DifficultyLevel = @Diff, Feedback = @Fb WHERE Id = @Id";
                        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = question.Id;
                        cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 1000).Value = question.QuestionText;
                        cmd.Parameters.Add("@Diff", SqlDbType.Int).Value = question.DifficultyLevel;
                        cmd.Parameters.Add("@Fb", SqlDbType.NVarChar, 200).Value = (object?)question.Feedback ?? DBNull.Value;
                        cmd.ExecuteNonQuery();
                    }

                    foreach (Answer a in question.Answers)
                    {
                        a.QuestionId = question.Id;
                        if (a.Id > 0) UpdateAnswer(conn, tx, a);
                        else InsertAnswer(conn, tx, a);
                    }

                    var existing = LoadTopicIds(conn, tx, question.Id);
                    foreach (int rmId in existing.Except(newTopicIds))
                        DeleteQuestionTopic(conn, tx, question.Id, rmId);
                    foreach (int addId in newTopicIds.Except(existing))
                    {
                        if (!TopicExists(conn, tx, addId))
                            throw new TopicException($"Topic #{addId} does not exist.");
                        InsertQuestionTopic(conn, tx, question.Id, addId);
                    }

                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }

    public void FlagQ(int id)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE Question SET IsFlagged = 1 WHERE Id = @Id";
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                cmd.ExecuteNonQuery();
            }
        }
    }

    private static bool TopicExists(SqlConnection conn, SqlTransaction? tx, int topicId)
    {
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT 1 FROM Topic WHERE Id = @Id AND IsFlagged = 0";
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = topicId;
            return cmd.ExecuteScalar() is not null;
        }
    }

    private static void InsertQuestionTopic(SqlConnection conn, SqlTransaction tx, int qId, int tId)
    {
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "IF NOT EXISTS (SELECT 1 FROM QuestionTopic WHERE QuestionId=@Q AND TopicId=@T) " +
                              "INSERT INTO QuestionTopic (QuestionId, TopicId, IsFlagged) VALUES (@Q, @T, 0)";
            cmd.Parameters.Add("@Q", SqlDbType.Int).Value = qId;
            cmd.Parameters.Add("@T", SqlDbType.Int).Value = tId;
            cmd.ExecuteNonQuery();
        }
    }

    private static void DeleteQuestionTopic(SqlConnection conn, SqlTransaction tx, int qId, int tId)
    {
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM QuestionTopic WHERE QuestionId = @Q AND TopicId = @T";
            cmd.Parameters.Add("@Q", SqlDbType.Int).Value = qId;
            cmd.Parameters.Add("@T", SqlDbType.Int).Value = tId;
            cmd.ExecuteNonQuery();
        }
    }

    private static List<int> LoadTopicIds(SqlConnection conn, SqlTransaction? tx, int qId)
    {
        var ids = new List<int>();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT TopicId FROM QuestionTopic WHERE QuestionId = @Q";
            cmd.Parameters.Add("@Q", SqlDbType.Int).Value = qId;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read()) ids.Add((int)reader["TopicId"]);
            }
        }
        return ids;
    }

    private static void InsertAnswer(SqlConnection conn, SqlTransaction tx, Answer a)
    {
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "INSERT INTO Answer (QuestionId, AnswerText, IsCorrect, Feedback) " +
                              "OUTPUT INSERTED.Id VALUES (@Q, @Text, @Correct, @Fb)";
            cmd.Parameters.Add("@Q", SqlDbType.Int).Value = a.QuestionId;
            cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 500).Value = a.AnswerText;
            cmd.Parameters.Add("@Correct", SqlDbType.Bit).Value = a.IsCorrect;
            cmd.Parameters.Add("@Fb", SqlDbType.NVarChar, 200).Value = (object?)a.Feedback ?? DBNull.Value;
            a.Id = (int)cmd.ExecuteScalar();
        }
    }

    private static void UpdateAnswer(SqlConnection conn, SqlTransaction tx, Answer a)
    {
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Answer SET AnswerText = @Text, IsCorrect = @Correct, Feedback = @Fb WHERE Id = @Id";
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = a.Id;
            cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 500).Value = a.AnswerText;
            cmd.Parameters.Add("@Correct", SqlDbType.Bit).Value = a.IsCorrect;
            cmd.Parameters.Add("@Fb", SqlDbType.NVarChar, 200).Value = (object?)a.Feedback ?? DBNull.Value;
            cmd.ExecuteNonQuery();
        }
    }

    private static List<Answer> LoadAnswers(SqlConnection conn, int qId, SqlTransaction? tx)
    {
        var list = new List<Answer>();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT Id, QuestionId, AnswerText, IsCorrect, Feedback FROM Answer WHERE QuestionId = @Q ORDER BY Id";
            cmd.Parameters.Add("@Q", SqlDbType.Int).Value = qId;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    list.Add(new Answer(
                        (int)reader["Id"], (int)reader["QuestionId"],
                        (string)reader["AnswerText"], (bool)reader["IsCorrect"],
                        reader["Feedback"] == DBNull.Value ? null : (string)reader["Feedback"]));
            }
        }
        return list;
    }

    private static List<Topic> LoadTopicsForQuestion(SqlConnection conn, int qId, SqlTransaction? tx)
    {
        var list = new List<Topic>();
        using (SqlCommand cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT t.Id, t.Name, t.IsFlagged FROM Topic t " +
                              "INNER JOIN QuestionTopic qt ON t.Id = qt.TopicId " +
                              "WHERE qt.QuestionId = @Q AND t.IsFlagged = 0";
            cmd.Parameters.Add("@Q", SqlDbType.Int).Value = qId;
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    list.Add(new Topic((int)reader["Id"], (string)reader["Name"], (bool)reader["IsFlagged"]));
            }
        }
        return list;
    }
}
