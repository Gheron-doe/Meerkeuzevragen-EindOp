using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface IQuestionRepository
{
    List<Question> GetAll();

    List<Question> GetByTopicsAndDifficulties(List<int>? topicIds, List<int>? difficulties);

    Question? GetById(int questionId);

    List<Topic> GetTopicsForQuestion(int questionId);

    void Add(Question question);

    void UpdateWithAnswersAndTopics(Question question, List<int> newTopicIds);

    void FlagQ(int questionId);
}
