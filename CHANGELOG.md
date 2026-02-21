# Changelog

## [1.0.0]

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

[1.0.0]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.0.0
