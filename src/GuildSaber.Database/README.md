# GuildSaber.Database

Database project containing EF Core models and contexts.

## Database Migrations (EFCore)

GuildSaber uses Entity Framework Core (EFCore) for database schema management.
Each DBContext migrations are organized in their respective `Migrations` folder.

### Prerequisites

- [EFCore CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (install with
  `dotnet tool install --global dotnet-ef` if not already installed)

However, I would simply recommend using the `Entity Framework Core UI` plugin from JetBrains Rider.

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
- They will be applied automatically when the `GuildSaber.Migrator` service starts.
- Do **NOT** push migrations to the repositories unless they reflect changes in the production database schema. Until
  then, no migrations should be pushed to the repository.

