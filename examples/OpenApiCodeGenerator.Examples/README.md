# OpenAPI Code Generator — Examples

This project downloads public OpenAPI specifications and generates C# code
from their schemas, letting you preview what the generator produces for real-world APIs.

## Included Specifications

| Name | Source |
|------|--------|
| **GitHub API** | [api.github.com.yaml](https://raw.githubusercontent.com/github/rest-api-description/main/descriptions/api.github.com/api.github.com.yaml) |
| **GitHub API (next)** | [api.github.com.yaml](https://raw.githubusercontent.com/github/rest-api-description/main/descriptions-next/api.github.com/api.github.com.yaml) |
| **Octokit GHES 3.6 diff** | [ghes-3.6-diff-to-api.github.com.json](https://raw.githubusercontent.com/octokit/octokit-next.js/main/cache/types-openapi/ghes-3.6-diff-to-api.github.com.json) |
| **Stripe API** | [spec3.yaml](https://raw.githubusercontent.com/stripe/openapi/master/openapi/spec3.yaml) |
| **Petstore API** | [petstore.yaml](https://petstore3.swagger.io/api/v3/openapi.json) |

## Running

```bash
cd examples/OpenApiCodeGenerator.Examples
dotnet run
```

Generated `.cs` files are written to the `output/` directory alongside the project.

## Adding More Specs

Edit `Program.cs` and add entries to the `specifications` dictionary:

```csharp
var specifications = new Dictionary<string, string>
{
    ["my-api"] = "https://example.com/openapi.yaml",
    // ...
};
```
