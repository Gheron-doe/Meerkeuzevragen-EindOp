using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface IUserRepository
{
	IReadOnlyList<User> GetAll();
	User? GetById(int id);
	User? GetByUsername(string username);
	int Add(User user);
}
