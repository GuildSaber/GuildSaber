# GuildSaber.Api

The project is the Heart of GuildSaber, I will surely update this file with more information regarding the arctitecture
and design decisions. But for now, you can find the API documentation in the next section.

## Public Access

- API (Dev): https://api-dev.guildsaber.com
- API Documentation: OpenAPI/Scalar documentation available at the API URL

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/products/docker-desktop)
- [JetBrains Rider](https://www.jetbrains.com/rider/) (recommended) or Visual Studio 2026+

### Why Aspire?

I've opted for **Aspire** because it makes it easy to manage and orchestrate the whole stack of services.
You need a database? Here is one. You need to make sure the Bot only starts when the Api is ready? Sure.

Integrating it into the pipeline wasn't that easy, but it was worth it in the end. I hope you guys will like how easy it
is to get started developing on GuildSaber thanks to Aspire.

### Make the Configuration

Before starting the application, you need to make sure you have your configuration file ready.
One way to do it is copy the [`appsettings.json`](appsettings.json) file into an `appsettings.Development.json`
and fill it in your own values. This file will be ignored by git. As your whole environment will be running locally,
just make sure nothing in here is used in production in case you leak anything, or get compromised.

### Start the Application

You should be able to run the AppHost project, which should also automatically attach your debugger to all the services.
Pretty neat eh?

In case you need to populate the database with some data, you might wanna have a look at the Debug endpoint of this API.

A basic authentication flow is mostly about going to /auth/login/beatleader, then copying the token you get and use it
in the scalar client.

You might also need to add the following URLs to your oauth2 client configuration for both Discord and BeatLeader
respectively:
http://localhost:5042/signin-discord
http://localhost:5042/signin-beatleader
