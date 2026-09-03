using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenApiCodeGenerator;

namespace OpenApiCodeGenerator.Wasm;

/// <summary>
/// Generation options serialized from the JavaScript playground.
/// </summary>
public sealed class WasmGeneratorOptions
{
    public string Namespace { get; set; } = "GeneratedModels";
    public string? ModelPrefix { get; set; }

    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "DTO deserialized from JSON at the interop boundary; mutability is never used.")]
    public string[] IncludeSchemas { get; set; } = [];
    public bool GenerateDocComments { get; set; } = true;
    public bool GenerateFileHeader { get; set; } = true;
    public bool DefaultNonNullable { get; set; } = true;
    public bool AddDefaultValuesToProperties { get; set; } = true;
    public bool UseImmutableArrays { get; set; } = true;
    public bool UseImmutableDictionaries { get; set; } = true;
    public bool OmitJsonPropertyNameAttributes { get; set; }
    public bool InlinePrimitiveTypeAliases { get; set; }
    public bool EmitValidationAttributes { get; set; } = true;
    public bool EmitObsoleteAttribute { get; set; } = true;
}

/// <summary>
/// Result payload serialized back to the JavaScript playground.
/// </summary>
public sealed class WasmGenerationResult
{
    public bool Success { get; set; }
    public string? Code { get; set; }
    public string? Error { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WasmGeneratorOptions))]
[JsonSerializable(typeof(WasmGenerationResult))]
internal sealed partial class WasmJsonContext : JsonSerializerContext;

/// <summary>
/// JavaScript-callable entry points for the browser playground.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class WasmInterop
{
    /// <summary>
    /// Generates C# code from an OpenAPI document (JSON or YAML) using the supplied options.
    /// Never throws: failures are returned as a <see cref="WasmGenerationResult"/> with
    /// <c>success: false</c> and an error message.
    /// </summary>
    /// <param name="openApiText">The OpenAPI document text.</param>
    /// <param name="optionsJson">JSON-serialized <see cref="WasmGeneratorOptions"/> (camelCase).</param>
    /// <returns>JSON-serialized <see cref="WasmGenerationResult"/> (camelCase).</returns>
    [JSExport]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Interop boundary: all failures must be surfaced to JavaScript as a result payload instead of an unhandled exception.")]
    public static string Generate(string openApiText, string optionsJson)
    {
        try
        {
            WasmGeneratorOptions wasmOptions =
                JsonSerializer.Deserialize(optionsJson, WasmJsonContext.Default.WasmGeneratorOptions)
                ?? new WasmGeneratorOptions();

            var options = new GeneratorOptions
            {
                Namespace = wasmOptions.Namespace,
                ModelPrefix = string.IsNullOrWhiteSpace(wasmOptions.ModelPrefix) ? null : wasmOptions.ModelPrefix,
                IncludeSchemas = wasmOptions.IncludeSchemas is { Length: > 0 } schemas ? schemas : null,
                GenerateDocComments = wasmOptions.GenerateDocComments,
                GenerateFileHeader = wasmOptions.GenerateFileHeader,
                DefaultNonNullable = wasmOptions.DefaultNonNullable,
                AddDefaultValuesToProperties = wasmOptions.AddDefaultValuesToProperties,
                UseImmutableArrays = wasmOptions.UseImmutableArrays,
                UseImmutableDictionaries = wasmOptions.UseImmutableDictionaries,
                OmitJsonPropertyNameAttributes = wasmOptions.OmitJsonPropertyNameAttributes,
                InlinePrimitiveTypeAliases = wasmOptions.InlinePrimitiveTypeAliases,
                EmitValidationAttributes = wasmOptions.EmitValidationAttributes,
                EmitObsoleteAttribute = wasmOptions.EmitObsoleteAttribute,
            };

            var generator = new CSharpSchemaGenerator(options);
            string code = generator.GenerateFromText(openApiText);

            return JsonSerializer.Serialize(
                new WasmGenerationResult { Success = true, Code = code },
                WasmJsonContext.Default.WasmGenerationResult);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(
                new WasmGenerationResult { Success = false, Error = ex.Message },
                WasmJsonContext.Default.WasmGenerationResult);
        }
    }
}
