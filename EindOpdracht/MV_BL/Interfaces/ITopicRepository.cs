using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface ITopicRepository
{
	IReadOnlyList<Topic> GetAll(bool includeFlagged = false);
	Topic? GetById(int id);
	int Add(Topic topic);
	void Update(Topic topic);
}
