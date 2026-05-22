# GuildSaber.DiscordBot

This is the Discord bot integration for GuildSaber.
Similarly to the Website, this service relies on an authentication to access the API, however, it got a more privileged
access.

This bot uses an API key authentication method which allows the bot to perform actions on behalf of the user using the
bot.
To make this more bearable, the Client authentication logic is abstracted away in a Lazy client that automatically
grabs the command context user and uses their ID to act on behalf of them.

Hence why the bot code primarily consists of compile time constructs and switch expression to reduce the likelihood of
bugs and security issues due to unchecked cases.

## Development

- Part of the main solution and orchestrated via .NET Aspire.

## Configuration

To get started, you must setup a configuration file named `appsettings.Development.json`, following
[`appsettings.json`](appsettings.json) as a template.

Once done, you can start the AppHost project, which will automatically start the bot once the API is available.