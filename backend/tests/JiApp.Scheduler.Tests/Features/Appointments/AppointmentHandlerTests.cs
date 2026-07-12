using JiApp.Common.Abstractions;
using JiApp.Common.Resilience;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Appointments.CreateAppointment;
using JiApp.Scheduler.Features.Appointments.DeleteAppointment;
using JiApp.Scheduler.Features.Appointments.GetAppointment;
using JiApp.Scheduler.Features.Appointments.ListAppointments;
using JiApp.Scheduler.Features.Appointments.UpdateAppointment;
using JiApp.Scheduler.Features.Appointments.UpdateAppointmentStatus;
using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Tests.Features.Appointments;

public sealed class AppointmentHandlerTests : HandlerTestBase
{
    private sealed class Fixture
    {
        private readonly ISchedulerDbContext _dbContext;
        private readonly SchedulerDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IRetryPolicyFactory _retryPolicy;
        private readonly DateOnly _saturday;
        private Board? _board;
        private Client? _client;
        private Service? _service;

        private Fixture(ISchedulerDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _db = (SchedulerDbContext)dbContext;
            var currentUserMock = MockCurrentUserService.GetSuccessful();
            _currentUser = currentUserMock.Mock.Object;
            CurrentUserMock = currentUserMock;
            _retryPolicy = new RetryPolicyFactory();

            var start = DateOnly.FromDateTime(DateTime.UtcNow);
            while (start.DayOfWeek != DayOfWeek.Saturday)
                start = start.AddDays(1);
            _saturday = start;
        }

        public MockCurrentUserService CurrentUserMock { get; }
        public DateOnly Saturday => _saturday;
        public Board Board => _board!;
        public Client Client => _client!;
        public Service Service => _service!;

        public CreateAppointmentHandler Sut => new(_dbContext, _currentUser, _retryPolicy);
        public CreateAppointmentHandler CreateAppointment => new(_dbContext, _currentUser, _retryPolicy);
        public GetAppointmentHandler GetAppointment => new(_dbContext, _currentUser);
        public ListAppointmentsHandler ListAppointments => new(_dbContext, _currentUser);
        public UpdateAppointmentHandler UpdateAppointment => new(_dbContext, _currentUser, _retryPolicy);
        public UpdateAppointmentStatusHandler UpdateAppointmentStatus => new(_dbContext, _currentUser);
        public DeleteAppointmentHandler DeleteAppointment => new(_dbContext, _currentUser);

        public static Fixture Init(ISchedulerDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithSeededEntities()
        {
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

            _db.Boards.Add(board);
            _db.Clients.Add(client);
            _db.Services.Add(service);
            _db.SaveChanges();
            _db.ChangeTracker.Clear();

            _board = board;
            _client = client;
            _service = service;

            return this;
        }

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
    }

    [Fact]
    public async Task CreateAppointment_WithValidData_ReturnsAppointmentId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.Sut;
        var request = new CreateAppointmentRequest(
            fixture.Board.Id, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAppointment_WithOverlappingTime_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
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
        StoreInDb(existing);

        var sut = fixture.CreateAppointment;
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
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.CreateAppointment;
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
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.CreateAppointment;
        var request = new CreateAppointmentRequest(
            fixture.Board.Id, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        var appointment = Db.Find<Appointment>(result.Value);
        appointment!.Price.Amount.Should().Be(100);
        appointment.Price.Currency.Should().Be("PLN");
    }

    [Fact]
    public async Task CreateAppointment_WithProvidedPrice_UsesProvidedPrice()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.CreateAppointment;
        var request = new CreateAppointmentRequest(
            fixture.Board.Id, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", new PriceRequest(200));

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        var appointment = Db.Find<Appointment>(result.Value);
        appointment!.Price.Amount.Should().Be(200);
    }

    [Fact]
    public async Task GetAppointment_WithValidId_ReturnsAppointment()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.Location = "Room 1");
        var sut = fixture.GetAppointment;

        var result = await sut.HandleAsync(appointmentId, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Location.Should().Be("Room 1");
        result.Value.Status.Should().Be("Created");
    }

    [Fact]
    public async Task GetAppointment_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.GetAppointment;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ToDone_Succeeds()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.UpdateAppointmentStatus;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("done"),
            CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Appointment>(appointmentId);
        updated!.Status.Should().Be(AppointmentStatus.Done);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ToCancelled_Succeeds()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.UpdateAppointmentStatus;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("cancel"),
            CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Appointment>(appointmentId);
        updated!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ToCancelledViaCancelled_Succeeds()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.UpdateAppointmentStatus;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("cancelled"),
            CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Appointment>(appointmentId);
        updated!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_FromDone_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Done, out _));
        var sut = fixture.UpdateAppointmentStatus;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("cancel"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAppointmentStatus_FromCancelled_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Cancelled, out _));
        var sut = fixture.UpdateAppointmentStatus;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("done"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ListAppointments_ByBoardAndDate_ReturnsFiltered()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sunday = fixture.Saturday.AddDays(1);
        var boardId = fixture.Board.Id;
        var clientId = fixture.Client.Id;
        var serviceId = fixture.Service.Id;
        StoreInDb(new Appointment
        {
            BoardId = boardId, ClientId = clientId, ServiceId = serviceId,
            Date = fixture.Saturday, StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
            Price = new Price(100), Location = "Room 1", CreatedBy = 1L
        });
        StoreInDb(new Appointment
        {
            BoardId = boardId, ClientId = clientId, ServiceId = serviceId,
            Date = sunday, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0),
            Price = new Price(150), Location = "Room 2", CreatedBy = 1L
        });

        var sut = fixture.ListAppointments;
        var result = await sut.HandleAsync(fixture.Board.Id, [fixture.Saturday], CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().ContainSingle(a => a.Location == "Room 1");
    }

    [Fact]
    public async Task UpdateAppointment_WithValidData_Updates()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.Location = "Room 1");
        var sut = fixture.UpdateAppointment;
        var request = new UpdateAppointmentRequest(
            fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(11, 0), new TimeOnly(12, 0),
            "Updated description", "Room 2", new PriceRequest(120));

        var result = await sut.HandleAsync(appointmentId, request, CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Appointment>(appointmentId);
        updated!.StartTime.Should().Be(new TimeOnly(11, 0));
        updated.EndTime.Should().Be(new TimeOnly(12, 0));
        updated.Location.Should().Be("Room 2");
        updated.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteAppointment_WithValidId_Deletes()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.DeleteAppointment;

        var result = await sut.HandleAsync(appointmentId, CancellationToken.None);

        AssertSuccess(result);
        var deleted = Db.Find<Appointment>(appointmentId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAppointment_WhenDone_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Done, out _));
        var sut = fixture.DeleteAppointment;

        var result = await sut.HandleAsync(appointmentId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cannot delete a completed appointment");
    }

    [Fact]
    public async Task DeleteAppointment_WithNotFound_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.DeleteAppointment;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task DeleteAppointment_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        StoreInDb(otherBoard);

        var client = new Client { Name = "Alice", BoardId = otherBoard.Id };
        var service = new Service
        {
            Name = "Haircut",
            Category = ServiceCategory.MensHaircut,
            BaseDuration = 30,
            BasePrice = new Price(100),
            BoardId = otherBoard.Id
        };
        StoreInDb(client);
        StoreInDb(service);
        var appointment = new Appointment
        {
            BoardId = otherBoard.Id,
            ClientId = client.Id,
            ServiceId = service.Id,
            Date = fixture.Saturday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Price = new Price(100),
            CreatedBy = 2L
        };
        StoreInDb(appointment);

        var sut = fixture.DeleteAppointment;
        var result = await sut.HandleAsync(appointment.Id, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task DeleteAppointment_WhenDone_ReturnsConflictErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Done, out _));
        var sut = fixture.DeleteAppointment;

        var result = await sut.HandleAsync(appointmentId, CancellationToken.None);

        AssertConflict(result);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_WithInvalidStatus_ReturnsValidationErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId);
        var sut = fixture.UpdateAppointmentStatus;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("invalid"),
            CancellationToken.None);

        AssertValidationFailure(result);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_FromDone_ReturnsValidationErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        fixture.WithAppointment(out var appointmentId, a => a.TryTransitionTo(AppointmentStatus.Done, out _));
        var sut = fixture.UpdateAppointmentStatus;

        var result = await sut.HandleAsync(appointmentId, new UpdateAppointmentStatusRequest("cancel"),
            CancellationToken.None);

        AssertValidationFailure(result);
    }

    [Fact]
    public async Task CreateAppointment_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var sut = fixture.CreateAppointment;
        var request = new CreateAppointmentRequest(
            999L, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task CreateAppointment_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSeededEntities();
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        StoreInDb(otherBoard);

        var sut = fixture.CreateAppointment;
        var request = new CreateAppointmentRequest(
            otherBoard.Id, fixture.Client.Id, fixture.Service.Id,
            fixture.Saturday, new TimeOnly(10, 0), new TimeOnly(11, 0),
            null, "Room 1", null);

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task ListAppointments_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.ListAppointments;

        var result = await sut.HandleAsync(999L, null, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task ListAppointments_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var otherBoard = new Board { Name = "Other", MemberUserIds = [2L] };
        StoreInDb(otherBoard);

        var sut = fixture.ListAppointments;
        var result = await sut.HandleAsync(otherBoard.Id, null, CancellationToken.None);

        AssertAccessDenied(result);
    }
}
