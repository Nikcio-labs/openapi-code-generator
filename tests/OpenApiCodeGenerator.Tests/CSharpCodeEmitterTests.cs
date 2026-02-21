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
        Assert.Contains("public record User", result, StringComparison.Ordinal);

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
        Assert.Contains("[JsonConverter(typeof(JsonStringEnumConverter))]", result, StringComparison.Ordinal);
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

        Assert.Contains("public record Entry", result, StringComparison.Ordinal);

        Assert.Contains("public enum Status", result, StringComparison.Ordinal);
        Assert.Contains("[JsonConverter(typeof(JsonStringEnumConverter))]", result, StringComparison.Ordinal);
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
        Assert.Contains("public record Cat : Pet", result, StringComparison.Ordinal);

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

        Assert.Contains("public abstract record Shape", result, StringComparison.Ordinal);
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

        Assert.Contains("public abstract record Result", result, StringComparison.Ordinal);
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

        Assert.Contains("public record struct ObjectId(Guid Value)", result, StringComparison.Ordinal);
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

        Assert.Contains("public record EmptyObject", result, StringComparison.Ordinal);
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
        Assert.Contains("public record WorkbookTable : Entity", result, StringComparison.Ordinal);

        // WorkbookTable should have its own 'name' property
        Assert.Contains("public string? Name { get; init; }", result, StringComparison.Ordinal);

        // Split the output to isolate the WorkbookTable record body.
        // The Entity record has odataType; WorkbookTable should NOT re-declare it.
        string workbookSection = result.Substring(result.IndexOf("public record WorkbookTable", StringComparison.Ordinal));
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
        Assert.Contains("public record Asset", result, StringComparison.Ordinal);

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
        Assert.Contains("public record struct MyString(string Value)", result, StringComparison.Ordinal);

        // "my_string" (has underscore) gets differentiated
        Assert.Contains("public record struct MyUnderscoreString(string Value)", result, StringComparison.Ordinal);

        // User is unaffected
        Assert.Contains("public record User", result, StringComparison.Ordinal);
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
        Assert.Contains("public record struct MyType(string Value)", result, StringComparison.Ordinal);

        // "myType" (camelCase) gets differentiated with naming style suffix
        Assert.Contains("public record struct MyTypeCamelCase(string Value)", result, StringComparison.Ordinal);
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

        Assert.Contains("public record Flexible", result, StringComparison.Ordinal);
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
        Assert.Contains("public record Order", result, StringComparison.Ordinal);
        Assert.Contains("public record User", result, StringComparison.Ordinal);

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

        Assert.Contains("public LogLevel? Level { get; init; } = TestModels.LogLevel.Info;", result, StringComparison.Ordinal);
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
    public void Emit_DefaultDateTimeString_EmitsDateTimeParse()
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

        Assert.Contains("DateTime.Parse(", result, StringComparison.Ordinal);
        Assert.Contains("DateTimeStyles.AdjustToUniversal", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultDateString_EmitsDateParse()
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

        Assert.Contains("DateTime.Parse(\"2025-06-15\")", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Emit_DefaultTimeString_EmitsTimeSpanParse()
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

        Assert.Contains("TimeSpan.Parse(\"12:30:00\")", result, StringComparison.Ordinal);
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
}
