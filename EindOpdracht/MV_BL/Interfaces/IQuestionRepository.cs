using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface IQuestionRepository
{
    IReadOnlyList<Question> GetByTopic(int topicId, bool excludeFlagged = true, bool activeOnly = false, int? difficulty = null);
    IReadOnlyList<Question> GetAll(bool excludeFlagged = true);
    Question? GetById(int id);
    int Add(Question question);
    void UpdateWithAnswers(Question question);
    void Deactivate(int id);
    void Activate(int id);
    void Flag(int id);
}
