# JiApp Scheduler — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Scheduler microservice (JiApp.Scheduler) + React Native mobile module for weekend appointment management with expenses, clients, and revenue reports.

**Architecture:** New ASP.NET Core 10 microservice on port 5004 with its own PostgreSQL/SQLite database, following the existing feature-slice pattern (Endpoint/Handler/Request/Response/Validator). YARP Gateway routes `/api/v1/scheduler/**` to it. Mobile module follows the yt-downloader pattern with JiModule interface, navigator, screens, hooks, and services.

**Tech Stack:** .NET 10, EF Core 10, FluentValidation, JWT Bearer, Serilog, React Native 0.85, TypeScript, axios, i18next

**Spec:** `docs/superpowers/specs/2026-05-28-scheduler-design.md`

---

## File Structure

### Backend — New Files

```
backend/src/JiApp.Scheduler/
├── JiApp.Scheduler.csproj
├── Program.cs
├── Startup.cs
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
├── Configuration/
│   └── SchedulerSettings.cs
├── Domain/
│   ├── Board.cs
│   ├── Client.cs
│   ├── Service.cs
│   ├── ServiceCategory.cs
│   ├── Appointment.cs
│   ├── AppointmentStatus.cs
│   ├── Expense.cs
│   ├── ExpenseCategory.cs
│   └── Price.cs
├── Persistence/
│   ├── SchedulerDbContext.cs
│   ├── SchedulerDbContextFactory.cs
│   └── Configurations/
│       ├── BoardConfiguration.cs
│       ├── ClientConfiguration.cs
│       ├── ServiceConfiguration.cs
│       ├── AppointmentConfiguration.cs
│       └── ExpenseConfiguration.cs
├── Features/
│   ├── Boards/
│   │   ├── CreateBoard/
│   │   │   ├── CreateBoardEndpoint.cs
│   │   │   ├── CreateBoardHandler.cs
│   │   │   ├── CreateBoardRequest.cs
│   │   │   └── CreateBoardValidator.cs
│   │   ├── GetBoard/
│   │   │   ├── GetBoardEndpoint.cs
│   │   │   ├── GetBoardHandler.cs
│   │   │   └── GetBoardResponse.cs
│   │   ├── UpdateBoard/
│   │   │   ├── UpdateBoardEndpoint.cs
│   │   │   ├── UpdateBoardHandler.cs
│   │   │   ├── UpdateBoardRequest.cs
│   │   │   └── UpdateBoardValidator.cs
│   │   └── AddBoardMember/
│   │       ├── AddBoardMemberEndpoint.cs
│   │       ├── AddBoardMemberHandler.cs
│   │       ├── AddBoardMemberRequest.cs
│   │       └── AddBoardMemberValidator.cs
│   ├── Clients/
│   │   ├── CreateClient/
│   │   ├── ListClients/
│   │   ├── GetClient/
│   │   ├── UpdateClient/
│   │   └── DeleteClient/
│   ├── Services/
│   │   ├── CreateService/
│   │   ├── ListServices/
│   │   ├── GetService/
│   │   ├── UpdateService/
│   │   └── DeleteService/
│   ├── Appointments/
│   │   ├── CreateAppointment/
│   │   ├── ListAppointments/
│   │   ├── GetAppointment/
│   │   ├── UpdateAppointment/
│   │   ├── UpdateAppointmentStatus/
│   │   └── DeleteAppointment/
│   ├── Expenses/
│   │   ├── CreateExpense/
│   │   ├── ListExpenses/
│   │   ├── GetExpense/
│   │   ├── UpdateExpense/
│   │   └── DeleteExpense/
│   └── Reports/
│       ├── RevenueReport/
│       │   ├── RevenueReportEndpoint.cs
│       │   ├── RevenueReportHandler.cs
│       │   ├── RevenueReportRequest.cs
│       │   └── RevenueReportResponse.cs
│       └── ClientReport/
│           ├── ClientReportEndpoint.cs
│           ├── ClientReportHandler.cs
│           ├── ClientReportRequest.cs
│           └── ClientReportResponse.cs

backend/tests/JiApp.Scheduler.Tests/
├── JiApp.Scheduler.Tests.csproj
└── [test files per feature]
```

### Backend — Modified Files
- `backend/JiApp.sln` — add 2 projects (Scheduler + Scheduler.Tests)
- `backend/docker-compose.yml` — add scheduler service
- `backend/docker-compose.dev.yml` — add scheduler dev service
- `backend/src/JiApp.Gateway/appsettings.json` — add Scheduler route to YARP config
- `backend/src/JiApp.Gateway/Configuration/GatewaySettings.cs` — add Scheduler health check URL
- `backend/src/JiApp.Gateway/Features/HealthDashboard/HealthDashboardEndpoint.cs` — add Scheduler to dashboard

### Mobile — New Files
```
mobile/src/modules/scheduler/
├── index.ts
├── navigator.tsx
├── screens/
│   ├── WeekendGridScreen.tsx
│   ├── AppointmentDetailScreen.tsx
│   ├── CreateAppointmentScreen.tsx
│   ├── ClientListScreen.tsx
│   ├── ClientDetailScreen.tsx
│   ├── ServiceListScreen.tsx
│   ├── ServiceEditScreen.tsx
│   └── ReportsScreen.tsx
├── components/
│   ├── AppointmentCard.tsx
│   ├── DayColumn.tsx
│   ├── DayTotalFooter.tsx
│   ├── ExpenseCard.tsx
│   ├── SummaryBar.tsx
│   ├── WeekendNavigator.tsx
│   └── ClientPicker.tsx
├── hooks/
│   ├── useWeekendGrid.ts
│   ├── useAppointments.ts
│   ├── useClients.ts
│   ├── useServices.ts
│   ├── useExpenses.ts
│   └── useReports.ts
├── services/
│   ├── appointmentService.ts
│   ├── clientService.ts
│   ├── serviceCatalogService.ts
│   ├── expenseService.ts
│   └── reportService.ts
├── types/
│   └── api.ts
└── utils/
    └── weekendUtils.ts
```

### Mobile — Modified Files
- `mobile/src/shell/ModuleRegistry.ts` — register scheduler module
- `mobile/src/i18n/en.json` — add scheduler strings
- `mobile/src/i18n/pl.json` — add scheduler strings

---

## Phase 1: Foundation

### Task 1.1: Create JiApp.Scheduler project

**Files:**
- Create: `backend/src/JiApp.Scheduler/JiApp.Scheduler.csproj`
- Create: `backend/src/JiApp.Scheduler/Program.cs`
- Create: `backend/src/JiApp.Scheduler/Startup.cs`
- Create: `backend/src/JiApp.Scheduler/Configuration/SchedulerSettings.cs`
- Create: `backend/src/JiApp.Scheduler/appsettings.json`
- Create: `backend/src/JiApp.Scheduler/appsettings.Development.json`
- Create: `backend/src/JiApp.Scheduler/Dockerfile`
- Modify: `backend/JiApp.sln`

- [ ] **Step 1: Create JiApp.Scheduler.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\JiApp.Common\JiApp.Common.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.8" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.8">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.8" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.2" />
    <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
  </ItemGroup>

  <PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="JiApp.Scheduler.Tests" />
    <InternalsVisibleTo Include="DynamicProxyGenAssembly2" Key="0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create SchedulerSettings.cs**

```csharp
namespace JiApp.Scheduler.Configuration;

public sealed class SchedulerSettings
{
    public string? ConnectionString { get; set; }
    public JwtSettings? Jwt { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("ConnectionString is required");
        Jwt?.Validate();
    }
}

public sealed class JwtSettings
{
    public string? Key { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("Jwt:Key is required");
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("Jwt:Issuer is required");
        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Jwt:Audience is required");
    }
}
```

- [ ] **Step 3: Create Program.cs**

```csharp
using JiApp.Scheduler;
using JiApp.Scheduler.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var settings = new SchedulerSettings();
builder.Configuration.Bind(settings);
settings.Validate();

var startup = new Startup(settings);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
Startup.Configure(app);
app.Run();
```

- [ ] **Step 4: Create Startup.cs** (scaffold without features)

```csharp
using System.Text;
using System.Text.Json;
using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using JiApp.Common.Services;
using JiApp.Scheduler.Configuration;
using JiApp.Scheduler.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;

namespace JiApp.Scheduler;

public class Startup(SchedulerSettings settings)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<GlobalExceptionMiddleware>();

        services.AddDbContext<SchedulerDbContext>(options =>
            options.UseSqlite(settings.ConnectionString!));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = settings.Jwt!.Issuer!,
                    ValidAudience = settings.Jwt!.Audience!,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.Jwt!.Key!)),
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var response = JsonSerializer.Serialize(
                            new ApiErrorResponse(Error: "Unauthorized"), ApiErrorResponse.JsonOptions);
                        return context.Response.WriteAsync(response);
                    }
                };
            });

        services.AddAuthorization();
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyMethod().AllowAnyHeader().AllowCredentials()
                    .SetIsOriginAllowed(_ => true);
            });
        });

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton(settings);
        services.AddHttpContextAccessor();
    }

    public static void Configure(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseSerilogRequestLogging();

        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
                using (LogContext.PushProperty("CorrelationId", correlationId))
                {
                    await next();
                }
            }
            else
            {
                await next();
            }
        });

        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        var scheduler = app.MapGroup("/api/v1/scheduler");

        // Features will map here in subsequent tasks

        scheduler.MapGet("/health", async (SchedulerDbContext db) =>
            {
                var dbOk = await db.Database.CanConnectAsync();
                return dbOk
                    ? Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow })
                    : Results.Problem("Database unavailable", statusCode: 503);
            })
            .WithTags("System")
            .WithSummary("Health check")
            .Produces(StatusCodes.Status200OK);
    }
}
```

- [ ] **Step 5: Create appsettings.json**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "ConnectionString": "Data Source=../../.data/scheduler_dev.db",
  "Jwt": {
    "Key": "",
    "Issuer": "JiApp",
    "Audience": "JiApp"
  }
}
```

- [ ] **Step 6: Create appsettings.Development.json**

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}
```

- [ ] **Step 7: Create Dockerfile**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/src/JiApp.Scheduler/JiApp.Scheduler.csproj backend/src/JiApp.Scheduler/
COPY backend/src/JiApp.Common/JiApp.Common.csproj backend/src/JiApp.Common/
RUN dotnet restore backend/src/JiApp.Scheduler/JiApp.Scheduler.csproj
COPY backend/ .
RUN dotnet publish backend/src/JiApp.Scheduler/JiApp.Scheduler.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5004
ENTRYPOINT ["dotnet", "JiApp.Scheduler.dll"]
```

- [ ] **Step 8: Add to solution**

```bash
cd backend && dotnet sln JiApp.sln add src/JiApp.Scheduler/JiApp.Scheduler.csproj
```

- [ ] **Step 9: Build to verify**

```bash
dotnet build backend/JiApp.sln
```

Expected: 0 errors.

- [ ] **Step 10: Commit**

```bash
git add backend/src/JiApp.Scheduler/ backend/JiApp.sln
git commit -m "feat: scaffold JiApp.Scheduler project with Startup pattern"
```

---

### Task 1.2: Create domain model

**Files:**
- Create: `backend/src/JiApp.Scheduler/Domain/Price.cs`
- Create: `backend/src/JiApp.Scheduler/Domain/ServiceCategory.cs`
- Create: `backend/src/JiApp.Scheduler/Domain/AppointmentStatus.cs`
- Create: `backend/src/JiApp.Scheduler/Domain/ExpenseCategory.cs`
- Create: `backend/src/JiApp.Scheduler/Domain/Board.cs`
- Create: `backend/src/JiApp.Scheduler/Domain/Client.cs`
- Create: `backend/src/JiApp.Scheduler/Domain/Service.cs`
- Create: `backend/src/JiApp.Scheduler/Domain/Appointment.cs`
- Create: `backend/src/JiApp.Scheduler/Domain/Expense.cs`

- [ ] **Step 1: Create Price value object**

```csharp
namespace JiApp.Scheduler.Domain;

public sealed record Price
{
    public decimal Amount { get; init set; }
    public string Currency { get; init set; } = "PLN";

    public Price() { }

    public Price(decimal amount, string currency = "PLN")
    {
        Amount = amount;
        Currency = currency;
    }
}
```

- [ ] **Step 2: Create enums**

```csharp
// ServiceCategory.cs
namespace JiApp.Scheduler.Domain;

public enum ServiceCategory
{
    MensHaircut,
    WomensHaircut,
    WomensStyling,
    Coloring,
    Treatment,
    Other
}
```

```csharp
// AppointmentStatus.cs
namespace JiApp.Scheduler.Domain;

public enum AppointmentStatus
{
    Created,
    Done,
    Cancelled
}
```

```csharp
// ExpenseCategory.cs
namespace JiApp.Scheduler.Domain;

public enum ExpenseCategory
{
    Fuel,
    Hotel,
    Parking,
    Supplies,
    Food,
    Other
}
```

- [ ] **Step 3: Create Board entity**

```csharp
using JiApp.Common.Domain;

namespace JiApp.Scheduler.Domain;

public sealed class Board : BaseEntity<long>
{
    public string Name { get; set; } = string.Empty;
    public List<long> MemberUserIds { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 4: Create Client entity**

```csharp
using JiApp.Common.Domain;

namespace JiApp.Scheduler.Domain;

public sealed class Client : BaseEntity<long>
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public List<Appointment> Appointments { get; set; } = [];
}
```

- [ ] **Step 5: Create Service entity**

```csharp
using JiApp.Common.Domain;

namespace JiApp.Scheduler.Domain;

public sealed class Service : BaseEntity<long>
{
    public long BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ServiceCategory Category { get; set; }
    public int BaseDuration { get; set; }
    public Price BasePrice { get; set; } = new();
    public Board Board { get; set; } = null!;
}
```

- [ ] **Step 6: Create Appointment entity**

```csharp
using JiApp.Common.Domain;

namespace JiApp.Scheduler.Domain;

public sealed class Appointment : BaseEntity<long>
{
    public long BoardId { get; set; }
    public long ClientId { get; set; }
    public long ServiceId { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public Price Price { get; set; } = new();
    public string Location { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }

    public Board Board { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
```

- [ ] **Step 7: Create Expense entity**

```csharp
using JiApp.Common.Domain;

namespace JiApp.Scheduler.Domain;

public sealed class Expense : BaseEntity<long>
{
    public long BoardId { get; set; }
    public DateOnly Date { get; set; }
    public ExpenseCategory Category { get; set; }
    public Price Amount { get; set; } = new();
    public string? Note { get; set; }
    public Board Board { get; set; } = null!;
}
```

- [ ] **Step 8: Build to verify**

```bash
dotnet build backend/JiApp.sln
```

Expected: 0 errors.

- [ ] **Step 9: Commit**

```bash
git add backend/src/JiApp.Scheduler/Domain/
git commit -m "feat: add Scheduler domain model (Board, Client, Service, Appointment, Expense, Price)"
```

---

### Task 1.3: Create DbContext and EF Core configurations

**Files:**
- Create: `backend/src/JiApp.Scheduler/Persistence/SchedulerDbContext.cs`
- Create: `backend/src/JiApp.Scheduler/Persistence/SchedulerDbContextFactory.cs`
- Create: `backend/src/JiApp.Scheduler/Persistence/Configurations/BoardConfiguration.cs`
- Create: `backend/src/JiApp.Scheduler/Persistence/Configurations/ClientConfiguration.cs`
- Create: `backend/src/JiApp.Scheduler/Persistence/Configurations/ServiceConfiguration.cs`
- Create: `backend/src/JiApp.Scheduler/Persistence/Configurations/AppointmentConfiguration.cs`
- Create: `backend/src/JiApp.Scheduler/Persistence/Configurations/ExpenseConfiguration.cs`

- [ ] **Step 1: Create SchedulerDbContext.cs**

```csharp
using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Persistence;

public sealed class SchedulerDbContext : DbContext
{
    public SchedulerDbContext(DbContextOptions<SchedulerDbContext> options) : base(options) { }

    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BoardConfiguration());
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceConfiguration());
        modelBuilder.ApplyConfiguration(new AppointmentConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
    }
}
```

- [ ] **Step 2: Create SchedulerDbContextFactory.cs**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace JiApp.Scheduler.Persistence;

public sealed class SchedulerDbContextFactory : IDesignTimeDbContextFactory<SchedulerDbContext>
{
    public SchedulerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<SchedulerDbContext>();
        builder.UseSqlite(configuration.GetConnectionString());
        return new SchedulerDbContext(builder.Options);
    }
}
```

- [ ] **Step 3: Create EF configurations** — each follows the same pattern:

```csharp
// BoardConfiguration.cs
using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Scheduler.Persistence;

public sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}
```

```csharp
// ClientConfiguration.cs
using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Scheduler.Persistence;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);
    }
}
```

```csharp
// ServiceConfiguration.cs
using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Scheduler.Persistence;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
        builder.OwnsOne(x => x.BasePrice, price =>
        {
            price.Property(p => p.Amount).HasColumnName("BasePrice_Amount").HasColumnType("decimal(18,2)");
            price.Property(p => p.Currency).HasColumnName("BasePrice_Currency").HasMaxLength(3).HasDefaultValue("PLN");
        });
        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId);
    }
}
```

```csharp
// AppointmentConfiguration.cs
using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Scheduler.Persistence;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.OwnsOne(x => x.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("Price_Amount").HasColumnType("decimal(18,2)");
            price.Property(p => p.Currency).HasColumnName("Price_Currency").HasMaxLength(3).HasDefaultValue("PLN");
        });
        builder.HasOne(x => x.Client).WithMany(c => c.Appointments).HasForeignKey(x => x.ClientId);
        builder.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId);
        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId);
    }
}
```

```csharp
// ExpenseConfiguration.cs
using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Scheduler.Persistence;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.OwnsOne(x => x.Amount, price =>
        {
            price.Property(p => p.Amount).HasColumnName("Amount_Amount").HasColumnType("decimal(18,2)");
            price.Property(p => p.Currency).HasColumnName("Amount_Currency").HasMaxLength(3).HasDefaultValue("PLN");
        });
        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId);
    }
}
```

- [ ] **Step 4: Register DbContext in Startup.cs** — already done in Task 1.1 Step 4 (line: `services.AddDbContext<SchedulerDbContext>`).

- [ ] **Step 5: Create initial migration**

```bash
cd backend/src/JiApp.Scheduler && dotnet ef migrations add InitialCreate
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build backend/JiApp.sln
```

Expected: 0 errors.

- [ ] **Step 7: Commit**

```bash
git add backend/src/JiApp.Scheduler/Persistence/ backend/src/JiApp.Scheduler/Migrations/
git commit -m "feat: add SchedulerDbContext with EF Core configurations and initial migration"
```

---

### Task 1.4: Add Docker Compose + Gateway routing

**Files:**
- Modify: `backend/docker-compose.yml`
- Modify: `backend/docker-compose.dev.yml`
- Modify: `backend/src/JiApp.Gateway/appsettings.json`

- [ ] **Step 1: Add scheduler to docker-compose.yml**

Add under `services:`:

```yaml
  scheduler:
    build:
      context: ..
      dockerfile: backend/src/JiApp.Scheduler/Dockerfile
    ports:
      - "5004:5004"
    environment:
      - ConnectionString=Host=postgres;Port=5432;Database=jiapp_scheduler;Username=postgres;Password=postgres
      - Jwt__Key=${JWT_KEY}
      - Jwt__Issuer=${JWT_ISSUER:-JiApp}
      - Jwt__Audience=${JWT_AUDIENCE:-JiApp}
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - jiapp-network
```

- [ ] **Step 2: Add scheduler to docker-compose.dev.yml**

Add under `services:`:

```yaml
  scheduler:
    build:
      context: ..
      dockerfile: backend/src/JiApp.Scheduler/Dockerfile
    ports:
      - "5004:5004"
    environment:
      - ConnectionString=Data Source=/data/scheduler_dev.db
      - Jwt__Key=${JWT_KEY:-dev-jwt-key-change-in-production}
      - Jwt__Issuer=${JWT_ISSUER:-JiApp}
      - Jwt__Audience=${JWT_AUDIENCE:-JiApp}
    volumes:
      - ../.data:/data
    networks:
      - jiapp-network
```

- [ ] **Step 3: Add Gateway route in appsettings.json**

In the `ReverseProxy.Clusters` section, add:

```json
"SchedulerCluster": {
  "Destinations": {
    "scheduler": {
      "Address": "http://scheduler:5004/"
    }
  }
}
```

In `ReverseProxy.Routes`, add:

```json
"SchedulerRoute": {
  "ClusterId": "SchedulerCluster",
  "Match": {
    "Path": "/api/v1/scheduler/{**catch-all}"
  },
  "Transforms": [
    { "PathPattern": "/api/v1/scheduler/{**catch-all}" }
  ]
}
```

- [ ] **Step 4: Add Scheduler health check to Gateway**

In `backend/src/JiApp.Gateway/Configuration/GatewaySettings.cs`, add `HealthCheckUrls` entry:

```csharp
public Dictionary<string, string> HealthCheckUrls { get; set; } = new()
{
    ["Identity"] = "http://identity:5001/api/v1/auth/health",
    ["YtDownloader"] = "http://ytdownloader:5002/api/v1/yt/health",
    ["ImageTools"] = "http://imagetools:5003/api/v1/imagetools/health",
    ["Scheduler"] = "http://scheduler:5004/api/v1/scheduler/health"
};
```

- [ ] **Step 5: Build to verify**

```bash
dotnet build backend/JiApp.sln
```

Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add backend/docker-compose.yml backend/docker-compose.dev.yml backend/src/JiApp.Gateway/
git commit -m "feat: add Scheduler to Docker Compose and Gateway routing"
```

---

### Task 1.5: Create JiApp.Scheduler.Tests project

**Files:**
- Create: `backend/tests/JiApp.Scheduler.Tests/JiApp.Scheduler.Tests.csproj`
- Modify: `backend/JiApp.sln`

- [ ] **Step 1: Create test project**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.8" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\JiApp.Scheduler\JiApp.Scheduler.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add to solution**

```bash
cd backend && dotnet sln JiApp.sln add tests/JiApp.Scheduler.Tests/JiApp.Scheduler.Tests.csproj
```

- [ ] **Step 3: Build and run tests (should pass with 0 tests)**

```bash
dotnet test backend/tests/JiApp.Scheduler.Tests/
```

Expected: Build succeeds, 0 tests.

- [ ] **Step 4: Commit**

```bash
git add backend/tests/JiApp.Scheduler.Tests/ backend/JiApp.sln
git commit -m "feat: scaffold JiApp.Scheduler.Tests project"
```

---

### Task 1.6: Mobile module scaffold

**Files:**
- Create: `mobile/src/modules/scheduler/index.ts`
- Create: `mobile/src/modules/scheduler/navigator.tsx`
- Create: `mobile/src/modules/scheduler/types/api.ts`
- Create: `mobile/src/modules/scheduler/screens/WeekendGridScreen.tsx` (placeholder)
- Modify: `mobile/src/shell/ModuleRegistry.ts`

- [ ] **Step 1: Create types/api.ts**

```typescript
export interface Price {
  amount: number;
  currency: string;
}

export type ServiceCategory =
  | 'MensHaircut'
  | 'WomensHaircut'
  | 'WomensStyling'
  | 'Coloring'
  | 'Treatment'
  | 'Other';

export type AppointmentStatus = 'Created' | 'Done' | 'Cancelled';

export type ExpenseCategory = 'Fuel' | 'Hotel' | 'Parking' | 'Supplies' | 'Food' | 'Other';

export interface Board {
  id: number;
  name: string;
  memberUserIds: number[];
  createdAt: string;
}

export interface Client {
  id: number;
  name: string;
  phone?: string;
  notes?: string;
}

export interface ServiceItem {
  id: number;
  boardId: number;
  name: string;
  category: ServiceCategory;
  baseDuration: number;
  basePrice: Price;
}

export interface Appointment {
  id: number;
  boardId: number;
  client: Client;
  service: ServiceItem;
  description?: string;
  date: string;
  startTime: string;
  endTime: string;
  price: Price;
  location: string;
  status: AppointmentStatus;
}

export interface Expense {
  id: number;
  boardId: number;
  date: string;
  category: ExpenseCategory;
  amount: Price;
  note?: string;
}

export interface DayTotal {
  revenue: number;
  expenses: number;
  net: number;
}

export interface RevenueReport {
  groupKey: string;
  revenue: number;
  expenses: number;
  net: number;
  appointmentCount: number;
}

export interface ClientReportItem {
  client: Client;
  visitCount: number;
  totalSpent: number;
  lastVisit: string;
  averagePerVisit: number;
}
```

- [ ] **Step 2: Create index.ts**

```typescript
import { JiModule } from '../../../shell/types';
import { SchedulerNavigator } from './navigator';

export const schedulerModule: JiModule = {
  id: 'scheduler',
  name: 'modules.scheduler.title',
  icon: 'scheduler',
  component: SchedulerNavigator,
  enabled: true,
};
```

- [ ] **Step 3: Create navigator.tsx** (placeholder)

```typescript
import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { WeekendGridScreen } from './screens/WeekendGridScreen';

export type SchedulerStackParamList = {
  WeekendGrid: undefined;
};

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

export const SchedulerNavigator: React.FC = () => {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="WeekendGrid" component={WeekendGridScreen} />
    </Stack.Navigator>
  );
};
```

- [ ] **Step 4: Create placeholder WeekendGridScreen**

```typescript
import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors } from '../../../../styles/theme';

export const WeekendGridScreen: React.FC = () => {
  return (
    <View style={styles.container}>
      <Text style={styles.text}>Scheduler — Coming soon</Text>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
    alignItems: 'center',
    justifyContent: 'center',
  },
  text: {
    fontSize: 18,
    color: colors.textSecondary,
  },
});
```

- [ ] **Step 5: Register module in ModuleRegistry.ts**

Add to `initModules()`:

```typescript
// In the lazy import section:
const schedulerModule = await import('../modules/scheduler');
registerModule(schedulerModule.schedulerModule);
```

- [ ] **Step 6: Type check**

```bash
cd mobile && npx tsc --noEmit
```

Expected: No type errors.

- [ ] **Step 7: Commit**

```bash
git add mobile/src/modules/scheduler/ mobile/src/shell/ModuleRegistry.ts
git commit -m "feat: scaffold scheduler mobile module with placeholder screen"
```

---

### Task 1.7: Board CRUD backend

**Files:**
- Create: `backend/src/JiApp.Scheduler/Features/Boards/CreateBoard/` (4 files)
- Create: `backend/src/JiApp.Scheduler/Features/Boards/GetBoard/` (3 files)
- Create: `backend/src/JiApp.Scheduler/Features/Boards/UpdateBoard/` (4 files)
- Create: `backend/src/JiApp.Scheduler/Features/Boards/AddBoardMember/` (4 files)
- Modify: `backend/src/JiApp.Scheduler/Startup.cs` — register handlers and map endpoints

- [ ] **Step 1: Create CreateBoard feature files**

```csharp
// CreateBoardRequest.cs
namespace JiApp.Scheduler.Features.Boards.CreateBoard;

[Serializable]
public sealed record CreateBoardRequest(string Name);
```

```csharp
// CreateBoardValidator.cs
using FluentValidation;

namespace JiApp.Scheduler.Features.Boards.CreateBoard;

public sealed class CreateBoardValidator : AbstractValidator<CreateBoardRequest>
{
    public CreateBoardValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
```

```csharp
// CreateBoardHandler.cs — returns the created Board ID
using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Boards.CreateBoard;

public sealed class CreateBoardHandler(SchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(CreateBoardRequest request, CancellationToken ct)
    {
        var board = new Board
        {
            Name = request.Name,
            MemberUserIds = [currentUser.UserId]
        };
        db.Boards.Add(board);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(board.Id);
    }
}
```

```csharp
// CreateBoardEndpoint.cs
using FluentValidation;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Boards.CreateBoard;

public static class CreateBoardEndpoint
{
    public static void MapCreateBoard(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/boards", async (
            CreateBoardRequest request,
            IValidator<CreateBoardRequest> validator,
            CreateBoardHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
                return validation.ToValidationError();

            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Created($"/boards/{result.Value}", new { id = result.Value })
                : Results.Problem(result.Error);
        })
        .RequireAuthorization()
        .WithTags("Boards")
        .WithSummary("Create a board")
        .Produces(StatusCodes.Status201Created);
    }
}
```

- [ ] **Step 2: Create GetBoard, UpdateBoard, AddBoardMember** following the same pattern. Each feature needs:
  - Request record (except Get which uses route param)
  - Handler with DbContext injection
  - Endpoint static class with MapXxx extension method on IEndpointRouteBuilder
  - Validator for mutating operations

- [ ] **Step 3: Register in Startup.cs ConfigureServices**

```csharp
// Handlers
services.AddScoped<CreateBoardHandler>();
services.AddScoped<GetBoardHandler>();
services.AddScoped<UpdateBoardHandler>();
services.AddScoped<AddBoardMemberHandler>();

// Validators
services.AddScoped<IValidator<CreateBoardRequest>, CreateBoardValidator>();
services.AddScoped<IValidator<UpdateBoardRequest>, UpdateBoardValidator>();
services.AddScoped<IValidator<AddBoardMemberRequest>, AddBoardMemberValidator>();
```

- [ ] **Step 4: Map endpoints in Startup.Configure**

```csharp
scheduler.MapCreateBoard();
scheduler.MapGetBoard();
scheduler.MapUpdateBoard();
scheduler.MapAddBoardMember();
```

- [ ] **Step 5: Build and verify**

```bash
dotnet build backend/JiApp.sln
```

Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add backend/src/JiApp.Scheduler/Features/Boards/
git commit -m "feat: add Board CRUD endpoints with validation"
```

---

### Task 1.8: Client CRUD backend

**Files:**
- Create: `backend/src/JiApp.Scheduler/Features/Clients/CreateClient/` (4 files)
- Create: `backend/src/JiApp.Scheduler/Features/Clients/ListClients/` (3 files)
- Create: `backend/src/JiApp.Scheduler/Features/Clients/GetClient/` (3 files)
- Create: `backend/src/JiApp.Scheduler/Features/Clients/UpdateClient/` (4 files)
- Create: `backend/src/JiApp.Scheduler/Features/Clients/DeleteClient/` (2 files)
- Modify: `backend/src/JiApp.Scheduler/Startup.cs`

- [ ] **Step 1: Create all Client feature files** following the same pattern as Boards. Key differences:
  - Clients are global (no BoardId filter on list)
  - List supports `?q=search` query parameter
  - GetClient includes appointment history in response
  - DeleteClient checks for existing appointments before deleting

- [ ] **Step 2: Create ListClientsHandler** — the key query handler:

```csharp
using JiApp.Common.Abstractions;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Clients.ListClients;

public sealed class ListClientsHandler(SchedulerDbContext db)
{
    public async Task<Result<List<ClientResponse>>> HandleAsync(string? q, CancellationToken ct)
    {
        var query = db.Clients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }
        var clients = await query
            .OrderBy(c => c.Name)
            .Select(c => new ClientResponse(c.Id, c.Name, c.Phone, c.Notes))
            .ToListAsync(ct);
        return Result<List<ClientResponse>>.Success(clients);
    }
}

[Serializable]
public sealed record ClientResponse(long Id, string Name, string? Phone, string? Notes);
```

- [ ] **Step 3: Create DeleteClientHandler** with appointment check:

```csharp
using JiApp.Common.Abstractions;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Clients.DeleteClient;

public sealed class DeleteClientHandler(SchedulerDbContext db)
{
    public async Task<Result> HandleAsync(long id, CancellationToken ct)
    {
        var hasAppointments = await db.Appointments.AnyAsync(a => a.ClientId == id, ct);
        if (hasAppointments)
            return Result.Failure("Cannot delete client with existing appointments");

        var client = await db.Clients.FindAsync([id], ct);
        if (client is null)
            return Result.Failure("Client not found");

        db.Clients.Remove(client);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

- [ ] **Step 4: Register handlers/validators in Startup.cs, map endpoints**

- [ ] **Step 5: Build and verify**

- [ ] **Step 6: Commit**

---

### Task 1.9: Verify Phase 1 end-to-end

- [ ] **Step 1: Build full solution**

```bash
dotnet build backend/JiApp.sln
```

Expected: 0 errors, 13 projects built.

- [ ] **Step 2: Run all backend tests**

```bash
dotnet test backend/JiApp.sln
```

Expected: All existing tests pass + any new Scheduler tests.

- [ ] **Step 3: Run mobile type check**

```bash
cd mobile && npx tsc --noEmit
```

Expected: No type errors.

- [ ] **Step 4: Run mobile tests**

```bash
cd mobile && npx jest --passWithNoTests
```

Expected: All existing tests pass.

- [ ] **Step 5: Commit**

```bash
git commit -m "chore: Phase 1 complete — Scheduler foundation with Boards and Clients"
```

**Phase 1 checkpoint.** Backend has Scheduler project with domain model, DbContext, migrations, Docker/Gateway integration, Board + Client CRUD. Mobile has module scaffold registered in shell.

---

## Phase 2: Appointments

### Task 2.1: Service catalog CRUD backend

Follow the same feature-slice pattern as Clients. 5 feature folders: CreateService, ListServices, GetService, UpdateService, DeleteService.

Key behaviors:
- Services are board-scoped (filter by BoardId)
- List supports `?category=X&boardId=Y`
- Delete prevents if service is used in appointments
- Service includes Price as owned entity

### Task 2.2: Appointment CRUD backend

6 feature folders: CreateAppointment, ListAppointments, GetAppointment, UpdateAppointment, UpdateAppointmentStatus, DeleteAppointment.

Key behaviors:
- **Create validation**: Date must be Saturday or Sunday, no overlapping appointments for same board+date, StartTime < EndTime, valid BoardId/ClientId/ServiceId
- **Overlap detection**: Query existing appointments for same board+date where `(startTime < new.EndTime) && (endTime > new.StartTime)`
- **Price**: Defaults from Service.BasePrice if not provided
- **Status transitions**: PATCH endpoint accepts `"done"` or `"cancel"` — Created → Done, Created → Cancelled
- **List**: Supports `?boardId=X&date=2026-05-30&date=2026-05-31` for date range

### Task 2.3: Backend tests for appointments

Write tests using the existing Fixture/Context pattern from YtDownloader.Tests:
- Create appointment with valid data → 201
- Create with weekday date → 400
- Create with overlapping time → 400
- Create with invalid client/service/board IDs → 400
- Change status to done → 200
- Change status to cancelled → 200
- List by board + date range → returns correct appointments

### Task 2.4: Weekend utility + i18n strings

- Create `mobile/src/modules/scheduler/utils/weekendUtils.ts` with `getWeekendDates(referenceDate: Date)` returning `{ saturday: Date; sunday: Date }`
- Add all scheduler i18n keys to `en.json` and `pl.json` under `modules.scheduler.*`

### Task 2.5: WeekendGridScreen + AppointmentCard + DayColumn

Build the main screen with:
- `WeekendNavigator` — prev/next weekend arrows with date label
- `SummaryBar` — 2×2 grid showing Appointments | Revenue | Expenses | Net Profit
- Two `DayColumn` components side by side
- `AppointmentCard` component — time, duration chip, client name, service with gender dot, price, location
- Empty slot placeholders
- FAB "+" button

### Task 2.6: CreateAppointmentScreen + AppointmentDetailScreen

- Date picker (weekend-only constraint)
- Client picker with search dropdown + quick-create inline modal
- Service picker that auto-fills duration and price
- Time slot entry
- Description field, location field
- Submit with validation errors from API

### Task 2.7: API services (mobile)

Create service files in `services/`:
- `appointmentService.ts` — createAppointment, listAppointments, getAppointment, updateAppointment, updateStatus, deleteAppointment
- `clientService.ts` — createClient, listClients, getClient, updateClient, deleteClient
- `serviceCatalogService.ts` — createService, listServices, getService, updateService, deleteService

### Task 2.8: Phase 2 verification

- Build + test backend
- Mobile type check + tests
- Manual: create a board, services, clients, then create appointments via Postman
- Manual: view weekend grid on mobile with appointments

---

## Phase 3: Expenses + Day P&L

### Task 3.1: Expense CRUD backend

5 feature folders: CreateExpense, ListExpenses, GetExpense, UpdateExpense, DeleteExpense.

Key behaviors:
- List supports `?boardId=X&date=Y`
- Expense amount uses Price value object
- Day totals computed in a separate query or handler

### Task 3.2: Day totals endpoint

Add a lightweight endpoint or embed day totals in the list response:

```
GET /api/v1/scheduler/day-totals?boardId=X&date=Y
Response: { revenue: decimal, expenses: decimal, net: decimal }
```

### Task 3.3: Expense UI components

- `ExpenseCard` — category label, amount (amber), optional note, left border
- `DayTotalFooter` — sticky at bottom of each day column: Revenue, Expenses, Day net
- "+ Add expense" button below expenses section

### Task 3.4: Create/modify expense screen

Simple modal: category picker, amount, optional note. Edit/delete from the card.

### Task 3.5: Phase 3 verification

- Backend: expense CRUD tests pass
- Mobile: expenses visible in day columns, day totals compute correctly

---

## Phase 4: Reports

### Task 4.1: Revenue report backend

```
GET /reports/revenue?boardId=X&from=Y&to=Z&groupBy=weekend|service|location|client
```

Returns: groupKey, revenue, expenses, net, appointmentCount — grouped by the specified dimension.

### Task 4.2: Client analytics backend

```
GET /reports/clients?boardId=X&sortBy=frequency|totalSpent|lastVisit
```

Returns per client: client info, visitCount, totalSpent, lastVisit, averagePerVisit. Flag clients with no visits in 30+ days.

### Task 4.3: ReportsScreen (mobile)

Two tabs: Revenue (with groupBy selector) and Clients (with sort selector). Simple card-based display.

### Task 4.4: Phase 4 verification

- Revenue report returns correct grouped data
- Client analytics show correct stats
- Mobile reports screen renders with data

---

## Phase 5: Polish

### Task 5.1: i18n completion

Add all strings to `en.json` and `pl.json`:
- Module title, tab labels, screen titles
- Service categories, expense categories, appointment statuses
- Report labels, groupBy and sortBy options
- Validation error messages
- Empty states

### Task 5.2: Error handling

- Network errors show toast
- Validation errors display inline
- Empty states on all lists
- Loading states on all screens

### Task 5.3: Postman collection

Create Postman collection with all Scheduler endpoints:
- Environment variable for `JWT_TOKEN` (obtained from login)
- Pre-request script to auto-refresh token
- Test scripts for each endpoint

### Task 5.4: Final QA

- `dotnet build backend/JiApp.sln` — 0 errors
- `dotnet test backend/JiApp.sln` — all pass
- `cd mobile && npx tsc --noEmit` — no errors
- `cd mobile && npx jest` — all pass
- Manual: full user flow — create board, add clients/services, schedule appointments, add expenses, view reports

---

## Verification Checklist

- [ ] `dotnet build backend/JiApp.sln` — 0 errors, 13+ projects
- [ ] `dotnet test backend/JiApp.sln` — all pass (existing + new Scheduler tests)
- [ ] `docker compose up` — all 5 services healthy
- [ ] Gateway `/health/dashboard` shows Scheduler green
- [ ] Postman: CRUD on all endpoint groups
- [ ] `cd mobile && npx tsc --noEmit` — 0 errors
- [ ] `cd mobile && npx jest` — all pass
- [ ] Weekend grid renders with appointments
- [ ] Create appointment flow works end-to-end
- [ ] Expense tracking and day totals work
- [ ] Reports display correct data
