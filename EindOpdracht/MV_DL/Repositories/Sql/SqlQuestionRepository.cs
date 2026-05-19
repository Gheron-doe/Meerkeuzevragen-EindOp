using Microsoft.Data.SqlClient;
using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_DL.Repositories.Sql
{
    public class SqlQuestionRepository : IQuestionRepository
    {
        private readonly DbConnectionFactory _dbfactory;

        public SqlQuestionRepository(DbConnectionFactory factory)
        {
            _dbfactory = factory;
        }

        private const string SelectColumns =
    "Id, TopicId, QuestionText, DifficultyLevel, IsFlagged, IsActive, CreatedAt, Feedback";

        public IReadOnlyList<Question> GetAll(bool excludeFlagged = true)
        {
            var list = new List<Question>();
            using var conn = _dbfactory.Create();
            var sql = $"SELECT {SelectColumns} FROM Question";
            if (excludeFlagged) sql += " WHERE IsFlagged = 0";
            sql += " ORDER BY TopicId, Id";

            using var cmd = new SqlCommand(sql, conn);
            using (var r = cmd.ExecuteReader()) { while (r.Read()) list.Add(ReadQuestion(r)); }
            foreach (var q in list) q.Answers = LoadAnswers(conn, q.Id);
            return list;
        }

        public IReadOnlyList<Question> GetByTopic(int topicId, bool excludeFlagged = true, bool activeOnly = false, int? difficulty = null)
        {
            var list = new List<Question>();
            using var conn = _dbfactory.Create();

            var sql = $"SELECT {SelectColumns} FROM Question WHERE TopicId = @TopicId";
            if (excludeFlagged) sql += " AND IsFlagged = 0";
            if (activeOnly) sql += " AND IsActive = 1";
            if (difficulty.HasValue) sql += " AND DifficultyLevel = @Difficulty";
            sql += " ORDER BY Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TopicId", topicId);
            if (difficulty.HasValue) cmd.Parameters.AddWithValue("@Difficulty", difficulty.Value);

            using (var r = cmd.ExecuteReader()) { while (r.Read()) list.Add(ReadQuestion(r)); }
            foreach (var q in list) q.Answers = LoadAnswers(conn, q.Id);
            return list;
        }

        public Question? GetById(int id)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand(
                $"SELECT {SelectColumns} FROM Question WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            Question? q = null;
            using (var r = cmd.ExecuteReader()) { if (r.Read()) q = ReadQuestion(r); }
            if (q is not null) q.Answers = LoadAnswers(conn, q.Id);
            return q;
        }

        public int Add(Question question)
        {
            using var conn = _dbfactory.Create();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SqlCommand(
                    "INSERT INTO Question (TopicId, QuestionText, DifficultyLevel, IsFlagged, IsActive, CreatedAt, Feedback) " +
                    "OUTPUT INSERTED.Id VALUES (@TopicId, @Text, @Diff, @IsFlagged, @IsActive, @CreatedAt, @Feedback)", conn, tx);
                cmd.Parameters.AddWithValue("@TopicId", question.TopicId);
                cmd.Parameters.AddWithValue("@Text", question.QuestionText);
                cmd.Parameters.AddWithValue("@Diff", question.DifficultyLevel);
                cmd.Parameters.AddWithValue("@IsFlagged", question.IsFlagged);
                cmd.Parameters.AddWithValue("@IsActive", question.IsActive);
                cmd.Parameters.AddWithValue("@CreatedAt", (object?)question.CreatedAt ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Feedback", (object?)question.Feedback ?? DBNull.Value);
                int newId = (int)cmd.ExecuteScalar();
                question.Id = newId;

                foreach (var a in question.Answers)
                    InsertAnswer(conn, tx, newId, a);

                tx.Commit();
                return newId;
            }
            catch { tx.Rollback(); throw; }
        }
        public void UpdateWithAnswers(Question question)
        {
            using var conn = _dbfactory.Create();
            using var tx = conn.BeginTransaction();
            try
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Question SET QuestionText=@Text, DifficultyLevel=@Diff, Feedback=@Feedback WHERE Id=@Id",
                    conn, tx))
                {
                    cmd.Parameters.AddWithValue("@Text", question.QuestionText);
                    cmd.Parameters.AddWithValue("@Diff", question.DifficultyLevel);
                    cmd.Parameters.AddWithValue("@Feedback", (object?)question.Feedback ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Id", question.Id);
                    cmd.ExecuteNonQuery();
                }

                foreach (var a in question.Answers)
                {
                    if (a.Id > 0) UpdateAnswer(conn, tx, a);
                    else InsertAnswer(conn, tx, question.Id, a);
                }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
        public void Deactivate(int id) => SetBitColumn(id, "IsActive", 0);
        public void Activate(int id) => SetBitColumn(id, "IsActive", 1);
        public void Flag(int id) => SetBitColumn(id, "IsFlagged", 1);

        private void SetBitColumn(int id, string column, int value)
        {
            using var conn = _dbfactory.Create();
            using var cmd = new SqlCommand($"UPDATE Question SET {column} = @Val WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Val", value);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }


        private static void InsertAnswer(SqlConnection conn, SqlTransaction tx, int questionId, Answer a)
        {
            using var cmd = new SqlCommand(
                "INSERT INTO Answer (QuestionId, AnswerText, IsCorrect, OriginalOrder, Feedback) " +
                "VALUES (@QId, @Text, @Correct, @Order, @Feedback)", conn, tx);
            cmd.Parameters.AddWithValue("@QId", questionId);
            cmd.Parameters.AddWithValue("@Text", a.AnswerText);
            cmd.Parameters.AddWithValue("@Correct", a.IsCorrect);
            cmd.Parameters.AddWithValue("@Order", a.OriginalOrder);
            cmd.Parameters.AddWithValue("@Feedback", (object?)a.Feedback ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateAnswer(SqlConnection conn, SqlTransaction tx, Answer a)
        {
            using var cmd = new SqlCommand(
                "UPDATE Answer SET AnswerText=@Text, IsCorrect=@Correct, OriginalOrder=@Order, Feedback=@Feedback WHERE Id=@Id",
                conn, tx);
            cmd.Parameters.AddWithValue("@Text", a.AnswerText);
            cmd.Parameters.AddWithValue("@Correct", a.IsCorrect);
            cmd.Parameters.AddWithValue("@Order", a.OriginalOrder);
            cmd.Parameters.AddWithValue("@Feedback", (object?)a.Feedback ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", a.Id);
            cmd.ExecuteNonQuery();
        }

        private static Question ReadQuestion(SqlDataReader r) => new()
        {
            Id = r.GetInt32(0),
            TopicId = r.GetInt32(1),
            QuestionText = r.GetString(2),
            DifficultyLevel = r.GetInt32(3),
            IsFlagged = r.GetBoolean(4),
            IsActive = r.GetBoolean(5),
            CreatedAt = r.IsDBNull(6) ? null : r.GetDateTime(6),
            Feedback = r.IsDBNull(7) ? null : r.GetString(7)
        };

        private static List<Answer> LoadAnswers(SqlConnection conn, int questionId)
        {
            var list = new List<Answer>();
            using var cmd = new SqlCommand(
                "SELECT Id, QuestionId, AnswerText, IsCorrect, OriginalOrder, Feedback " +
                "FROM Answer WHERE QuestionId = @QId ORDER BY OriginalOrder", conn);
            cmd.Parameters.AddWithValue("@QId", questionId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Answer
                {
                    Id = r.GetInt32(0),
                    QuestionId = r.GetInt32(1),
                    AnswerText = r.GetString(2),
                    IsCorrect = r.GetBoolean(3),
                    OriginalOrder = r.GetInt32(4),
                    Feedback = r.IsDBNull(5) ? null : r.GetString(5)
                });
            }
            return list;
        }
    }
}

