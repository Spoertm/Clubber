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
- **Discord Library**: Discord.Net 3.19.0
- **Web Framework**: ASP.NET Core with MVC and Razor Pages
- **API Documentation**: Swashbuckle.AspNetCore (Swagger)
- **Logging**: Serilog
- **Testing**: xUnit with NSubstitute for mocking

## Project Structure

The solution follows a **Clean Architecture** pattern with three main projects:

### 1. Clubber.Domain (Core Layer)
Location: `Clubber.Domain/`

Contains the core business logic and is independent of external frameworks.

| Directory | Purpose |
|-----------|---------|
| `BackgroundTasks/` | Background service base classes (`RepeatingBackgroundService`, `KeepAppAliveService`) |
| `Configuration/` | Application configuration (`AppConfig.cs`) including role IDs and Discord settings |
| `Data/` | Data files copied to output directory |
| `Extensions/` | Extension methods |
| `Features/` | Feature implementations organized by domain (DdSplits, HomingPeaks, Leaderboard, News, Roles, Splits, Users) |
| `Helpers/` | Utility classes (`DatabaseHelper`, `CollectionUtils`, `ExtensionMethods`) |
| `Models/` | Domain models, DTOs, and API response types |
| `Services/` | Core services (`DbService`, `WebService`, `UserService`) |

**Key Models:**
- `DdUser` - Registered user linking Discord ID to Devil Daggers leaderboard ID
- `EntryResponse` - Leaderboard entry data from ddinfo API
- `DdNewsItem` - News item for player achievements
- `BestSplit` - Best homing dagger count at specific time milestones
- `HomingPeakRun` - Top homing peak runs tracking

### 2. Clubber.Discord (Discord Bot Layer)
Location: `Clubber.Discord/`

Contains the Discord bot implementation including commands and interactions.

| Directory | Purpose |
|-----------|---------|
| `Commands/` | Discord slash commands organized by category |
| `Helpers/` | Discord-specific helpers (`DiscordHelper`, `EmbedHelper`) |
| `Logging/` | Custom Serilog sink for Discord logging |
| `Models/` | Discord-specific models and response types |
| `Modules/` | Discord interaction modules (`InfoCommands`, `ModeratorCommands`, `UserManagementCommands`, etc.) |
| `Preconditions/` | Command preconditions for access control |
| `Services/` | Discord services (`ScoreRoleService`, `RegistrationRequestHandler`, etc.) |

**Command Categories:**
- **Info Commands** (`InfoCommands.cs`): `help`, `bestsplits`, `toppeaks`
- **User Management** (`UserManagementCommands.cs`): Registration, role updates
- **Moderator Commands** (`ModeratorCommands.cs`): Admin operations
- **Owner Commands** (`OwnerCommands.cs`): Bot owner operations
- **Text Commands** (`TextCommands.cs`): Message-based commands

### 3. Clubber.Web (Presentation/Entry Point Layer)
Location: `Clubber.Web/`

The ASP.NET Core web application that hosts both the web UI and the Discord bot.

| Directory | Purpose |
|-----------|---------|
| `Configuration/` | Configuration setup (`ConfigurationSetup.cs`) |
| `Controllers/` | MVC controllers (`HomeController`) |
| `Endpoints/` | Minimal API endpoints (`ClubberEndpoints.cs`) |
| `Models/` | View models and DTOs |
| `Views/` | Razor views and layouts |
| `wwwroot/` | Static files (CSS, images, client-side libraries) |

**API Endpoints:**
- `GET /users` - All registered users
- `GET /user-count` - Registered user count
- `GET /users/by-leaderboardId` - Find user by leaderboard ID
- `GET /users/by-discordId` - Find user by Discord ID
- `GET /dailynews` - Recent DD news items
- `GET /bestsplits` - All best splits
- `GET /bestsplits/by-splitname` - Best split by name

### 4. Clubber.UnitTests (Test Layer)
Location: `Clubber.UnitTests/`

Unit tests using xUnit framework with NSubstitute for mocking.

| Test File | Coverage |
|-----------|----------|
| `CollectionUtilsTests.cs` | Collection manipulation utilities |
| `DdNewsMessageBuilderTests.cs` | News message formatting |
| `ExtentionMethodsTests.cs` | Extension method tests |
| `ScoreRoleServiceTests.cs` | Score role calculation logic |
| `UserServiceTests.cs` | User service operations |

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
dotnet test Clubber.UnitTests
```

### Publish
```bash
# Publish for deployment
dotnet publish -c Release -o ./publish
```

## Configuration

### Environment Variables (Production)
The application uses environment variables for configuration in production:

- `Configuration` - JSON string containing full app configuration (see `AppConfig.cs` for structure)
- `PostgresConnectionString` - PostgreSQL database connection string

### Development Configuration
In development, the app uses `appsettings.Development.json`:

```json
{
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
  "Serilog": {
    "MinimumLevel": "Debug"
  }
}
```

### Database
The application uses Entity Framework Core with PostgreSQL. The `DbService` class configures the connection via the `PostgresConnectionString` environment variable.

## Code Style Guidelines

### EditorConfig
The project uses `.editorconfig` for consistent code formatting:

- **Indentation**: Tabs (size 4) for C# files
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

Many StyleCop rules are explicitly suppressed (see `.editorconfig` lines 196-226).

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
- `appsettings.Dingling.json` - Additional test configuration

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

### Azure Web App
The project is configured for deployment to Azure Web App via GitHub Actions:

**Workflow**: `.github/workflows/master_clubberbot.yml`

**Deployment Process:**
1. Build on `push` to `temp` branch
2. Publish with `dotnet publish`
3. Deploy to Azure Web App named `clubberbot`

**Note**: The workflow currently triggers on pushes to `temp` branch (not `master`).

### Docker Support
The project includes Docker configuration:
- `DockerDefaultTargetOS` set to Linux in `Clubber.Web.csproj`
- `.dockerignore` file configured

## Key Dependencies

### Clubber.Domain
- `Microsoft.EntityFrameworkCore` 10.0.3
- `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0
- `SixLabors.ImageSharp` 3.1.12 - Image generation for leaderboard
- `Serilog` 4.3.1 - Logging

### Clubber.Discord
- `Discord.Net` 3.19.0 - Discord bot framework
- `Serilog` 4.3.1 - Logging

### Clubber.Web
- `Swashbuckle.AspNetCore.*` 10.1.4 - Swagger/OpenAPI
- `Serilog.Extensions.Hosting` 10.0.0
- `Serilog.Sinks.Console` 6.1.1

### Clubber.UnitTests
- `xUnit` 2.9.3
- `NSubstitute` 5.3.0
- `Microsoft.NET.Test.Sdk` 18.3.0

## Security Considerations

- **Bot Token**: Stored in configuration environment variable, never committed
- **Connection Strings**: PostgreSQL connection string via environment variable
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

### Background Services
Uses `BackgroundService` base class with a custom `RepeatingBackgroundService` for periodic tasks.

## External APIs

1. **Devil Daggers Info API** (ddinfo) - `http://devildaggers.info/`
   - Leaderboard data
   - Player statistics
   - World records

2. **Discord API** - Via Discord.Net
   - Bot interactions
   - Role management
   - Message operations
