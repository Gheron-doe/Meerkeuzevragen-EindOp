using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_xUnitTest.Fakes;

public class FakeTopicRepository : ITopicRepository
{
	private readonly List<Topic> _store = new();
	private int _next = 1;

	public IReadOnlyList<Topic> GetAll(bool includeFlagged = false)
		=> includeFlagged ? _store : _store.Where(t => !t.IsFlagged).ToList();

	public Topic? GetById(int id) => _store.FirstOrDefault(t => t.Id == id);

	public int Add(Topic topic)
	{
		topic.Id = _next++;
		_store.Add(topic);
		return topic.Id;
	}

	public void Update(Topic topic)
	{
		var existing = _store.FirstOrDefault(t => t.Id == topic.Id);
		if (existing is null) return;
		existing.Name = topic.Name;
		existing.Description = topic.Description;
		existing.IsFlagged = topic.IsFlagged;
	}
}
