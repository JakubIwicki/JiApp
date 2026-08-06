using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using JiApp.Common;
using JiApp.Common.Constants;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Appointments;
using JiApp.Scheduler.Features.Appointments.UpdateAppointment;
using JiApp.Scheduler.Features.Appointments.UpdateAppointmentStatus;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace JiApp.Scheduler.Tests.Features.Appointments;

/// <summary>
/// Endpoint-level tests for the appointment id-route mappings. G3.2 collapsed the
/// AccessDenied result to a 404 so a non-member cannot distinguish an existing
/// appointment from a missing one — across GET, PUT, PATCH, and DELETE.
/// </summary>
public sealed class GetAppointmentEndpointTests : IDisposable
{
    private const string JwtKey = "test-key-that-is-at-least-32-chars!";
    private const string JwtIssuer = "JiApp-Identity";
    private const string JwtAudience = "jiapp-gateway";

    private readonly WebApplicationFactory<JiApp.Scheduler.Program> _factory;
    private readonly string _dbPath;
    private long _clientId;
    private long _serviceId;

    public GetAppointmentEndpointTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"scheduler_it_{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<JiApp.Scheduler.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.UseSetting("ConnectionString", $"Data Source={_dbPath}");
                builder.UseSetting("Jwt:Key", JwtKey);
                builder.UseSetting("Jwt:Issuer", JwtIssuer);
                builder.UseSetting("Jwt:Audience", JwtAudience);
                builder.ConfigureServices(services =>
                {
                    // The DELETE route rechecks the token stamp against the external
                    // Identity service; stub it so the collapse under test is reached.
                    services.RemoveAll<ISecurityStampValidator>();
                    services.AddSingleton<ISecurityStampValidator, NoOpSecurityStampValidator>();
                });
            });
    }

    public void Dispose()
    {
        _factory.Dispose();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task GetAppointment_ByMember_ReturnsOk()
    {
        var appointmentId = SeedAppointment();
        var client = CreateAuthenticatedClient(userId: 1L);

        var response = await client.GetAsync($"/api/v1/scheduler/appointments/{appointmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // The response stream is single-pass, so capture it once and derive both
        // the typed body and the wire-casing assertions from the same string.
        var rawBody = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<AppointmentResponse>(rawBody, JsonSerializerOptions.Web);
        body.Should().NotBeNull();
        body!.Client.Name.Should().Be("Alice");
        body.Service.Name.Should().Be("Haircut");
        body.Service.Category.Should().Be("MensHaircut");
        body.Service.BasePrice.Amount.Should().Be(100);

        // Pin the ACTUAL wire casing: the typed read is case-insensitive, so a
        // PascalCase serializer rename would still pass above.
        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        root.GetProperty("client").GetProperty("name").GetString().Should().Be("Alice");
        root.GetProperty("service").GetProperty("name").GetString().Should().Be("Haircut");
        root.GetProperty("service").GetProperty("category").GetString().Should().Be("MensHaircut");
    }

    [Fact]
    public async Task GetAppointment_ByNonMember_ReturnsNotFound()
    {
        var appointmentId = SeedAppointment();
        var client = CreateAuthenticatedClient(userId: 2L);

        var response = await client.GetAsync($"/api/v1/scheduler/appointments/{appointmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAppointment_ByNonMember_ReturnsNotFound()
    {
        var appointmentId = SeedAppointment();
        var client = CreateAuthenticatedClient(userId: 2L);
        var request = new UpdateAppointmentRequest(
            _clientId, _serviceId, new DateOnly(2026, 1, 31),
            new TimeOnly(11, 0), new TimeOnly(12, 0),
            null, "Room 2", new PriceRequest(120));

        var response = await client.PutAsJsonAsync($"/api/v1/scheduler/appointments/{appointmentId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ByNonMember_ReturnsNotFound()
    {
        var appointmentId = SeedAppointment();
        var client = CreateAuthenticatedClient(userId: 2L);
        var request = new UpdateAppointmentStatusRequest("done");

        var response = await client.PatchAsJsonAsync($"/api/v1/scheduler/appointments/{appointmentId}/status", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAppointment_ByNonMember_ReturnsNotFound()
    {
        var appointmentId = SeedAppointment();
        var client = CreateAuthenticatedClient(userId: 2L);

        var response = await client.DeleteAsync($"/api/v1/scheduler/appointments/{appointmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateAuthenticatedClient(long userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", MintToken(userId));
        return client;
    }

    private long SeedAppointment()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var board = new Board { Name = "Board", MemberUserIds = [1L] };
        var client = new Client { Name = "Alice", Board = board };
        var service = new Service
        {
            Name = "Haircut",
            Category = ServiceCategory.MensHaircut,
            BaseDuration = 30,
            BasePrice = new Price(100),
            Board = board
        };
        db.Boards.Add(board);
        db.Clients.Add(client);
        db.Services.Add(service);
        db.SaveChanges();
        _clientId = client.Id;
        _serviceId = service.Id;

        var appointment = new Appointment
        {
            BoardId = board.Id,
            ClientId = client.Id,
            ServiceId = service.Id,
            Date = new DateOnly(2026, 1, 31),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),
            CreatedBy = 1L
        };
        db.Appointments.Add(appointment);
        db.SaveChanges();

        // Pins the per-test DB isolation: the temp file only exists if the
        // UseSetting ConnectionString override actually reached the app.
        File.Exists(_dbPath).Should().BeTrue();
        return appointment.Id;
    }

    private static string MintToken(long userId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(Permissions.PermissionClaimType, Permissions.SchedulerAccess)
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
