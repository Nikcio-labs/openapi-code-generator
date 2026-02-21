namespace OpenApiCodeGenerator.Tests;

/// <summary>
/// Tests for <see cref="NameHelper"/> — converting OpenAPI names to valid C# identifiers.
/// </summary>
public class NameHelperTests
{
    [Theory]
    [InlineData("User", "User")]
    [InlineData("user", "User")]
    [InlineData("userStatus", "UserStatus")]
    [InlineData("user_status", "UserStatus")]
    [InlineData("user-status", "UserStatus")]
    [InlineData("user.status", "UserStatus")]
    [InlineData("UserStatus", "UserStatus")]
    [InlineData("USER_STATUS", "UserStatus")]
    [InlineData("myAPIResponse", "MyAPIResponse")]
    [InlineData("123invalid", "_123invalid")]
    [InlineData("class", "Class")]
    [InlineData("string", "String")]
    [InlineData("object", "Object")]
    [InlineData("", "UnknownType")]
    [InlineData("  ", "UnknownType")]
    public void ToTypeName_ConvertsCorrectly(string input, string expected)
    {
        string result = NameHelper.ToTypeName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("#/components/schemas/MyType", "MyType")]
    [InlineData("#/components/schemas/user-profile", "UserProfile")]
    public void ToTypeName_HandlesRefPaths(string input, string expected)
    {
        string result = NameHelper.ToTypeName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("firstName", "FirstName")]
    [InlineData("first_name", "FirstName")]
    [InlineData("first-name", "FirstName")]
    [InlineData("id", "Id")]
    [InlineData("class", "Class")]
    [InlineData("123start", "_123start")]
    public void ToPropertyName_ConvertsCorrectly(string input, string expected)
    {
        string result = NameHelper.ToPropertyName(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("active", "Active")]
    [InlineData("in-progress", "InProgress")]
    [InlineData("not_started", "NotStarted")]
    [InlineData("COMPLETED", "Completed")]
    [InlineData("123", "_123")]
    [InlineData("class", "Class")]
    public void ToEnumMemberName_ConvertsCorrectly(string input, string expected)
    {
        string result = NameHelper.ToEnumMemberName(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetJsonPropertyName_AlwaysReturnsOriginal()
    {
        string? result = NameHelper.GetJsonPropertyName("firstName", "FirstName");
        Assert.Equal("firstName", result);
    }

    #region ToDifferentiatedName

    [Fact]
    public void ToDifferentiatedName_UnderscorePrefixExpandsToUnderscoreWord()
    {
        var existing = new HashSet<string> { "Id" };
        string result = NameHelper.ToDifferentiatedName("_id", "Id", existing);
        Assert.Equal("UnderscoreId", result);
    }

    [Fact]
    public void ToDifferentiatedName_DashPrefixExpandsToDashWord()
    {
        var existing = new HashSet<string> { "Value" };
        string result = NameHelper.ToDifferentiatedName("-value", "Value", existing);
        // Leading dash before non-digit should expand to "Dash"
        Assert.Equal("DashValue", result);
    }

    [Fact]
    public void ToDifferentiatedName_MiddleSpecialCharExpandsWithFullExpand()
    {
        var existing = new HashSet<string> { "MyString" };
        string result = NameHelper.ToDifferentiatedName("my_string", "MyString", existing);
        // No special prefix, ExpandAll: "my Underscore string" → "MyUnderscoreString"
        Assert.Equal("MyUnderscoreString", result);
    }

    [Fact]
    public void ToDifferentiatedName_NamingStyleSuffix_CamelCase()
    {
        var existing = new HashSet<string> { "MyType" };
        string result = NameHelper.ToDifferentiatedName("myType", "MyType", existing);
        Assert.Equal("MyTypeCamelCase", result);
    }

    [Fact]
    public void ToDifferentiatedName_NamingStyleSuffix_Lowercase()
    {
        var existing = new HashSet<string> { "Name" };
        string result = NameHelper.ToDifferentiatedName("name", "Name", existing);
        Assert.Equal("NameLowercase", result);
    }

    [Fact]
    public void ToDifferentiatedName_FallsBackToNumericIfAllElseFails()
    {
        // Both expanded and style-suffixed names are taken
        var existing = new HashSet<string>
        {
            "Name", "NameLowercase", "NameCamelCase", "NamePascalCase",
            "NameSnakeCase", "NameKebabCase", "NameDotNotation",
            "NameUppercase"
        };
        string result = NameHelper.ToDifferentiatedName("name", "Name", existing);
        Assert.Equal("Name2", result);
    }

    #endregion

    #region NaturalnessScore

    [Fact]
    public void NaturalnessScore_ExactMatch_ReturnsZero()
    {
        Assert.Equal(0, NameHelper.NaturalnessScore("MyType", "MyType"));
    }

    [Fact]
    public void NaturalnessScore_CasingDifference_ReturnsOne()
    {
        Assert.Equal(1, NameHelper.NaturalnessScore("id", "Id"));
    }

    [Fact]
    public void NaturalnessScore_CamelCase_ReturnsOne()
    {
        // "myType" equals "MyType" case-insensitively → returns 1 (casing difference)
        Assert.Equal(1, NameHelper.NaturalnessScore("myType", "MyType"));
    }

    [Fact]
    public void NaturalnessScore_SpecialChars_ReturnsHighScore()
    {
        Assert.Equal(11, NameHelper.NaturalnessScore("_id", "Id"));
        Assert.Equal(11, NameHelper.NaturalnessScore("my_string", "MyString"));
    }

    #endregion
}
