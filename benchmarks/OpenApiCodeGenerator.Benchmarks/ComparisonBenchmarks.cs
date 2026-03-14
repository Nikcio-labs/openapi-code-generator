#pragma warning disable CA1303 // Do not pass literals as localized parameters

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace OpenApiCodeGenerator.Benchmarks;

/// <summary>
/// Side-by-side comparison of the <b>current</b> (in-tree) generator against the
/// <b>latest released</b> NuGet version (<c>Nikcio.OpenApiCodeGen</c>).
///
/// Two benchmark families are provided for each spec size:
/// <list type="bullet">
///   <item><description>
///     <b>ParseAndGenerate</b> — measures the full <c>GenerateFromText</c> pipeline
///     (YAML/JSON parsing + code emission). For large specs, parsing dominates.
///   </description></item>
///   <item><description>
///     <b>GenerateOnly</b> — the spec is pre-parsed once in <c>GlobalSetup</c>;
///     only the code-emission step is benchmarked. This isolates the impact of
///     our emitter optimizations.
///   </description></item>
/// </list>
///
/// Run with:
///   dotnet run -c Release -- --filter *Comparison*
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ComparisonBenchmarks
{
    private string _petstoreText = default!;
    private string _stripeApiText = default!;
    private string _githubApiText = default!;
    private string _githubApiNextText = default!;

    private static readonly Uri PetstoreUrl = new("https://petstore3.swagger.io/api/v3/openapi.json");
    private static readonly Uri StripeApiUrl = new("https://raw.githubusercontent.com/stripe/openapi/master/openapi/spec3.yaml");
    private static readonly Uri GithubApiUrl = new("https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.yaml");
    private static readonly Uri GitHubApiNextUrl = new("https://raw.githubusercontent.com/github/rest-api-description/main/descriptions-next/api.github.com/api.github.com.yaml");

    private OpenApiDocument _petstoreDoc = default!;
    private OpenApiDocument _stripeApiDoc = default!;
    private OpenApiDocument _githubApiDoc = default!;
    private OpenApiDocument _githubApiNextDoc = default!;

    private CSharpSchemaGenerator _current = default!;
    private ReleasedGeneratorProxy _released = default!;

    [GlobalSetup]
    public void Setup()
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        _petstoreText = LoadSpecFromUrlAsync(PetstoreUrl).GetAwaiter().GetResult();
        _stripeApiText = LoadSpecFromUrlAsync(StripeApiUrl).GetAwaiter().GetResult();
        _githubApiText = LoadSpecFromUrlAsync(GithubApiUrl).GetAwaiter().GetResult();
        _githubApiNextText = LoadSpecFromUrlAsync(GitHubApiNextUrl).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        _current = new CSharpSchemaGenerator(); // Run with defaults

        _released = new ReleasedGeneratorProxy();

        Console.WriteLine("Pre-parsing specs…");

        _petstoreDoc = ParseDocument(_petstoreText);
        _stripeApiDoc = ParseDocument(_stripeApiText);
        _githubApiDoc = ParseDocument(_githubApiText);
        _githubApiNextDoc = ParseDocument(_githubApiNextText);

        Console.WriteLine("Setup complete.");
    }

    [Benchmark(Description = "Released", Baseline = true)]
    [BenchmarkCategory("Petstore", "ParseAndGenerate")]
    public string Released_Petstore_ParseAndGenerate()
    {
        return _released.GenerateFromText(_petstoreText);
    }

    [Benchmark(Description = "Current")]
    [BenchmarkCategory("Petstore", "ParseAndGenerate")]
    public string Current_Petstore_ParseAndGenerate()
    {
        return _current.GenerateFromText(_petstoreText);
    }

    [Benchmark(Description = "Released", Baseline = true)]
    [BenchmarkCategory("StripeApi", "ParseAndGenerate")]
    public string Released_StripeApi_ParseAndGenerate()
    {
        return _released.GenerateFromText(_stripeApiText);
    }

    [Benchmark(Description = "Current")]
    [BenchmarkCategory("StripeApi", "ParseAndGenerate")]
    public string Current_StripeApi_ParseAndGenerate()
    {
        return _current.GenerateFromText(_stripeApiText);
    }

    [Benchmark(Description = "Released", Baseline = true)]
    [BenchmarkCategory("GitHubApi", "ParseAndGenerate")]
    public string Released_GitHubApi_ParseAndGenerate()
    {
        return _released.GenerateFromText(_githubApiText);
    }

    [Benchmark(Description = "Current")]
    [BenchmarkCategory("GitHubApi", "ParseAndGenerate")]
    public string Current_GitHubApi_ParseAndGenerate()
    {
        return _current.GenerateFromText(_githubApiText);
    }

    [Benchmark(Description = "Released", Baseline = true)]
    [BenchmarkCategory("GitHubApiNext", "ParseAndGenerate")]
    public string Released_GitHubApiNext_ParseAndGenerate()
    {
        return _released.GenerateFromText(_githubApiNextText);
    }

    [Benchmark(Description = "Current")]
    [BenchmarkCategory("GitHubApiNext", "ParseAndGenerate")]
    public string Current_GitHubApiNext_ParseAndGenerate()
    {
        return _current.GenerateFromText(_githubApiNextText);
    }

    [Benchmark(Description = "Current")]
    [BenchmarkCategory("Petstore", "GenerateOnly")]
    public string Current_Petstore_GenerateOnly()
    {
        return _current.GenerateFromDocument(_petstoreDoc);
    }

    [Benchmark(Description = "Current")]
    [BenchmarkCategory("StripeApi", "GenerateOnly")]
    public string Current_StripeApi_GenerateOnly()
    {
        return _current.GenerateFromDocument(_stripeApiDoc);
    }

    [Benchmark(Description = "Current")]
    [BenchmarkCategory("GitHubApi", "GenerateOnly")]
    public string Current_GitHubApi_GenerateOnly()
    {
        return _current.GenerateFromDocument(_githubApiDoc);
    }

    [Benchmark(Description = "Current")]
    [BenchmarkCategory("GitHubApiNext", "GenerateOnly")]
    public string Current_GitHubApiNext_GenerateOnly()
    {
        return _current.GenerateFromDocument(_githubApiNextDoc);
    }

    private static OpenApiDocument ParseDocument(string openApiText)
    {
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        ReadResult result = OpenApiDocument.Parse(openApiText, settings: settings);
        return result.Document
            ?? throw new InvalidOperationException("Failed to parse OpenAPI document.");
    }

    private static async Task<string> LoadSpecFromUrlAsync(Uri url)
    {
        Console.WriteLine($"Downloading spec from {url} ...");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("OpenApiCodeGenerator-Benchmarks/1.0");

        string spec = await http.GetStringAsync(url).ConfigureAwait(false);

        Console.WriteLine($"Downloaded {spec.Length / 1024} KB");

        return spec;
    }
}
