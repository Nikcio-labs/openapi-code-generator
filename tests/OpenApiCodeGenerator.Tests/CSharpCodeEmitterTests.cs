using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace OpenApiCodeGenerator.Tests;

/// <summary>
/// Tests for <see cref="CSharpCodeEmitter"/> — verifying the generated C# code structure and content.
/// </summary>
public class CSharpCodeEmitterTests
{
    private static string Generate(IDictionary<string, IOpenApiSchema> schemas, GeneratorOptions? options = null)
    {
        GeneratorOptions opts = options ?? new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "TestModels"
        };
        var typeResolver = new TypeResolver(opts, schemas);
        var emitter = new CSharpCodeEmitter(opts, typeResolver, schemas);
        return emitter.Emit();
    }

    #region Record Generation

    [Fact]
    public void Emit_SimpleRecord_GeneratesCorrectCode()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "A user",
                Required = new HashSet<string> { "name", "email" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "The user's name" },
                    ["email"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "The user's email" },
                    ["age"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                }
            }
        };

        string result = Generate(schemas);

        // Should contain record declaration
        Assert.Contains("public partial record User", result, StringComparison.Ordinal);

        // Required properties should have 'required' keyword
        Assert.Contains("public required string Name { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required string Email { get; init; }", result, StringComparison.Ordinal);

        // Optional properties should be nullable
        Assert.Contains("public int? Age { get; init; }", result, StringComparison.Ordinal);

        // JSON attributes
        Assert.Contains("[JsonPropertyName(\"name\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"email\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"age\")]", result, StringComparison.Ordinal);

        // Doc comments
        Assert.Contains("/// <summary>", result, StringComparison.Ordinal);
        Assert.Contains("/// A user", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_RecordWithNullableProperties_HandlesNullability()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Item"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "requiredNullable", "requiredNonNull" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["requiredNullable"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                    ["requiredNonNull"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["optionalField"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["optionalNullable"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null }
                }
            }
        };

        string result = Generate(schemas);

        // required + nullable = required string?
        Assert.Contains("public required string? RequiredNullable { get; init; }", result, StringComparison.Ordinal);

        // required + non-nullable = required string
        Assert.Contains("public required string RequiredNonNull { get; init; }", result, StringComparison.Ordinal);

        // optional = string?
        Assert.Contains("public string? OptionalField { get; init; }", result, StringComparison.Ordinal);

        // optional + nullable = string?
        Assert.Contains("public string? OptionalNullable { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_WithOmitJsonPropertyNameAttributes_SkipsPropertyAttributes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["firstName"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["email"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas, new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "TestModels",
            OmitJsonPropertyNameAttributes = true
        });

        Assert.Contains("public string? FirstName { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public string? Email { get; init; }", result, StringComparison.Ordinal);
        Assert.DoesNotContain("[JsonPropertyName(\"firstName\")]", result, StringComparison.Ordinal);
        Assert.DoesNotContain("[JsonPropertyName(\"email\")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_RecordWithDateTimeFormats_MapsCorrectly()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Timestamps"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "createdAt", "date", "id" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["createdAt"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" },
                    ["date"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" },
                    ["id"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
                    ["optionalUri"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public required DateTimeOffset CreatedAt { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required DateOnly Date { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required Guid Id { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public Uri? OptionalUri { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_RecordWithArrayProperties_GeneratesCorrectTypes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Container"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "items" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["items"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema { Type = JsonSchemaType.String }
                    },
                    ["numbers"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public required IReadOnlyList<string> Items { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public IReadOnlyList<int>? Numbers { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_RecordWithRefProperty_GeneratesCorrectType()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Person"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "address" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["address"] = new OpenApiSchemaReference("Address"),
                    ["alternativeAddress"] = new OpenApiSchemaReference("Address")
                }
            },
            ["Address"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["city"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        // Required ref property
        Assert.Contains("public required Address Address { get; init; }", result, StringComparison.Ordinal);

        // Optional ref property
        Assert.Contains("public Address? AlternativeAddress { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_WithModelPrefix_PrefixesGeneratedTypeDeclarationsAndReferences()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Order"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "status", "address" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"pending",
                            (JsonNode)"complete"
                        }
                    },
                    ["address"] = new OpenApiSchemaReference("Address")
                }
            },
            ["Address"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["city"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas, new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "TestModels",
            ModelPrefix = "Api"
        });

        Assert.Contains("public enum ApiStatus", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ApiOrder", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ApiAddress", result, StringComparison.Ordinal);
        Assert.Contains("public required ApiStatus Status { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required ApiAddress Address { get; init; }", result, StringComparison.Ordinal);
    }

    #endregion

    #region Enum Generation

    [Fact]
    public void Emit_StringEnum_GeneratesCorrectCode()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Status"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "Status values",
                Enum = new List<JsonNode>
                {
                    (JsonNode)"active",
                    (JsonNode)"inactive",
                    (JsonNode)"banned"
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public enum Status", result, StringComparison.Ordinal);
        Assert.Contains("[JsonConverter(typeof(JsonStringEnumConverter<Status>))]", result, StringComparison.Ordinal);
        Assert.Contains("Active", result, StringComparison.Ordinal);
        Assert.Contains("Inactive", result, StringComparison.Ordinal);
        Assert.Contains("Banned", result, StringComparison.Ordinal);

        // String enum members should use [JsonStringEnumMemberName], not [JsonPropertyName]
        Assert.Contains("[JsonStringEnumMemberName(\"active\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonStringEnumMemberName(\"inactive\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonStringEnumMemberName(\"banned\")]", result, StringComparison.Ordinal);
        Assert.DoesNotContain("[JsonPropertyName(\"active\")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_EnumWithDuplicateMemberNames_DeduplicatesWithSuffixes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Relationship"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = new List<JsonNode>
                {
                    (JsonNode)"unknown",
                    (JsonNode)"direct",
                    (JsonNode)"transitive",
                    (JsonNode)"inconclusive",
                    (JsonNode)""
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public enum Relationship", result, StringComparison.Ordinal);
        // "unknown" → Unknown (first occurrence keeps the name)
        Assert.Contains("Unknown,", result, StringComparison.Ordinal);
        // "" → Unknown2 (duplicate gets a suffix)
        Assert.Contains("Unknown2", result, StringComparison.Ordinal);
        Assert.Contains("[JsonStringEnumMemberName(\"unknown\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonStringEnumMemberName(\"\")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_IntegerEnum_GeneratesCorrectCode()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["HttpStatusCode"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Enum = new List<JsonNode>
                {
                    (JsonNode)200,
                    (JsonNode)404,
                    (JsonNode)500
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public enum HttpStatusCode", result, StringComparison.Ordinal);
        Assert.Contains("_200 = 200", result, StringComparison.Ordinal);
        Assert.Contains("_404 = 404", result, StringComparison.Ordinal);
        Assert.Contains("_500 = 500", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_Inline_StringEnum_GeneratesCorrectCode()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Entry"] = new OpenApiSchema
            {
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["Status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"active",
                            (JsonNode)"inactive",
                            (JsonNode)"banned"
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record Entry", result, StringComparison.Ordinal);

        Assert.Contains("public enum Status", result, StringComparison.Ordinal);
        Assert.Contains("[JsonConverter(typeof(JsonStringEnumConverter<Status>))]", result, StringComparison.Ordinal);
        Assert.Contains("Active", result, StringComparison.Ordinal);
        Assert.Contains("Inactive", result, StringComparison.Ordinal);
        Assert.Contains("Banned", result, StringComparison.Ordinal);

        // String enum members should use [JsonStringEnumMemberName], not [JsonPropertyName]
        Assert.Contains("[JsonStringEnumMemberName(\"active\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonStringEnumMemberName(\"inactive\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonStringEnumMemberName(\"banned\")]", result, StringComparison.Ordinal);
        Assert.DoesNotContain("[JsonPropertyName(\"active\")]", result, StringComparison.Ordinal);
    }

    #endregion

    #region Composition (allOf)

    [Fact]
    public void Emit_AllOfInheritance_GeneratesRecordWithBase()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Pet"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["Cat"] = new OpenApiSchema
            {
                AllOf = new List<IOpenApiSchema>
                {
                    new OpenApiSchemaReference("Pet"),
                    new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string> { "indoor" },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["indoor"] = new OpenApiSchema { Type = JsonSchemaType.Boolean },
                            ["declawed"] = new OpenApiSchema { Type = JsonSchemaType.Boolean }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Cat should inherit from Pet
        Assert.Contains("public partial record Cat : Pet", result, StringComparison.Ordinal);

        // Cat's own properties
        Assert.Contains("public required bool Indoor { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public bool? Declawed { get; init; }", result, StringComparison.Ordinal);
    }

    #endregion

    #region Union Types (oneOf)

    [Fact]
    public void Emit_OneOfWithDiscriminator_GeneratesAbstractRecordWithAttributes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Shape"] = new OpenApiSchema
            {
                OneOf = new List<IOpenApiSchema>
                {
                    new OpenApiSchemaReference("Circle"),
                    new OpenApiSchemaReference("Rectangle")
                },
                Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = "shapeType",
                    Mapping = new Dictionary<string, OpenApiSchemaReference>
                    {
                        ["circle"] = new OpenApiSchemaReference("Circle"),
                        ["rectangle"] = new OpenApiSchemaReference("Rectangle")
                    }
                }
            },
            ["Circle"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["radius"] = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" }
                }
            },
            ["Rectangle"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["width"] = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" },
                    ["height"] = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public abstract partial record Shape", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Circle), \"circle\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Rectangle), \"rectangle\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"shapeType\")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_OneOfWithoutDiscriminator_GeneratesAbstractRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Result"] = new OpenApiSchema
            {
                OneOf = new List<IOpenApiSchema>
                {
                    new OpenApiSchemaReference("SuccessResult"),
                    new OpenApiSchemaReference("ErrorResult")
                }
            },
            ["SuccessResult"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["data"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["ErrorResult"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["error"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public abstract partial record Result", result, StringComparison.Ordinal);
        Assert.Contains("Union of: SuccessResult | ErrorResult", result, StringComparison.Ordinal);
    }

    #endregion

    #region Type Aliases

    [Fact]
    public void Emit_TypeAlias_GeneratesRecordStruct()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["ObjectId"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uuid",
                Description = "A UUID identifier"
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[JsonConverter(typeof(OpenApiGeneratedTypeAliasJsonConverter<ObjectId, Guid>))]", result, StringComparison.Ordinal);
        Assert.Contains("public readonly partial record struct ObjectId(Guid Value) : IOpenApiGeneratedTypeAlias<ObjectId, Guid>", result, StringComparison.Ordinal);
        Assert.Contains("public static ObjectId Create(Guid value) => new(value);", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_TypeAlias_GeneratesConverterInfrastructure()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["ObjectId"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uuid"
            }
        };

        string result = Generate(schemas);

        Assert.Contains("using System.Text.Json;", result, StringComparison.Ordinal);
        Assert.Contains("file interface IOpenApiGeneratedTypeAlias<TSelf, TValue>", result, StringComparison.Ordinal);
        Assert.Contains("file sealed class OpenApiGeneratedTypeAliasJsonConverter<TAlias, TValue> : JsonConverter<TAlias>", result, StringComparison.Ordinal);
        Assert.Contains("where TAlias : struct, IOpenApiGeneratedTypeAlias<TAlias, TValue>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_BinaryTypeAlias_UsesSpecializedStreamConverter()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["FileContent"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "binary"
            }
        };

        string result = Generate(schemas);

        Assert.Contains("using System;", result, StringComparison.Ordinal);
        Assert.Contains("using System.IO;", result, StringComparison.Ordinal);
        Assert.Contains("file sealed class OpenApiGeneratedBinaryStreamTypeAliasJsonConverter<TAlias> : JsonConverter<TAlias>", result, StringComparison.Ordinal);
        Assert.Contains("[JsonConverter(typeof(OpenApiGeneratedBinaryStreamTypeAliasJsonConverter<FileContent>))]", result, StringComparison.Ordinal);
        Assert.Contains("public readonly partial record struct FileContent(Stream Value) : IOpenApiGeneratedTypeAlias<FileContent, Stream>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DirectBinaryStreamProperty_UsesSharedStreamConverter()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Attachment"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "content" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["content"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "binary"
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("using System.Text.Json;", result, StringComparison.Ordinal);
        Assert.Contains("using System;", result, StringComparison.Ordinal);
        Assert.Contains("using System.IO;", result, StringComparison.Ordinal);
        Assert.Contains("file sealed class OpenApiGeneratedBinaryStreamJsonConverter : JsonConverter<Stream>", result, StringComparison.Ordinal);
        Assert.Contains("[JsonConverter(typeof(OpenApiGeneratedBinaryStreamJsonConverter))]", result, StringComparison.Ordinal);
        Assert.Contains("public required Stream Content { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DirectBinaryStreamProperty_GeneratesNullHandlingBranches()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Attachment"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["content"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "binary"
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("if (reader.TokenType == JsonTokenType.Null)", result, StringComparison.Ordinal);
        Assert.Contains("return null;", result, StringComparison.Ordinal);
        Assert.Contains("if (value is null)", result, StringComparison.Ordinal);
        Assert.Contains("writer.WriteNullValue();", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_WithInlinePrimitiveTypeAliases_InlinesAliasUsagesAndSkipsWrapperType()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["AlertCreatedAt"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "date-time"
            },
            ["Alert"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "createdAt" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["createdAt"] = new OpenApiSchemaReference("AlertCreatedAt")
                }
            }
        };

        string result = Generate(schemas, new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "TestModels",
            InlinePrimitiveTypeAliases = true
        });

        Assert.DoesNotContain("public record struct AlertCreatedAt(DateTimeOffset Value)", result, StringComparison.Ordinal);
        Assert.DoesNotContain("IOpenApiGeneratedTypeAlias<", result, StringComparison.Ordinal);
        Assert.Contains("public required DateTimeOffset CreatedAt { get; init; }", result, StringComparison.Ordinal);
    }

    #endregion

    #region File Structure

    [Fact]
    public void Emit_WithFileHeader_IncludesAutoGenComment()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Empty"] = new OpenApiSchema { Type = JsonSchemaType.Object }
        };

        var options = new GeneratorOptions
        {
            GenerateFileHeader = true,
            Namespace = "Test"
        };

        string result = Generate(schemas, options);

        Assert.Contains("// <auto-generated>", result, StringComparison.Ordinal);
        Assert.Contains("// This file was auto-generated by OpenApiCodeGenerator.", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_WithNullableEnabled_IncludesDirective()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Empty"] = new OpenApiSchema { Type = JsonSchemaType.Object }
        };

        var options = new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        };

        string result = Generate(schemas, options);

        Assert.Contains("#nullable enable", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_IncludesSuppressionForUnusedUsings()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Empty"] = new OpenApiSchema { Type = JsonSchemaType.Object }
        };

        var options = new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        };

        string result = Generate(schemas, options);

        Assert.Contains("#pragma warning disable CS8019", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_WithJsonAttributes_IncludesUsingStatement()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Empty"] = new OpenApiSchema { Type = JsonSchemaType.Object }
        };

        var options = new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        };

        string result = Generate(schemas, options);

        Assert.Contains("using System.Text.Json.Serialization;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_SetsNamespace()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Empty"] = new OpenApiSchema { Type = JsonSchemaType.Object }
        };

        var options = new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "MyApp.Models"
        };

        string result = Generate(schemas, options);

        Assert.Contains("namespace MyApp.Models;", result, StringComparison.Ordinal);
    }

    #endregion

    #region Empty Object

    [Fact]
    public void Emit_EmptyObject_GeneratesEmptyRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["EmptyObject"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "An empty object"
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record EmptyObject", result, StringComparison.Ordinal);
    }

    #endregion

    #region Without Doc Comments

    [Fact]
    public void Emit_WithoutDocComments_DoesNotIncludeSummary()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "A user",
                Required = new HashSet<string> { "name" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Description = "The name"
                    }
                }
            }
        };

        var options = new GeneratorOptions
        {
            GenerateFileHeader = false,
            GenerateDocComments = false,
            Namespace = "Test"
        };

        string result = Generate(schemas, options);

        Assert.DoesNotContain("/// <summary>", result, StringComparison.Ordinal);
        Assert.DoesNotContain("/// A user", result, StringComparison.Ordinal);
    }

    #endregion

    #region CS9031: Required member hiding in inheritance

    [Fact]
    public void Emit_DerivedRecord_DoesNotRedeclareBaseProperties()
    {
        // Simulates a pattern where derived types re-declare
        // properties from their base type via allOf, causing CS9031.
        // The allOf inline schema contains the same property names that exist
        // in the base type — the generator should skip these duplicates.
        var sharedProperties = new Dictionary<string, IOpenApiSchema>
        {
            ["@odata.type"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["id"] = new OpenApiSchema { Type = JsonSchemaType.String }
        };

        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Entity"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "@odata.type" },
                Properties = sharedProperties
            },
            ["WorkbookTable"] = new OpenApiSchema
            {
                AllOf = new List<IOpenApiSchema>
                {
                    new OpenApiSchemaReference("Entity"),
                    new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string> { "@odata.type" },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            // Re-declared from base — should be skipped
                            ["@odata.type"] = new OpenApiSchema { Type = JsonSchemaType.String },
                            ["id"] = new OpenApiSchema { Type = JsonSchemaType.String },
                            // Own property — should be emitted
                            ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // WorkbookTable should inherit from Entity
        Assert.Contains("public partial record WorkbookTable : Entity", result, StringComparison.Ordinal);

        // WorkbookTable should have its own 'name' property
        Assert.Contains("public string? Name { get; init; }", result, StringComparison.Ordinal);

        // Split the output to isolate the WorkbookTable record body.
        // The Entity record has odataType; WorkbookTable should NOT re-declare it.
        string workbookSection = result.Substring(result.IndexOf("public partial record WorkbookTable", StringComparison.Ordinal));
        string workbookBody = workbookSection.Substring(0, workbookSection.IndexOf('}', StringComparison.Ordinal) + 1);

        // WorkbookTable's body should NOT contain odataType (it's inherited from Entity)
        Assert.DoesNotContain("odataType", workbookBody, StringComparison.Ordinal);
        // WorkbookTable's body should NOT contain Id (it's inherited from Entity)
        Assert.DoesNotContain("public string? Id", workbookBody, StringComparison.Ordinal);
    }

    #endregion

    #region CS0102: Duplicate property names after PascalCase conversion

    [Fact]
    public void Emit_DuplicatePropertyNamesAfterPascalCase_DifferentiatesMeaningfully()
    {
        // Simulates the mist.com pattern where "_id" and "id" both
        // become "Id" after PascalCase conversion, causing CS0102.
        // The more natural name ("id") keeps "Id", while "_id" becomes "UnderscoreId".
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Asset"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["_id"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["id"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        // Should have the record
        Assert.Contains("public partial record Asset", result, StringComparison.Ordinal);

        // Should have name property
        Assert.Contains("public string? Name { get; init; }", result, StringComparison.Ordinal);

        // "id" (most natural) keeps the clean name "Id"
        Assert.Contains("public string? Id { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"id\")]", result, StringComparison.Ordinal);

        // "_id" gets a meaningful differentiated name "UnderscoreId"
        Assert.Contains("public string? UnderscoreId { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"_id\")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_TripleDuplicatePropertyNames_DifferentiatesMeaningfully()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Widget"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["_name"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["Name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        // "Name" (exact PascalCase match) keeps the clean name
        Assert.Contains("public string? Name { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"Name\")]", result, StringComparison.Ordinal);

        // "_name" gets expanded prefix: "UnderscoreName"
        Assert.Contains("public string? UnderscoreName { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"_name\")]", result, StringComparison.Ordinal);

        // "name" (lowercase) gets naming style suffix: "NameLowercase"
        Assert.Contains("public string? NameLowercase { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPropertyName(\"name\")]", result, StringComparison.Ordinal);
    }

    #endregion

    #region CS8863: Duplicate type names from different schemas

    [Fact]
    public void Emit_SchemasWithSameTypeName_EmitsBothWithDifferentiatedNames()
    {
        // Simulates a pattern where two differently-named schemas
        // produce the same C# type name after PascalCase conversion.
        // e.g. "my_string" and "myString" both become "MyString".
        // The more natural name keeps it; the other gets differentiated.
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["my_string"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["myString"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        // "myString" (more natural) keeps "MyString"
        Assert.Contains("public readonly partial record struct MyString(string Value) : IOpenApiGeneratedTypeAlias<MyString, string>", result, StringComparison.Ordinal);

        // "my_string" (has underscore) gets differentiated
        Assert.Contains("public readonly partial record struct MyUnderscoreString(string Value) : IOpenApiGeneratedTypeAlias<MyUnderscoreString, string>", result, StringComparison.Ordinal);

        // User is unaffected
        Assert.Contains("public partial record User", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_SchemasWithDifferentCasingProducingSameTypeName_EmitsBothWithDifferentiatedNames()
    {
        // Two schemas with different casing that produce the same PascalCase type name.
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["myType"] = new OpenApiSchema { Type = JsonSchemaType.String },
            ["MyType"] = new OpenApiSchema { Type = JsonSchemaType.String },
        };

        string result = Generate(schemas);

        // "MyType" (exact match, most natural) keeps "MyType"
        Assert.Contains("public readonly partial record struct MyType(string Value) : IOpenApiGeneratedTypeAlias<MyType, string>", result, StringComparison.Ordinal);

        // "myType" (camelCase) gets differentiated with naming style suffix
        Assert.Contains("public readonly partial record struct MyTypeCamelCase(string Value) : IOpenApiGeneratedTypeAlias<MyTypeCamelCase, string>", result, StringComparison.Ordinal);
    }

    #endregion

    #region additionalProperties alongside regular properties

    [Fact]
    public void Emit_RecordWithAdditionalPropertiesAlongsideRegularProperties_EmitsJsonExtensionData()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Flexible"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "name" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                },
                AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.Object }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record Flexible", result, StringComparison.Ordinal);
        Assert.Contains("public required string Name { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("[JsonExtensionData]", result, StringComparison.Ordinal);
        Assert.Contains("AdditionalProperties", result, StringComparison.Ordinal);
    }

    #endregion

    #region DefaultNonNullable

    [Fact]
    public void Emit_DefaultNonNullable_OptionalWithDefault_EmitsNonNullable()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["enabled"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Boolean,
                        Default = JsonValue.Create(true)
                    },
                    ["threshold"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Format = "int32"
                    }
                }
            }
        };

        var options = new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "TestModels",
            DefaultNonNullable = true
        };

        string result = Generate(schemas, options);

        // 'enabled' has a default value → non-nullable even though not required, with default emitted
        Assert.Contains("public bool Enabled { get; init; } = true;", result, StringComparison.Ordinal);
        Assert.DoesNotContain("bool? Enabled", result, StringComparison.Ordinal);

        // 'threshold' has no default → still nullable
        Assert.Contains("public int? Threshold { get; init; }", result, StringComparison.Ordinal);
    }

    #endregion

    #region Inline Enum Dedup and Conflict Resolution

    [Fact]
    public void Emit_MatchingInlineEnumsAcrossSchemas_EmitsOnce()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Order"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"active",
                            (JsonNode)"inactive"
                        }
                    }
                }
            },
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"active",
                            (JsonNode)"inactive"
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Both records reference Status type
        Assert.Contains("public Status? Status { get; init; }", result, StringComparison.Ordinal);

        // The enum should be defined exactly once
        int enumCount = CountOccurrences(result, "public enum Status");
        Assert.Equal(1, enumCount);
    }

    [Fact]
    public void Emit_ConflictingInlineEnumsAcrossSchemas_EmitsBothWithDifferentiatedNames()
    {
        // Two schemas have a "status" property with different enum values.
        // Both enums should be emitted with differentiated names.
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Order"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"pending",
                            (JsonNode)"shipped",
                            (JsonNode)"delivered"
                        }
                    }
                }
            },
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"active",
                            (JsonNode)"inactive",
                            (JsonNode)"banned"
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Both records should exist
        Assert.Contains("public partial record Order", result, StringComparison.Ordinal);
        Assert.Contains("public partial record User", result, StringComparison.Ordinal);

        // Two distinct enum types should be emitted.
        // One keeps "Status", the other gets a differentiated name like "OrderStatus" / "UserStatus".
        int enumCount = CountOccurrences(result, "public enum ");
        Assert.Equal(2, enumCount);

        // Both sets of enum values should appear
        Assert.Contains("Pending", result, StringComparison.Ordinal);
        Assert.Contains("Shipped", result, StringComparison.Ordinal);
        Assert.Contains("Active", result, StringComparison.Ordinal);
        Assert.Contains("Banned", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_MixedMatchingAndConflictingInlineEnums_HandlesCorrectly()
    {
        // Three schemas: two share the same inline enum values for "status",
        // a third has a "status" enum with different values.
        // The matching pair should share one enum; the conflicting one gets a separate name.
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Order"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"active",
                            (JsonNode)"inactive"
                        }
                    }
                }
            },
            ["Invoice"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"active",
                            (JsonNode)"inactive"
                        }
                    }
                }
            },
            ["Ticket"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["status"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"open",
                            (JsonNode)"closed"
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Exactly two enum types: one shared, one differentiated
        int enumCount = CountOccurrences(result, "public enum ");
        Assert.Equal(2, enumCount);
    }

    private static int CountOccurrences(string text, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }

    #endregion

    #region Inline Object Hoisting

    [Fact]
    public void Emit_InlineObjectProperty_GeneratesNamedRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "id", "permissions" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["id"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
                    ["permissions"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["issues"] = new OpenApiSchema { Type = JsonSchemaType.Boolean },
                            ["pullRequests"] = new OpenApiSchema { Type = JsonSchemaType.Boolean }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // The inline object should be hoisted to a named record
        Assert.Contains("public partial record UserPermissions", result, StringComparison.Ordinal);
        Assert.Contains("public bool? Issues { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public bool? PullRequests { get; init; }", result, StringComparison.Ordinal);

        // The property should use the hoisted type, not "object"
        Assert.Contains("public required UserPermissions Permissions { get; init; }", result, StringComparison.Ordinal);
        Assert.DoesNotContain("public required object Permissions", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectWithoutExplicitType_GeneratesNamedRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "metadata" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["metadata"] = new OpenApiSchema
                    {
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["key"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record UserMetadata", result, StringComparison.Ordinal);
        Assert.Contains("public required UserMetadata Metadata { get; init; }", result, StringComparison.Ordinal);
        Assert.DoesNotContain("public required object Metadata", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectOptionalProperty_MakesTypeNullable()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["profile"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["bio"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record UserProfile", result, StringComparison.Ordinal);
        // Optional → nullable
        Assert.Contains("public UserProfile? Profile { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_NestedInlineObjects_GeneratesAllLevels()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["App"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "config" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["config"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string> { "database" },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["database"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["host"] = new OpenApiSchema { Type = JsonSchemaType.String },
                                    ["port"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record AppConfig", result, StringComparison.Ordinal);
        Assert.Contains("public partial record AppConfigDatabase", result, StringComparison.Ordinal);
        Assert.Contains("public required AppConfig Config { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required AppConfigDatabase Database { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectInArrayItem_GeneratesNamedRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Repository"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "webhooks" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["webhooks"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>
                            {
                                ["url"] = new OpenApiSchema { Type = JsonSchemaType.String },
                                ["active"] = new OpenApiSchema { Type = JsonSchemaType.Boolean }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record RepositoryWebhooks", result, StringComparison.Ordinal);
        Assert.Contains("public required IReadOnlyList<RepositoryWebhooks> Webhooks { get; init; }", result, StringComparison.Ordinal);
        Assert.DoesNotContain("IReadOnlyList<object>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectWithRefProperty_UsesRefType()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "owner" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["owner"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["address"] = new OpenApiSchemaReference("Address")
                        }
                    }
                }
            },
            ["Address"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["city"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record UserOwner", result, StringComparison.Ordinal);
        Assert.Contains("public required UserOwner Owner { get; init; }", result, StringComparison.Ordinal);
        // The $ref property in the hoisted inline object should use the referenced type
        Assert.Contains("public Address? Address { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectWithInlineEnum_GeneratesBothTypes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Repo"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "settings" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["settings"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["visibility"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Enum = new List<JsonNode> { (JsonNode)"public", (JsonNode)"private" }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record RepoSettings", result, StringComparison.Ordinal);
        Assert.Contains("public required RepoSettings Settings { get; init; }", result, StringComparison.Ordinal);
        // Inline enum in the hoisted object should be emitted
        Assert.Contains("public enum Visibility", result, StringComparison.Ordinal);
        Assert.Contains("Public", result, StringComparison.Ordinal);
        Assert.Contains("Private", result, StringComparison.Ordinal);
        Assert.Contains("public Visibility? Visibility { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectWithModelPrefix_PrefersPrefix()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "permissions" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["permissions"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["admin"] = new OpenApiSchema { Type = JsonSchemaType.Boolean }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas, new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "TestModels",
            ModelPrefix = "Api"
        });

        Assert.Contains("public partial record ApiUser", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ApiUserPermissions", result, StringComparison.Ordinal);
        Assert.Contains("public required ApiUserPermissions Permissions { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectWithAdditionalProperties_EmitsExtensionData()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Webhook"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "config" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["config"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["url"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        },
                        AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.String }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record WebhookConfig", result, StringComparison.Ordinal);
        Assert.Contains("public required WebhookConfig Config { get; init; }", result, StringComparison.Ordinal);
        // The hoisted object should have both the named property and JsonExtensionData
        Assert.Contains("[JsonExtensionData]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_MultipleInlineObjectsInSameSchema_GeneratesAllTypes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Integration"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "owner", "permissions" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["owner"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["login"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    },
                    ["permissions"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["issues"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record IntegrationOwner", result, StringComparison.Ordinal);
        Assert.Contains("public partial record IntegrationPermissions", result, StringComparison.Ordinal);
        Assert.Contains("public required IntegrationOwner Owner { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required IntegrationPermissions Permissions { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectNameCollision_DifferentiatesNames()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "address" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["address"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["city"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            },
            ["UserAddress"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["street"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        // The top-level UserAddress keeps its name
        Assert.Contains("public partial record UserAddress", result, StringComparison.Ordinal);

        // The inline object's synthesized name (UserAddress) collides, so it gets a suffix
        // The hoisted type should exist with a differentiated name
        Assert.Contains("public partial record UserAddress2", result, StringComparison.Ordinal);

        // The property should reference the differentiated name
        Assert.Contains("public required UserAddress2 Address { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectInAdditionalPropertiesValue_GeneratesNamedRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Webhook"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "events" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["events"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        AdditionalProperties = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>
                            {
                                ["count"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record WebhookEvents", result, StringComparison.Ordinal);
        Assert.Contains("public required IReadOnlyDictionary<string, WebhookEvents> Events { get; init; }", result, StringComparison.Ordinal);
        Assert.DoesNotContain("IReadOnlyDictionary<string, object>", result, StringComparison.Ordinal);
    }

    #endregion

    #region Inline allOf / oneOf / anyOf Hoisting

    [Fact]
    public void Emit_InlineAllOfWithoutRef_GeneratesNamedRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Webhook"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "forkee" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["forkee"] = new OpenApiSchema
                    {
                        AllOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["id"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
                                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record WebhookForkee", result, StringComparison.Ordinal);
        Assert.Contains("public required WebhookForkee Forkee { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public int? Id { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public string? Name { get; init; }", result, StringComparison.Ordinal);
        Assert.DoesNotContain("public required object Forkee", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineOneOfAllRefs_GeneratesAbstractUnionRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Integration"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "owner" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["owner"] = new OpenApiSchema
                    {
                        OneOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchemaReference("SimpleUser"),
                            new OpenApiSchemaReference("Enterprise")
                        }
                    }
                }
            },
            ["SimpleUser"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["login"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["Enterprise"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["slug"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public abstract partial record IntegrationOwner", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(SimpleUser), \"SimpleUser\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Enterprise), \"Enterprise\")]", result, StringComparison.Ordinal);
        Assert.Contains("public required IntegrationOwner Owner { get; init; }", result, StringComparison.Ordinal);
        Assert.DoesNotContain("public required object Owner", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineAnyOfAllRefs_GeneratesAbstractUnionRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Deployment"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "reviewer" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["reviewer"] = new OpenApiSchema
                    {
                        AnyOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchemaReference("User"),
                            new OpenApiSchemaReference("Team")
                        }
                    }
                }
            },
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["login"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["Team"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["slug"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public abstract partial record DeploymentReviewer", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(User), \"User\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Team), \"Team\")]", result, StringComparison.Ordinal);
        Assert.Contains("public required DeploymentReviewer Reviewer { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineOneOfInArrayItem_GeneratesUnionRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Event"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "reviewers" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["reviewers"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema
                        {
                            OneOf = new List<IOpenApiSchema>
                            {
                                new OpenApiSchemaReference("User"),
                                new OpenApiSchemaReference("Team")
                            }
                        }
                    }
                }
            },
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["login"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["Team"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["slug"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public abstract partial record EventReviewers", result, StringComparison.Ordinal);
        Assert.Contains("public required IReadOnlyList<EventReviewers> Reviewers { get; init; }", result, StringComparison.Ordinal);
        Assert.DoesNotContain("IReadOnlyList<object>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineOneOfMixedTypes_FallsBackToObject()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Page"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "id" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["id"] = new OpenApiSchema
                    {
                        OneOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" },
                            new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Mixed primitive unions can't be hoisted as a union record
        Assert.Contains("public required object Id { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_EmptyObjectProperty_GeneratesDictionary()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Webhook"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "payload" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["payload"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Empty type:object is a free-form map per OpenAPI 3.0
        Assert.Contains("public required IReadOnlyDictionary<string, object?> Payload { get; init; }", result, StringComparison.Ordinal);
        Assert.DoesNotContain("public required object Payload", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineAllOfWithMultipleInlineMembers_MergesProperties()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Webhook"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "data" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["data"] = new OpenApiSchema
                    {
                        AllOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["id"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                                }
                            },
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record WebhookData", result, StringComparison.Ordinal);
        Assert.Contains("public required WebhookData Data { get; init; }", result, StringComparison.Ordinal);
        // Properties from both allOf members should be present
        Assert.Contains("public int? Id { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public string? Name { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineAllOfWithRefAndInlineProps_NotHoisted_UsesInheritance()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Container"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "item" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["item"] = new OpenApiSchema
                    {
                        AllOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchemaReference("Base"),
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["extra"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            }
                        }
                    }
                }
            },
            ["Base"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["id"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                }
            }
        };

        string result = Generate(schemas);

        // Should NOT be hoisted — allOf with $ref is inheritance, resolved to Base type
        Assert.DoesNotContain("public partial record ContainerItem", result, StringComparison.Ordinal);
        // The property should use the $ref type
        Assert.Contains("public required Base Item { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_TopLevelEmptyObject_StillEmitsAsRecord()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["EmptyObject"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Description = "An empty object"
            }
        };

        string result = Generate(schemas);

        // Top-level empty objects should still be records (not dictionaries)
        Assert.Contains("public partial record EmptyObject", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineAnyOfNullableUnion_GeneratesAbstractRecordWithDerivedTypes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Deployment"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "reviewer" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["reviewer"] = new OpenApiSchema
                    {
                        AnyOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchemaReference("User"),
                            new OpenApiSchemaReference("Team"),
                            new OpenApiSchema { Type = JsonSchemaType.Null }
                        }
                    }
                }
            },
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["login"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["Team"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["slug"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public abstract partial record DeploymentReviewer", result, StringComparison.Ordinal);
        // Should have JsonDerivedType attributes for both non-null variants
        Assert.Contains("[JsonDerivedType(typeof(User), \"User\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Team), \"Team\")]", result, StringComparison.Ordinal);
        // Required property — non-nullable (null variant in anyOf is filtered by IsInlineRefUnion)
        Assert.Contains("public required DeploymentReviewer Reviewer { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_HoistedObjectNameCollisionBetweenTwoHoistedObjects_DifferentiatesNames()
    {
        // Two schemas whose hoisted inline objects would produce the same synthesized name
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Foo"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "bar" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["bar"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["value"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            },
            ["FooBar"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "baz" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["baz"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["value"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Foo's inline "bar" → FooBar (collides with top-level FooBar) → FooBar2
        // FooBar's inline "baz" → FooBarBaz
        Assert.Contains("public partial record FooBar2", result, StringComparison.Ordinal);
        Assert.Contains("public partial record FooBarBaz", result, StringComparison.Ordinal);
        Assert.Contains("public required FooBar2 Bar { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required FooBarBaz Baz { get; init; }", result, StringComparison.Ordinal);
    }

    #endregion

    #region oneOf/anyOf with Inline Object Variants

    [Fact]
    public void Emit_InlineOneOfWithRefAndInlineObject_HoistsInlineObject()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["A"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["MyRecord"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema
                    {
                        OneOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchemaReference("A"),
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["x"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Property should reference the hoisted union type, not "object"
        Assert.Contains("public MyRecordValue? Value { get; init; }", result, StringComparison.Ordinal);

        // Union should have [JsonDerivedType] for both $ref and hoisted inline object
        Assert.Contains("[JsonDerivedType(typeof(A), \"A\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(MyRecordValueVariant2), \"MyRecordValueVariant2\")]", result, StringComparison.Ordinal);

        // Inline object variant should be hoisted to a named record
        Assert.Contains("public partial record MyRecordValueVariant2", result, StringComparison.Ordinal);
        Assert.Contains("public string? X { get; init; }", result, StringComparison.Ordinal);

        // Union should be an abstract record
        Assert.Contains("public abstract partial record MyRecordValue;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineOneOfAllInlineObjects_HoistsAllVariants()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["MyRecord"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema
                    {
                        OneOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["type"] = new OpenApiSchema { Type = JsonSchemaType.String },
                                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            },
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["type"] = new OpenApiSchema { Type = JsonSchemaType.String },
                                    ["count"] = new OpenApiSchema { Type = JsonSchemaType.Integer }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public MyRecordValue? Value { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public abstract partial record MyRecordValue;", result, StringComparison.Ordinal);
        Assert.Contains("public partial record MyRecordValueVariant1", result, StringComparison.Ordinal);
        Assert.Contains("public partial record MyRecordValueVariant2", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(MyRecordValueVariant1), \"MyRecordValueVariant1\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(MyRecordValueVariant2), \"MyRecordValueVariant2\")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_ComponentLevelOneOfWithRefAndInlineObject_HoistsInlineObject()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["A"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["MyUnion"] = new OpenApiSchema
            {
                OneOf = new List<IOpenApiSchema>
                {
                    new OpenApiSchemaReference("A"),
                    new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["label"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public abstract partial record MyUnion;", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(A), \"A\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(MyUnionVariant2), \"MyUnionVariant2\")]", result, StringComparison.Ordinal);
        Assert.Contains("public partial record MyUnionVariant2", result, StringComparison.Ordinal);
        Assert.Contains("public string? Label { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Emit_InlineOneOfWithRefAndInlineObject_CompilesSuccessfully()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["A"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["MyRecord"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema
                    {
                        OneOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchemaReference("A"),
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["x"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        string tempRoot = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "TestResults", "InlineUnionCompile", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempRoot, "Generated.cs"), result, TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempRoot, "Harness.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <AnalysisMode>All</AnalysisMode>
                    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                  </PropertyGroup>
                </Project>
                """, TestContext.Current.CancellationToken);
            using var proc = new System.Diagnostics.Process();
            proc.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{Path.Combine(tempRoot, "Harness.csproj")}\" -v q --nologo",
                WorkingDirectory = tempRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            proc.Start();
            string stdout = await proc.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            string stderr = await proc.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await proc.WaitForExitAsync(TestContext.Current.CancellationToken);
            Assert.True(proc.ExitCode == 0,
                $"Inline union code failed to compile.{Environment.NewLine}STDOUT:{stdout}{Environment.NewLine}STDERR:{stderr}");
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Emit_InlineAnyOfWithRefAndInlineObject_HoistsInlineObject()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["A"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["MyRecord"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema
                    {
                        AnyOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchemaReference("A"),
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["x"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public MyRecordValue? Value { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public abstract partial record MyRecordValue;", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(A), \"A\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(MyRecordValueVariant2), \"MyRecordValueVariant2\")]", result, StringComparison.Ordinal);
        Assert.Contains("public partial record MyRecordValueVariant2", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineOneOfWithNestedInlineObject_HoistsNestedObject()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["A"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["MyRecord"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema
                    {
                        OneOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchemaReference("A"),
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["nested"] = new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.Object,
                                        Properties = new Dictionary<string, IOpenApiSchema>
                                        {
                                            ["deep"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public MyRecordValue? Value { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public abstract partial record MyRecordValue;", result, StringComparison.Ordinal);
        Assert.Contains("public partial record MyRecordValueVariant2", result, StringComparison.Ordinal);
        Assert.Contains("public MyRecordValueVariant2Nested? Nested { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public partial record MyRecordValueVariant2Nested", result, StringComparison.Ordinal);
        Assert.Contains("public string? Deep { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineOneOfWithTwoSameShapedInlineObjects_HoistsBothSeparately()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["MyRecord"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema
                    {
                        OneOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["label"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            },
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["label"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public MyRecordValue? Value { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public abstract partial record MyRecordValue;", result, StringComparison.Ordinal);
        Assert.Contains("public partial record MyRecordValueVariant1", result, StringComparison.Ordinal);
        Assert.Contains("public partial record MyRecordValueVariant2", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(MyRecordValueVariant1), \"MyRecordValueVariant1\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(MyRecordValueVariant2), \"MyRecordValueVariant2\")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DiscriminatedOneOfWithRefAndInlineObject_EmitsBothDerivedTypes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Cat"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "petType" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["petType"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create("cat")] },
                    ["meow"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["Pet"] = new OpenApiSchema
            {
                OneOf =
                [
                    new OpenApiSchemaReference("Cat"),
                    new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string> { "petType" },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["petType"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create("dog")] },
                            ["bark"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                ],
                Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = "petType",
                    Mapping = new Dictionary<string, OpenApiSchemaReference>
                    {
                        ["cat"] = new OpenApiSchemaReference("Cat")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public abstract partial record Pet;", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"petType\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Cat), \"cat\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(PetVariant2), \"dog\")]", result, StringComparison.Ordinal);
        Assert.Contains("public partial record PetVariant2", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DiscriminatedUnion_InlineVariantCollidingDiscriminatorValue_DoesNotOverwriteMapping()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Cat"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "petType" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["petType"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create("cat")] },
                    ["meow"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["Pet"] = new OpenApiSchema
            {
                OneOf =
                [
                    new OpenApiSchemaReference("Cat"),
                    new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string> { "petType" },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["petType"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create("cat")] },
                            ["bark"] = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                ],
                Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = "petType",
                    Mapping = new Dictionary<string, OpenApiSchemaReference>
                    {
                        ["cat"] = new OpenApiSchemaReference("Cat")
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Explicit mapping entry should be preserved
        Assert.Contains("[JsonDerivedType(typeof(Cat), \"cat\")]", result, StringComparison.Ordinal);
        // The inline variant's discriminator value "cat" collides with the explicit mapping
        // so it should NOT overwrite the mapping entry
        Assert.DoesNotContain("[JsonDerivedType(typeof(PetVariant2), \"cat\")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_PropertyLevelDiscriminatedUnion_InlineVariantInheritsFromUnionBase()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Cat"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "petType" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["petType"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create("cat")] },
                    ["meow"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["Owner"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "pet" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["pet"] = new OpenApiSchema
                    {
                        OneOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchemaReference("Cat"),
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Required = new HashSet<string> { "petType" },
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["petType"] = new OpenApiSchema { Type = JsonSchemaType.String, Enum = [JsonValue.Create("dog")] },
                                    ["bark"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            }
                        },
                        Discriminator = new OpenApiDiscriminator
                        {
                            PropertyName = "petType"
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // The property-level union should be an abstract record
        Assert.Contains("public abstract partial record OwnerPet;", result, StringComparison.Ordinal);
        // The Cat component schema should inherit from the union base type
        Assert.Contains("public partial record Cat : OwnerPet", result, StringComparison.Ordinal);
        // The inline variant should also inherit from the union base type
        Assert.Contains("public partial record OwnerPetVariant2 : OwnerPet", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_AnyOfWithNullVariantBetweenInlineObjects_SequentialVariantNames()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Container"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "value" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema
                    {
                        AnyOf = new List<IOpenApiSchema>
                        {
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["label"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            },
                            new OpenApiSchema { Type = JsonSchemaType.Null },
                            new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["count"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Null variant should be filtered from numbering, producing sequential names
        Assert.Contains("public partial record ContainerValueVariant1", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ContainerValueVariant2", result, StringComparison.Ordinal);
        // Variant3 should NOT exist (only 2 non-null inline objects)
        Assert.DoesNotContain("ContainerValueVariant3", result, StringComparison.Ordinal);
    }

    #endregion

    #region Default Value Emission

    [Fact]
    public void Emit_DefaultBoolTrue_EmitsDefaultValue()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Settings"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["enabled"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Boolean,
                        Default = JsonValue.Create(true)
                    },
                    ["verbose"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Boolean,
                        Default = JsonValue.Create(false)
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public bool Enabled { get; init; } = true;", result, StringComparison.Ordinal);
        Assert.Contains("public bool Verbose { get; init; } = false;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultString_EmitsDefaultValue()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Default = JsonValue.Create("default-name")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public string Name { get; init; } = \"default-name\";", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultInteger_EmitsDefaultValue()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["retries"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Format = "int32",
                        Default = JsonValue.Create(3)
                    },
                    ["maxSize"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Format = "int64",
                        Default = JsonValue.Create(1024L)
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public int Retries { get; init; } = 3;", result, StringComparison.Ordinal);
        Assert.Contains("public long MaxSize { get; init; } = 1024L;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultNumber_EmitsDefaultValue()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["rate"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Number,
                        Format = "double",
                        Default = JsonValue.Create(0.5)
                    },
                    ["factor"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Number,
                        Format = "float",
                        Default = JsonValue.Create(1.0)
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public double Rate { get; init; } = 0.5d;", result, StringComparison.Ordinal);
        Assert.Contains("public float Factor { get; init; } = 1f;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultEnumValue_EmitsEnumMember()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Settings"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["mode"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = new List<JsonNode>
                        {
                            (JsonNode)"fast",
                            (JsonNode)"slow",
                            (JsonNode)"auto"
                        },
                        Default = JsonValue.Create("auto")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public Mode? Mode { get; init; } = TestModels.Mode.Auto;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultEnumValue_TopLevelEnum_EmitsEnumMember()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["LogLevel"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = new List<JsonNode>
                {
                    (JsonNode)"debug",
                    (JsonNode)"info",
                    (JsonNode)"warn",
                    (JsonNode)"error"
                }
            },
            ["Logger"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["level"] = new OpenApiSchemaReference("LogLevel")
                    {
                        Default = JsonValue.Create("info")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public LogLevel Level { get; init; } = TestModels.LogLevel.Info;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultEmptyArray_EmitsEmptyCollection()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["tags"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema { Type = JsonSchemaType.String },
                        Default = new JsonArray()
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public IReadOnlyList<string> Tags { get; init; } = [];", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_NoDefault_DoesNotEmitDefault()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String
                    }
                }
            }
        };

        string result = Generate(schemas);

        // No default → property should not have " = "
        Assert.DoesNotContain("Name { get; init; } =", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_AddDefaultValuesDisabled_EmitsNullBang()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Default = JsonValue.Create("hello")
                    }
                }
            }
        };

        var options = new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "TestModels",
            AddDefaultValuesToProperties = false
        };

        string result = Generate(schemas, options);

        // When AddDefaultValuesToProperties is false, defaults should emit "null!" to suppress warnings
        Assert.Contains("= null!;", result, StringComparison.Ordinal);
        Assert.DoesNotContain("\"hello\"", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultDateTimeString_EmitsDateTimeOffsetParse()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Event"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["startDate"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "date-time",
                        Default = JsonValue.Create("2025-01-15T10:30:00Z")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("DateTimeOffset.Parse(", result, StringComparison.Ordinal);
        Assert.Contains("DateTimeStyles.RoundtripKind", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultDateString_EmitsDateOnlyParse()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Event"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["eventDate"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "date",
                        Default = JsonValue.Create("2025-06-15")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("DateOnly.ParseExact(\"2025-06-15\", \"yyyy-MM-dd\", CultureInfo.InvariantCulture)", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultTimeString_EmitsTimeOnlyParse()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Schedule"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["duration"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "time",
                        Default = JsonValue.Create("12:30:00")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("TimeOnly.Parse(\"12:30:00.0000000\", CultureInfo.InvariantCulture)", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultUuidString_EmitsGuidParse()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Entity"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["correlationId"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "uuid",
                        Default = JsonValue.Create("550e8400-e29b-41d4-a716-446655440000")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("Guid.Parse(\"550e8400-e29b-41d4-a716-446655440000\")", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultUriString_EmitsNewUri()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Link"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["homepage"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "uri",
                        Default = JsonValue.Create("https://example.com")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("new Uri(\"https://example.com/\")", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultStringWithUnknownFormat_EmitsNullBang()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["custom"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "custom-format",
                        Default = JsonValue.Create("some-value")
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Unknown format with string default falls through to null!
        Assert.Contains("= null!;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultStringWithQuotes_EscapesQuotes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["template"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Default = JsonValue.Create("say \"hello\"")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("= \"say \\\"hello\\\"\";", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultDecimalNumber_EmitsDecimalSuffix()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Pricing"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["price"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Number,
                        Format = "decimal",
                        Default = JsonValue.Create(19.99)
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public decimal Price { get; init; } = 19.99m;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultJsonObject_DoesNotEmitDefault()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["metadata"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Default = new JsonObject { ["key"] = "value" }
                    }
                }
            }
        };

        string result = Generate(schemas);

        // JsonObject defaults cannot be represented → no default emitted
        Assert.DoesNotContain("Metadata { get; init; } =", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultOnRequiredProperty_EmitsDefaultWithRequired()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Config"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "retries" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["retries"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Format = "int32",
                        Default = JsonValue.Create(5)
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Required properties with defaults should have both 'required' keyword and default value
        Assert.Contains("public required int Retries { get; init; } = 5;", result, StringComparison.Ordinal);
    }

    #endregion
    #region Circular References

    [Fact]
    public void Emit_SelfReferencingSchema_GeneratesCorrectType()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["TreeNode"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["children"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchemaReference("TreeNode")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record TreeNode", result, StringComparison.Ordinal);
        Assert.Contains("public IReadOnlyList<TreeNode>? Children { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_MutuallyReferencingSchemas_GeneratesCorrectTypes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["A"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["b"] = new OpenApiSchemaReference("B")
                }
            },
            ["B"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["a"] = new OpenApiSchemaReference("A")
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record A", result, StringComparison.Ordinal);
        Assert.Contains("public B? B { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public partial record B", result, StringComparison.Ordinal);
        Assert.Contains("public A? A { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_InlineObjectWithParentRef_GeneratesCorrectTypes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Category"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["subcategory"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["label"] = new OpenApiSchema { Type = JsonSchemaType.String },
                            ["parent"] = new OpenApiSchemaReference("Category")
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record Category", result, StringComparison.Ordinal);
        Assert.Contains("public CategorySubcategory? Subcategory { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public partial record CategorySubcategory", result, StringComparison.Ordinal);
        Assert.Contains("public Category? Parent { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DeeplyNestedInlineObjects_GeneratesAllLevels()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Tree"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["child"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["value"] = new OpenApiSchema { Type = JsonSchemaType.String },
                            ["child"] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Object,
                                Properties = new Dictionary<string, IOpenApiSchema>
                                {
                                    ["value"] = new OpenApiSchema { Type = JsonSchemaType.String }
                                }
                            }
                        }
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record Tree", result, StringComparison.Ordinal);
        Assert.Contains("public TreeChild? Child { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public partial record TreeChild", result, StringComparison.Ordinal);
        Assert.Contains("public TreeChildChild? Child { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public partial record TreeChildChild", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_SelfReferencingThroughArray_GeneratesCorrectType()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["LinkedList"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["value"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["next"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchemaReference("LinkedList")
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("public partial record LinkedList", result, StringComparison.Ordinal);
        Assert.Contains("public IReadOnlyList<LinkedList>? Next { get; init; }", result, StringComparison.Ordinal);
    }

    #endregion
    #region Validation Attributes

    [Fact]
    public void Emit_StringWithMinLengthAndMaxLength_EmitsStringLengthAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        MinLength = 1,
                        MaxLength = 100
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[StringLength(100, MinimumLength = 1)]", result, StringComparison.Ordinal);
        Assert.Contains("using System.ComponentModel.DataAnnotations;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_StringWithOnlyMaxLength_EmitsStringLengthAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        MaxLength = 50
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[StringLength(50)]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_StringWithOnlyMinLength_EmitsMinLengthAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        MinLength = 3
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[MinLength(3)]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_StringWithPattern_EmitsRegularExpressionAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["email"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Pattern = @"^[^@]+@[^@]+$"
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains(@"[RegularExpression(""^[^@]+@[^@]+$"")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_NumberWithMinimumAndMaximum_EmitsRangeAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Product"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["price"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Number,
                        Minimum = "0",
                        Maximum = "999.99"
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[Range(0d, 999.99d)]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_NumberWithOnlyMinimum_EmitsRangeAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Product"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["quantity"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Minimum = "1"
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[Range(1d, double.MaxValue)]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_ArrayWithMinItemsAndMaxItems_EmitsMinLengthAndMaxLengthAttributes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["Collection"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["tags"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema { Type = JsonSchemaType.String },
                        MinItems = 1,
                        MaxItems = 10
                    }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[MinLength(1)]", result, StringComparison.Ordinal);
        Assert.Contains("[MaxLength(10)]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_NoValidationConstraints_DoesNotEmitDataAnnotationsUsing()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        Assert.DoesNotContain("System.ComponentModel.DataAnnotations", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_WithEmitValidationDisabled_DoesNotEmitValidationAttributes()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        MinLength = 1,
                        MaxLength = 100
                    }
                }
            }
        };

        string result = Generate(schemas, new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "TestModels",
            EmitValidationAttributes = false
        });

        Assert.DoesNotContain("StringLength", result, StringComparison.Ordinal);
        Assert.DoesNotContain("System.ComponentModel.DataAnnotations", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Emit_ValidationAttributes_CompilesSuccessfully()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        MinLength = 1,
                        MaxLength = 100,
                        Pattern = @"^[a-zA-Z]+$"
                    },
                    ["age"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Integer,
                        Minimum = "0",
                        Maximum = "150"
                    },
                    ["tags"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema { Type = JsonSchemaType.String },
                        MinItems = 0,
                        MaxItems = 5
                    }
                }
            }
        };

        string result = Generate(schemas);

        // Verify compilation (no implicit usings since we need System.ComponentModel.DataAnnotations)
        string tempRoot = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "TestResults", "ValidationCompile", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempRoot, "Generated.cs"), result, TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(tempRoot, "Harness.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <AnalysisMode>All</AnalysisMode>
                  </PropertyGroup>
                </Project>
                """, TestContext.Current.CancellationToken);
            using var proc = new System.Diagnostics.Process();
            proc.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{Path.Combine(tempRoot, "Harness.csproj")}\" -v q --nologo",
                WorkingDirectory = tempRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            proc.Start();
            string stdout = await proc.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            string stderr = await proc.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await proc.WaitForExitAsync(TestContext.Current.CancellationToken);
            Assert.True(proc.ExitCode == 0,
                $"Validation attributes code failed to compile.{Environment.NewLine}STDOUT:{stdout}{Environment.NewLine}STDERR:{stderr}");
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
        }
    }

    #endregion

    #region Deprecated / Obsolete

    [Fact]
    public void Emit_DeprecatedRecord_EmitsObsoleteAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["OldModel"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Deprecated = true,
                Description = "An old model",
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[Obsolete]", result, StringComparison.Ordinal);
        Assert.Contains("public partial record OldModel", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DeprecatedProperty_EmitsObsoleteAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String },
                    ["oldField"] = new OpenApiSchema { Type = JsonSchemaType.String, Deprecated = true }
                }
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[Obsolete]", result, StringComparison.Ordinal);
        Assert.Contains("public string? OldField { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DeprecatedEnum_EmitsObsoleteAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["OldStatus"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Deprecated = true,
                Enum = [JsonValue.Create("active"), JsonValue.Create("inactive")]
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[Obsolete]", result, StringComparison.Ordinal);
        Assert.Contains("public enum OldStatus", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DeprecatedTypeAlias_EmitsObsoleteAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["OldId"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "uuid",
                Deprecated = true
            }
        };

        string result = Generate(schemas);

        Assert.Contains("[Obsolete]", result, StringComparison.Ordinal);
        Assert.Contains("public readonly partial record struct OldId", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_NonDeprecatedSchema_DoesNotEmitObsoleteAttribute()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas);

        Assert.DoesNotContain("[Obsolete]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DeprecatedRecord_WithEmitObsoleteDisabled_DoesNotEmitObsolete()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["OldModel"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Deprecated = true,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        string result = Generate(schemas, new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "TestModels",
            EmitObsoleteAttribute = false
        });

        Assert.DoesNotContain("[Obsolete]", result, StringComparison.Ordinal);
    }

    #endregion
}
