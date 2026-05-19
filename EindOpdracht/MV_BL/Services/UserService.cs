using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_BL.Services
{
    public class UserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo) => _repo = repo;

        public IReadOnlyList<User> GetAll() => _repo.GetAll();
        public User? GetById(int id) => _repo.GetById(id);
    }
}
