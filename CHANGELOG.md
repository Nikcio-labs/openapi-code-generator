# Changelog

## [1.6.1](https://github.com/Nikcio-labs/openapi-code-generator/compare/v1.6.0...v1.6.1) (2026-09-05)


### Fixed

* reuse referenced enum component types ([#189](https://github.com/Nikcio-labs/openapi-code-generator/issues/189)) ([b251aee](https://github.com/Nikcio-labs/openapi-code-generator/commit/b251aeee8eae1051317344cebaa43512ae3af5f8))

## [1.6.0](https://github.com/Nikcio-labs/openapi-code-generator/compare/v1.5.0...v1.6.0) (2026-09-03)


### Added

* add browser playground app to the docs site ([#180](https://github.com/Nikcio-labs/openapi-code-generator/issues/180)) ([f022f6b](https://github.com/Nikcio-labs/openapi-code-generator/commit/f022f6b156160a201418da80da61f75c7eac5901))
* enable Native AOT compatibility ([#179](https://github.com/Nikcio-labs/openapi-code-generator/issues/179)) ([6fa7e8a](https://github.com/Nikcio-labs/openapi-code-generator/commit/6fa7e8aa99366741d4281249985ded76839df207))


### Fixed

* guard null discriminator mapping references ([#173](https://github.com/Nikcio-labs/openapi-code-generator/issues/173)) ([3716994](https://github.com/Nikcio-labs/openapi-code-generator/commit/3716994639d43d2c4be74cac0188f11f53b837fb))
* handle missing values for CLI options ([#170](https://github.com/Nikcio-labs/openapi-code-generator/issues/170)) ([34b81fc](https://github.com/Nikcio-labs/openapi-code-generator/commit/34b81fca5a82e97d2a6591f994a1bbca70f663d7))
* throw InvalidOperationException when OpenAPI document fails to parse ([#169](https://github.com/Nikcio-labs/openapi-code-generator/issues/169)) ([360967d](https://github.com/Nikcio-labs/openapi-code-generator/commit/360967dfbc0b9fd91f3c769bcce39948b2a2a292))


### Changed

* remove dead branch in ResolveAllOf ([#175](https://github.com/Nikcio-labs/openapi-code-generator/issues/175)) ([210dc37](https://github.com/Nikcio-labs/openapi-code-generator/commit/210dc3754922de053266b7f80553474a4629db63))
* reuse ExtractEnumValues in EmitEnum ([#174](https://github.com/Nikcio-labs/openapi-code-generator/issues/174)) ([bf8f36b](https://github.com/Nikcio-labs/openapi-code-generator/commit/bf8f36b60282088473c8458124961b3f41352468))

## [1.5.0](https://github.com/Nikcio-labs/openapi-code-generator/compare/v1.4.0...v1.5.0) (2026-08-26)


### Added

* Emit [Obsolete] attribute on deprecated schemas and properties ([#144](https://github.com/Nikcio-labs/openapi-code-generator/issues/144)) ([1e25d83](https://github.com/Nikcio-labs/openapi-code-generator/commit/1e25d832855714196fa04ffc6fb65616ed8e067d))
* Emit validation attributes from OpenAPI constraints ([#145](https://github.com/Nikcio-labs/openapi-code-generator/issues/145)) ([cf0082d](https://github.com/Nikcio-labs/openapi-code-generator/commit/cf0082dcb3c829274ed6abd8a9de30ce5b86c823))

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
