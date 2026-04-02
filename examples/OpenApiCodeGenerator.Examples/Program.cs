#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA1303 // Do not pass literals as localized parameters

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using OpenApiCodeGenerator;

string outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "output"));

string showcaseSpecPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "specs", "showcase-openapi.yaml"));

var specifications = new Dictionary<string, string>
{
    ["showcase-openapi"] = showcaseSpecPath,
    ["github-api"] = "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.yaml",
    ["github-api-next"] = "https://raw.githubusercontent.com/github/rest-api-description/main/descriptions-next/api.github.com/api.github.com.yaml",
    ["octokit-ghes-3.6-diff-to-api"] = "https://raw.githubusercontent.com/octokit/octokit-next.js/main/cache/types-openapi/ghes-3.6-diff-to-api.github.com.json",
    ["stripe-api"] = "https://raw.githubusercontent.com/stripe/openapi/master/openapi/spec3.yaml",
    ["petstore"] = "https://petstore3.swagger.io/api/v3/openapi.json",
};

if (Directory.Exists(outputDir))
{
    Directory.Delete(outputDir, recursive: true);
}

Directory.CreateDirectory(outputDir);

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenApiCodeGenerator-Examples/1.0");

Console.WriteLine("OpenAPI C# Code Generator — Example Preview");
Console.WriteLine(new string('=', 50));
Console.WriteLine();

int succeeded = 0;
int failed = 0;

foreach ((string? name, string source) in specifications)
{
    Console.Write($"  {name,-40} ");
    var sw = Stopwatch.StartNew();

    try
    {
        string namespaceName = "Generated." + NameToNamespace(name);
        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            Namespace = namespaceName,
            GenerateDocComments = true,
            GenerateFileHeader = true,
        });

        using Stream stream = await OpenSpecificationStreamAsync(httpClient, source).ConfigureAwait(false);

        string code = generator.GenerateFromStream(stream);
        sw.Stop();

        string outputPath = Path.Combine(outputDir, $"{name}.cs");
        await File.WriteAllTextAsync(outputPath, code).ConfigureAwait(false);

        int lines = code.Split('\n').Length;
        int schemas = CountSchemas(code);
        Console.WriteLine($"OK  ({sw.Elapsed.TotalSeconds:F1}s, {schemas} types, {lines:N0} lines)");
        succeeded++;
    }
    catch (Exception ex)
    {
        sw.Stop();
        Console.WriteLine($"FAIL ({sw.Elapsed.TotalSeconds:F1}s)");
        Console.WriteLine($"    Error: {ex.Message}");

        // Write error report
        string errorPath = Path.Combine(outputDir, $"{name}.error.txt");
        await File.WriteAllTextAsync(errorPath, $"Source: {source}\n\n{ex}").ConfigureAwait(false);
        failed++;
    }
}

Console.WriteLine();
Console.WriteLine(new string('-', 50));
Console.WriteLine($"  Results: {succeeded} succeeded, {failed} failed");
Console.WriteLine($"  Output:  {outputDir}{Path.DirectorySeparatorChar}");
Console.WriteLine();

if (succeeded > 0)
{
    Console.WriteLine("Generated files:");
    foreach (string? file in Directory.GetFiles(outputDir, "*.cs").Order())
    {
        var info = new FileInfo(file);
        Console.WriteLine($"  {info.Name,-45} {info.Length / 1024.0:F0} KB");
    }
}

return failed > 0 ? 1 : 0;

static int CountSchemas(string code)
{
    int count = 0;
    ReadOnlySpan<char> span = code.AsSpan();
    foreach (ReadOnlySpan<char> line in span.EnumerateLines())
    {
        ReadOnlySpan<char> trimmed = line.TrimStart();
        if (trimmed.StartsWith("public record ") ||
            trimmed.StartsWith("public enum ") ||
            trimmed.StartsWith("public record struct "))
        {
            count++;
        }
    }
    return count;
}

static string NameToNamespace(string name)
{
    string[] parts = name.Split('-', '.', '_');

    return string.Concat(parts.Select(p =>
    {
        if (p.Length == 0)
        {
            return "";
        }

        return char.ToUpperInvariant(p[0]) + p[1..];
    }));
}

[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Stream ownership is transferred to the caller.")]
static async Task<Stream> OpenSpecificationStreamAsync(HttpClient httpClient, string source)
{
    if (File.Exists(source))
    {
        return File.OpenRead(source);
    }

    if (Uri.TryCreate(source, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
    {
        HttpResponseMessage response = await httpClient.GetAsync(uri).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }

    throw new FileNotFoundException($"Could not resolve specification source '{source}'.", source);
}
