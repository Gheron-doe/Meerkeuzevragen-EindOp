using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_BL.Services;

public class QuestionService
{
    private readonly IQuestionRepository _repo;

   public QuestionService(IQuestionRepository repo) => _repo = repo;

    public List<Question> GetAll() => _repo.GetAll();

    public List<Question> GetByTopicsAndDifficulties(List<int>? topicIds, List<int>? difficulties)
        => _repo.GetByTopicsAndDifficulties(topicIds, difficulties);

    public Question? GetById(int id) => _repo.GetById(id);

    public List<Topic> GetTopicsForQuestion(int qId) => _repo.GetTopicsForQuestion(qId);

    public void Add(Question q) => _repo.Add(q);

    public void UpdateWithAnswersAndTopics(Question q, List<int> topicIds)
        => _repo.UpdateWithAnswersAndTopics(q, topicIds);

    public void Flag(int id) => _repo.FlagQ(id);
}
