#pragma warning disable CA2234 // Pass system uri objects instead of strings

using System.Reflection;
using OpenApiCodeGenerator;

return await RunAsync(args).ConfigureAwait(false);

static async Task<int> RunAsync(string[] args)
{
    if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
    {
        PrintUsage();
        return args.Length == 0 ? 1 : 0;
    }

    if (args.Contains("--version") || args.Contains("-v"))
    {
        Console.WriteLine(GetVersion());
        return 0;
    }

    // Parse arguments
    string? inputPath = null;
    string? outputPath = null;
    string namespaceName = "GeneratedModels";
    bool docComments = true;
    bool fileHeader = true;
    bool defaultNonNullable = true;
    bool addDefaultValuesToProperties = true;
    bool immutableArrays = true;
    bool immutableDictionaries = true;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--input" or "-i":
                inputPath = GetNextArg(args, ref i, "--input");
                break;
            case "--output" or "-o":
                outputPath = GetNextArg(args, ref i, "--output");
                break;
            case "--namespace" or "-n":
                namespaceName = GetNextArg(args, ref i, "--namespace");
                break;
            case "--no-doc-comments":
                docComments = false;
                break;
            case "--no-header":
                fileHeader = false;
                break;
            case "--no-default-non-nullable":
                defaultNonNullable = false;
                break;
            case "--no-add-default-values":
                addDefaultValuesToProperties = false;
                break;
            case "--mutable-arrays":
                immutableArrays = false;
                break;
            case "--mutable-dictionaries":
                immutableDictionaries = false;
                break;
            default:
                // Positional: first is input, second is output
                if (inputPath == null)
                {
                    inputPath = args[i];
                }
                else if (outputPath == null)
                {
                    outputPath = args[i];
                }
                else
                {
                    await Console.Error.WriteLineAsync($"Unknown argument: {args[i]}").ConfigureAwait(false);
                    return 1;
                }
                break;
        }
    }

    if (inputPath == null)
    {
        await Console.Error.WriteLineAsync("Error: No input file specified.").ConfigureAwait(false);
        await Console.Error.WriteLineAsync("Run 'openapi-codegen --help' for usage information.").ConfigureAwait(false);
        return 1;
    }

    // Support reading from URL
    Stream inputStream;
    HttpClient? httpClient = null;
    HttpResponseMessage? httpResponse = null;
    if (inputPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        inputPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenApiCodeGen/1.0");
        httpResponse = await httpClient.GetAsync(inputPath).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();
        inputStream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }
    else
    {
        if (!File.Exists(inputPath))
        {
            await Console.Error.WriteLineAsync($"Error: Input file not found: {inputPath}").ConfigureAwait(false);
            return 1;
        }
        using Stream _ = inputStream = File.OpenRead(inputPath);
    }

    try
    {
        var options = new GeneratorOptions
        {
            Namespace = namespaceName,
            GenerateDocComments = docComments,
            GenerateFileHeader = fileHeader,
            DefaultNonNullable = defaultNonNullable,
            UseImmutableArrays = immutableArrays,
            UseImmutableDictionaries = immutableDictionaries,
            AddDefaultValuesToProperties = addDefaultValuesToProperties,
        };

        var generator = new CSharpSchemaGenerator(options);
        string code = generator.GenerateFromStream(inputStream);

        if (outputPath != null)
        {
            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            await File.WriteAllTextAsync(outputPath, code).ConfigureAwait(false);
            Console.WriteLine($"Generated: {outputPath}");
        }
        else
        {
            Console.Write(code);
        }

        return 0;
    }
    catch (InvalidOperationException ex)
    {
        await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
        return 1;
    }
    finally
    {
        if (inputStream is not null)
        {
            await inputStream.DisposeAsync().ConfigureAwait(false);
        }
        httpResponse?.Dispose();
        httpClient?.Dispose();
    }
}

static string GetNextArg(string[] args, ref int index, string flag)
{
    if (index + 1 >= args.Length)
    {
        throw new InvalidOperationException($"Missing value for {flag}");
    }
    return args[++index];
}

static string GetVersion()
{
    Assembly assembly = typeof(CSharpSchemaGenerator).Assembly;
    Version? version = assembly.GetName().Version;
    return $"openapi-codegen {version?.ToString(3) ?? "1.0.0"}";
}

static void PrintUsage()
{
    Console.WriteLine($"""
        openapi-codegen - C# code generator for OpenAPI specifications

        USAGE:
            openapi-codegen [OPTIONS] <input> [output]
            openapi-codegen --input <file-or-url> --output <file>

        ARGUMENTS:
            <input>     Path or URL to an OpenAPI specification (JSON or YAML)
            [output]    Output file path (defaults to stdout)

        OPTIONS:
            -i, --input <path>          Input OpenAPI spec file or URL
            -o, --output <path>         Output C# file path
            -n, --namespace <name>      C# namespace (default: GeneratedModels)
                --no-doc-comments       Disable XML doc comment generation
                --no-header             Disable auto-generated file header
                --no-default-non-nullable  Don't treat defaults as non-nullable
                --no-add-default-values     Don't add default values from OpenAPI to properties
                --mutable-arrays        Use List<T> instead of IReadOnlyList<T>
                --mutable-dictionaries  Use Dictionary<K,V> instead of IReadOnlyDictionary<K,V>
            -v, --version               Show version information
            -h, --help                  Show this help message

        EXAMPLES:
            openapi-codegen petstore.yaml -o Models.cs
            openapi-codegen https://petstore3.swagger.io/api/v3/openapi.json -o PetStore.cs -n MyApp.Models
            openapi-codegen spec.yaml --mutable-arrays --mutable-dictionaries
        """);
}
