using System.Reflection;
using System.Runtime.Loader;

namespace OpenApiCodeGenerator.Benchmarks;

/// <summary>
/// Used to activate the latest released version of the generator (from the "Released" subdirectory) in a separate <see cref="AssemblyLoadContext"/> for side-by-side comparison against the current in-tree version.
/// </summary>
internal sealed class ReleasedGeneratorProxy
{
    private readonly object _generator;
    private readonly MethodInfo _generateFromText;

    public ReleasedGeneratorProxy()
    {
        string releasedDir = Path.Combine(Path.GetDirectoryName(typeof(ComparisonBenchmarks).Assembly.Location)!, "Released");

        Console.WriteLine($"Released version loaded from: {releasedDir}");
        Console.WriteLine($"Released version: {GetReleasedVersion(releasedDir)}");

        var loadContext = new ReleasedGeneratorLoadContext(releasedDir);

        string coreAssemblyPath = Path.Combine(releasedDir, "OpenApiCodeGenerator.dll");
        Assembly coreAssembly = loadContext.LoadFromAssemblyPath(coreAssemblyPath);

        Type generatorType = coreAssembly.GetType("OpenApiCodeGenerator.CSharpSchemaGenerator")
            ?? throw new InvalidOperationException("CSharpSchemaGenerator type not found in released assembly.");

        _generator = Activator.CreateInstance(generatorType)
                ?? throw new InvalidOperationException("Could not create CSharpSchemaGenerator instance.");

        // Cache GenerateFromText(string) method so per-iteration cost is minimal.
        _generateFromText = generatorType.GetMethod("GenerateFromText", [typeof(string)])
            ?? throw new InvalidOperationException("GenerateFromText(string) method not found in released assembly.");
    }

    /// <summary>
    /// Invokes <see cref="CSharpSchemaGenerator.GenerateFromText(string)"/>
    /// </summary>
    public string GenerateFromText(string openApiText)
    {
        return (string) _generateFromText.Invoke(_generator, [openApiText])!;
    }

    public static string GetReleasedVersion(string releasedDir)
    {
        string path = Path.Combine(releasedDir, "OpenApiCodeGenerator.dll");
        if (!File.Exists(path))
        {
            return "not found";
        }

        return AssemblyName.GetAssemblyName(path).Version?.ToString() ?? "unknown";
    }

    private sealed class ReleasedGeneratorLoadContext : AssemblyLoadContext
    {
        private readonly string _releasedDir;

        public ReleasedGeneratorLoadContext(string releasedDir) : base(name: "ReleasedGenerator", isCollectible: false)
        {
            _releasedDir = releasedDir;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string candidate = Path.Combine(_releasedDir, assemblyName.Name + ".dll");
            if (File.Exists(candidate))
            {
                return LoadFromAssemblyPath(candidate);
            }

            return null;
        }
    }

}
