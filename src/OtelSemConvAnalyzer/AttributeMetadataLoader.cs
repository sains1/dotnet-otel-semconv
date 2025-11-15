using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OtelSemConvAnalyzer;

internal static class AttributeMetadataLoader
{
    private const string MetadataFileName = "otel-attributes-metadata.json";

    /// <summary>
    /// Loads attribute metadata from AdditionalFiles.
    /// Returns null if no metadata file is found.
    /// </summary>
    public static AttributeRegistry? LoadFromAdditionalFiles(ImmutableArray<AdditionalText> additionalFiles)
    {
        var metadataFile = additionalFiles
            .FirstOrDefault(f => f.Path.EndsWith(MetadataFileName, StringComparison.OrdinalIgnoreCase));

        if (metadataFile == null)
            return null;

        var sourceText = metadataFile.GetText();
        if (sourceText == null)
            return null;

        try
        {
            var json = sourceText.ToString();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var metadata = JsonSerializer.Deserialize<AttributeMetadataFile>(json, options);

            if (metadata?.Attributes == null)
                return null;

            // Build dictionaries for fast lookup
            var byName = new Dictionary<string, AttributeInfo>(StringComparer.Ordinal);
            var byConstant = new Dictionary<string, AttributeInfo>(StringComparer.Ordinal);

            foreach (var attr in metadata.Attributes)
            {
                if (attr.AttributeName != null)
                {
                    var info = new AttributeInfo(
                        attr.AttributeName,
                        attr.ConstantName ?? string.Empty,
                        attr.Type ?? "string",
                        attr.Stability ?? "stable",
                        attr.Deprecated);

                    byName[attr.AttributeName] = info;

                    if (attr.ConstantName != null)
                    {
                        byConstant[attr.ConstantName] = info;
                    }
                }
            }

            return new AttributeRegistry(byName, byConstant, metadata.Version ?? "unknown");
        }
        catch (JsonException)
        {
            // Invalid JSON - return null and skip validation
            return null;
        }
    }
}

/// <summary>
/// Container for deserialized JSON metadata.
/// </summary>
internal sealed class AttributeMetadataFile
{
    public string? Version { get; set; }
    public List<AttributeMetadataJson>? Attributes { get; set; }
}

/// <summary>
/// JSON model for a single attribute.
/// </summary>
internal sealed class AttributeMetadataJson
{
    public string? AttributeName { get; set; }
    public string? ConstantName { get; set; }
    public string? Type { get; set; }
    public string? Stability { get; set; }
    public string? Brief { get; set; }
    public string? Deprecated { get; set; }
}

/// <summary>
/// Fast-lookup registry for attribute metadata.
/// </summary>
internal sealed class AttributeRegistry(
    IReadOnlyDictionary<string, AttributeInfo> byName,
    IReadOnlyDictionary<string, AttributeInfo> byConstant,
    string version)
{
    public IReadOnlyDictionary<string, AttributeInfo> ByName { get; } = byName;
    public IReadOnlyDictionary<string, AttributeInfo> ByConstant { get; } = byConstant;
    public string Version { get; } = version;
}

/// <summary>
/// Attribute information for validation.
/// </summary>
internal sealed class AttributeInfo(
    string attributeName,
    string constantName,
    string type,
    string stability,
    string? deprecated)
{
    public string AttributeName { get; } = attributeName;
    public string ConstantName { get; } = constantName;
    public string Type { get; } = type;
    public string Stability { get; } = stability;
    public string? Deprecated { get; } = deprecated;
}
