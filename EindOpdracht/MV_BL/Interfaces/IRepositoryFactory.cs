using MV_BL.Interfaces;

namespace MV_BL.Interfaces
{
    public interface IRepositoryFactory
    {
        ITopicRepository CreateTopicRepository();
        IQuestionRepository CreateQuestionRepository();
        ITestRepository CreateTestRepository(IQuestionRepository questionRepo);
        IUserRepository CreateUserRepository();
        IAttemptRepository CreateAttemptRepository();
    }
}
