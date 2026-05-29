using MV_BL.Domain;
using MV_BL.Exceptions;

namespace MV_xUnitTesting.DomainTests;

public class AppUserTests
{
    [Fact]
    public void FullCtor_SetsProperties()
    {
        var u = new AppUser(5, "alice");
        Assert.Equal(5, u.Id);
        Assert.Equal("alice", u.Username);
    }

    [Fact]
    public void MinimalCtor_IdDefaultsToZero()
    {
        var u = new AppUser("bob");
        Assert.Equal(0, u.Id);
    }

    [Fact]
    public void Id_NegativeValue_ThrowsUserException()
    {
        var u = new AppUser("carol");
        Assert.Throws<UserException>(() => u.Id = -1);
    }

    [Fact]
    public void Username_EmptyString_ThrowsUserException()
        => Assert.Throws<UserException>(() => new AppUser(""));

    [Fact]
    public void Username_WhitespaceOnly_ThrowsUserException()
        => Assert.Throws<UserException>(() => new AppUser("   "));

    [Fact]
    public void Username_IsTrimmedOnAssignment()
    {
        var u = new AppUser("  dave  ");
        Assert.Equal("dave", u.Username);
    }
}
