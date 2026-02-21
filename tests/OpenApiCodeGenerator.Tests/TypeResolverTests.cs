using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace OpenApiCodeGenerator.Tests;

/// <summary>
/// Tests for <see cref="TypeResolver"/> — resolving OpenAPI schemas to C# type strings.
/// </summary>
public class TypeResolverTests
{
    private static TypeResolver CreateResolver(GeneratorOptions? options = null)
    {
        var schemas = new Dictionary<string, IOpenApiSchema>();
        return new TypeResolver(options ?? new GeneratorOptions(), schemas);
    }

    #region Primitive Types

    [Fact]
    public void Resolve_StringType_ReturnsString()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.String };
        Assert.Equal("string", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_IntegerInt32_ReturnsInt()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
        Assert.Equal("int", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_IntegerInt64_ReturnsLong()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" };
        Assert.Equal("long", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_IntegerNoFormat_ReturnsInt()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer };
        Assert.Equal("int", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_NumberFloat_ReturnsFloat()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "float" };
        Assert.Equal("float", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_NumberDouble_ReturnsDouble()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double" };
        Assert.Equal("double", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_NumberNoFormat_ReturnsDouble()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number };
        Assert.Equal("double", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_Boolean_ReturnsBool()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.Boolean };
        Assert.Equal("bool", resolver.Resolve(schema));
    }

    #endregion

    #region String Formats

    [Theory]
    [InlineData("date-time", "DateTimeOffset")]
    [InlineData("date", "DateOnly")]
    [InlineData("time", "TimeOnly")]
    [InlineData("duration", "TimeSpan")]
    [InlineData("uuid", "Guid")]
    [InlineData("uri", "Uri")]
    [InlineData("byte", "byte[]")]
    [InlineData("binary", "Stream")]
    public void Resolve_StringFormats_MapsCorrectly(string format, string expected)
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = format };
        Assert.Equal(expected, resolver.Resolve(schema));
    }

    #endregion

    #region Nullable

    [Fact]
    public void Resolve_NullableString_ReturnsNullableString()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null };
        Assert.Equal("string?", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_NullableInt32_ReturnsNullableInt()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer | JsonSchemaType.Null, Format = "int32" };
        Assert.Equal("int?", resolver.Resolve(schema));
    }

    [Fact]
    public void ResolveWithNullability_RequiredNonNullable_NoQuestionMark()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.String };
        Assert.Equal("string", resolver.ResolveWithNullability(schema, isRequired: true));
    }

    [Fact]
    public void ResolveWithNullability_OptionalField_AddsQuestionMark()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.String };
        Assert.Equal("string?", resolver.ResolveWithNullability(schema, isRequired: false));
    }

    [Fact]
    public void ResolveWithNullability_RequiredButNullable_AddsQuestionMark()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null };
        Assert.Equal("string?", resolver.ResolveWithNullability(schema, isRequired: true));
    }

    #endregion

    #region Array Types

    [Fact]
    public void Resolve_ArrayOfStrings_ReturnsIReadOnlyList()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema { Type = JsonSchemaType.String }
        };
        Assert.Equal("IReadOnlyList<string>", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_ArrayOfInts_ReturnsIReadOnlyListInt()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
        };
        Assert.Equal("IReadOnlyList<int>", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_ArrayOfObjects_WithMutableOption_ReturnsList()
    {
        TypeResolver resolver = CreateResolver(new GeneratorOptions { UseImmutableArrays = false });
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema { Type = JsonSchemaType.String }
        };
        Assert.Equal("List<string>", resolver.Resolve(schema));
    }

    #endregion

    #region Object Types

    [Fact]
    public void Resolve_ObjectWithAdditionalProperties_ReturnsDictionary()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.String }
        };
        Assert.Equal("IReadOnlyDictionary<string, string>", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_ObjectWithAdditionalProperties_MutableOption()
    {
        TypeResolver resolver = CreateResolver(new GeneratorOptions { UseImmutableDictionaries = false });
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.String }
        };
        Assert.Equal("Dictionary<string, string>", resolver.Resolve(schema));
    }

    [Fact]
    public void Resolve_EmptyObject_ReturnsObject()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchema { Type = JsonSchemaType.Object };
        Assert.Equal("object", resolver.Resolve(schema));
    }

    #endregion

    #region Reference Types

    [Fact]
    public void Resolve_Reference_ReturnsTypeName()
    {
        TypeResolver resolver = CreateResolver();
        var schema = new OpenApiSchemaReference("UserStatus");
        Assert.Equal("UserStatus", resolver.Resolve(schema));
    }

    #endregion

    #region Schema Classification

    [Fact]
    public void IsEnum_StringEnum_ReturnsTrue()
    {
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = new List<JsonNode> { (JsonNode)"a", (JsonNode)"b" }
        };
        Assert.True(TypeResolver.IsEnum(schema));
    }

    [Fact]
    public void IsEnum_NoEnum_ReturnsFalse()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String };
        Assert.False(TypeResolver.IsEnum(schema));
    }

    [Fact]
    public void IsTypeAlias_SimpleString_ReturnsTrue()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
        Assert.True(TypeResolver.IsTypeAlias(schema));
    }

    [Fact]
    public void IsTypeAlias_ObjectWithProperties_ReturnsFalse()
    {
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
            }
        };
        Assert.False(TypeResolver.IsTypeAlias(schema));
    }

    #endregion

    #region DefaultNonNullable

    [Fact]
    public void ResolveWithNullability_OptionalWithDefault_DefaultNonNullableEnabled_NoQuestionMark()
    {
        TypeResolver resolver = CreateResolver(new GeneratorOptions { DefaultNonNullable = true });
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Default = JsonValue.Create("hello")
        };
        // Optional but has a default, so should be non-nullable when DefaultNonNullable is true
        Assert.Equal("string", resolver.ResolveWithNullability(schema, isRequired: false));
    }

    [Fact]
    public void ResolveWithNullability_OptionalWithDefault_DefaultNonNullableDisabled_AddsQuestionMark()
    {
        TypeResolver resolver = CreateResolver(new GeneratorOptions { DefaultNonNullable = false });
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Default = JsonValue.Create("hello")
        };
        // DefaultNonNullable is off, so optional property is still nullable
        Assert.Equal("string?", resolver.ResolveWithNullability(schema, isRequired: false));
    }

    [Fact]
    public void ResolveWithNullability_OptionalNoDefault_DefaultNonNullableEnabled_AddsQuestionMark()
    {
        TypeResolver resolver = CreateResolver(new GeneratorOptions { DefaultNonNullable = true });
        var schema = new OpenApiSchema { Type = JsonSchemaType.String };
        // No default value, so still nullable even with DefaultNonNullable on
        Assert.Equal("string?", resolver.ResolveWithNullability(schema, isRequired: false));
    }

    #endregion
}
