using MV_BL.Interfaces;
using MV_DL.Repositories.Sql;

namespace MV_DL.Repositories
{
    public class SqlRepositoryFactory : IRepositoryFactory
    {
        private readonly DbConnectionFactory _connectionFactory;

        public SqlRepositoryFactory(string connectionString)
        {
            var config = new DatabaseConfig { ConnectionString = connectionString };
            _connectionFactory = new DbConnectionFactory(config);
        }

        public ITopicRepository CreateTopicRepository() => new SqlTopicRepository(_connectionFactory);
        public IQuestionRepository CreateQuestionRepository() => new SqlQuestionRepository(_connectionFactory);
        public ITestRepository CreateTestRepository(IQuestionRepository qRepo) => new SqlTestRepository(_connectionFactory, qRepo);
        public IUserRepository CreateUserRepository() => new SqlUserRepository(_connectionFactory);
        public IAttemptRepository CreateAttemptRepository() => new SqlAttemptRepository(_connectionFactory);
    }
}
