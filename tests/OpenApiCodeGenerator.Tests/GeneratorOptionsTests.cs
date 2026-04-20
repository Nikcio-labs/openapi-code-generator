namespace OpenApiCodeGenerator.Tests;

public class GeneratorOptionsTests
{
    [Fact]
    public void Validate_DefaultOptions_DoesNotThrow()
    {
        var options = new GeneratorOptions();

        options.Validate();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".Models")]
    [InlineData("Models.")]
    [InlineData("My..Models")]
    [InlineData("123.Models")]
    [InlineData("My-App.Models")]
    [InlineData("My.class.Models")]
    public void Validate_InvalidNamespace_ThrowsArgumentException(string namespaceName)
    {
        var options = new GeneratorOptions
        {
            Namespace = namespaceName
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Theory]
    [InlineData("1Api")]
    [InlineData("api-prefix")]
    [InlineData("api prefix")]
    public void Validate_InvalidModelPrefix_ThrowsArgumentException(string modelPrefix)
    {
        var options = new GeneratorOptions
        {
            ModelPrefix = modelPrefix
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Ctor_InvalidOptions_ThrowsArgumentException()
    {
        var options = new GeneratorOptions
        {
            Namespace = "123.Invalid"
        };

        Assert.Throws<ArgumentException>(() => new CSharpSchemaGenerator(options));
    }

    [Fact]
    public void Validate_BlankIncludedSchema_ThrowsArgumentException()
    {
        var options = new GeneratorOptions
        {
            IncludeSchemas = ["User", " "]
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }
}
