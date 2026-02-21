namespace OpenApiCodeGenerator.Tests;

/// <summary>
/// Integration tests for <see cref="CSharpSchemaGenerator"/> — testing end-to-end generation
/// from OpenAPI specification files (JSON fixtures).
/// </summary>
public class CSharpSchemaGeneratorTests
{
    private static string GetFixturePath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }

    #region Comprehensive API Fixture

    [Fact]
    public void Generate_ComprehensiveApi_DoesNotThrow()
    {
        var generator = new CSharpSchemaGenerator();
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_ComprehensiveApi_ContainsAllSchemas()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            Namespace = "ComprehensiveApi"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        // Records
        Assert.Contains("public record User", result, StringComparison.Ordinal);
        Assert.Contains("public record Address", result, StringComparison.Ordinal);
        Assert.Contains("public record PaginatedResponse", result, StringComparison.Ordinal);
        Assert.Contains("public record ErrorResponse", result, StringComparison.Ordinal);
        Assert.Contains("public record ValidationError", result, StringComparison.Ordinal);
        Assert.Contains("public record Circle", result, StringComparison.Ordinal);
        Assert.Contains("public record Rectangle", result, StringComparison.Ordinal);
        Assert.Contains("public record Triangle", result, StringComparison.Ordinal);
        Assert.Contains("public record EmailNotification", result, StringComparison.Ordinal);
        Assert.Contains("public record SmsNotification", result, StringComparison.Ordinal);
        Assert.Contains("public record FileUpload", result, StringComparison.Ordinal);
        Assert.Contains("public record NullableFields", result, StringComparison.Ordinal);
        Assert.Contains("public record StringFormats", result, StringComparison.Ordinal);
        Assert.Contains("public record NumericTypes", result, StringComparison.Ordinal);
        Assert.Contains("public record ArrayTypes", result, StringComparison.Ordinal);
        Assert.Contains("public record DictionaryTypes", result, StringComparison.Ordinal);

        // Enums
        Assert.Contains("public enum UserStatus", result, StringComparison.Ordinal);
        Assert.Contains("public enum Priority", result, StringComparison.Ordinal);
        Assert.Contains("public enum HttpStatusCode", result, StringComparison.Ordinal);

        // allOf inheritance
        Assert.Contains("public record Cat : Pet", result, StringComparison.Ordinal);
        Assert.Contains("public record Dog : Pet", result, StringComparison.Ordinal);

        // oneOf with discriminator
        Assert.Contains("public abstract record Shape", result, StringComparison.Ordinal);

        // Namespace
        Assert.Contains("namespace ComprehensiveApi;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ComprehensiveApi_RequiredProperties_MarkedCorrectly()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        // User has required id, name, email, status
        Assert.Contains("public required int Id { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required string Name { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required string Email { get; init; }", result, StringComparison.Ordinal);

        // Optional property age on User should be nullable
        Assert.Contains("public int? Age { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ComprehensiveApi_NullableFieldsSchema_HandledCorrectly()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        // Required + nullable: should have required keyword but nullable type
        Assert.Contains("public required string? RequiredNullable { get; init; }", result, StringComparison.Ordinal);

        // Required + non-nullable: should have required keyword with non-nullable type
        Assert.Contains("public required string RequiredNonNullable { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ComprehensiveApi_StringFormats_MappedCorrectly()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        // StringFormats schema
        Assert.Contains("public required DateTimeOffset DateTime { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required DateOnly Date { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required Guid Uuid { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required Uri Uri { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ComprehensiveApi_NumericTypes_MappedCorrectly()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        Assert.Contains("public required int Int32Value { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required long Int64Value { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required float FloatValue { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required double DoubleValue { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ComprehensiveApi_ArrayTypes_MappedCorrectly()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        Assert.Contains("public required IReadOnlyList<string> StringArray { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required IReadOnlyList<int> IntArray { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ComprehensiveApi_EnumStringValues_Generated()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        Assert.Contains("[JsonConverter(typeof(JsonStringEnumConverter))]", result, StringComparison.Ordinal);
        Assert.Contains("public enum UserStatus", result, StringComparison.Ordinal);
        Assert.Contains("Active", result, StringComparison.Ordinal);
        Assert.Contains("Inactive", result, StringComparison.Ordinal);
        Assert.Contains("Banned", result, StringComparison.Ordinal);
        Assert.Contains("Pending", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ComprehensiveApi_ShapeDiscriminator_Generated()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        Assert.Contains("public abstract record Shape", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Circle), \"circle\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Rectangle), \"rectangle\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Triangle), \"triangle\")]", result, StringComparison.Ordinal);
        Assert.Contains("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"shapeType\")]", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ComprehensiveApi_TypeAlias_Generated()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        Assert.Contains("public record struct ObjectId(Guid Value)", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ComprehensiveApi_ProducesValidCSharpStructure()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            Namespace = "ValidCSharp"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        // Basic structural validity checks
        Assert.Contains("#nullable enable", result, StringComparison.Ordinal);
        Assert.Contains("using System.Text.Json.Serialization;", result, StringComparison.Ordinal);
        Assert.Contains("namespace ValidCSharp;", result, StringComparison.Ordinal);

        // Brackets should be balanced
        int openBraces = result.Count(c => c == '{');
        int closeBraces = result.Count(c => c == '}');
        Assert.Equal(openBraces, closeBraces);
    }

    #endregion

    #region Umbraco Management API Fixture

    [Fact]
    public void Generate_UmbracoApi_DoesNotThrow()
    {
        var generator = new CSharpSchemaGenerator();
        string result = generator.GenerateFromFile(GetFixturePath("umbraco-management-api.json"));

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Generate_UmbracoApi_ContainsAllSchemas()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            Namespace = "Umbraco.Api.Models"
        });
        string result = generator.GenerateFromFile(GetFixturePath("umbraco-management-api.json"));

        // Content models
        Assert.Contains("public record ContentItemResponseModel", result, StringComparison.Ordinal);
        Assert.Contains("public record ContentTypeReferenceModel", result, StringComparison.Ordinal);
        Assert.Contains("public record ContentValueModel", result, StringComparison.Ordinal);
        Assert.Contains("public record ContentVariantModel", result, StringComparison.Ordinal);
        Assert.Contains("public record ContentUrlModel", result, StringComparison.Ordinal);
        Assert.Contains("public record CreateContentRequestModel", result, StringComparison.Ordinal);
        Assert.Contains("public record UpdateContentRequestModel", result, StringComparison.Ordinal);

        // Media
        Assert.Contains("public record MediaItemResponseModel", result, StringComparison.Ordinal);

        // Content types
        Assert.Contains("public record ContentTypeResponseModel", result, StringComparison.Ordinal);
        Assert.Contains("public record PropertyTypeModel", result, StringComparison.Ordinal);

        // Users
        Assert.Contains("public record UserResponseModel", result, StringComparison.Ordinal);

        // Enums
        Assert.Contains("public enum ContentVariantState", result, StringComparison.Ordinal);
        Assert.Contains("public enum CompositionType", result, StringComparison.Ordinal);
        Assert.Contains("public enum PropertyGroupType", result, StringComparison.Ordinal);
        Assert.Contains("public enum UserState", result, StringComparison.Ordinal);
        Assert.Contains("public enum RuntimeMode", result, StringComparison.Ordinal);
        Assert.Contains("public enum ServerStatus", result, StringComparison.Ordinal);
        Assert.Contains("public enum HealthCheckResultType", result, StringComparison.Ordinal);

        // Health checks
        Assert.Contains("public record HealthCheckGroupResponseModel", result, StringComparison.Ordinal);
        Assert.Contains("public record HealthCheckModel", result, StringComparison.Ordinal);
        Assert.Contains("public record HealthCheckResultResponseModel", result, StringComparison.Ordinal);

        // Problem details
        Assert.Contains("public record ProblemDetails", result, StringComparison.Ordinal);

        // Namespace
        Assert.Contains("namespace Umbraco.Api.Models;", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_UmbracoApi_ContentItemResponseModel_HasCorrectProperties()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("umbraco-management-api.json"));

        // Required properties
        Assert.Contains("public required Guid Id { get; init; }", result, StringComparison.Ordinal);

        // Required ref properties
        Assert.Contains("public required ContentTypeReferenceModel ContentType { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_UmbracoApi_NullableProperties_HandledCorrectly()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("umbraco-management-api.json"));

        // ContentValueModel.culture is nullable
        // ContentVariantModel.publishDate is nullable + optional date-time
        // ContentTypeResponseModel.description is nullable

        // The generated code should handle these nullable patterns
        Assert.Contains("Culture", result, StringComparison.Ordinal);
        Assert.Contains("Segment", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_UmbracoApi_EnumValues_CorrectlyGenerated()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });
        string result = generator.GenerateFromFile(GetFixturePath("umbraco-management-api.json"));

        // ContentVariantState enum
        Assert.Contains("Draft", result, StringComparison.Ordinal);
        Assert.Contains("Published", result, StringComparison.Ordinal);
        Assert.Contains("PublishedPendingChanges", result, StringComparison.Ordinal);
        Assert.Contains("NotCreated", result, StringComparison.Ordinal);

        // UserState enum
        Assert.Contains("Active", result, StringComparison.Ordinal);
        Assert.Contains("Disabled", result, StringComparison.Ordinal);
        Assert.Contains("LockedOut", result, StringComparison.Ordinal);
        Assert.Contains("Invited", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_UmbracoApi_BracesBalanced()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            Namespace = "Umbraco"
        });
        string result = generator.GenerateFromFile(GetFixturePath("umbraco-management-api.json"));

        int openBraces = result.Count(c => c == '{');
        int closeBraces = result.Count(c => c == '}');
        Assert.Equal(openBraces, closeBraces);
    }

    #endregion

    #region Generator from Text

    [Fact]
    public void GenerateFromText_MinimalSpec_Works()
    {
        string spec = """
        {
          "openapi": "3.0.3",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {},
          "components": {
            "schemas": {
              "Item": {
                "type": "object",
                "required": ["id", "name"],
                "properties": {
                  "id": { "type": "integer", "format": "int32" },
                  "name": { "type": "string" }
                }
              }
            }
          }
        }
        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });

        string result = generator.GenerateFromText(spec);

        Assert.Contains("public record Item", result, StringComparison.Ordinal);
        Assert.Contains("public required int Id { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public required string Name { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateFromText_EmptySchemas_DoesNotThrow()
    {
        string spec = """
        {
          "openapi": "3.0.3",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {}
        }
        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test"
        });

        string result = generator.GenerateFromText(spec);

        Assert.NotNull(result);
        Assert.Contains("namespace Test;", result, StringComparison.Ordinal);
    }

    #endregion

    #region Options

    [Fact]
    public void Generate_WithMutableCollections_UsesListAndDictionary()
    {
        string spec = """
        {
          "openapi": "3.0.3",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {},
          "components": {
            "schemas": {
              "Container": {
                "type": "object",
                "required": ["items"],
                "properties": {
                  "items": {
                    "type": "array",
                    "items": { "type": "string" }
                  }
                }
              }
            }
          }
        }
        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            UseImmutableArrays = false,
            UseImmutableDictionaries = false,
            Namespace = "Test"
        });

        string result = generator.GenerateFromText(spec);

        Assert.Contains("List<string>", result, StringComparison.Ordinal);
        Assert.DoesNotContain("IReadOnlyList", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_CustomNamespace_Applied()
    {
        string spec = """
        {
          "openapi": "3.0.3",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {},
          "components": {
            "schemas": {
              "Item": { "type": "object" }
            }
          }
        }
        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "My.Custom.Namespace"
        });

        string result = generator.GenerateFromText(spec);
        Assert.Contains("namespace My.Custom.Namespace;", result, StringComparison.Ordinal);
    }

    #endregion

    #region HandleDiagnostics

    [Fact]
    public void GenerateFromText_InvalidSpecWithNoSchemas_Throws()
    {
        // Completely invalid JSON that isn't an OpenAPI spec
        string spec = "{ \"not\": \"openapi\" }";

        var generator = new CSharpSchemaGenerator();

        Assert.Throws<InvalidOperationException>(() => generator.GenerateFromText(spec));
    }

    #endregion
}
