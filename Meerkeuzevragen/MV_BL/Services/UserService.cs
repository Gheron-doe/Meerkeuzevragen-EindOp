using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_BL.Services;

public class UserService
{
    private readonly IUserRepository _repo;

    public UserService(IUserRepository repo) => _repo = repo;

    public List<AppUser> GetAll() => _repo.GetAll();

    public AppUser? GetById(int id) => _repo.GetById(id);

    public AppUser? GetByUsername(string username) => _repo.GetByUsername(username);

    public void Add(AppUser user) => _repo.Add(user);
}
