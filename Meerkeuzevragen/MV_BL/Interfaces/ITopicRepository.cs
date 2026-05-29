using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface ITopicRepository
{
    List<Topic> GetAll();

    Topic? GetById(int id);

    Topic? GetByName(string name);

    void Add(Topic topic);

    void Update(Topic topic);
}
