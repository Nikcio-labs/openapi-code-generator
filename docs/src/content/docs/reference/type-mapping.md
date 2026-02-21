---
title: Type Mapping
description: How OpenAPI types are mapped to C# types.
---

This reference documents how the code generator maps OpenAPI schema types to C# types.

## Primitive Types

### String

| OpenAPI Type | Format | C# Type |
|-------------|--------|---------|
| `string` | _(none)_ | `string` |
| `string` | `date-time` | `DateTimeOffset` |
| `string` | `date` | `DateOnly` |
| `string` | `time` | `TimeOnly` |
| `string` | `duration` | `TimeSpan` |
| `string` | `uuid` | `Guid` |
| `string` | `uri` | `Uri` |
| `string` | `byte` | `byte[]` |
| `string` | `binary` | `Stream` |

### Integer

| OpenAPI Type | Format | C# Type |
|-------------|--------|---------|
| `integer` | _(none)_ | `int` |
| `integer` | `int32` | `int` |
| `integer` | `int64` | `long` |

### Number

| OpenAPI Type | Format | C# Type |
|-------------|--------|---------|
| `number` | _(none)_ | `double` |
| `number` | `float` | `float` |
| `number` | `double` | `double` |
| `number` | `decimal` | `decimal` |

### Boolean

| OpenAPI Type | C# Type |
|-------------|---------|
| `boolean` | `bool` |

## Complex Types

### Object

Objects with named properties are generated as C# `record` types:

```yaml
# Input
MyModel:
  type: object
  properties:
    name:
      type: string
```

```csharp
// Output
public record MyModel
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
```

### Array

Array types are mapped to generic collections:

| Configuration | C# Type |
|--------------|---------|
| `UseImmutableArrays = true` (default) | `IReadOnlyList<T>` |
| `UseImmutableArrays = false` | `List<T>` |

```yaml
# Input
tags:
  type: array
  items:
    type: string
```

```csharp
// Output (default)
public IReadOnlyList<string>? Tags { get; init; }

// Output (mutable)
public List<string>? Tags { get; init; }
```

### Enum

String enums are generated as C# `enum` types:

```yaml
# Input
Status:
  type: string
  enum: [active, inactive, pending]
```

```csharp
// Output (StringEnums = true)
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Status
{
    [JsonStringEnumMemberName("active")]
    Active,
    [JsonStringEnumMemberName("inactive")]
    Inactive,
    [JsonStringEnumMemberName("pending")]
    Pending,
}
```

## Composition

### allOf

`allOf` schemas are flattened into a single record with all properties merged. If a `$ref` is included, properties from the referenced schema are included:

```yaml
# Input
Animal:
  type: object
  properties:
    name:
      type: string
Dog:
  allOf:
    - $ref: '#/components/schemas/Animal'
    - type: object
      properties:
        breed:
          type: string
```

```csharp
// Output
public record Dog
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("breed")]
    public string? Breed { get; init; }
}
```

### oneOf / anyOf

`oneOf` and `anyOf` schemas generate a record that can hold any of the variant types. Since C# doesn't have native union types, these are represented as records with the combined properties or as `object` fallbacks.

## Nullability Rules

Properties are made nullable (`T?`) when:

1. The property is **not** listed in `required`
2. The schema explicitly includes `nullable: true`
3. The schema type includes `null` in the type array

Properties are **non-nullable** (or use `required`) when:

1. Listed in the `required` array
2. Have a `default` value (when `DefaultNonNullable = true`)

## $ref Resolution

Schema references (`$ref: '#/components/schemas/MyType'`) are resolved to the C# type name of the referenced schema. The reference ID is converted to PascalCase:

| OpenAPI Reference | C# Type |
|------------------|---------|
| `$ref: '#/components/schemas/User'` | `User` |
| `$ref: '#/components/schemas/pet-status'` | `PetStatus` |
| `$ref: '#/components/schemas/API_Response'` | `ApiResponse` |

## Name Conversion

Schema names and property names are converted to valid C# identifiers:

- **Type names:** PascalCase (`user-profile` → `UserProfile`)
- **Property names:** PascalCase (`first_name` → `FirstName`)
- **Enum members:** PascalCase (`in_progress` → `InProgress`)
- **C# keywords** are escaped (`class` → `@Class`)
- **Digit-leading names** are prefixed with `_` (`123abc` → `_123abc`)
