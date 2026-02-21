using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;

namespace OpenApiCodeGenerator;

/// <summary>
/// Resolves OpenAPI schema types to C# type strings.
/// Handles $ref resolution, primitives, arrays, dictionaries,
/// nullable types, and composition (allOf / oneOf / anyOf).
/// </summary>
internal class TypeResolver
{
    private readonly GeneratorOptions _options;
    private readonly IDictionary<string, IOpenApiSchema> _allSchemas;

    public TypeResolver(GeneratorOptions options, IDictionary<string, IOpenApiSchema> allSchemas)
    {
        _options = options;
        _allSchemas = allSchemas;
    }

    /// <summary>
    /// Resolve an <see cref="IOpenApiSchema"/> to a C# type string.
    /// </summary>
    public string Resolve(IOpenApiSchema schema)
    {
        return ResolveCore(schema, nullable: false);
    }

    /// <summary>
    /// Resolve the underlying type of a schema, ignoring its own reference wrapper.
    /// Used for type alias resolution where we need the actual primitive type.
    /// </summary>
    public string ResolveUnderlyingType(IOpenApiSchema schema)
    {
        // In v3, component schemas are plain OpenApiSchema objects (not OpenApiSchemaReference),
        // so Resolve already returns the underlying type. Kept for API compatibility.
        return ResolveCore(schema, nullable: false);
    }

    /// <summary>
    /// Resolve an <see cref="IOpenApiSchema"/> to a C# type string, accounting for nullability.
    /// </summary>
    public string ResolveWithNullability(IOpenApiSchema schema, bool isRequired)
    {
        string baseType = ResolveCore(schema, nullable: false);

        // When DefaultNonNullable is enabled, a property with a non-null default
        // value is treated as non-nullable even when it is not in the required set.
        bool hasNonNullDefault = _options.DefaultNonNullable && schema.Default is not null;
        bool isNullable = IsNullableType(schema) || (!isRequired && !hasNonNullDefault);

        // Value types already carry ? from ResolveCore when schema is nullable,
        // but for reference types we need to add ?
        if (isNullable && !baseType.EndsWith('?'))
        {
            return baseType + "?";
        }

        return baseType;
    }

    private string ResolveCore(IOpenApiSchema schema, bool nullable)
    {
        // Handle $ref (Reference) — in v3, references are OpenApiSchemaReference objects
        if (schema is OpenApiSchemaReference schemaRef)
        {
            string refTypeName = NameHelper.ToTypeName(schemaRef.Reference.Id);
            return nullable ? refTypeName + "?" : refTypeName;
        }

        // Handle allOf composition - if single $ref, treat as that type
        if (schema.AllOf is { Count: > 0 })
        {
            return ResolveAllOf(schema, nullable);
        }

        // Handle oneOf - generate as the first concrete type or object if multiple
        if (schema.OneOf is { Count: > 0 })
        {
            return ResolveOneOf(schema, nullable);
        }

        // Handle anyOf
        if (schema.AnyOf is { Count: > 0 })
        {
            return ResolveAnyOf(schema, nullable);
        }

        // Handle enum - reference to the enum type
        if (schema.Enum is { Count: > 0 } && HasTypeFlag(schema, JsonSchemaType.String))
        {
            // Enum types are generated separately; this will be the type name if inline
            return "string";
        }

        JsonSchemaType? baseType = GetBaseType(schema);
        string type = baseType switch
        {
            JsonSchemaType.String => ResolveStringType(schema),
            JsonSchemaType.Integer => ResolveIntegerType(schema),
            JsonSchemaType.Number => ResolveNumberType(schema),
            JsonSchemaType.Boolean => "bool",
            JsonSchemaType.Array => ResolveArrayType(schema),
            JsonSchemaType.Object => ResolveObjectType(schema),
            _ => "object"
        };

        if ((nullable || IsNullableType(schema)) && !type.EndsWith('?'))
        {
            type += "?";
        }

        return type;
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Intended")]
    private static string ResolveStringType(IOpenApiSchema schema)
    {
        return schema.Format?.ToLowerInvariant() switch
        {
            "date-time" => "DateTimeOffset",
            "date" => "DateOnly",
            "time" => "TimeOnly",
            "duration" => "TimeSpan",
            "uuid" => "Guid",
            "uri" => "Uri",
            "byte" => "byte[]",
            "binary" => "Stream",
            _ => "string"
        };
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Intended")]
    private static string ResolveIntegerType(IOpenApiSchema schema)
    {
        return schema.Format?.ToLowerInvariant() switch
        {
            "int32" => "int",
            "int64" => "long",
            _ => "int"
        };
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Intended")]
    private static string ResolveNumberType(IOpenApiSchema schema)
    {
        return schema.Format?.ToLowerInvariant() switch
        {
            "float" => "float",
            "double" => "double",
            "decimal" => "decimal",
            _ => "double"
        };
    }

    private string ResolveArrayType(IOpenApiSchema schema)
    {
        string itemType = schema.Items != null ? ResolveCore(schema.Items, nullable: false) : "object";
        string collectionType = _options.UseImmutableArrays ? "IReadOnlyList" : "List";
        return $"{collectionType}<{itemType}>";
    }

    private string ResolveObjectType(IOpenApiSchema schema)
    {
        // Object with additionalProperties only (dictionary/map)
        if (schema.AdditionalProperties != null && (schema.Properties == null || schema.Properties.Count == 0))
        {
            string valueType = ResolveCore(schema.AdditionalProperties, nullable: false);
            string dictType = _options.UseImmutableDictionaries ? "IReadOnlyDictionary" : "Dictionary";
            return $"{dictType}<string, {valueType}>";
        }

        // Typed object with properties → will be a named record, return object as fallback
        if (schema.Properties is { Count: > 0 })
        {
            return "object";
        }

        // Empty object
        return "object";
    }

    private static string ResolveAllOf(IOpenApiSchema schema, bool nullable)
    {
        // Common pattern: allOf with a single $ref (possibly + additional properties)
        // Return the $ref type name
        OpenApiSchemaReference? refSchema = schema.AllOf!.OfType<OpenApiSchemaReference>().FirstOrDefault();
        if (refSchema != null)
        {
            string typeName = NameHelper.ToTypeName(refSchema.Reference.Id);
            return nullable ? typeName + "?" : typeName;
        }

        // Fallback: pick the first type that has properties
        IOpenApiSchema? withProps = schema.AllOf!.FirstOrDefault(s => s.Properties is { Count: > 0 });
        if (withProps != null)
        {
            return "object";
        }

        return "object";
    }

    private string ResolveOneOf(IOpenApiSchema schema, bool nullable)
    {
        // If all oneOf entries are $refs, in C# we can't easily express a union.
        // Return object. The generator will produce a marker interface or base class if appropriate.
        // If there's a discriminator, the generator handles inheritance.
        if (schema.OneOf!.Count == 1)
        {
            return ResolveCore(schema.OneOf[0], nullable);
        }

        return "object";
    }

    private string ResolveAnyOf(IOpenApiSchema schema, bool nullable)
    {
        if (schema.AnyOf!.Count == 1)
        {
            return ResolveCore(schema.AnyOf[0], nullable);
        }

        // For nullable anyOf patterns (common in OpenAPI 3.1): [type, null]
        var nonNull = schema.AnyOf!.Where(s =>
            !(s.Type.HasValue && s.Type.Value == JsonSchemaType.Null)).ToList();
        if (nonNull.Count == 1)
        {
            return ResolveCore(nonNull[0], nullable: true);
        }

        return "object";
    }

    #region Schema Classification Helpers

    /// <summary>
    /// Gets the base type without the Null flag.
    /// </summary>
    public static JsonSchemaType? GetBaseType(IOpenApiSchema schema)
    {
        if (schema.Type is not { } type)
        {
            return null;
        }

        JsonSchemaType nonNull = type & ~JsonSchemaType.Null;
        return nonNull == 0 ? null : nonNull;
    }

    /// <summary>
    /// Checks if the Null flag is set on the schema's type.
    /// </summary>
    public static bool IsNullableType(IOpenApiSchema schema)
    {
        return schema.Type.HasValue && schema.Type.Value.HasFlag(JsonSchemaType.Null);
    }

    /// <summary>
    /// Checks if a specific type flag is set (ignoring the Null flag).
    /// </summary>
    public static bool HasTypeFlag(IOpenApiSchema schema, JsonSchemaType flag)
    {
        return schema.Type.HasValue && (schema.Type.Value & flag) == flag;
    }

    /// <summary>
    /// Checks whether a schema represents a type that should be generated as a C# enum.
    /// </summary>
    public static bool IsEnum(IOpenApiSchema schema)
    {
        JsonSchemaType? baseType = GetBaseType(schema);
        return schema.Enum is { Count: > 0 } &&
               baseType is JsonSchemaType.String or JsonSchemaType.Integer;
    }

    /// <summary>
    /// Checks whether a schema is an object with its own properties (i.e. should become a record).
    /// </summary>
    public static bool IsObjectSchema(IOpenApiSchema schema)
    {
        // Explicit object with properties
        if (schema.Properties is { Count: > 0 })
        {
            return true;
        }

        // allOf composing properties
        if (schema.AllOf is { Count: > 0 } && schema.AllOf.Any(s => s.Properties is { Count: > 0 }))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a schema is a simple type alias (e.g., type: string with format: uuid).
    /// </summary>
    public static bool IsTypeAlias(IOpenApiSchema schema)
    {
        if (schema.Properties is { Count: > 0 })
        {
            return false;
        }

        if (schema.AllOf is { Count: > 0 })
        {
            return false;
        }

        if (schema.OneOf is { Count: > 0 })
        {
            return false;
        }

        if (schema.AnyOf is { Count: > 0 })
        {
            return false;
        }

        if (schema.Enum is { Count: > 0 })
        {
            return false;
        }

        JsonSchemaType? baseType = GetBaseType(schema);
        return baseType is JsonSchemaType.String or JsonSchemaType.Integer
            or JsonSchemaType.Number or JsonSchemaType.Boolean;
    }

    #endregion
}
