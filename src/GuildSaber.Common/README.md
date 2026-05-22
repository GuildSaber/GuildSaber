# GuildSaber.Common

This project contains shared code and utilities used across the different GuildSaber projects.
It contains the following (non-exhaustive):

Client for the following Services:

- BeatLeader (Fully typed client with websocket support as an asynchronous stream abstraction returning discriminated
  unions of events and responses).
- ScoreSaber (Just the strict minimum to get the data we need).
- LegacyGuildSaber (To allow synchronizing data with the old API while the transition is still ongoing).
- BeatSaver (To get the maps data and metadata we need).

There is also one abstraction that the Common project offers by default:

- Strongly typed constructs.

Those are a list of types that are used across the codebase to represent concepts like "SongHash", "GuildId", "
PlayerId",
"BeatLeaderId", "SteamId", "ScoreSaberId", "BeatSaverKey", etc, removing primitive obsession.

You will find that most of the code follows the "Parse don't validate" principle, which is one of my favorite software
design principles.