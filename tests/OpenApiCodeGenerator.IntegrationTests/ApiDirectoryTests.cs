using System.Text.Json;

namespace OpenApiCodeGenerator.IntegrationTests;

/// <summary>
/// Runs the generator against OpenAPI specifications.
/// These tests are slow (they download specs from the internet) and are meant to be
/// run manually for integration testing and inspection of generated code, not as part of the regular unit test suite.
/// </summary>
public class ApiDirectoryTests : IAsyncLifetime
{
    private static readonly string OutputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "output");

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private static List<ApiEntry>? _apis;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    static ApiDirectoryTests()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("OpenApiCodeGenerator-IntegrationTests/1.0");
    }

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(OutputDir);
        await EnsureApisLoadedAsync().ConfigureAwait(true);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    [Trait("Category", "Integration")]
    [Theory(Skip = "Long-running test that generates code from a large collection of OpenAPI specs. Run manually for integration testing and inspection of generated code, not as part of the regular unit test suite.")]
    [MemberData(nameof(GetAllApis))]
    public async Task GenerateFromSpecAsync(string apiId, string specUrl)
    {
        ArgumentNullException.ThrowIfNull(apiId);

        // Download spec
        using HttpResponseMessage response = await Http.GetAsync(specUrl, TestContext.Current.CancellationToken).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();

        using Stream stream = (await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));

        var generator = new CSharpSchemaGenerator(new GeneratorOptions
        {
            Namespace = "Generated." + NameHelper.ToTypeName(apiId),
            GenerateDocComments = true,
            GenerateFileHeader = true,
        });

        // Generate C# code — should not throw
        string code = generator.GenerateFromStream(stream);

        Assert.False(string.IsNullOrWhiteSpace(code), $"Generated code for {apiId} was empty.");

        // Write output for inspection
        string safeFileName = SanitizeFileName(apiId);
        string outputPath = Path.Combine(OutputDir, $"{safeFileName}.cs");
        await File.WriteAllTextAsync(outputPath, code, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    public static IEnumerable<object[]> GetAllApis()
    {
        // MemberData is evaluated synchronously — load the API list synchronously
        List<ApiEntry> apis = LoadApisSync();
        foreach (ApiEntry api in apis)
        {
            yield return new object[] { api.Id, api.SpecUrl };
        }
    }

    private static List<ApiEntry> LoadApisSync()
    {
        if (_apis != null)
        {
            return _apis;
        }

        _initLock.Wait();
        try
        {
#pragma warning disable CA1508 // Avoid dead conditional code
            if (_apis != null)
            {
                return _apis;
            }
#pragma warning restore CA1508 // Avoid dead conditional code

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            _apis = FetchApiListAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            return _apis;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static async Task EnsureApisLoadedAsync()
    {
        if (_apis != null)
        {
            return;
        }

        await _initLock.WaitAsync().ConfigureAwait(true);
        try
        {
#pragma warning disable CA1508 // Avoid dead conditional code
            _apis ??= await FetchApiListAsync().ConfigureAwait(true);
#pragma warning restore CA1508 // Avoid dead conditional code
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static async Task<List<ApiEntry>> FetchApiListAsync()
    {
        string json = await Http.GetStringAsync("https://api.apis.guru/v2/list.json").ConfigureAwait(true);
        using var doc = JsonDocument.Parse(json);

        var entries = new List<ApiEntry>();

        foreach (JsonProperty providerProp in doc.RootElement.EnumerateObject())
        {
            string providerId = providerProp.Name;

            if (!providerProp.Value.TryGetProperty("versions", out JsonElement versions))
            {
                continue;
            }

            foreach (JsonProperty versionProp in versions.EnumerateObject())
            {
                JsonElement versionObj = versionProp.Value;

                // Prefer the JSON URL as it's more compact
                string? specUrl = null;
                if (versionObj.TryGetProperty("swaggerUrl", out JsonElement jsonUrl))
                {
                    specUrl = jsonUrl.GetString();
                }
                else if (versionObj.TryGetProperty("swaggerYamlUrl", out JsonElement yamlUrl))
                {
                    specUrl = yamlUrl.GetString();
                }

                if (specUrl == null)
                {
                    continue;
                }

                string apiId = versions.EnumerateObject().Count() > 1
                    ? $"{providerId}@{versionProp.Name}"
                    : providerId;

                entries.Add(new ApiEntry(apiId, specUrl));
            }
        }

        return entries.OrderBy(e => e.Id).ToList();
    }

    private static string SanitizeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        char[] sanitized = new char[name.Length];
        for (int i = 0; i < name.Length; i++)
        {
            sanitized[i] = Array.IndexOf(invalid, name[i]) >= 0 ? '_' : name[i];
        }
        return new string(sanitized);
    }

    private sealed record ApiEntry(string Id, string SpecUrl);
}
