# Changelog

## [1.1.0] (2026-04-02)

### Added

- Support `--model-prefix` and `GeneratorOptions.ModelPrefix` to prefix every generated model type name
- Support generator option validation for invalid namespaces and model prefixes before generation starts
- Support `--inline-type-aliases` and `GeneratorOptions.InlinePrimitiveTypeAliases` to inline primitive aliases at usage sites
- Support `string`/`binary` component aliases as `Stream`, including generated JSON converters for wrapper aliases

### Changed

- Bumps Microsoft.OpenApi from 3.4.0 to 3.5.1
- Bumps Microsoft.OpenApi.YamlReader from 3.4.0 to 3.5.1

### Fixed

- Fixes default value handling for date-based properties
- Fixes stream alias detection during code generation

## [1.0.1] (2026-03-14)

### Changes

- Bumps Microsoft.OpenApi from 3.3.1 to 3.4.0
- Bumps Microsoft.OpenApi.YamlReader from 3.3.1 to 3.4.0

## [1.0.0] (2026-02-21)

### Added

- Initial release of OpenAPI Code Generator
- C# code generation from OpenAPI 3.x specifications (JSON and YAML)
- `record` type generation with init-only properties
- Enum generation with string-backed `[JsonStringEnumConverter]` support
- Type alias generation for simple schemas
- `allOf`, `oneOf`, and `anyOf` composition support
- Nullable reference type support with `#nullable enable`
- `System.Text.Json` serialization attributes (`[JsonPropertyName]`, `[JsonConverter]`)
- Immutable collection types (`IReadOnlyList<T>`, `IReadOnlyDictionary<string, T>`)
- XML documentation comment generation from OpenAPI descriptions
- Auto-generated file header
- CLI tool (`openapi-codegen`) installable via `dotnet tool install`
- URL input support for remote OpenAPI specifications
- Configurable generation options (namespace, enums, nullable, collections, etc.)

[1.1.0]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.1.0
[1.0.1]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.0.1
[1.0.0]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.0.0
