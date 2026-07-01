using JiApp.Common.Models;
using JiApp.Identity.Services;

namespace JiApp.Identity.Tests.Services;

public sealed class AccessTokenRevocationTests
{
    [Fact]
    public void IsValid_ReturnsFalse_WhenUserIsNull()
    {
        AccessTokenRevocation.IsValid(null, "stamp").Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenTokenSecurityStampIsNull()
    {
        var user = new User { SecurityStamp = "stamp" };

        AccessTokenRevocation.IsValid(user, null).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenStampsMatch()
    {
        var user = new User { SecurityStamp = "stamp-123" };

        AccessTokenRevocation.IsValid(user, "stamp-123").Should().BeTrue();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenStampsDiffer()
    {
        var user = new User { SecurityStamp = "stamp-old" };

        AccessTokenRevocation.IsValid(user, "stamp-new").Should().BeFalse();
    }
}
