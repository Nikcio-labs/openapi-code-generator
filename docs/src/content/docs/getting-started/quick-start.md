---
title: Quick Start
description: Generate your first C# models from an OpenAPI specification in minutes.
---

This guide walks you through generating C# code from an OpenAPI specification in just a few steps.

## 1. Install the tool

```bash
dotnet tool install --global Nikcio.OpenApiCodeGen
```

## 2. Get an OpenAPI specification

You can use any OpenAPI 3.x specification. For this example, we'll use the Petstore sample spec:

```bash
# Download the Petstore spec
curl -o petstore.json https://petstore3.swagger.io/api/v3/openapi.json
```

Or you can skip downloading and generate directly from the URL (see step 3).

## 3. Generate C# code

From a local file:

```bash
openapi-codegen petstore.json -o PetStore.cs -n MyApp.Models
```

Or directly from a URL:

```bash
openapi-codegen https://petstore3.swagger.io/api/v3/openapi.json -o PetStore.cs -n MyApp.Models
```

## 4. Use the generated code

The generated file contains ready-to-use C# records. Add it to your project and start using the types:

```csharp
using System.Net.Http.Json;
using MyApp.Models;

var client = new HttpClient { BaseAddress = new Uri("https://petstore3.swagger.io/api/v3/") };

// The generated Pet record works directly with System.Text.Json
Pet? pet = await client.GetFromJsonAsync<Pet>("pet/1");

Console.WriteLine($"Pet: {pet?.Name} (Status: {pet?.Status})");
```

## 5. Explore the options

Customize the output with CLI flags:

```bash
# Use mutable collections instead of IReadOnlyList<T>
openapi-codegen spec.yaml -o Models.cs --mutable-arrays

# Disable doc comments for a cleaner output
openapi-codegen spec.yaml -o Models.cs --no-doc-comments
```

## What's Next?

- [CLI Usage](../../guides/cli-usage/) — Learn all the CLI commands and patterns
- [Configuration](../../guides/configuration/) — Understand all available options
