using MV_BL.Interfaces;
using MV_DL.Repositories.Sql;

namespace MV_DL.Repositories;

public class SqlRepositoryFactory : IRepositoryFactory
{
    private readonly string _connectionString;

    public SqlRepositoryFactory(string connectionString) => _connectionString = connectionString;

    public ITopicRepository CreateTopicRepository() => new SqlTopicRepository(_connectionString);

    public IQuestionRepository CreateQuestionRepository() => new SqlQuestionRepository(_connectionString);

    public ITestRepository CreateTestRepository(IQuestionRepository qRepo)
        => new SqlTestRepository(_connectionString, qRepo);

    public IUserRepository CreateUserRepository() => new SqlAppUserRepository(_connectionString);

    public IAttemptRepository CreateAttemptRepository() => new SqlAttemptRepository(_connectionString);
}
