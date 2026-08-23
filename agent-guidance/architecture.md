# Architecture

## Generation Pipeline

```
CSharpSchemaGenerator → TypeResolver → CSharpCodeEmitter
                                     ↗
                        NameHelper (utility)
```

**CSharpSchemaGenerator** (`src/OpenApiCodeGenerator/CSharpSchemaGenerator.cs`) — entry point. Reads an OpenAPI spec from file path, stream, text, or `OpenApiDocument`. Selects which schemas to generate.

**TypeResolver** (`src/OpenApiCodeGenerator/TypeResolver.cs`) — resolves OpenAPI schema types to C# type strings. Handles `$ref` resolution, primitive type mapping, arrays → `IReadOnlyList<T>`, dictionaries / `additionalProperties` → `Dictionary<string, T>`, nullable reference types, `allOf` / `oneOf` / `anyOf` composition, and enum detection.

**CSharpCodeEmitter** (`src/OpenApiCodeGenerator/CSharpCodeEmitter.cs`) — Produces C# source via `StringBuilder`. Emits records, enums, type aliases, discriminated unions, custom JSON converters, and handles two project-specific algorithms:

- **Inline enum deduplication**: matching inline enums across schemas are emitted once; conflicting ones get schema-prefixed names.
- **Name collision resolution**: two-pass — the most "natural" name keeps clean PascalCase; others get meaningfully differentiated names by expanding special chars to words ("Underscore", "Dot") rather than numeric suffixes.

**NameHelper** (`src/OpenApiCodeGenerator/NameHelper.cs`) — converts OpenAPI names to valid C# identifiers: PascalCase conversion, keyword escaping, collision differentiation, enum member naming.

**CLI** (`src/OpenApiCodeGenerator.Cli/Program.cs`) — top-level-statements entry point: argument parsing, file/URL input, generation, output to file or stdout. Published as `Nikcio.OpenApiCodeGen` dotnet tool (`openapi-codegen`).

## Design Decisions

- **Two-layer design**: core library is separate from CLI, enabling programmatic usage without the CLI overhead.
