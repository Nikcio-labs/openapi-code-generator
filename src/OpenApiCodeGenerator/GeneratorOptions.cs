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
}
