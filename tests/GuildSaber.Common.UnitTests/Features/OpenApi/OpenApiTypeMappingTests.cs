using System.Text.Json;
using AwesomeAssertions;
using GuildSaber.Api.Transformers;
using GuildSaber.Common.StrongTypes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace GuildSaber.Common.UnitTests.Features.OpenApi;

public class OpenApiTypeMappingTests
{
    [Test]
    public async Task StrongTypeMapping_ShouldPreserveExamplesForNullableAndNonNullableIds()
    {
        OpenApiTypeMappings.RegisterGuildSaberTypeMappings();
        await using var services = new ServiceCollection().BuildServiceProvider();
        var transformer = new OpenApiTypeTransformer.TypeTransformer();
        foreach (var type in new[] { typeof(DiscordId), typeof(DiscordId?) })
        {
            var schema = new OpenApiSchema();
            await transformer.TransformAsync(schema, new OpenApiSchemaTransformerContext
            {
                DocumentName = "v1",
                ParameterDescription = null,
                JsonTypeInfo = JsonSerializerOptions.Default.GetTypeInfo(type),
                JsonPropertyInfo = null,
                ApplicationServices = services
            }, CancellationToken.None);

            schema.Type.Should().Be(JsonSchemaType.String);
            schema.Format.Should().Be("int64");
            schema.Examples.Should().ContainSingle();
            schema.Examples![0]!.GetValue<string>().Should().Be("123456789012345678");
        }
    }
}