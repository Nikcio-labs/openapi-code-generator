---
title: Installation
description: How to install OpenAPI Code Generator as a .NET tool.
---

OpenAPI Code Generator is distributed as a .NET tool via NuGet under the package name `Nikcio.OpenApiCodeGen`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Global Tool

Install as a global tool available anywhere on your system:

```bash
dotnet tool install --global Nikcio.OpenApiCodeGen
```

After installation, the `openapi-codegen` command is available globally:

```bash
openapi-codegen --version
```

### Updating

```bash
dotnet tool update --global Nikcio.OpenApiCodeGen
```

### Uninstalling

```bash
dotnet tool uninstall --global Nikcio.OpenApiCodeGen
```

## Local Tool

Install as a local tool scoped to your repository. This is recommended for team projects to ensure consistent versions:

```bash
# Create a tool manifest (if you don't have one)
dotnet new tool-manifest

# Install the tool
dotnet tool install Nikcio.OpenApiCodeGen
```

When installed locally, run the tool with `dotnet tool run`:

```bash
dotnet tool run openapi-codegen petstore.yaml -o Models.cs
```

Or restore and use directly after `dotnet tool restore`:

```bash
dotnet tool restore
openapi-codegen petstore.yaml -o Models.cs
```
