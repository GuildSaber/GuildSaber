# GuildSaber

A multi tenant ranking system for Beat Saber, designed to manage and provide player rankings across multiple guilds.
This project is built using .NET 10 and uses .NET Aspire for its orchestration.

## 📝 License Information

This project is primarily licensed under GNU AGPL-3.0.

Common sense utility code that isn't business logic (such as helper methods, extensions, and convenience utilities) is
excluded from AGPL-3.0 restrictions and can be freely used without limitations.

This project has been a part of my life for over 4 years. While it's a passion project, it has also required significant
investment of time and resources. The licensing structure ensures the core code remains open source even if forked,
benefiting the entire community.

By contributing to this project, you agree to assign copyright to Kuurama to maintain unified project governance. This
allows for consistent decision-making while ensuring your work remains accessible through open source licensing. All
contributions are valued and recognized. See [LICENSE-NOTICE.md](LICENSE-NOTICE.md) for details.

## 🌐 Public Access

- API (Dev): None
- API Documentation: OpenAPI/Scalar documentation available at the API URL

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [JetBrain Mono Font](https://www.jetbrains.com/lp/mono/)
- [Docker](https://www.docker.com/products/docker-desktop)
- [JetBrains Rider](https://www.jetbrains.com/rider/) (recommended) or Visual Studio 2026+

### Configuration

Before running the project, you need to configure the following settings:

1. **API Settings** (`src/GuildSaber.Api/appsettings.json`):
   ```json
   {
     "AuthSettings": {
       "Jwt": {
         "Secret": "YourSecureJwtSecret"
       },
       "BeatLeader": {
         "ClientId": "YourBeatLeaderClientId",
         "ClientSecret": "YourBeatLeaderClientSecret"
       },
       "Discord": {
         "ClientId": "YourDiscordClientId",
         "ClientSecret": "YourDiscordClientSecret"
       }
     }
   }
   ```

2. **Discord Bot Settings** (`src/GuildSaber.DiscordBot/appsettings.json`):
   ```json
   {
     "DiscordBotOptions": {
       "Id": 123456789012345678,
       "Name": "GuildSaber Bot",
       "Status": "Managing guilds",
       "Token": "YourDiscordBotToken",
       "ManagerId": 123456789012345678,
       "GuildId": 123456789012345678
     }
   }
   ```

### Starting the Project with Aspire

GuildSaber uses .NET Aspire for orchestrating its microservices architecture.

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Kuurama/GuildSaber.git
   cd GuildSaber
   ```

2. **Run the Aspire application:**
   ```bash
   cd aspire/GuildSaber.AppHost
   dotnet run
   ```

This will start the following services:

- Postgre (with PgWeb)
- Redis (with Redis Commander)
- Database Migrator
- API Service
- Discord Bot

The Aspire dashboard will open automatically, showing the status of all services.

## 🧩 Project Structure

- `src/GuildSaber.Api` - Main API service
- `src/GuildSaber.Database` - Database models and contexts
- `src/GuildSaber.DiscordBot` - Discord bot integration
- `src/GuildSaber.Common` - Shared utilities and services
- `aspire/` - .NET Aspire project orchestration
- `tools/GuildSaber.Migrator` - Database migration tool

## 🗄️ Database Migrations (EFCore)

GuildSaber uses Entity Framework Core (EFCore) for database schema management. Migrations are organized in the
`Migrations` folder inside each context directory under `src/GuildSaber.Database/Contexts`.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [EFCore CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (install with
  `dotnet tool install --global dotnet-ef` if not already installed)

### Generating a Migration

1. **Navigate to the database project:**
   ```bash
   cd src/GuildSaber.Database
   ```

2. **Add a migration for a specific context:**

   For example, to add a migration for the `ServerDbContext` (migrations will be placed in
   `Contexts/Server/Migrations`):

   ```bash
   dotnet ef migrations add <MigrationName> \
     --context GuildSaber.Database.Contexts.Server.ServerDbContext \
     --output-dir Contexts/Server/Migrations
   ```

   For the DiscordBot context (replace with the actual context class name if different):

   ```bash
   dotnet ef migrations add <MigrationName> \
     --context GuildSaber.Database.Contexts.DiscordBot.DiscordBotDbContext \
     --output-dir Contexts/DiscordBot/Migrations
   ```

   Replace `<MigrationName>` with a descriptive name for your migration.

3. The database migrations will be automatically applied by the `GuildSaber.Migrator` service when it starts.

### Notes

- Always ensure you are in the `src/GuildSaber.Database` directory when running EFCore commands.
- Each context should have its own migrations in its respective `Migrations` folder.
- If you add or modify models, generate a new migration as shown above.
- They should be applied automatically when the `GuildSaber.Migrator` service starts.
- Do **NOT** push migrations to the repositories unless they reflect changes in the production database schema. Until
  then, no migrations should be pushed to the repository.

## 🤝 Contributing

Before contributing, please ensure you understand the license implications. All contributions will be subject to the
project's licensing terms.

1. Set up your development environment as described above
2. Fork the repository and create a feature branch
3. Make your changes
4. Ensure all tests pass by running:
   ```bash
   dotnet test
   ```
5. Submit a pull request

When committing, sign your commits to acknowledge the contribution terms:

```bash
git commit -s -m "Your commit message"
```

## 📚 Documentation

API documentation is available at the root URL of the API when running locally or at the hosted API URL.

For detailed implementation documentation, refer to the code comments and unit tests.
