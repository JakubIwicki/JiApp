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

public sealed class AppointmentHandlerTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SchedulerDbContext _db;
        private readonly Mock<ICurrentUserService> _currentUser;
        private Board? _board;
        private Client? _client;
        private Service? _service;
        private DateOnly _saturday;

        public Fixture()
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
        }

        public SchedulerDbContext Db => _db;
        public ICurrentUserService CurrentUser => _currentUser.Object;
        public DateOnly Saturday => _saturday;
        public Board Board => _board!;
        public Client Client => _client!;
        public Service Service => _service!;

        /// <summary>
        /// Seeds the standard Board, Client, and Service entities for appointment tests.
        /// Must be called before tests that need prepopulated entities.
        /// </summary>
        public Fixture WithSeededEntities()
        {
            var board = new Board { Name = "Board", MemberUserIds = [1L] };
            var client = new Client { Name = "Alice", Board = board };
            var service = new Service
            {
                Name = "Haircut",
                Category = ServiceCategory.MensHaircut,
                BaseDuration = 30,
                BasePrice = new Price(100)
            };

            _db.Boards.Add(board);
            _db.Clients.Add(client);
            service.Board = board;
            _db.Services.Add(service);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();

            // Re-attach them so Db can find them by ID in tests
            _db.Attach(board);
            _db.Attach(client);
            _db.Attach(service);

            _board = board;
            _client = client;
            _service = service;

            return this;
        }

        /// <summary>
        /// Seeds a standard appointment using the seeded Board, Client, and Service.
        /// Requires WithSeededEntities() to have been called first.
        /// </summary>
        public Fixture WithAppointment(Action<Appointment>? configure = null)
        {
            var appointment = new Appointment
            {
                BoardId = _board!.Id,
                ClientId = _client!.Id,
                ServiceId = _service!.Id,
                Date = _saturday,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(11, 0),
                Price = new Price(100),
                CreatedBy = 1L
            };
            configure?.Invoke(appointment);
            _db.Appointments.Add(appointment);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        /// <summary>
        /// Seeds a standard appointment and returns its ID.
        /// Requires WithSeededEntities() to have been called first.
        /// </summary>
        public Fixture WithAppointment(out long appointmentId, Action<Appointment>? configure = null)
        {
            var appointment = new Appointment
            {
                BoardId = _board!.Id,
                ClientId = _client!.Id,
                ServiceId = _service!.Id,
                Date = _saturday,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(11, 0),
                Price = new Price(100),
                CreatedBy = 1L
            };
            configure?.Invoke(appointment);
            _db.Appointments.Add(appointment);
            _db.SaveChanges();
            appointmentId = appointment.Id;
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithBoard(Board board)
        {
            _db.Boards.Add(board);
            _db.SaveChanges();
            return this;
        }

        public CreateAppointmentHandler CreateAppointmentSut => new(_db, _currentUser.Object);
        public GetAppointmentHandler GetAppointmentSut => new(_db, _currentUser.Object);
        public ListAppointmentsHandler ListAppointmentsSut => new(_db, _currentUser.Object);
        public UpdateAppointmentHandler UpdateAppointmentSut => new(_db, _currentUser.Object);
        public UpdateAppointmentStatusHandler UpdateAppointmentStatusSut => new(_db, _currentUser.Object);
        public DeleteAppointmentHandler DeleteAppointmentSut => new(_db, _currentUser.Object);

        public void Dispose()
        {
            _db.Dispose();
            _connection.Close();
        }
    }

    [Fact]
    public async Task CreateAppointment_WithValidData_ReturnsAppointmentId()
    {
        using var fixture = new Fixture().WithSeededEntities();
        var sut = fixture.CreateAppointmentSut;
        var request = new CreateAppointmentRequest(
            fixture.Board.Id, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAppointment_WithOverlappingTime_ReturnsFailure()
    {
        using var fixture = new Fixture().WithSeededEntities();
        // Seed an existing appointment that overlaps
        var existing = new Appointment
        {
            BoardId = fixture.Board.Id,
            ClientId = fixture.Client.Id,
            ServiceId = fixture.Service.Id,
            Date = fixture.Saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),
            CreatedBy = 1L
        };
        fixture.Db.Appointments.Add(existing);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var sut = fixture.CreateAppointmentSut;
        var request = new CreateAppointmentRequest(
            fixture.Board.Id, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 30), new TimeOnly(11, 30),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAppointment_WithInvalidBoardId_ReturnsFailure()
    {
        using var fixture = new Fixture().WithSeededEntities();
        var sut = fixture.CreateAppointmentSut;
        var request = new CreateAppointmentRequest(
            999L, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAppointment_PriceDefaultsFromService()
    {
        using var fixture = new Fixture().WithSeededEntities();
        var sut = fixture.CreateAppointmentSut;
        var request = new CreateAppointmentRequest(
            fixture.Board.Id, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var appointment = await fixture.Db.Appointments.FindAsync(result.Value);
        appointment!.Price.Amount.Should().Be(100);
        appointment.Price.Currency.Should().Be("PLN");
    }

    [Fact]
    public async Task CreateAppointment_WithProvidedPrice_UsesProvidedPrice()
    {
        using var fixture = new Fixture().WithSeededEntities();
        var sut = fixture.CreateAppointmentSut;
        var request = new CreateAppointmentRequest(
            fixture.Board.Id, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", new PriceRequest(200));

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var appointment = await fixture.Db.Appointments.FindAsync(result.Value);
        appointment!.Price.Amount.Should().Be(200);
    }

    [Fact]
    public async Task GetAppointment_WithValidId_ReturnsAppointment()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.Location = "Room 1");
        var sut = fixture.GetAppointmentSut;

        var result = await sut.HandleAsync(appointmentId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Location.Should().Be("Room 1");
        result.Value.Status.Should().Be("Created");
    }

    [Fact]
    public async Task GetAppointment_WithInvalidId_ReturnsFailure()
    {
        using var fixture = new Fixture();
        var sut = fixture.GetAppointmentSut;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ToDone_Succeeds()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.UpdateAppointmentStatusSut;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("done"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Appointments.FindAsync(appointmentId);
        updated!.Status.Should().Be(AppointmentStatus.Done);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ToCancelled_Succeeds()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.UpdateAppointmentStatusSut;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("cancel"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Appointments.FindAsync(appointmentId);
        updated!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ToCancelledViaCancelled_Succeeds()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.UpdateAppointmentStatusSut;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("cancelled"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Appointments.FindAsync(appointmentId);
        updated!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_FromDone_ReturnsFailure()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Done, out _));
        var sut = fixture.UpdateAppointmentStatusSut;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("cancel"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAppointmentStatus_FromCancelled_ReturnsFailure()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Cancelled, out _));
        var sut = fixture.UpdateAppointmentStatusSut;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("done"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListAppointments_ByBoardAndDate_ReturnsFiltered()
    {
        using var fixture = new Fixture().WithSeededEntities();
        var sunday = fixture.Saturday.AddDays(1);
        fixture.Db.Attach(fixture.Board);
        fixture.Db.Attach(fixture.Client);
        fixture.Db.Attach(fixture.Service);
        fixture.Db.Appointments.AddRange(
            new Appointment
            {
                BoardId = fixture.Board.Id, ClientId = fixture.Client.Id, ServiceId = fixture.Service.Id,
                Date = fixture.Saturday, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
                Price = new Price(100), Location = "Room 1", CreatedBy = 1L
            },
            new Appointment
            {
                BoardId = fixture.Board.Id, ClientId = fixture.Client.Id, ServiceId = fixture.Service.Id,
                Date = sunday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0),
                Price = new Price(150), Location = "Room 2", CreatedBy = 1L
            });
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var sut = fixture.ListAppointmentsSut;
        var result = await sut.HandleAsync(fixture.Board.Id, [fixture.Saturday], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Location == "Room 1");
    }

    [Fact]
    public async Task UpdateAppointment_WithValidData_Updates()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.Location = "Room 1");
        var sut = fixture.UpdateAppointmentSut;
        var request = new UpdateAppointmentRequest(
            fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(11, 0), new TimeOnly(12, 0),
            "Updated description", "Room 2", new PriceRequest(120));

        var result = await sut.HandleAsync(appointmentId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await fixture.Db.Appointments.FindAsync(appointmentId);
        updated!.StartTime.Should().Be(new TimeOnly(11, 0));
        updated.EndTime.Should().Be(new TimeOnly(12, 0));
        updated.Location.Should().Be("Room 2");
        updated.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteAppointment_WithValidId_Deletes()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.DeleteAppointmentSut;

        var result = await sut.HandleAsync(appointmentId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var deleted = await fixture.Db.Appointments.FindAsync(appointmentId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAppointment_WhenDone_ReturnsFailure()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Done, out _));
        var sut = fixture.DeleteAppointmentSut;

        var result = await sut.HandleAsync(appointmentId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cannot delete a completed appointment");
    }

    [Fact]
    public async Task DeleteAppointment_WithNotFound_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.DeleteAppointmentSut;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task DeleteAppointment_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithSeededEntities();
        // Create an appointment on a board the current user (1L) does not belong to
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        fixture.Db.Boards.Add(otherBoard);
        await fixture.Db.SaveChangesAsync();

        fixture.Db.Attach(otherBoard);
        fixture.Db.Attach(fixture.Client);
        fixture.Db.Attach(fixture.Service);
        var appointment = new Appointment
        {
            BoardId = otherBoard.Id,
            ClientId = fixture.Client.Id,
            ServiceId = fixture.Service.Id,
            Date = fixture.Saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),
            CreatedBy = 2L
        };
        fixture.Db.Appointments.Add(appointment);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var sut = fixture.DeleteAppointmentSut;
        var result = await sut.HandleAsync(appointment.Id, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task DeleteAppointment_WhenDone_ReturnsConflictErrorCategory()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Done, out _));
        var sut = fixture.DeleteAppointmentSut;

        var result = await sut.HandleAsync(appointmentId, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Conflict);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_WithInvalidStatus_ReturnsValidationErrorCategory()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.UpdateAppointmentStatusSut;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("invalid"),
            CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_FromDone_ReturnsValidationErrorCategory()
    {
        using var fixture = new Fixture().WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Done, out _));
        var sut = fixture.UpdateAppointmentStatusSut;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("cancel"),
            CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task CreateAppointment_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture().WithSeededEntities();
        var sut = fixture.CreateAppointmentSut;
        var request = new CreateAppointmentRequest(
            999L, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task CreateAppointment_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture().WithSeededEntities();
        // Create a board the current user is not a member of
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        fixture.Db.Boards.Add(otherBoard);
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.CreateAppointmentSut;
        var request = new CreateAppointmentRequest(
            otherBoard.Id, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }

    [Fact]
    public async Task ListAppointments_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        using var fixture = new Fixture();
        var sut = fixture.ListAppointmentsSut;

        var result = await sut.HandleAsync(999L, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task ListAppointments_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        using var fixture = new Fixture();
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        fixture.Db.Boards.Add(otherBoard);
        await fixture.Db.SaveChangesAsync();

        var sut = fixture.ListAppointmentsSut;
        var result = await sut.HandleAsync(otherBoard.Id, null, CancellationToken.None);

        result.ErrorCategory.Should().Be(ResultCategories.AccessDenied);
    }
}
