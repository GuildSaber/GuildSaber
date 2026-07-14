using GuildSaber.Api.Features.RankedMaps.Http;
using Microsoft.OpenApi;

namespace GuildSaber.Api.Transformers;

public static class OpenApiTypeMappings
{
    public static void RegisterGuildSaberTypeMappings()
    {
        OpenApiTypeTransformer.MapType<GuildId>(new OpenApiSchema
            { Type = JsonSchemaType.Integer, Format = "int32" });
        OpenApiTypeTransformer.MapType<ContextId>(new OpenApiSchema
            { Type = JsonSchemaType.Integer, Format = "int32" });
        OpenApiTypeTransformer.MapType<PlayerId>(new OpenApiSchema
            { Type = JsonSchemaType.Integer, Format = "int32" });
        OpenApiTypeTransformer.MapType<CategoryId>(new OpenApiSchema
            { Type = JsonSchemaType.Integer, Format = "int32" });
        OpenApiTypeTransformer.MapType<DiscordId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "123456789012345678", Format = "int64" });
        OpenApiTypeTransformer.MapType<SteamId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "12345678901234567", Format = "int64" });
        OpenApiTypeTransformer.MapType<MetaPCId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "1234567890123456", Format = "int64" });
        OpenApiTypeTransformer.MapType<BLNativeId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "123456789", Format = "int64" });
        OpenApiTypeTransformer.MapType<ScoreSaberId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "12345678901234567", Format = "int64" });
        OpenApiTypeTransformer.MapType<DiscordGuildId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "987654321098765432" });
        OpenApiTypeTransformer.MapType<DiscordChannelId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "987654321098765432" });
        OpenApiTypeTransformer.MapType<DiscordRoleId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "987654321098765432" });

        OpenApiTypeTransformer.MapType<ScoreId>(new OpenApiSchema
            { Type = JsonSchemaType.Integer, Format = "int32" });
        OpenApiTypeTransformer.MapType<RankedScoreId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "123456789" });
        OpenApiTypeTransformer.MapType<RankedMapId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "123456789" });

        OpenApiTypeTransformer.MapType<BeatSaverKey>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "a3c3" });
        OpenApiTypeTransformer.MapType<SongHash>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "ABCD1234EFGH5678IJKL9012MNOP3456QRST7890" });
        OpenApiTypeTransformer.MapType<SSLeaderboardId>(new OpenApiSchema
            { Type = JsonSchemaType.Integer, Format = "int32" });
        OpenApiTypeTransformer.MapType<BLLeaderboardId>(new OpenApiSchema
            { Type = JsonSchemaType.String, Example = "a3c391" });
        OpenApiTypeTransformer.MapType<RankedMapRequests.EModifiers>(new OpenApiSchema
            { Example = nameof(RankedMapRequests.EModifiers.None) });
    }
}