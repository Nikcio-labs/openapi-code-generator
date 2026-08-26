# Changelog

## [1.4.1](https://github.com/Nikcio-labs/openapi-code-generator/compare/v1.4.0...v1.4.1) (2026-08-26)


### Fixed

* **ci:** Point version-file to manifest to avoid creating version.txt ([20dbc49](https://github.com/Nikcio-labs/openapi-code-generator/commit/20dbc499954d578b2edc8675ee95d6cf63480c1e))
* **ci:** Remove version-file override, rely on extra-files for csproj ([8a21436](https://github.com/Nikcio-labs/openapi-code-generator/commit/8a2143628c9e931b09819212e33f87a1b0dbe479))
* **ci:** Use generic annotation updater for csproj version ([8d3cf3b](https://github.com/Nikcio-labs/openapi-code-generator/commit/8d3cf3bbf7c129c223db385a301b6ad243e2f048))
* **ci:** Use xml xpath updater for csproj version instead of version-file ([dabe4be](https://github.com/Nikcio-labs/openapi-code-generator/commit/dabe4be65af316bfd4fa3f286e9f6a1800f2e292))

## [1.4.0] (2026-08-26)

### Added

- Hoist inline object schemas into named C# records (nested objects, `allOf` compositions, and `oneOf`/`anyOf` unions of `$ref`s)
- Empty `type: object` schemas now resolve to `IReadOnlyDictionary<string, object?>` instead of `object` (per OpenAPI 3.0 where `additionalProperties` defaults to `true`)

## [1.3.0] (2026-08-23)

### Added

- Emit records and aliases as `partial` to support extending generated types

### Changed

- Use `JsonStringEnumConverter<>` instead of `JsonStringEnumConverter` in generated code
- Bumps Microsoft.OpenApi from 3.5.2 to 3.10.2
- Bumps Microsoft.OpenApi.YamlReader from 3.5.2 to 3.10.2
- Migrate test projects to Microsoft Testing Platform (MTP) mode
- Bumps xunit.v3 from 3.2.2 to 4.0.0

### Fixed

- Filter `JsonNullSentinel` values from nullable enum emission (introduced by Microsoft.OpenApi 3.8.0)

## [1.2.0] (2026-04-20)

### Added

- Support `--include-schema` and `GeneratorOptions.IncludeSchemas` to generate only selected component schemas and their referenced dependencies
- Support `--omit-json-attributes` and `GeneratorOptions.OmitJsonPropertyNameAttributes` to omit generated `[JsonPropertyName]` attributes when serializer naming policies are preferred
- Support `string`/`binary` schema properties as `Stream`, including generated JSON converters for direct property serialization

### Changed

- Bumps Microsoft.OpenApi from 3.5.1 to 3.5.2
- Bumps Microsoft.OpenApi.YamlReader from 3.5.1 to 3.5.2

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

[1.4.0]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.4.0
[1.3.0]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.3.0
[1.2.0]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.2.0
[1.1.0]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.1.0
[1.0.1]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.0.1
[1.0.0]: https://github.com/Nikcio-labs/openapi-code-generator/releases/tag/v1.0.0
