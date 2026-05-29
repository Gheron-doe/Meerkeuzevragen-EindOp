using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_BL.Services;

public class TopicService
{
    private readonly ITopicRepository _repo;

    public TopicService(ITopicRepository repo) => _repo = repo;

    public List<Topic> GetAll() => _repo.GetAll();

    public Topic? GetById(int id) => _repo.GetById(id);

    public Topic? GetByName(string name) => _repo.GetByName(name);

    public void Add(Topic topic) => _repo.Add(topic);

    public void Update(Topic topic) => _repo.Update(topic);
}
