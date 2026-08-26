using System.Diagnostics;

namespace OpenApiCodeGenerator.Tests;

/// <summary>
/// Tests for the CLI entry point (Program.cs) — testing argument parsing,
/// help/version output, file I/O, and error handling via process invocation.
/// </summary>
public class CliTests
{
    private static string CliDllPath => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "OpenApiCodeGenerator.Cli", "bin", "Debug", "net10.0", "OpenApiCodeGen.dll"));

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunCliAsync(params string[] args)
    {
        var allArgs = new List<string> { CliDllPath };
        allArgs.AddRange(args);
        return await RunProcessAsync("dotnet", allArgs);
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessAsync(string fileName, List<string> args)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        foreach (string arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static string CreateTempSpecFile()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"spec_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "User": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" }
                    }
                  }
                }
              }
            }
            """);
        return tempFile;
    }

    #region Help and Version

    [Fact]
    public async Task Help_Flag_PrintsUsage_ReturnsZero()
    {
        (int exitCode, string stdout, string stderr) = await RunCliAsync("--help");

        Assert.True(exitCode == 0, $"Expected exit code 0 but got {exitCode}. Stderr: {stderr}");
        Assert.Contains("openapi-codegen", stdout, StringComparison.Ordinal);
        Assert.Contains("USAGE", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Help_ShortFlag_PrintsUsage_ReturnsZero()
    {
        (int exitCode, string stdout, _) = await RunCliAsync("-h");

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Version_Flag_PrintsVersion_ReturnsZero()
    {
        (int exitCode, string stdout, _) = await RunCliAsync("--version");

        Assert.Equal(0, exitCode);
        Assert.Contains("openapi-codegen", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Version_ShortFlag_PrintsVersion_ReturnsZero()
    {
        (int exitCode, string stdout, _) = await RunCliAsync("-v");

        Assert.Equal(0, exitCode);
        Assert.Contains("openapi-codegen", stdout, StringComparison.Ordinal);
    }

    #endregion

    #region No Arguments

    [Fact]
    public async Task NoArgs_PrintsUsage_ReturnsOne()
    {
        (int exitCode, string stdout, _) = await RunCliAsync();

        Assert.Equal(1, exitCode);
        Assert.Contains("USAGE", stdout, StringComparison.Ordinal);
    }

    #endregion

    #region File Input / Output

    [Fact]
    public async Task ValidFile_GeneratesToStdout_ReturnsZero()
    {
        string specFile = CreateTempSpecFile();
        try
        {
            (int exitCode, string stdout, string stderr) = await RunCliAsync(specFile);

            Assert.Equal(0, exitCode);
            Assert.Contains("public partial record User", stdout, StringComparison.Ordinal);
            Assert.Contains("namespace GeneratedModels", stdout, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    [Fact]
    public async Task ValidFile_OutputToFile_WritesFile_ReturnsZero()
    {
        string specFile = CreateTempSpecFile();
        string outputFile = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.cs");
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync(specFile, outputFile);

            Assert.Equal(0, exitCode);
            Assert.Contains($"Generated: {outputFile}", stdout, StringComparison.Ordinal);
            Assert.True(File.Exists(outputFile));
            string generatedContent = File.ReadAllText(outputFile);
            Assert.Contains("public partial record User", generatedContent, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(specFile);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }

    [Fact]
    public async Task ValidFile_OutputFlag_WritesFile_ReturnsZero()
    {
        string specFile = CreateTempSpecFile();
        string outputFile = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.cs");
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync("--input", specFile, "--output", outputFile);

            Assert.Equal(0, exitCode);
            Assert.Contains($"Generated: {outputFile}", stdout, StringComparison.Ordinal);
            Assert.True(File.Exists(outputFile));
        }
        finally
        {
            File.Delete(specFile);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }

    [Fact]
    public async Task OutputFile_CreatesParentDirectory()
    {
        string specFile = CreateTempSpecFile();
        string outputDir = Path.Combine(Path.GetTempPath(), $"cli_test_{Guid.NewGuid():N}", "subdir");
        string outputFile = Path.Combine(outputDir, "Models.cs");
        try
        {
            (int exitCode, _, _) = await RunCliAsync(specFile, outputFile);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputFile));
        }
        finally
        {
            File.Delete(specFile);
            if (Directory.Exists(outputDir)) Directory.Delete(Path.GetDirectoryName(outputDir)!, recursive: true);
        }
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task MissingInputFile_ReturnsError()
    {
        (int exitCode, _, string stderr) = await RunCliAsync("nonexistent_file.json");

        Assert.Equal(1, exitCode);
        Assert.Contains("not found", stderr, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidNamespace_ReturnsError()
    {
        string specFile = CreateTempSpecFile();
        try
        {
            (int exitCode, _, string stderr) = await RunCliAsync(specFile, "--namespace", "123Invalid");

            Assert.Equal(1, exitCode);
            Assert.Contains("Namespace", stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    [Fact]
    public async Task UnknownArgument_ReturnsError()
    {
        string specFile = CreateTempSpecFile();
        string outputFile = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid():N}.cs");
        try
        {
            (int exitCode, _, string stderr) = await RunCliAsync(specFile, outputFile, "--unknown-flag");

            Assert.Equal(1, exitCode);
            Assert.Contains("Unknown argument", stderr, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(specFile);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }

    #endregion

    #region Options

    [Fact]
    public async Task NamespaceFlag_SetsGeneratedNamespace()
    {
        string specFile = CreateTempSpecFile();
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync(specFile, "--namespace", "MyApp.Models");

            Assert.Equal(0, exitCode);
            Assert.Contains("namespace MyApp.Models", stdout, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    [Fact]
    public async Task NamespaceShortFlag_SetsGeneratedNamespace()
    {
        string specFile = CreateTempSpecFile();
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync(specFile, "-n", "Custom.Namespace");

            Assert.Equal(0, exitCode);
            Assert.Contains("namespace Custom.Namespace", stdout, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    [Fact]
    public async Task ModelPrefixFlag_AddsPrefixToTypes()
    {
        string specFile = CreateTempSpecFile();
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync(specFile, "--model-prefix", "Api");

            Assert.Equal(0, exitCode);
            Assert.Contains("public partial record ApiUser", stdout, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    [Fact]
    public async Task NoHeaderFlag_SuppressesAutoGeneratedHeader()
    {
        string specFile = CreateTempSpecFile();
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync(specFile, "--no-header");

            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("<auto-generated>", stdout, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    [Fact]
    public async Task NoDocCommentsFlag_SuppressesDocComments()
    {
        string specFile = CreateTempSpecFile();
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync(specFile, "--no-doc-comments");

            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("/// <summary>", stdout, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    [Fact]
    public async Task MutableArraysFlag_UsesMutableList()
    {
        string specFile = CreateTempSpecFile();
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync(specFile, "--mutable-arrays");

            Assert.Equal(0, exitCode);
            // No arrays in our test spec, but should not error
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    [Fact]
    public async Task OmitJsonAttributesFlag_SuppressesJsonPropertyName()
    {
        string specFile = CreateTempSpecFile();
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync(specFile, "--omit-json-attributes");

            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("[JsonPropertyName", stdout, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    [Fact]
    public async Task InlineTypeAliasesFlag_InlinesPrimitiveAliases()
    {
        string specFile = Path.Combine(Path.GetTempPath(), $"spec_{Guid.NewGuid():N}.json");
        File.WriteAllText(specFile, """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "UserId": { "type": "string", "format": "uuid" },
                  "User": {
                    "type": "object",
                    "properties": {
                      "id": { "$ref": "#/components/schemas/UserId" }
                    }
                  }
                }
              }
            }
            """);
        try
        {
            (int exitCode, string stdout, _) = await RunCliAsync(specFile, "--inline-type-aliases");

            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("readonly partial record struct UserId", stdout, StringComparison.Ordinal);
            Assert.Contains("Guid", stdout, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(specFile);
        }
    }

    #endregion
}
