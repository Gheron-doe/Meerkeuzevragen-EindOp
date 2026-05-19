using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_xUnitTest.Fakes;

public class FakeUserRepository : IUserRepository
{
	private readonly List<User> _store = new();
	private int _next = 1;

	public IReadOnlyList<User> GetAll() => _store;
	public User? GetById(int id) => _store.FirstOrDefault(u => u.Id == id);
	public User? GetByUsername(string username) => _store.FirstOrDefault(u => u.Username == username);

	public int Add(User user)
	{
		user.Id = _next++;
		_store.Add(user);
		return user.Id;
	}
}
