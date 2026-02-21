---
title: CLI Usage
description: How to use the openapi-codegen command-line tool.
---

The `openapi-codegen` CLI is the primary way to generate C# code from OpenAPI specifications. This guide covers common usage patterns and workflows.

## Basic Usage

The simplest usage takes an input spec and writes the generated code to a file:

```bash
openapi-codegen <input> -o <output>
```

### From a Local File

```bash
openapi-codegen petstore.yaml -o Models.cs
```

### From a URL

The tool can fetch specs directly from URLs — useful for always generating from the latest version:

```bash
openapi-codegen https://petstore3.swagger.io/api/v3/openapi.json -o PetStore.cs
```

### To stdout

Omit the output flag to write to stdout. This is useful for piping or previewing:

```bash
openapi-codegen spec.yaml
```

## Setting the Namespace

By default, generated code uses the `GeneratedModels` namespace. Override it with `-n`:

```bash
openapi-codegen spec.yaml -o Models.cs -n MyApp.Api.Models
```

## Disabling Features

Toggle individual features off with `--no-*` flags:

```bash
# No XML doc comments
openapi-codegen spec.yaml -o Models.cs --no-doc-comments

# No auto-generated file header
openapi-codegen spec.yaml -o Models.cs --no-header
```

Combine multiple flags:

```bash
openapi-codegen spec.yaml -o Models.cs --no-doc-comments --no-header
```

## Mutable Collections

By default, arrays and dictionaries use immutable interfaces. Use mutable types instead:

```bash
# List<T> instead of IReadOnlyList<T>
openapi-codegen spec.yaml -o Models.cs --mutable-arrays

# Dictionary<string, T> instead of IReadOnlyDictionary<string, T>
openapi-codegen spec.yaml -o Models.cs --mutable-dictionaries
```

## Build Integration

### MSBuild Target

Add code generation as a pre-build step in your `.csproj`:

```xml
<Target Name="GenerateApiModels" BeforeTargets="BeforeBuild">
  <Exec Command="openapi-codegen $(ProjectDir)specs/api.yaml -o $(ProjectDir)Generated/Models.cs -n $(RootNamespace).Models" />
</Target>
```

### CI/CD Pipeline

Example for GitHub Actions:

```yaml
- name: Install OpenAPI Code Generator
  run: dotnet tool install --global Nikcio.OpenApiCodeGen

- name: Generate API Models
  run: openapi-codegen specs/api.yaml -o src/Models.cs -n MyApp.Models
```

## Error Handling

The tool returns exit code `0` on success and `1` on failure. Error messages are written to stderr:

```bash
openapi-codegen nonexistent.yaml -o Models.cs
# Error: Input file not found: nonexistent.yaml
```

If the OpenAPI spec has parsing errors but still contains valid schemas, the tool will generate code from the available schemas and proceed.

## Full Reference

See the [CLI Reference](../../reference/cli/) for a complete list of all flags and options.
