using System.Reflection;
using JiApp.Identity.Persistence;

namespace JiApp.Identity.Tests.Conventions;

/// <summary>
/// Fitness guard (G12): the Register handler must keep a real-context suite
/// derived from HandlerTestBase&lt;IdentityDbContext&gt;. If it ever regresses
/// to mock-only coverage, the persistence paths (unique-constraint handling,
/// identity-column generation, role-assignment compensation) silently go
/// unverified.
/// </summary>
public sealed class RegisterHandlerRealContextConventionTests
{
    private const string RegisterHandlerDbTestsTypeName =
        "JiApp.Identity.Tests.Features.Auth.Register.RegisterHandlerDbTests";

    [Fact]
    public void RegisterHandlerDbTests_DerivesFromRealContextBase()
    {
        var dbTestsType = Assembly.GetAssembly(typeof(RegisterHandlerRealContextConventionTests))!
            .GetType(RegisterHandlerDbTestsTypeName);

        dbTestsType.Should().NotBeNull(
            "RegisterHandlerDbTests must exist — deleting it silently regresses the suite to mock-only coverage");
        var baseType = dbTestsType!.BaseType;

        baseType.Should().NotBeNull();
        baseType!.IsGenericType.Should().BeTrue();
        baseType.GetGenericTypeDefinition().Should().Be(typeof(HandlerTestBase<>));
        baseType.GenericTypeArguments.Should().ContainSingle().Which.Should().Be(typeof(IdentityDbContext));
    }
}
