# Clubber - AI Agent Guide

## Project Overview

Clubber is a Discord bot for the DD Pals Discord server that integrates with the Devil Daggers game leaderboard. It automatically manages user roles based on their game scores, posts daily news about player achievements, and provides a web API for accessing community data.

**Primary Features:**

- **Role Management**: Automatically updates Discord roles based on Devil Daggers scores (ranging from 0s to 1300s)
- **DD News**: Notifies when players achieve personal bests above 1000 seconds
- **Personal Stats**: Provides detailed player statistics via the `stats` command
- **Best Splits Tracking**: Tracks and displays the best homing dagger counts at various time milestones
- **Web API**: RESTful API for accessing registered users, daily news, and best splits data

## Technology Stack

- **Framework**: .NET 10.0
- **Language**: C# 14.0
- **Database**: PostgreSQL with Entity Framework Core 10.0
- **Discord Library**: Discord.Net 3.19.1
- **Web Framework**: ASP.NET Core with MVC and Razor Pages
- **API Documentation**: Scalar (OpenAPI/Swagger alternative)
- **Logging**: Serilog
- **Testing**: xUnit with NSubstitute for mocking

## Project Structure

The solution follows a **Clean Architecture** pattern with three main projects:

### 1. Clubber.Domain (Core Layer)

Location: `Clubber.Domain/`

Contains the core business logic and is independent of external frameworks.

| Directory          | Purpose                                                                                                         |
| ------------------ | --------------------------------------------------------------------------------------------------------------- |
| `Assets/`          | Static assets (fonts, flags) copied to output directory                                                         |
| `BackgroundTasks/` | Background service base classes (`RepeatingBackgroundService`, `ExactBackgroundService`, `KeepAppAliveService`) |
| `Configuration/`   | Application configuration (`AppConfig.cs`, `Endpoints.cs`) including role IDs and Discord settings              |
| `Data/`            | Entity Framework entities and `AppDbContext`                                                                    |
| `Extensions/`      | Extension methods (`ExtensionMethods.cs`)                                                                       |
| `Helpers/`         | Utility classes (`CollectionUtils`, `LeaderboardImageGenerator`, `RegistrationTracker`, `RunAnalyzer`)          |
| `Repositories/`    | Data access layer (`IUserRepository`, `INewsRepository`, `ILeaderboardRepository`, `IPlayerPbRepository`)       |
| `Models/`          | Domain models, DTOs, and API response types                                                                     |
| `Services/`        | Core services (`IWebService`, `WebService`, `RoleConfigService`)                                                |

**Key Models:**

- `DdUser` - Registered user linking Discord ID to Devil Daggers leaderboard ID
- `EntryResponse` - Leaderboard entry data from ddinfo API
- `DdNewsItem` - News item for player achievements
- `BestSplit` - Best homing dagger count at specific time milestones
- `HomingPeakRun` - Top homing peak runs tracking
- `ScoreRole` - Configurable score role threshold
- `RankRole` - Configurable rank role threshold

### 2. Clubber.Discord (Discord Bot Layer)

Location: `Clubber.Discord/`

Contains the Discord bot implementation including commands and interactions.

| Directory        | Purpose                                                                                                                                                                                             |
| ---------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Commands/`      | Empty directory (commands are defined in Modules)                                                                                                                                                   |
| `Helpers/`       | Discord-specific helpers (`DiscordHelper`, `EmbedHelper`, `DdNewsMessageBuilder`)                                                                                                                   |
| `Logging/`       | Custom Serilog sink for Discord logging                                                                                                                                                             |
| `Models/`        | Discord-specific models and response types                                                                                                                                                          |
| `Modules/`       | Discord interaction modules (`InfoCommands`, `ModeratorCommands`, `UserManagementCommands`, `OwnerCommands`, `TextCommands`, `ComponentInteractions`)                                               |
| `Preconditions/` | Command preconditions for access control                                                                                                                                                            |
| `Services/`      | Discord services (`ScoreRoleService`, `RegistrationRequestHandler`, `UserService`, `DatabaseUpdateService`, `DdNewsPostService`, `ChannelClearingService`, `TextCommandHandler`, `UserJoinHandler`) |

**Command Categories:**

- **Info Commands** (`InfoCommands.cs`): `help`, `bestsplits`, `toppeaks`
- **User Management** (`UserManagementCommands.cs`): `register`, `unregister`, `update`, `stats`
- **Moderator Commands** (`ModeratorCommands.cs`): Admin operations
- **Owner Commands** (`OwnerCommands.cs`): Bot owner operations
- **Text Commands** (`TextCommands.cs`): Message-based commands

### 3. Clubber.Web (Presentation/Entry Point Layer)

Location: `Clubber.Web/`

The ASP.NET Core web application that hosts both the web UI and the Discord bot.

| Directory      | Purpose                                           |
| -------------- | ------------------------------------------------- |
| `Controllers/` | MVC controllers (`HomeController`)                |
| `Endpoints/`   | Minimal API endpoints (`ClubberEndpoints.cs`)     |
| `Models/`      | View models and DTOs                              |
| `Views/`       | Razor views and layouts                           |
| `wwwroot/`     | Static files (CSS, images, client-side libraries) |

**API Endpoints:**

- `GET /users` - All registered users
- `GET /user-count` - Registered user count
- `GET /users/by-leaderboardId` - Find user by leaderboard ID
- `GET /users/by-discordId` - Find user by Discord ID
- `GET /dailynews` - Recent DD news items
- `GET /bestsplits` - All best splits
- `GET /bestsplits/by-splitname` - Best split by name

### 4. Clubber.Tests (Test Layer)

Location: `Clubber.Tests/`

Test project containing both unit tests and integration tests using xUnit framework with NSubstitute for mocking.

| Folder              | Purpose                                   |
| ------------------- | ----------------------------------------- |
| `UnitTests/`        | Unit tests for isolated component testing |
| `IntegrationTests/` | Integration tests for end-to-end testing  |

**Unit Tests:**

| Test File                       | Coverage                                         |
| ------------------------------- | ------------------------------------------------ |
| `CollectionUtilsTests.cs`       | Collection manipulation utilities                |
| `DdNewsMessageBuilderTests.cs`  | News message formatting                          |
| `ExtentionMethodsTests.cs`      | Extension method tests (note: filename has typo) |
| `RunAnalyzerTests.cs`           | Run analysis logic                               |
| `ScoreRoleServiceTests.cs`      | Score role calculation logic                     |
| `UserServiceTests.cs`           | User service operations                          |
| `LeaderboardRepositoryTests.cs` | Leaderboard repository operations                |
| `NewsRepositoryTests.cs`        | News repository operations                       |

## Build and Run Commands

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL database
- Discord Bot Token

### Build

```bash
# Build the entire solution
dotnet build

# Build in Release mode
dotnet build --configuration Release
```

### Run

```bash
# Run the web application (entry point)
dotnet run --project Clubber.Web

# Or run from solution
dotnet run --project Clubber.Web --configuration Release
```

### Test

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run tests for specific project
dotnet test Clubber.Tests
```

### Publish

```bash
# Publish for deployment
dotnet publish -c Release -o ./publish
```

## Configuration

### Environment Variables (Production)

The application uses standard .NET environment variable configuration in production. Hierarchical settings are mapped using double underscores (`__`) as section delimiters.

Examples:

- `BotConfig__BotToken` - Discord bot token
- `BotConfig__Prefix` - Command prefix (default: `+`)
- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string

See `AppConfig.cs` for all available configuration keys.

### Development Configuration

In development, the app uses `appsettings.Development.json`:

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres;"
    },
    "Serilog": {
        "Using": ["Serilog.Sinks.Console"],
        "MinimumLevel": {
            "Default": "Debug"
        },
        "WriteTo": [
            {
                "Name": "Console"
            }
        ],
        "Enrich": ["FromLogContext"]
    },
    "BotConfig": {
        "Prefix": "+",
        "BotToken": "your-discord-bot-token",
        "RegisterChannelId": 123456789,
        "RoleAssignerRoleId": 123456789,
        "CheaterRoleId": 123456789,
        "DdPalsId": 123456789,
        "UnregisteredRoleId": 123456789,
        "DailyUpdateChannelId": 123456789,
        "DailyUpdateLoggingChannelId": 123456789,
        "DdNewsChannelId": 123456789,
        "ModsChannelId": 123456789,
        "NoScoreRoleId": 123456789,
        "Endpoints": {
            "GetMultipleUsersById": "",
            "GetScores": "",
            "GetWorldRecords": "",
            "GetCountryCodeForPlayer": "",
            "GetPlayerHistory": "",
            "GetDdstatsResponse": ""
        }
    }
}
```

### Database

The application uses Entity Framework Core 10.0 with PostgreSQL. The connection string is read from configuration (`ConnectionStrings:DefaultConnection`).

## Code Style Guidelines

### EditorConfig

The project uses `.editorconfig` for consistent code formatting:

- **Indentation**: Spaces (size 4) for all files
- **Project Files**: Spaces (size 2) for `.csproj` files
- **Encoding**: UTF-8
- **Line Endings**: Unix-style with final newlines

### Naming Conventions

- **Private Fields**: `_camelCase` (e.g., `_databaseHelper`)
- **Public Members**: `PascalCase`
- **Interfaces**: `IPascalCase` with `I` prefix
- **Methods**: `PascalCase`
- **Parameters**: `camelCase`
- **Constants**: `PascalCase`

### Language Preferences

- **var**: Disabled - always use explicit types
- **Expression-bodied Members**: Allowed for accessors and properties
- **this.**: Not required (set to suggestion)
- **Nullable**: Enabled project-wide

### Analyzers

The project includes multiple static analysis tools:

- **StyleCop.Analyzers** - Style enforcement
- **SonarAnalyzer.CSharp** - Code quality
- **Roslynator.Analyzers** - Additional analyzers
- **AnalysisMode**: Set to `All` for maximum analysis

Many StyleCop rules are explicitly suppressed (see `.editorconfig` for specific rule severities).

## Testing Strategy

### Test Framework

- **xUnit** - Primary testing framework
- **NSubstitute** - Mocking framework
- **Microsoft.NET.Test.Sdk** - Test SDK

### Test Organization

Tests are organized by the class they test:

- `ScoreRoleServiceTests` tests `ScoreRoleService`
- `CollectionUtilsTests` tests `CollectionUtils`
- etc.

### Test Data

Test configuration files:

- `appsettings.Testing.json` - Test-specific configuration

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbosity
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~ScoreRoleServiceTests"
```

## Background Services

The application runs several background services in production:

1. **DatabaseUpdateService** - Daily updates of all user roles
2. **DdNewsPostService** - Monitors and posts news about player achievements
3. **KeepAppAliveService** - Prevents Azure Web App from sleeping
4. **ChannelClearingService** - Automated channel maintenance

## Deployment

### Railway

The project is hosted on [Railway](https://railway.app/) using their managed platform:

- **CI/CD**: Automatic deployments triggered by commits to the connected GitHub repository
- **Infrastructure**: Managed by Railway (no manual workflow files)
- **Database**: PostgreSQL hosted on Supabase

### Docker Support

The project includes Docker configuration:

- `DockerDefaultTargetOS` set to Linux in `Clubber.Web.csproj`
- `.dockerignore` file configured

## Key Dependencies

### Clubber.Domain

- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `SkiaSharp` - Image generation for leaderboard
- `SkiaSharp.NativeAssets.Linux` - Linux native assets for cross-platform deployment
- `Serilog` - Logging
- `Roslynator.Analyzers` - Code analyzers
- `SonarAnalyzer.CSharp` - Code quality analyzers
- `StyleCop.Analyzers` - Style analyzers

### Clubber.Discord

- `Discord.Net` - Discord bot framework
- `Serilog` - Logging

### Clubber.Web

- `Microsoft.AspNetCore.OpenApi` - OpenAPI support
- `Microsoft.EntityFrameworkCore.Relational`
- `Scalar.AspNetCore` - API documentation UI
- `Serilog.Extensions.Hosting`
- `Serilog.Settings.Configuration`
- `Serilog.Sinks.Console`

### Clubber.Tests

- `xUnit`
- `NSubstitute`
- `Microsoft.NET.Test.Sdk`
- `Microsoft.EntityFrameworkCore.Sqlite` - In-memory testing

## Security Considerations

- **Bot Token**: Stored in configuration environment variable, never committed
- **Connection Strings**: PostgreSQL connection string via configuration
- **Configuration**: Production configuration via `Configuration` environment variable as JSON
- **CORS**: API allows any origin (configured in `Program.cs`)

## Architecture Patterns

### Clean Architecture

- Domain layer has no external dependencies
- Discord layer depends on Domain
- Web layer depends on both Discord and Domain

### Dependency Injection

Heavy use of DI throughout:

- Singleton services for Discord client and handlers
- Transient services for database operations
- Scoped services for request handling

### Result Pattern

The application uses a `Result<T>` and `Result` type for operation results instead of exceptions for expected failures.

### Repository Pattern

Data access is organized through repository interfaces in `Clubber.Domain/Repositories/`:

- **IUserRepository** - User registration, Twitch linking, lookups by Discord/Leaderboard ID
- **INewsRepository** - DD news items and cleanup
- **ILeaderboardRepository** - Best splits, and top homing peaks
- **IPlayerPbRepository** - Player personal best tracking for news detection

Repositories are registered as transient services.

### Background Services

Uses `BackgroundService` base class with custom implementations:

- `RepeatingBackgroundService` - For periodic tasks
- `ExactBackgroundService` - For tasks running at specific UTC times

## External APIs

1. **Hasmodai API** - `http://dd.hasmodai.com/`
    - Official Devil Daggers leaderboard data
    - Player scores and IDs

2. **Devil Daggers Info API** (ddinfo) - `https://devildaggers.info/`
    - Extended leaderboard data
    - Player statistics and history
    - World records
    - Country codes

3. **DDStats** - `https://ddstats.com/`
    - Full run data and statistics

4. **Discord API** - Via Discord.Net
    - Bot interactions
    - Role management
    - Message operations
