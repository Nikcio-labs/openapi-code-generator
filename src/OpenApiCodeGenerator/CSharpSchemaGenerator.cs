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
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
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
        ArgumentNullException.ThrowIfNull(schemas);

        IDictionary<string, IOpenApiSchema> selectedSchemas = SelectSchemas(schemas);
        var typeResolver = new TypeResolver(_options, selectedSchemas);
        var emitter = new CSharpCodeEmitter(_options, typeResolver, selectedSchemas);
        return emitter.Emit();
    }

    private IDictionary<string, IOpenApiSchema> SelectSchemas(IDictionary<string, IOpenApiSchema> schemas)
    {
        if (_options.IncludeSchemas is not { Count: > 0 } includedSchemas)
        {
            return schemas;
        }

        var reachableSchemas = new HashSet<string>(StringComparer.Ordinal);
        var pendingSchemaNames = new Queue<string>(includedSchemas.Distinct(StringComparer.Ordinal));
        var missingSchemaNames = new HashSet<string>(StringComparer.Ordinal);

        while (pendingSchemaNames.Count > 0)
        {
            string schemaName = pendingSchemaNames.Dequeue();
            if (!reachableSchemas.Add(schemaName))
            {
                continue;
            }

            if (!schemas.TryGetValue(schemaName, out IOpenApiSchema? schema))
            {
                missingSchemaNames.Add(schemaName);
                continue;
            }

            CollectReferencedSchemaNames(schema, pendingSchemaNames);
        }

        if (missingSchemaNames.Count > 0)
        {
            string missingNames = string.Join(", ", missingSchemaNames.OrderBy(name => name, StringComparer.Ordinal));
            throw new InvalidOperationException($"IncludeSchemas references schema(s) not found in the provided schemas: {missingNames}");
        }

        return schemas
            .Where(kvp => reachableSchemas.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
    }

    private static void CollectReferencedSchemaNames(IOpenApiSchema schema, Queue<string> pendingSchemaNames)
    {
        if (schema is OpenApiSchemaReference schemaReference)
        {
            if (!string.IsNullOrWhiteSpace(schemaReference.Reference?.Id))
            {
                pendingSchemaNames.Enqueue(schemaReference.Reference.Id);
            }

            return;
        }

        if (schema.Properties is { Count: > 0 })
        {
            foreach (IOpenApiSchema propertySchema in schema.Properties.Values)
            {
                CollectReferencedSchemaNames(propertySchema, pendingSchemaNames);
            }
        }

        if (schema.Items is not null)
        {
            CollectReferencedSchemaNames(schema.Items, pendingSchemaNames);
        }

        if (schema.AdditionalProperties is not null)
        {
            CollectReferencedSchemaNames(schema.AdditionalProperties, pendingSchemaNames);
        }

        if (schema.AllOf is { Count: > 0 })
        {
            foreach (IOpenApiSchema subSchema in schema.AllOf)
            {
                CollectReferencedSchemaNames(subSchema, pendingSchemaNames);
            }
        }

        if (schema.OneOf is { Count: > 0 })
        {
            foreach (IOpenApiSchema subSchema in schema.OneOf)
            {
                CollectReferencedSchemaNames(subSchema, pendingSchemaNames);
            }
        }

        if (schema.AnyOf is { Count: > 0 })
        {
            foreach (IOpenApiSchema subSchema in schema.AnyOf)
            {
                CollectReferencedSchemaNames(subSchema, pendingSchemaNames);
            }
        }

        if (schema.Discriminator?.Mapping is { Count: > 0 } discriminatorMapping)
        {
            foreach (OpenApiSchemaReference mappedSchema in discriminatorMapping.Values)
            {
                if (!string.IsNullOrWhiteSpace(mappedSchema.Reference?.Id))
                {
                    pendingSchemaNames.Enqueue(mappedSchema.Reference.Id);
                }
            }
        }
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
