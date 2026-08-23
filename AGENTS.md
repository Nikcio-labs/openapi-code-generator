# OpenAPI Code Generator
Transforms OpenAPI JSON/YAML into C#

## Commands

| What | Command |
|------|---------|
| Run CLI | `dotnet run --project src/OpenApiCodeGenerator.Cli -- petstore.yaml -o output.cs` |
| Run examples | `dotnet run --project examples/OpenApiCodeGenerator.Examples` |
| Benchmarks | `dotnet run -c Release --project benchmarks/OpenApiCodeGenerator.Benchmarks -- --filter *Comparison*` |

## Documentation

Use based on your task:

- **[Versioning](agent-guidance/versioning.md)** — Semantic versioning policy, changelog format, release process. Read when preparing a release or updating the changelog.
- **[Commit Messages](agent-guidance/commit-messages.md)** — Conventional Commits style, types, scopes, examples. Read when writing commit messages.

## Key Constraints

- NuGet package versions are centralized in `Directory.Packages.props` — never set versions in `.csproj` files
- Generated code must compile with zero runtime dependencies beyond `System.Text.Json`
