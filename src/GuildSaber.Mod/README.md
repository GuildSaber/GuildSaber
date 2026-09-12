# GuildSaber.Mod

This project contains the in-game PC game mod for GuildSaber. Rather than using BSML (which you might be familiar with
if you've done modding before), this mod uses the ChatPlex_SDK UI framework (also called XUI), which is a code first
approach to building UI.

Why this choice? Because working with BSML also induces overheads. (You might also be aware of BeatLeader's Reactive UI
framework, which is another framework just like XUI, but made later by other devs that wanted to make their own things
for the same reasons.)

Another good reason to use XUI is that we have a good way to translate the UI code into a Quest mod code. As XUI was
originally made to bridge the gap between PC and Quest modding, we can expect a Quest mod to be trivially portable once
someone decides to make one.

## Architecture

I've chosen a Feature-based architecture for this mod, following the most modern approach to modding Beat Saber that I
could find. Which mean using Zenject, an assembly publicizer, and a modular approach to features.

This mod uses the GuildSaber.CSharpClient to interact with the API, which is the same client the DiscordBot uses. Beside
the compile target being a different framework, it makes sure that the behavior and code API will be the exact same
between the mod and the bot. Making it easier to maintain and develop features on both sides without worrying about
discrepancies between the two.

The publishing mechanism in place also uses ILRepack to include all the dependencies this mod uses into a single DLL. As
this project have a strict policy regarding compiler warnings, outdated and vulnerable dependencies will cause build
failures, and shipping it's own dependencies is a way to mitigate the risks of mod conflicts due to dependency
mismatches.

## Regarding supported versions

I've taken extra care of trying to support many versions of the game using a single build output (a single DLL). This is
made seamless thanks to some C# features like global usings and reflections. (Where version agnostic code with global
overloads replaces version specific code, and reflections is used to bridge the gap when there are not the same APIs
between versions).

It would be great if this can stay that way, however, if it becomes too much of a burden to maintain in the future, this
policy might just be dropped. However, the current state of the codebase is fairly good and clean in that regard.

## About compiling and releases

There is limitations with `BeatSaberModdingTools.Tasks` when it comes to zip-release when using a path incompatible
token in the semver mod version. I do compile this mod on Linux, so I don't have any problem making Release builds with
Zip outputs.

However, on Windows, you might wanna disable Zip release on zip the output manually. It shouldn't be an issue during
development as a Debug build is what should be used, just keep that in mind!
(As I'm the one making releases, this doesn't impact the project yet, but a PR to that repo might be needed in the
future to fix that file sanitization issue with \< and \> characters)

Obviously, to have the references working, you just need a `GuildSaber.Mod.csproj.user` file with the following
(adjusted for your game path):

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
    <PropertyGroup>
        <!-- Change this path if necessary. Make sure it ends with a backslash. -->
        <BeatSaberDir>/home/kuurama/BSManager/BSInstances/1.40.8</BeatSaberDir>
    </PropertyGroup>
</Project>
```

Also make sure to have the required dependencies in place, which you can probably find in the previous releases.