namespace OpenApiCodeGenerator;

/// <summary>
/// Options controlling C# code generation from OpenAPI schemas.
/// </summary>
public sealed class GeneratorOptions
{
    /// <summary>
    /// The C# namespace for generated types. Defaults to "GeneratedModels".
    /// </summary>
    public string Namespace { get; init; } = "GeneratedModels";

    /// <summary>
    /// Prefix applied to every generated model type name.
    /// Must start with a letter or underscore and contain only letters, digits, or underscores.
    /// </summary>
    public string? ModelPrefix { get; init; }

    /// <summary>
    /// When true, generate XML documentation comments from OpenAPI descriptions.
    /// </summary>
    public bool GenerateDocComments { get; init; } = true;

    /// <summary>
    /// When true, add a file-level auto-generated comment header.
    /// </summary>
    public bool GenerateFileHeader { get; init; } = true;

    /// <summary>
    /// When true, properties with default values are treated as non-nullable.
    /// </summary>
    public bool DefaultNonNullable { get; init; } = true;

    /// <summary>
    /// When true, use <c>IReadOnlyList&lt;T&gt;</c> for array types.
    /// When false, use <c>List&lt;T&gt;</c>.
    /// </summary>
    public bool UseImmutableArrays { get; init; } = true;

    /// <summary>
    /// When true, use <c>IReadOnlyDictionary&lt;string, T&gt;</c> for additionalProperties.
    /// When false, use <c>Dictionary&lt;string, T&gt;</c>.
    /// </summary>
    public bool UseImmutableDictionaries { get; init; } = true;

    /// <summary>
    /// When true, properties with default values are initialized to those defaults.
    /// </summary>
    public bool AddDefaultValuesToProperties { get; init; } = true;

    /// <summary>
    /// Validates the configured options before generation starts.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a configured option is not a valid C# identifier shape.</exception>
    public void Validate()
    {
        ValidateNamespace(Namespace);

        if (!string.IsNullOrEmpty(ModelPrefix))
        {
            NameHelper.ValidateTypeNamePrefix(ModelPrefix);
        }
    }

    private static void ValidateNamespace(string? namespaceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(namespaceName);

        string[] segments = namespaceName.Split('.');
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment)))
        {
            throw new ArgumentException("Namespace must use dot-separated C# identifiers without empty segments.", nameof(namespaceName));
        }

        foreach (string segment in segments)
        {
            if (!NameHelper.IsIdentifierStart(segment[0]))
            {
                throw new ArgumentException($"Namespace segment '{segment}' must start with a letter or underscore.", nameof(namespaceName));
            }

            if (segment.Any(ch => !NameHelper.IsIdentifierPart(ch)))
            {
                throw new ArgumentException($"Namespace segment '{segment}' must contain only letters, digits, or underscores.", nameof(namespaceName));
            }

            if (NameHelper.CSharpKeywords.Contains(segment))
            {
                throw new ArgumentException($"Namespace segment '{segment}' cannot be a C# keyword.", nameof(namespaceName));
            }
        }
    }
}
