using MV_BL.Exceptions;

namespace MV_BL.Domain;

public class AppUser
{
    public AppUser(int id, string username)
    {
        Id = id;
        Username = username;
    }

    public AppUser(string username)
    {
        Username = username;
    }

    private int _id;

    public int Id
    {
        get => _id;
        set
        {
            if (value < 0) throw new UserException("AppUser Id cannot be negative.");
            _id = value;
        }
    }

    private string _username = string.Empty;

    public string Username
    {
        get => _username;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new UserException("Username cannot be empty.");
            _username = value.Trim();
        }
    }
}
