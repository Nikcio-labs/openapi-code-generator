---
title: Introduction
description: What is OpenAPI Code Generator and why should you use it?
---

**OpenAPI Code Generator** is a .NET tool that transforms OpenAPI 3.x specifications into clean, modern C# source code. It generates `record` types, enums, and type aliases — ready to use with `System.Text.Json` serialization out of the box.

## The Problem

When working with REST APIs described by OpenAPI specifications, you need C# types that match the API's data contracts. Manually creating and maintaining these models is tedious, error-prone, and a waste of developer time — especially for large APIs with hundreds of schemas.

## The Solution

OpenAPI Code Generator reads your OpenAPI spec and produces idiomatic C# code automatically:

- **Records** with `init`-only properties for API models
- **Enums** with string-backed JSON serialization
- **Type aliases** for simple wrapper types
- **Nullable reference types** for optional fields
- **XML doc comments** from OpenAPI descriptions

## Key Features

### Modern C# Patterns
Generated code uses `record` types, `required` properties, `init` accessors, and nullable reference types — following current C# best practices.

### Zero Runtime Dependencies
The generated code only depends on `System.Text.Json`, which is built into .NET. No additional NuGet packages needed in your consuming project.

### Flexible Usage
Use it as a CLI tool for quick one-off generation, or reference the core library for integration into your own build pipeline or tooling.

## How It Works

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  OpenAPI Spec   │────▶│  Code Generator  │────▶│   C# Source     │
│  (JSON / YAML)  │     │                  │     │   (.cs file)    │
└─────────────────┘     └──────────────────┘     └─────────────────┘
```

1. **Parse** — The OpenAPI specification is parsed using the official Microsoft OpenAPI library
2. **Resolve** — Schema types are resolved to C# types, handling `$ref`, composition (`allOf`/`oneOf`/`anyOf`), and primitives
3. **Emit** — Clean C# source code is generated with proper naming, formatting, and serialization attributes

## Next Steps

- [Install the tool](../../getting-started/installation/) to get started
- [Quick Start guide](../../getting-started/quick-start/) for your first code generation
- [Configuration options](../../guides/configuration/) to customize the output
