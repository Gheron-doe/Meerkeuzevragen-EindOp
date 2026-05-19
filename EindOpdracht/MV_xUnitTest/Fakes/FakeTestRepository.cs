using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_xUnitTest.Fakes;

public class FakeTestRepository : ITestRepository
{
	private readonly List<Test> _store = new();
	private int _next = 1;
	private int _tqNext = 1;

	public IReadOnlyList<Test> GetAll() => _store.Where(t => !t.IsFlagged).ToList();
	public Test? GetById(int id) => _store.FirstOrDefault(t => t.Id == id);

	public int Add(Test test)
	{
		test.Id = _next++;
		foreach (var tq in test.Questions)
		{
			tq.Id = _tqNext++;
			tq.TestId = test.Id;
		}
		_store.Add(test);
		return test.Id;
	}

	/// <summary>Soft-delete: sets IsFlagged=true so test is hidden from GetAll().</summary>
	public void Deactivate(int id)
	{
		var t = _store.FirstOrDefault(x => x.Id == id);
		if (t is not null) t.IsFlagged = true;
	}
}
