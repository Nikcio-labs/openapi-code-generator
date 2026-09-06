using System.Diagnostics;
using Microsoft.OpenApi;

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

    private static async Task<string[]> GetSerializationLinesAsync(string generatedCode, string programSource)
    {
        string tempRoot = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "TestResults",
            "GeneratedCodeSerialization",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempRoot);

        try
        {
            string generatedPath = Path.Combine(tempRoot, "Generated.cs");
            string programPath = Path.Combine(tempRoot, "Program.cs");
            string projectPath = Path.Combine(tempRoot, "SerializationHarness.csproj");

            await File.WriteAllTextAsync(generatedPath, generatedCode).ConfigureAwait(false);
            await File.WriteAllTextAsync(programPath, programSource).ConfigureAwait(false);
            await File.WriteAllTextAsync(projectPath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """).ConfigureAwait(false);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{projectPath}\" -v q --nologo",
                    WorkingDirectory = tempRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(
                standardOutputTask,
                standardErrorTask,
                process.WaitForExitAsync()).ConfigureAwait(false);

            string standardOutput = await standardOutputTask.ConfigureAwait(false);
            string standardError = await standardErrorTask.ConfigureAwait(false);

            Assert.True(
                process.ExitCode == 0,
                $"Generated serialization harness failed with exit code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{standardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{standardError}");

            return standardOutput
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static async Task AssertGeneratedCodeCompilesAsync(string generatedCode, bool implicitUsings, bool treatWarningsAsErrors = false)
    {
        string tempRoot = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "TestResults",
            "GeneratedCodeCompilation",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempRoot);

        try
        {
            string generatedPath = Path.Combine(tempRoot, "Generated.cs");
            string projectPath = Path.Combine(tempRoot, "CompilationHarness.csproj");

            await File.WriteAllTextAsync(generatedPath, generatedCode).ConfigureAwait(false);
            await File.WriteAllTextAsync(projectPath, $$"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>{{(implicitUsings ? "enable" : "disable")}}</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <TreatWarningsAsErrors>{{(treatWarningsAsErrors ? "true" : "false")}}</TreatWarningsAsErrors>
                    <AnalysisMode>All</AnalysisMode>
                  </PropertyGroup>
                </Project>
                """).ConfigureAwait(false);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{projectPath}\" -v q --nologo",
                    WorkingDirectory = tempRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(
                standardOutputTask,
                standardErrorTask,
                process.WaitForExitAsync()).ConfigureAwait(false);

            string standardOutput = await standardOutputTask.ConfigureAwait(false);
            string standardError = await standardErrorTask.ConfigureAwait(false);

            Assert.True(
                process.ExitCode == 0,
                $"Generated code failed to compile with ImplicitUsings={(implicitUsings ? "enable" : "disable")}, TreatWarningsAsErrors={(treatWarningsAsErrors ? "true" : "false")}.{Environment.NewLine}STDOUT:{Environment.NewLine}{standardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{standardError}");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
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
        Assert.Contains("public partial record User", result, StringComparison.Ordinal);
        Assert.Contains("public partial record Address", result, StringComparison.Ordinal);
        Assert.Contains("public partial record PaginatedResponse", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ErrorResponse", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ValidationError", result, StringComparison.Ordinal);
        Assert.Contains("public partial record Circle", result, StringComparison.Ordinal);
        Assert.Contains("public partial record Rectangle", result, StringComparison.Ordinal);
        Assert.Contains("public partial record Triangle", result, StringComparison.Ordinal);
        Assert.Contains("public partial record EmailNotification", result, StringComparison.Ordinal);
        Assert.Contains("public partial record SmsNotification", result, StringComparison.Ordinal);
        Assert.Contains("public partial record FileUpload", result, StringComparison.Ordinal);
        Assert.Contains("public partial record NullableFields", result, StringComparison.Ordinal);
        Assert.Contains("public partial record StringFormats", result, StringComparison.Ordinal);
        Assert.Contains("public partial record NumericTypes", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ArrayTypes", result, StringComparison.Ordinal);
        Assert.Contains("public partial record DictionaryTypes", result, StringComparison.Ordinal);

        // Enums
        Assert.Contains("public enum UserStatus", result, StringComparison.Ordinal);
        Assert.Contains("public enum Priority", result, StringComparison.Ordinal);
        Assert.Contains("public enum HttpStatusCode", result, StringComparison.Ordinal);

        // allOf inheritance
        Assert.Contains("public partial record Cat : Pet", result, StringComparison.Ordinal);
        Assert.Contains("public partial record Dog : Pet", result, StringComparison.Ordinal);

        // oneOf with discriminator
        Assert.Contains("public abstract partial record Shape", result, StringComparison.Ordinal);

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
    public async Task Generate_WithOmitJsonPropertyNameAttributes_UsesSerializerNamingPolicy()
    {
        const string openApi = """
            {
              "openapi": "3.0.1",
              "info": {
                "title": "Test",
                "version": "1.0.0"
              },
              "paths": {},
              "components": {
                "schemas": {
                  "User": {
                    "type": "object",
                    "properties": {
                      "firstName": { "type": "string" },
                      "email": { "type": "string" }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Test",
            OmitJsonPropertyNameAttributes = true
        });

        string result = generator.GenerateFromStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(openApi)));

        Assert.DoesNotContain("[JsonPropertyName(", result, StringComparison.Ordinal);

        string[] lines = await GetSerializationLinesAsync(
            result,
            """
            using System.Text.Json;
            using Test;

            var user = new User
            {
                FirstName = "Ada",
                Email = "ada@example.com"
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            Console.WriteLine(JsonSerializer.Serialize(user, options));
            """
        );

        Assert.Contains("""{"firstName":"Ada","email":"ada@example.com"}""", lines);
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

        Assert.Contains("[JsonConverter(typeof(JsonStringEnumConverter<UserStatus>))]", result, StringComparison.Ordinal);
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

        Assert.Contains("public abstract partial record Shape", result, StringComparison.Ordinal);
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

        Assert.Contains("[JsonConverter(typeof(OpenApiGeneratedTypeAliasJsonConverter<ObjectId, Guid>))]", result, StringComparison.Ordinal);
        Assert.Contains("public readonly partial record struct ObjectId(Guid Value) : IOpenApiGeneratedTypeAlias<ObjectId, Guid>", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_FromText_TypeAliasWrapper_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Alias Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "AlertCreatedAt": {
                                        "type": "string",
                                        "format": "date-time"
                                    },
                                    "Alert": {
                                        "type": "object",
                                        "required": ["createdAt"],
                                        "properties": {
                                            "createdAt": {
                                                "$ref": "#/components/schemas/AlertCreatedAt"
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                Alert? alert = JsonSerializer.Deserialize<Alert>("{\"createdAt\":\"2024-01-02T03:04:05Z\"}");
                Console.WriteLine(alert?.CreatedAt.Value.ToString("O"));
                Console.WriteLine(JsonSerializer.Serialize(alert));
                """);

        Assert.Equal("2024-01-02T03:04:05.0000000+00:00", lines[^2]);
        Assert.Equal("{\"createdAt\":\"2024-01-02T03:04:05+00:00\"}", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_StringTypeAlias_RootNull_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Nullable Alias Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Tag": {
                                        "type": "string"
                                    }
                                }
                            }
                        }
                        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                Tag tag = JsonSerializer.Deserialize<Tag>("null");
                Console.WriteLine(tag.Value is null ? "<null>" : tag.Value);
                Console.WriteLine(JsonSerializer.Serialize(tag));
                """);

        Assert.Equal("<null>", lines[^2]);
        Assert.Equal("null", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_RecordAndEnum_RoundTripWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Record Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "UserStatus": {
                                        "type": "string",
                                        "enum": ["active", "inactive"]
                                    },
                                    "User": {
                                        "type": "object",
                                        "required": ["id", "name", "status"],
                                        "properties": {
                                            "id": { "type": "integer", "format": "int32" },
                                            "name": { "type": "string" },
                                            "status": { "$ref": "#/components/schemas/UserStatus" }
                                        }
                                    }
                                }
                            }
                        }
                        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                User? user = JsonSerializer.Deserialize<User>("{\"id\":7,\"name\":\"Ada\",\"status\":\"active\"}");
                Console.WriteLine($"{user?.Id}|{user?.Name}|{user?.Status}");
                Console.WriteLine(JsonSerializer.Serialize(user));
                """);

        Assert.Equal("7|Ada|Active", lines[^2]);
        Assert.Equal("{\"id\":7,\"name\":\"Ada\",\"status\":\"active\"}", lines[^1]);
    }

    [Fact]
    public void Generate_FromText_ReferencedEnumWithMatchingPropertyName_EmitsReferencedEnumOnce()
    {
        const string spec = """
            {
              "openapi": "3.0.3",
              "info": { "title": "Referenced Enum Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "MessageType": {
                    "type": "string",
                    "enum": ["info", "warning", "error"]
                  },
                  "SystemMessage": {
                    "type": "object",
                    "required": ["messageType"],
                    "properties": {
                      "messageType": { "$ref": "#/components/schemas/MessageType" }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.Equal(1, generatedCode.Split("public enum MessageType", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("public enum MessageType2", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public required MessageType MessageType { get; init; }", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_FromText_StringEnumWithoutType_InfersStringEnum()
    {
        const string spec = """
            {
              "openapi": "3.0.3",
              "info": { "title": "Typeless Enum Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "MessageType": {
                    "enum": ["info", "warning", "error"]
                  },
                  "SystemMessage": {
                    "type": "object",
                    "required": ["messageType"],
                    "properties": {
                      "messageType": { "$ref": "#/components/schemas/MessageType" }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.Contains("[JsonConverter(typeof(JsonStringEnumConverter<MessageType>))]", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public enum MessageType", generatedCode, StringComparison.Ordinal);
        Assert.Contains("[JsonStringEnumMemberName(\"info\")]", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public required MessageType MessageType { get; init; }", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_FromText_IntegerEnumWithoutType_InfersIntegerEnum()
    {
        const string spec = """
            {
              "openapi": "3.0.3",
              "info": { "title": "Typeless Integer Enum Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "HttpStatusCode": {
                    "enum": [200, 404, 500]
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.Contains("public enum HttpStatusCode", generatedCode, StringComparison.Ordinal);
        Assert.Contains("_200 = 200", generatedCode, StringComparison.Ordinal);
        Assert.Contains("_404 = 404", generatedCode, StringComparison.Ordinal);
        Assert.Contains("_500 = 500", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_FromText_PropertyNameWithBackslash_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Escaped Property Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Payload": {
                                        "type": "object",
                                        "required": ["bad\\q"],
                                        "properties": {
                                            "bad\\q": { "type": "string" }
                                        }
                                    }
                                }
                            }
                        }
                        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                Payload? payload = JsonSerializer.Deserialize<Payload>("{\"bad\\\\q\":\"hello\"}");
                Console.WriteLine(payload?.Badq);
                Console.WriteLine(JsonSerializer.Serialize(payload));
                """);

        Assert.Equal("hello", lines[^2]);
        Assert.Equal("{\"bad\\\\q\":\"hello\"}", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_EnumValueWithBackslash_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Escaped Enum Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Status": {
                                        "type": "string",
                                        "enum": ["bad\\q"]
                                    }
                                }
                            }
                        }
                        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                Status value = JsonSerializer.Deserialize<Status>("\"bad\\\\q\"");
                Console.WriteLine(value);
                Console.WriteLine(JsonSerializer.Serialize(value));
                """);

        Assert.Equal("Badq", lines[^2]);
        Assert.Equal("\"bad\\\\q\"", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_StringDefaultWithBackslash_PreservesDefaultValue()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Escaped Default Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Payload": {
                                        "type": "object",
                                        "properties": {
                                            "content": {
                                                "type": "string",
                                                "default": "bad\\q"
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                var payload = new Payload();
                Console.WriteLine(payload.Content);
                Console.WriteLine(JsonSerializer.Serialize(payload));
                """);

        Assert.Equal("bad\\q", lines[^2]);
        Assert.Equal("{\"content\":\"bad\\\\q\"}", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_BinaryTypeAlias_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Binary Alias Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "FileContent": {
                                        "type": "string",
                                        "format": "binary"
                                    },
                                    "Attachment": {
                                        "type": "object",
                                        "required": ["content"],
                                        "properties": {
                                            "content": {
                                                "$ref": "#/components/schemas/FileContent"
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
            using System.IO;
            using System.Text;
            using System.Text.Json;
            using GeneratedModels;

            Attachment? attachment = JsonSerializer.Deserialize<Attachment>("{\"content\":\"aGVsbG8=\"}");
            using var reader = new StreamReader(attachment!.Content.Value, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            Console.WriteLine(reader.ReadToEnd());
            Console.WriteLine(JsonSerializer.Serialize(attachment));
            """);

        Assert.Equal("hello", lines[^2]);
        Assert.Equal("{\"content\":\"aGVsbG8=\"}", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_BinaryTypeAlias_RootNull_DeserializesAsEmptyStream()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Binary Alias Null Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "FileContent": {
                                        "type": "string",
                                        "format": "binary"
                                    }
                                }
                            }
                        }
                        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
            using System.Text.Json;
            using GeneratedModels;

            FileContent content = JsonSerializer.Deserialize<FileContent>("null");
            Console.WriteLine(content.Value.GetType().Name);
            Console.WriteLine(content.Value.Length);
            Console.WriteLine(JsonSerializer.Serialize(content));
            """);

        Assert.Equal("MemoryStream", lines[^3]);
        Assert.Equal("0", lines[^2]);
        Assert.Equal("\"\"", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromSchemas_NullableBinaryTypeAlias_RoundTripsWithSystemTextJsonDefaults()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["NullableFileContent"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String | JsonSchemaType.Null,
                Format = "binary"
            },
            ["Attachment"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "content" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["content"] = new OpenApiSchemaReference("NullableFileContent")
                }
            }
        };

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromSchemas(schemas);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
            using System.IO;
            using System.Text;
            using System.Text.Json;
            using GeneratedModels;

            Attachment? attachment = JsonSerializer.Deserialize<Attachment>("{\"content\":\"aGVsbG8=\"}");
            using var reader = new StreamReader(attachment!.Content.Value!, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            Console.WriteLine(reader.ReadToEnd());
            Console.WriteLine(JsonSerializer.Serialize(attachment));
            """);

        Assert.Equal("hello", lines[^2]);
        Assert.Equal("{\"content\":\"aGVsbG8=\"}", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromSchemas_NullableBinaryTypeAlias_RoundTripsNullWithSystemTextJsonDefaults()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["NullableFileContent"] = new OpenApiSchema
            {
                Type = JsonSchemaType.String | JsonSchemaType.Null,
                Format = "binary"
            },
            ["Attachment"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["content"] = new OpenApiSchemaReference("NullableFileContent")
                }
            }
        };

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromSchemas(schemas);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
            using System.Text.Json;
            using GeneratedModels;

            Attachment? attachment = JsonSerializer.Deserialize<Attachment>("{\"content\":null}");
            Console.WriteLine(attachment?.Content is null ? "<null>" : "<value>");
            Console.WriteLine(JsonSerializer.Serialize(attachment));
            """);

        Assert.Equal("<null>", lines[^2]);
        Assert.Equal("{\"content\":null}", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromSchemas_WithIncludedSchemas_EmitsRequestedSchemasAndDependencies()
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
                AllOf =
                [
                    new OpenApiSchemaReference("Pet"),
                    new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["lives"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" }
                        }
                    }
                ]
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

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels",
            IncludeSchemas = ["Cat"]
        });

        string generatedCode = generator.GenerateFromSchemas(schemas);

        Assert.Contains("public partial record Pet", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public partial record Cat : Pet", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("public partial record Address", generatedCode, StringComparison.Ordinal);

        await AssertGeneratedCodeCompilesAsync(generatedCode, implicitUsings: true);
    }

    [Fact]
    public async Task Generate_FromSchemas_WithIncludedSchemas_EmitsPropertyReferenceDependencies()
    {
        var schemas = new Dictionary<string, IOpenApiSchema>
        {
            ["User"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
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
            },
            ["Scope"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            },
            ["Product"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["name"] = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            }
        };

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels",
            IncludeSchemas = ["User", "Scope"]
        });

        string generatedCode = generator.GenerateFromSchemas(schemas);

        Assert.Contains("public partial record User", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public partial record Address", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public partial record Scope", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("public partial record Product", generatedCode, StringComparison.Ordinal);

        await AssertGeneratedCodeCompilesAsync(generatedCode, implicitUsings: true);
    }

    [Fact]
    public void Generate_FromSchemas_WithUnknownIncludedSchema_ThrowsClearError()
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

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            IncludeSchemas = ["MissingSchema"]
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => generator.GenerateFromSchemas(schemas));

        Assert.Contains("IncludeSchemas references schema(s) not found in the provided schemas", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MissingSchema", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_FromText_NullableDirectBinaryStreamProperty_RoundTripsNullWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Binary Property Null Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Attachment": {
                                        "type": "object",
                                        "properties": {
                                            "content": {
                                                "type": "string",
                                                "format": "binary",
                                                "nullable": true
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
            using System.Text.Json;
            using GeneratedModels;

            Attachment? attachment = JsonSerializer.Deserialize<Attachment>("{\"content\":null}");
            Console.WriteLine(attachment?.Content is null ? "<null>" : "<value>");
            Console.WriteLine(JsonSerializer.Serialize(attachment));
            """);

        Assert.Equal("<null>", lines[^2]);
        Assert.Equal("{\"content\":null}", lines[^1]);
    }

    [Fact]
    public async Task Generate_ComprehensiveApi_AllOfDerivedRecord_RoundTripsWithSystemTextJsonDefaults()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                Cat? cat = JsonSerializer.Deserialize<Cat>("{\"name\":\"Milo\",\"petType\":\"cat\",\"indoor\":true,\"declawed\":false}");
                Console.WriteLine($"{cat?.Name}|{cat?.PetType}|{cat?.Indoor}|{cat?.Declawed}");
                Console.WriteLine(JsonSerializer.Serialize(cat));
                """);

        Assert.Equal("Milo|cat|True|False", lines[^2]);
        Assert.Equal("{\"indoor\":true,\"declawed\":false,\"name\":\"Milo\",\"petType\":\"cat\"}", lines[^1]);
    }

    [Fact]
    public async Task Generate_ComprehensiveApi_OneOfDiscriminatedUnion_DefaultSystemTextJsonReportsUnsupportedDerivedType()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System;
                using System.Text.Json;
                using GeneratedModels;

                try
                {
                    Shape? shape = JsonSerializer.Deserialize<Shape>("{\"shapeType\":\"circle\",\"radius\":2.5}");
                    Console.WriteLine(shape?.GetType().Name ?? "<null>");
                    Console.WriteLine(JsonSerializer.Serialize(shape));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType().Name);
                    Console.WriteLine(ex.Message);
                }
                """);

        Assert.Equal("InvalidOperationException", lines[^2]);
        Assert.Contains("not a supported derived type", lines[^1], StringComparison.Ordinal);
        Assert.Contains("GeneratedModels.Shape", lines[^1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_ComprehensiveApi_AnyOfUnion_DefaultSystemTextJsonReportsUnsupportedDerivedType()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));
        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System;
                using System.Text.Json;
                using GeneratedModels;

                try
                {
                    NotificationPreference? preference = JsonSerializer.Deserialize<NotificationPreference>("{\"email\":\"ada@example.com\",\"enabled\":true}");
                    Console.WriteLine(preference?.GetType().Name ?? "<null>");
                    Console.WriteLine(JsonSerializer.Serialize(preference));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType().Name);
                    Console.WriteLine(ex.Message);
                }
                """);

        Assert.Equal("InvalidOperationException", lines[^2]);
        Assert.Contains("not a supported derived type", lines[^1], StringComparison.Ordinal);
        Assert.Contains("GeneratedModels.NotificationPreference", lines[^1], StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_FromText_WithInlinePrimitiveTypeAliases_InlinesAliasReferences()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Alias Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "AlertCreatedAt": {
                                        "type": "string",
                                        "format": "date-time"
                                    },
                                    "Alert": {
                                        "type": "object",
                                        "required": ["createdAt"],
                                        "properties": {
                                            "createdAt": {
                                                "$ref": "#/components/schemas/AlertCreatedAt"
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
            Namespace = "Test",
            InlinePrimitiveTypeAliases = true,
        });

        string result = generator.GenerateFromText(spec);

        Assert.DoesNotContain("public record struct AlertCreatedAt(DateTimeOffset Value)", result, StringComparison.Ordinal);
        Assert.Contains("public required DateTimeOffset CreatedAt { get; init; }", result, StringComparison.Ordinal);
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

    [Fact]
    public async Task Generate_ComprehensiveApi_CompilesWithoutImplicitUsings()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            Namespace = "ValidCSharp"
        });

        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: false);
    }

    [Fact]
    public async Task Generate_ComprehensiveApi_CompilesWithImplicitUsingsWhenWarningsAreErrors()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            Namespace = "ValidCSharp"
        });

        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: true, treatWarningsAsErrors: true);
    }

    [Fact]
    public async Task Generate_ComprehensiveApi_CompilesWithoutImplicitUsingsWhenWarningsAreErrors()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            Namespace = "ValidCSharp"
        });

        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: false, treatWarningsAsErrors: true);
    }

    [Fact]
    public void Generate_ComprehensiveApi_WithModelPrefix_PrefixesGeneratedTypes()
    {
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "Prefixed",
            ModelPrefix = "Api"
        });
        string result = generator.GenerateFromFile(GetFixturePath("comprehensive-api.json"));

        Assert.Contains("public partial record ApiUser", result, StringComparison.Ordinal);
        Assert.Contains("public enum ApiUserStatus", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ApiCat : ApiPet", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ApiAddress", result, StringComparison.Ordinal);
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
        Assert.Contains("public partial record ContentItemResponseModel", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ContentTypeReferenceModel", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ContentValueModel", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ContentVariantModel", result, StringComparison.Ordinal);
        Assert.Contains("public partial record ContentUrlModel", result, StringComparison.Ordinal);
        Assert.Contains("public partial record CreateContentRequestModel", result, StringComparison.Ordinal);
        Assert.Contains("public partial record UpdateContentRequestModel", result, StringComparison.Ordinal);

        // Media
        Assert.Contains("public partial record MediaItemResponseModel", result, StringComparison.Ordinal);

        // Content types
        Assert.Contains("public partial record ContentTypeResponseModel", result, StringComparison.Ordinal);
        Assert.Contains("public partial record PropertyTypeModel", result, StringComparison.Ordinal);

        // Users
        Assert.Contains("public partial record UserResponseModel", result, StringComparison.Ordinal);

        // Enums
        Assert.Contains("public enum ContentVariantState", result, StringComparison.Ordinal);
        Assert.Contains("public enum CompositionType", result, StringComparison.Ordinal);
        Assert.Contains("public enum PropertyGroupType", result, StringComparison.Ordinal);
        Assert.Contains("public enum UserState", result, StringComparison.Ordinal);
        Assert.Contains("public enum RuntimeMode", result, StringComparison.Ordinal);
        Assert.Contains("public enum ServerStatus", result, StringComparison.Ordinal);
        Assert.Contains("public enum HealthCheckResultType", result, StringComparison.Ordinal);

        // Health checks
        Assert.Contains("public partial record HealthCheckGroupResponseModel", result, StringComparison.Ordinal);
        Assert.Contains("public partial record HealthCheckModel", result, StringComparison.Ordinal);
        Assert.Contains("public partial record HealthCheckResultResponseModel", result, StringComparison.Ordinal);

        // Problem details
        Assert.Contains("public partial record ProblemDetails", result, StringComparison.Ordinal);

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

        Assert.Contains("public partial record Item", result, StringComparison.Ordinal);
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

    #region Nullable Enum (JsonNullSentinel filtering)

    [Fact]
    public void Generate_FromText_NullableEnum_DoesNotEmitNullSentinelMember()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Nullable Enum Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "secret-scanning-alert-resolution": {
                                        "type": "string",
                                        "description": "**Required when the `state` is `resolved`.** The reason for resolving the alert.",
                                        "nullable": true,
                                        "enum": [null, "false_positive", "wont_fix", "revoked", "used_in_tests"]
                                    }
                                }
                            }
                        }
                        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.DoesNotContain("openapi-json-null-sentinel", generatedCode, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OpenapiJsonNullSentinel", generatedCode, StringComparison.Ordinal);
        Assert.Contains("FalsePositive", generatedCode, StringComparison.Ordinal);
        Assert.Contains("WontFix", generatedCode, StringComparison.Ordinal);
        Assert.Contains("Revoked", generatedCode, StringComparison.Ordinal);
        Assert.Contains("UsedInTests", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_FromText_NullableEnum_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Nullable Enum Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "secret-scanning-alert-resolution": {
                                        "type": "string",
                                        "description": "**Required when the `state` is `resolved`.** The reason for resolving the alert.",
                                        "nullable": true,
                                        "enum": [null, "false_positive", "wont_fix", "revoked", "used_in_tests"]
                                    },
                                    "Alert": {
                                        "type": "object",
                                        "required": ["id"],
                                        "properties": {
                                            "id": { "type": "integer", "format": "int32" },
                                            "resolution": { "$ref": "#/components/schemas/secret-scanning-alert-resolution" }
                                        }
                                    }
                                }
                            }
                        }
                        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.DoesNotContain("openapi-json-null-sentinel", generatedCode, StringComparison.OrdinalIgnoreCase);

        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                // Non-null enum value round-trips
                Alert? alert = JsonSerializer.Deserialize<Alert>("{\"id\":1,\"resolution\":\"wont_fix\"}");
                Console.WriteLine($"{alert?.Id}|{alert?.Resolution}");
                Console.WriteLine(JsonSerializer.Serialize(alert));

                // Null enum value round-trips
                Alert? nullAlert = JsonSerializer.Deserialize<Alert>("{\"id\":2,\"resolution\":null}");
                Console.WriteLine($"{nullAlert?.Id}|{nullAlert?.Resolution}");
                Console.WriteLine(JsonSerializer.Serialize(nullAlert));
                """);

        Assert.Equal("1|WontFix", lines[^4]);
        Assert.Equal("{\"id\":1,\"resolution\":\"wont_fix\"}", lines[^3]);
        Assert.Equal("2|", lines[^2]);
        Assert.Equal("{\"id\":2,\"resolution\":null}", lines[^1]);
    }

    #endregion

    #region Inline Object Hoisting

    [Fact]
    public async Task Generate_FromText_InlineObjectProperty_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Inline Object Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Integration": {
                                        "type": "object",
                                        "required": ["id", "permissions"],
                                        "properties": {
                                            "id": { "type": "integer", "format": "int32" },
                                            "permissions": {
                                                "type": "object",
                                                "properties": {
                                                    "issues": { "type": "boolean" },
                                                    "pullRequests": { "type": "boolean" }
                                                }
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        // Verify the inline object is hoisted
        Assert.Contains("public partial record IntegrationPermissions", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public required IntegrationPermissions Permissions { get; init; }", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("public required object Permissions", generatedCode, StringComparison.Ordinal);

        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                Integration? integration = JsonSerializer.Deserialize<Integration>("{\"id\":7,\"permissions\":{\"issues\":true,\"pullRequests\":false}}");
                Console.WriteLine($"{integration?.Id}|{integration?.Permissions.Issues}|{integration?.Permissions.PullRequests}");
                Console.WriteLine(JsonSerializer.Serialize(integration));
                """);

        Assert.Equal("7|True|False", lines[^2]);
        Assert.Equal("""{"id":7,"permissions":{"issues":true,"pullRequests":false}}""", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_NestedInlineObjects_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Nested Inline Object Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "App": {
                                        "type": "object",
                                        "required": ["config"],
                                        "properties": {
                                            "config": {
                                                "type": "object",
                                                "required": ["database"],
                                                "properties": {
                                                    "database": {
                                                        "type": "object",
                                                        "required": ["host"],
                                                        "properties": {
                                                            "host": { "type": "string" },
                                                            "port": { "type": "integer", "format": "int32" }
                                                        }
                                                    }
                                                }
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.Contains("public partial record AppConfig", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public partial record AppConfigDatabase", generatedCode, StringComparison.Ordinal);

        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                App? app = JsonSerializer.Deserialize<App>("{\"config\":{\"database\":{\"host\":\"localhost\",\"port\":5432}}}");
                Console.WriteLine($"{app?.Config.Database.Host}:{app?.Config.Database.Port}");
                Console.WriteLine(JsonSerializer.Serialize(app));
                """);

        Assert.Equal("localhost:5432", lines[^2]);
        Assert.Equal("""{"config":{"database":{"host":"localhost","port":5432}}}""", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_InlineObjectInArray_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Array Inline Object Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Repository": {
                                        "type": "object",
                                        "required": ["webhooks"],
                                        "properties": {
                                            "webhooks": {
                                                "type": "array",
                                                "items": {
                                                    "type": "object",
                                                    "required": ["url"],
                                                    "properties": {
                                                        "url": { "type": "string" },
                                                        "active": { "type": "boolean" }
                                                    }
                                                }
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.Contains("public partial record RepositoryWebhooks", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public required IReadOnlyList<RepositoryWebhooks> Webhooks { get; init; }", generatedCode, StringComparison.Ordinal);

        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                Repository? repo = JsonSerializer.Deserialize<Repository>("{\"webhooks\":[{\"url\":\"https://example.com/hook\",\"active\":true}]}");
                Console.WriteLine($"{repo?.Webhooks[0].Url}|{repo?.Webhooks[0].Active}");
                Console.WriteLine(JsonSerializer.Serialize(repo));
                """);

        Assert.Equal("https://example.com/hook|True", lines[^2]);
        Assert.Equal("""{"webhooks":[{"url":"https://example.com/hook","active":true}]}""", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_InlineObjectWithRefProperty_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Inline Object With Ref Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Address": {
                                        "type": "object",
                                        "required": ["city"],
                                        "properties": {
                                            "city": { "type": "string" }
                                        }
                                    },
                                    "User": {
                                        "type": "object",
                                        "required": ["id", "profile"],
                                        "properties": {
                                            "id": { "type": "integer", "format": "int32" },
                                            "profile": {
                                                "type": "object",
                                                "required": ["address"],
                                                "properties": {
                                                    "address": { "$ref": "#/components/schemas/Address" }
                                                }
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.Contains("public partial record UserProfile", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public required UserProfile Profile { get; init; }", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public required Address Address { get; init; }", generatedCode, StringComparison.Ordinal);

        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                User? user = JsonSerializer.Deserialize<User>("{\"id\":1,\"profile\":{\"address\":{\"city\":\"London\"}}}");
                Console.WriteLine($"{user?.Id}|{user?.Profile.Address.City}");
                Console.WriteLine(JsonSerializer.Serialize(user));
                """);

        Assert.Equal("1|London", lines[^2]);
        Assert.Equal("""{"id":1,"profile":{"address":{"city":"London"}}}""", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_InlineObject_CompilesWithImplicitUsingsAndWarningsAsErrors()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Compile Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Webhook": {
                                        "type": "object",
                                        "required": ["config"],
                                        "properties": {
                                            "config": {
                                                "type": "object",
                                                "required": ["url"],
                                                "properties": {
                                                    "url": { "type": "string" },
                                                    "contentType": { "type": "string" }
                                                }
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        await AssertGeneratedCodeCompilesAsync(generatedCode, implicitUsings: true, treatWarningsAsErrors: true);
    }

    [Fact]
    public async Task Generate_FromText_InlineObject_CompilesWithoutImplicitUsings()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Compile Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Webhook": {
                                        "type": "object",
                                        "required": ["config"],
                                        "properties": {
                                            "config": {
                                                "type": "object",
                                                "required": ["url"],
                                                "properties": {
                                                    "url": { "type": "string" },
                                                    "contentType": { "type": "string" }
                                                }
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        await AssertGeneratedCodeCompilesAsync(generatedCode, implicitUsings: false);
    }

    #endregion

    #region Inline allOf / oneOf / anyOf Hoisting

    [Fact]
    public async Task Generate_FromText_InlineOneOfAllRefs_CompilesAndGeneratesCorrectStructure()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Union Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Integration": {
                                        "type": "object",
                                        "required": ["id", "owner"],
                                        "properties": {
                                            "id": { "type": "integer", "format": "int32" },
                                            "owner": {
                                                "oneOf": [
                                                    { "$ref": "#/components/schemas/SimpleUser" },
                                                    { "$ref": "#/components/schemas/Enterprise" }
                                                ]
                                            }
                                        }
                                    },
                                    "SimpleUser": {
                                        "type": "object",
                                        "required": ["login"],
                                        "properties": {
                                            "login": { "type": "string" }
                                        }
                                    },
                                    "Enterprise": {
                                        "type": "object",
                                        "required": ["slug"],
                                        "properties": {
                                            "slug": { "type": "string" }
                                        }
                                    }
                                }
                            }
                        }
                        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.Contains("public abstract partial record IntegrationOwner", generatedCode, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(SimpleUser), \"SimpleUser\")]", generatedCode, StringComparison.Ordinal);
        Assert.Contains("[JsonDerivedType(typeof(Enterprise), \"Enterprise\")]", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public required IntegrationOwner Owner { get; init; }", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("public required object Owner", generatedCode, StringComparison.Ordinal);

        await AssertGeneratedCodeCompilesAsync(generatedCode, implicitUsings: true, treatWarningsAsErrors: true);
    }

    [Fact]
    public async Task Generate_FromText_InlineAllOfWithoutRef_RoundTripsWithSystemTextJsonDefaults()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "AllOf Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Webhook": {
                                        "type": "object",
                                        "required": ["forkee"],
                                        "properties": {
                                            "forkee": {
                                                "allOf": [
                                                    {
                                                        "type": "object",
                                                        "required": ["id"],
                                                        "properties": {
                                                            "id": { "type": "integer", "format": "int32" },
                                                            "name": { "type": "string" }
                                                        }
                                                    }
                                                ]
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.Contains("public partial record WebhookForkee", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public required WebhookForkee Forkee { get; init; }", generatedCode, StringComparison.Ordinal);

        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using GeneratedModels;

                Webhook? webhook = JsonSerializer.Deserialize<Webhook>("{\"forkee\":{\"id\":42,\"name\":\"repo\"}}");
                Console.WriteLine($"{webhook?.Forkee.Id}|{webhook?.Forkee.Name}");
                Console.WriteLine(JsonSerializer.Serialize(webhook));
                """);

        Assert.Equal("42|repo", lines[^2]);
        Assert.Equal("""{"forkee":{"id":42,"name":"repo"}}""", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_EmptyObjectProperty_RoundTripsAsDictionary()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Empty Object Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Webhook": {
                                        "type": "object",
                                        "required": ["payload"],
                                        "properties": {
                                            "payload": {
                                                "type": "object",
                                                "additionalProperties": true
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
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        Assert.Contains("public required IReadOnlyDictionary<string, object?> Payload { get; init; }", generatedCode, StringComparison.Ordinal);

        string[] lines = await GetSerializationLinesAsync(generatedCode, """
                using System.Text.Json;
                using System.Text.Json.Nodes;
                using GeneratedModels;

                Webhook? webhook = JsonSerializer.Deserialize<Webhook>("{\"payload\":{\"action\":\"opened\",\"number\":42}}");
                Console.WriteLine(webhook?.Payload?["action"]);
                Console.WriteLine(JsonSerializer.Serialize(webhook));
                """);

        Assert.Equal("opened", lines[^2]);
        Assert.Equal("""{"payload":{"action":"opened","number":42}}""", lines[^1]);
    }

    [Fact]
    public async Task Generate_FromText_InlineAllOfAndOneOf_CompilesWithWarningsAsErrors()
    {
        const string spec = """
                        {
                            "openapi": "3.0.3",
                            "info": { "title": "Compile Test", "version": "1.0.0" },
                            "components": {
                                "schemas": {
                                    "Container": {
                                        "type": "object",
                                        "required": ["union", "composed"],
                                        "properties": {
                                            "union": {
                                                "oneOf": [
                                                    { "$ref": "#/components/schemas/Cat" },
                                                    { "$ref": "#/components/schemas/Dog" }
                                                ]
                                            },
                                            "composed": {
                                                "allOf": [
                                                    {
                                                        "type": "object",
                                                        "required": ["name"],
                                                        "properties": {
                                                            "name": { "type": "string" }
                                                        }
                                                    }
                                                ]
                                            }
                                        }
                                    },
                                    "Cat": {
                                        "type": "object",
                                        "properties": { "meow": { "type": "string" } }
                                    },
                                    "Dog": {
                                        "type": "object",
                                        "properties": { "bark": { "type": "string" } }
                                    }
                                }
                            }
                        }
                        """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            GenerateFileHeader = false,
            Namespace = "GeneratedModels"
        });

        string generatedCode = generator.GenerateFromText(spec);

        await AssertGeneratedCodeCompilesAsync(generatedCode, implicitUsings: true, treatWarningsAsErrors: true);
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
    #region Circular References

    [Fact]
    public void Generate_SelfReferencingSchema_DoesNotInfiniteLoop()
    {
        string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "TreeNode": {
                    "type": "object",
                    "properties": {
                      "value": { "type": "string" },
                      "children": {
                        "type": "array",
                        "items": { "$ref": "#/components/schemas/TreeNode" }
                      }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator();
        string result = generator.GenerateFromText(spec);

        Assert.Contains("public partial record TreeNode", result, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<TreeNode>", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_MutuallyReferencingSchemas_DoesNotInfiniteLoop()
    {
        string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "A": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "b": { "$ref": "#/components/schemas/B" }
                    }
                  },
                  "B": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "a": { "$ref": "#/components/schemas/A" }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator();
        string result = generator.GenerateFromText(spec);

        Assert.Contains("public partial record A", result, StringComparison.Ordinal);
        Assert.Contains("public partial record B", result, StringComparison.Ordinal);
        Assert.Contains("public B? B { get; init; }", result, StringComparison.Ordinal);
        Assert.Contains("public A? A { get; init; }", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_SelfReferencingSchema_CompilesSuccessfully()
    {
        string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "TreeNode": {
                    "type": "object",
                    "properties": {
                      "value": { "type": "string" },
                      "children": {
                        "type": "array",
                        "items": { "$ref": "#/components/schemas/TreeNode" }
                      }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator();
        string result = generator.GenerateFromText(spec);

        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: true);
        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: false);
        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: true, treatWarningsAsErrors: true);
    }

    [Fact]
    public async Task Generate_MutuallyReferencingSchemas_CompilesSuccessfully()
    {
        string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "A": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "b": { "$ref": "#/components/schemas/B" }
                    }
                  },
                  "B": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "a": { "$ref": "#/components/schemas/A" }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator();
        string result = generator.GenerateFromText(spec);

        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: true);
        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: false);
        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: true, treatWarningsAsErrors: true);
    }

    [Fact]
    public void Generate_CircularReference_WithIncludeSchemas_DoesNotInfiniteLoop()
    {
        string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "A": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "b": { "$ref": "#/components/schemas/B" }
                    }
                  },
                  "B": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "a": { "$ref": "#/components/schemas/A" }
                    }
                  },
                  "C": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            IncludeSchemas = ["A"]
        });
        string result = generator.GenerateFromText(spec);

        // A and B should both be included (B is a dependency of A)
        Assert.Contains("public partial record A", result, StringComparison.Ordinal);
        Assert.Contains("public partial record B", result, StringComparison.Ordinal);
        // C should NOT be included
        Assert.DoesNotContain("public partial record C", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_CircularReference_SerializationRoundTrip()
    {
        string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "TreeNode": {
                    "type": "object",
                    "properties": {
                      "value": { "type": "string" },
                      "children": {
                        "type": "array",
                        "items": { "$ref": "#/components/schemas/TreeNode" }
                      }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator();
        string generatedCode = generator.GenerateFromText(spec);

        string programSource = """
            using System.Text.Json;
            using GeneratedModels;

            var node = new TreeNode
            {
                Value = "root",
                Children =
                [
                    new TreeNode { Value = "child1" },
                    new TreeNode { Value = "child2", Children = [new TreeNode { Value = "grandchild" }] }
                ]
            };

            string json = JsonSerializer.Serialize(node);
            TreeNode? deserialized = JsonSerializer.Deserialize<TreeNode>(json);

            Console.WriteLine(deserialized!.Value);
            Console.WriteLine(deserialized.Children![0].Value);
            Console.WriteLine(deserialized.Children[1].Value);
            Console.WriteLine(deserialized.Children[1].Children![0].Value);
            """;

        string[] lines = await GetSerializationLinesAsync(generatedCode, programSource);

        Assert.Equal("root", lines[0]);
        Assert.Equal("child1", lines[1]);
        Assert.Equal("child2", lines[2]);
        Assert.Equal("grandchild", lines[3]);
    }

    #endregion
    #region Deprecated / Obsolete

    [Fact]
    public void Generate_DeprecatedSchema_EmitsObsoleteAttribute()
    {
        string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "OldModel": {
                    "type": "object",
                    "deprecated": true,
                    "properties": {
                      "name": { "type": "string" }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator();
        string result = generator.GenerateFromText(spec);

        Assert.Contains("[Obsolete]", result, StringComparison.Ordinal);
        Assert.Contains("public partial record OldModel", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_DeprecatedProperty_EmitsObsoleteAttribute()
    {
        string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "User": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "oldField": { "type": "string", "deprecated": true }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator();
        string result = generator.GenerateFromText(spec);

        Assert.Contains("[Obsolete]", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generate_DeprecatedSchema_CompilesSuccessfully()
    {
        string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "OldModel": {
                    "type": "object",
                    "deprecated": true,
                    "properties": {
                      "name": { "type": "string" }
                    }
                  },
                  "NewModel": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" }
                    }
                  }
                }
              }
            }
            """;

        var generator = new CSharpSchemaGenerator();
        string result = generator.GenerateFromText(spec);

        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: true);
        await AssertGeneratedCodeCompilesAsync(result, implicitUsings: false);
    }

    #endregion
}
