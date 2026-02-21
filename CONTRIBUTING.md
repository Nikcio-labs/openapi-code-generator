# Contributing to OpenAPI Code Generator

Thank you for your interest in contributing to OpenAPI Code Generator! This document provides guidelines and information for contributors.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for everyone.

## How to Contribute

### Reporting Bugs

If you find a bug, please open an issue with:

- A clear, descriptive title
- Steps to reproduce the issue
- The OpenAPI specification (or a minimal example) that triggers the bug
- Expected vs. actual behavior
- Your environment (OS, .NET version)

### Suggesting Features

Feature requests are welcome! Please open an issue describing:

- The problem you're trying to solve
- Your proposed solution
- Any alternatives you've considered

### Pull Requests

1. **Fork** the repository and create your branch from `main`
2. **Install prerequisites**: .NET 10 SDK
3. **Build** the solution: `dotnet build`
4. **Run tests**: `dotnet test`
5. **Make your changes** with clear, descriptive commits
6. **Add tests** for any new functionality
7. **Ensure all tests pass** before submitting
8. **Open a pull request** with a clear description of your changes

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Building

```bash
# Clone the repository
git clone https://github.com/Nikcio-labs/openapi-code-generator.git
cd openapi-code-generator

# Build the solution
dotnet build

# Run all tests
dotnet test

# Run the CLI tool locally
dotnet run --project src/OpenApiCodeGenerator.Cli -- petstore.yaml -o output.cs
```

### Running Examples

The examples project generates C# code from popular public OpenAPI specifications:

```bash
dotnet run --project examples/OpenApiCodeGenerator.Examples
```

## Release Process

Releases are published to NuGet as `Nikcio.OpenApiCodeGen`. Version numbers follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes to CLI or library API
- **MINOR**: New features, backward-compatible
- **PATCH**: Bug fixes, backward-compatible

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
