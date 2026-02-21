using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace OpenApiCodeGenerator;

/// <summary>
/// Main entry point for the C# code generator. Reads OpenAPI specifications
/// and produces C# source code with records, enums, and type aliases.
/// </summary>
public sealed class CSharpSchemaGenerator
{
    private readonly GeneratorOptions _options;

    public CSharpSchemaGenerator() : this(new GeneratorOptions()) { }

    public CSharpSchemaGenerator(GeneratorOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Generate C# code from an OpenAPI document provided as a <see cref="Stream"/>.
    /// Supports both JSON and YAML formats.
    /// </summary>
    public string GenerateFromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var memStream = new MemoryStream();
        stream.CopyTo(memStream);
        memStream.Position = 0;

        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        ReadResult result = OpenApiDocument.Load(memStream, settings: settings);

        HandleDiagnostics(result);

        ArgumentNullException.ThrowIfNull(result.Document, "Failed to parse OpenAPI document.");

        return GenerateFromDocument(result.Document);
    }

    /// <summary>
    /// Generate C# code from an OpenAPI specification provided as text.
    /// Supports both JSON and YAML formats.
    /// </summary>
    public string GenerateFromText(string openApiText)
    {
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        ReadResult result = OpenApiDocument.Parse(openApiText, settings: settings);

        HandleDiagnostics(result);

        ArgumentNullException.ThrowIfNull(result.Document, "Failed to parse OpenAPI document.");

        return GenerateFromDocument(result.Document);
    }

    /// <summary>
    /// Generate C# code from an OpenAPI specification file (JSON or YAML).
    /// </summary>
    public string GenerateFromFile(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        return GenerateFromStream(stream);
    }

    /// <summary>
    /// Generate C# code from a parsed <see cref="OpenApiDocument"/>.
    /// </summary>
    public string GenerateFromDocument(OpenApiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return GenerateFromSchemas(document.Components?.Schemas ?? new Dictionary<string, IOpenApiSchema>());
    }

    /// <summary>
    /// Generate C# code from a dictionary of named schemas.
    /// Useful for testing or when schemas are obtained independently.
    /// </summary>
    public string GenerateFromSchemas(IDictionary<string, IOpenApiSchema> schemas)
    {
        var typeResolver = new TypeResolver(_options, schemas);
        var emitter = new CSharpCodeEmitter(_options, typeResolver, schemas);
        return emitter.Emit();
    }

    private static void HandleDiagnostics(ReadResult result)
    {
        // If the document parsed successfully with components/schemas, proceed
        // even if there are path-level or other non-schema validation errors.
        if (result.Document?.Components?.Schemas is { Count: > 0 })
        {
            return;
        }

        if (result.Diagnostic?.Errors is { Count: > 0 } errors)
        {
            string messages = string.Join(Environment.NewLine,
                errors.Select(e => $"  - {e.Message}"));
            throw new InvalidOperationException($"OpenAPI specification has errors:{Environment.NewLine}{messages}");
        }
    }
}
