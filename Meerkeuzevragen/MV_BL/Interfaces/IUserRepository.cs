using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface IUserRepository
{
    List<AppUser> GetAll();

    AppUser? GetById(int id);

    AppUser? GetByUsername(string username);

    void Add(AppUser user);
}
