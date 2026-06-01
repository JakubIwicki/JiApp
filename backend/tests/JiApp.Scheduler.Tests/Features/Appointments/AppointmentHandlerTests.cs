using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Appointments.CreateAppointment;
using JiApp.Scheduler.Features.Appointments.DeleteAppointment;
using JiApp.Scheduler.Features.Appointments.GetAppointment;
using JiApp.Scheduler.Features.Appointments.ListAppointments;
using JiApp.Scheduler.Features.Appointments.UpdateAppointment;
using JiApp.Scheduler.Features.Appointments.UpdateAppointmentStatus;
using JiApp.Scheduler.Features.Common;
using Microsoft.Data.Sqlite;

namespace JiApp.Scheduler.Tests.Features.Appointments;

public sealed class AppointmentHandlerTests : IDisposable
{
    private readonly Board _board;
    private readonly Client _client;
    private readonly SqliteConnection _connection;
    private readonly Mock<ICurrentUserService> _currentUser;
    private readonly SchedulerDbContext _db;
    private readonly DateOnly _saturday;
    private readonly Service _service;

    public AppointmentHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SchedulerDbContext(options);
        _db.Database.EnsureCreated();

        _currentUser = new Mock<ICurrentUserService>();
        _currentUser.Setup(x => x.UserId).Returns(1L);

        // Calculate a Saturday date for test data
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        while (start.DayOfWeek != DayOfWeek.Saturday)
            start = start.AddDays(1);
        _saturday = start;

        _board = new Board { Name = "Board", MemberUserIds = [1L] };
        _client = new Client { Name = "Alice", Board = _board };
        _service = new Service
        {
            Name = "Haircut",
            Category = ServiceCategory.MensHaircut,
            BaseDuration = 30,
            BasePrice = new Price(100)
        };

        // Save entities and let IDs be assigned by DB. Use Board navigation for Service to satisfy EF.
        _db.Boards.Add(_board);
        _db.Clients.Add(_client);
        _service.Board = _board;
        _db.Services.Add(_service);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Close();
    }

    [Fact]
    public async Task CreateAppointment_WithValidData_ReturnsAppointmentId()
    {
        var handler = new CreateAppointmentHandler(_db, _currentUser.Object);
        var request = new CreateAppointmentRequest(
            _board.Id, _client.Id, _service.Id,
            _saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAppointment_WithOverlappingTime_ReturnsFailure()
    {
        // Attach entities back to context so EF recognizes them
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var existing = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),
            CreatedBy = 1L
        };
        _db.Appointments.Add(existing);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new CreateAppointmentHandler(_db, _currentUser.Object);
        var request = new CreateAppointmentRequest(
            _board.Id, _client.Id, _service.Id,
            _saturday, new TimeOnly(10, 30), new TimeOnly(11, 30),
            null, "Room 1", null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAppointment_WithInvalidBoardId_ReturnsFailure()
    {
        var handler = new CreateAppointmentHandler(_db, _currentUser.Object);
        var request = new CreateAppointmentRequest(
            999L, _client.Id, _service.Id,
            _saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAppointment_PriceDefaultsFromService()
    {
        var handler = new CreateAppointmentHandler(_db, _currentUser.Object);
        var request = new CreateAppointmentRequest(
            _board.Id, _client.Id, _service.Id,
            _saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var appointment = await _db.Appointments.FindAsync(result.Value);
        appointment!.Price.Amount.Should().Be(100);
        appointment.Price.Currency.Should().Be("PLN");
    }

    [Fact]
    public async Task CreateAppointment_WithProvidedPrice_UsesProvidedPrice()
    {
        var handler = new CreateAppointmentHandler(_db, _currentUser.Object);
        var request = new CreateAppointmentRequest(
            _board.Id, _client.Id, _service.Id,
            _saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", new PriceRequest(200));

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var appointment = await _db.Appointments.FindAsync(result.Value);
        appointment!.Price.Amount.Should().Be(200);
    }

    [Fact]
    public async Task GetAppointment_WithValidId_ReturnsAppointment()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),
            Location = "Room 1",

            CreatedBy = 1L
        };
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new GetAppointmentHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Location.Should().Be("Room 1");
        result.Value.Status.Should().Be("Created");
    }

    [Fact]
    public async Task GetAppointment_WithInvalidId_ReturnsFailure()
    {
        var handler = new GetAppointmentHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ToDone_Succeeds()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateAppointmentStatusHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, new UpdateAppointmentStatusRequest("done"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Appointments.FindAsync(appointment.Id);
        updated!.Status.Should().Be(AppointmentStatus.Done);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ToCancelled_Succeeds()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateAppointmentStatusHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, new UpdateAppointmentStatusRequest("cancel"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Appointments.FindAsync(appointment.Id);
        updated!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ToCancelledViaCancelled_Succeeds()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateAppointmentStatusHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, new UpdateAppointmentStatusRequest("cancelled"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Appointments.FindAsync(appointment.Id);
        updated!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_FromDone_ReturnsFailure()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        appointment.TryTransitionTo(AppointmentStatus.Done, out _);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateAppointmentStatusHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, new UpdateAppointmentStatusRequest("cancel"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAppointmentStatus_FromCancelled_ReturnsFailure()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        appointment.TryTransitionTo(AppointmentStatus.Cancelled, out _);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateAppointmentStatusHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, new UpdateAppointmentStatusRequest("done"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListAppointments_ByBoardAndDate_ReturnsFiltered()
    {
        var sunday = _saturday.AddDays(1);
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        _db.Appointments.AddRange(
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = _saturday, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(100), Location = "Room 1", CreatedBy = 1L
            },
            new Appointment
            {
                BoardId = _board.Id, ClientId = _client.Id, ServiceId = _service.Id,
                Date = sunday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0),
                Price = new Price(150), Location = "Room 2", CreatedBy = 1L
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new ListAppointmentsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(_board.Id, [_saturday], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Location == "Room 1");
    }

    [Fact]
    public async Task UpdateAppointment_WithValidData_Updates()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),
            Location = "Room 1",

            CreatedBy = 1L
        };
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateAppointmentHandler(_db, _currentUser.Object);
        var request = new UpdateAppointmentRequest(
            _client.Id, _service.Id,
            _saturday, new TimeOnly(11, 0), new TimeOnly(12, 0),
            "Updated description", "Room 2", new PriceRequest(120));

        var result = await handler.HandleAsync(appointment.Id, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await _db.Appointments.FindAsync(appointment.Id);
        updated!.StartTime.Should().Be(new TimeOnly(11, 0));
        updated.EndTime.Should().Be(new TimeOnly(12, 0));
        updated.Location.Should().Be("Room 2");
        updated.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteAppointment_WithValidId_Deletes()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DeleteAppointmentHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var deleted = await _db.Appointments.FindAsync(appointment.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAppointment_WhenDone_ReturnsFailure()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        appointment.TryTransitionTo(AppointmentStatus.Done, out _);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DeleteAppointmentHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cannot delete a completed appointment");
    }

    [Fact]
    public async Task DeleteAppointment_WithNotFound_ReturnsNotFoundErrorCategory()
    {
        var handler = new DeleteAppointmentHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task DeleteAppointment_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        _db.Boards.Add(otherBoard);
        await _db.SaveChangesAsync();

        _db.Attach(otherBoard);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = otherBoard.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 2L
        };
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DeleteAppointmentHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task DeleteAppointment_WhenDone_ReturnsConflictErrorCategory()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        appointment.TryTransitionTo(AppointmentStatus.Done, out _);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new DeleteAppointmentHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Conflict);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_WithInvalidStatus_ReturnsValidationErrorCategory()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateAppointmentStatusHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, new UpdateAppointmentStatusRequest("invalid"),
            CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_FromDone_ReturnsValidationErrorCategory()
    {
        _db.Attach(_board);
        _db.Attach(_client);
        _db.Attach(_service);
        var appointment = new Appointment
        {
            BoardId = _board.Id,
            ClientId = _client.Id,
            ServiceId = _service.Id,
            Date = _saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),

            CreatedBy = 1L
        };
        appointment.TryTransitionTo(AppointmentStatus.Done, out _);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var handler = new UpdateAppointmentStatusHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(appointment.Id, new UpdateAppointmentStatusRequest("cancel"),
            CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task CreateAppointment_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var handler = new CreateAppointmentHandler(_db, _currentUser.Object);
        var request = new CreateAppointmentRequest(
            999L, _client.Id, _service.Id,
            _saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task CreateAppointment_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        _db.Boards.Add(otherBoard);
        await _db.SaveChangesAsync();

        var handler = new CreateAppointmentHandler(_db, _currentUser.Object);
        var request = new CreateAppointmentRequest(
            otherBoard.Id, _client.Id, _service.Id,
            _saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await handler.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task ListAppointments_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var handler = new ListAppointmentsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(999L, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task ListAppointments_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        _db.Boards.Add(otherBoard);
        await _db.SaveChangesAsync();

        var handler = new ListAppointmentsHandler(_db, _currentUser.Object);
        var result = await handler.HandleAsync(otherBoard.Id, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}