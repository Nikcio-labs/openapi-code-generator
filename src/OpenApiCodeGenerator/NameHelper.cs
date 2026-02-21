using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenApiCodeGenerator;

/// <summary>
/// Utilities for converting OpenAPI schema names into valid C# identifiers.
/// </summary>
internal static partial class NameHelper
{
    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate",
        "do", "double", "else", "enum", "event", "explicit", "extern", "false",
        "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
        "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private",
        "protected", "public", "readonly", "record", "ref", "return", "sbyte",
        "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
        "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong",
        "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    ];

    /// <summary>
    /// Convert an OpenAPI schema name to a valid C# type name (PascalCase).
    /// </summary>
    public static string ToTypeName(string? schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return "UnknownType";
        }

        // Handle $ref-style paths like "#/components/schemas/MyType"
        if (schemaName.Contains('/', StringComparison.Ordinal))
        {
            schemaName = schemaName.Split('/').Last();
        }

        string result = ToPascalCase(schemaName);
        result = InvalidIdentifierCharsRegex().Replace(result, "");

        if (result.Length == 0)
        {
            return "UnknownType";
        }

        // Ensure starts with letter or underscore
        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return EscapeKeyword(result);
    }

    /// <summary>
    /// Convert an OpenAPI property name to a valid C# property name (PascalCase).
    /// </summary>
    /// <param name="propertyName">The original property name from the OpenAPI spec.</param>
    /// <param name="enclosingTypeName">The C# type name of the enclosing record/class, used to detect CS0542 collisions.</param>
    public static string ToPropertyName(string propertyName, string? enclosingTypeName = null)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "Unknown";
        }

        string result = ToPascalCase(propertyName);
        result = InvalidIdentifierCharsRegex().Replace(result, "");

        if (result.Length == 0)
        {
            return "Unknown";
        }

        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        result = EscapeKeyword(result);

        // CS0542: member names cannot be the same as their enclosing type
        if (enclosingTypeName != null && result == enclosingTypeName)
        {
            result += "Value";
        }

        return result;
    }

    /// <summary>
    /// Convert an OpenAPI enum value to a valid C# enum member name.
    /// </summary>
    public static string ToEnumMemberName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        string result = ToPascalCase(value);
        result = InvalidIdentifierCharsRegex().Replace(result, "");

        if (result.Length == 0)
        {
            return "Unknown";
        }

        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return EscapeKeyword(result);
    }

    /// <summary>
    /// Returns the original name if the property name differs after PascalCase conversion.
    /// Returns null if no JSON attribute is needed.
    /// </summary>
    public static string? GetJsonPropertyName(string originalName, string propertyName)
    {
        // Always return the original name so JSON serialization uses the original casing
        return originalName;
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Intended")]
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Replace special characters with meaningful words before splitting.
        // '+' is always replaced (never used as a word separator).
        // '-' is only replaced when at the start or before a digit (negative sign, not hyphen separator).
        input = input.Replace("+", "Plus", StringComparison.Ordinal);
        input = LeadingMinusRegex().Replace(input, "Minus");

        // Split on common separators: -, _, ., space, camelCase boundaries
        string[] parts = SplitWordsRegex().Split(input)
            .Where(p => p.Length > 0)
            .ToArray();

        if (parts.Length == 0)
        {
            return input;
        }

        return string.Concat(parts.Select(p =>
        {
            // If the entire part is uppercase (e.g. "USER" from "USER_STATUS"),
            // lowercase the tail so it becomes "User" instead of "USER".
            // Mixed-case parts like "APIResponse" are left as-is so acronyms
            // at camelCase boundaries are preserved (→ "MyAPIResponse").
            bool allUpper = p.All(char.IsUpper);
            string tail = allUpper ? p[1..].ToLowerInvariant() : p[1..];
            return char.ToUpperInvariant(p[0]) + tail;
        }));
    }

    /// <summary>
    /// Produces a differentiated C# name from an original name whose PascalCase form
    /// collides with another. Instead of a numeric suffix, expands special characters
    /// (leading underscores, dots, dashes, etc.) into meaningful words like
    /// "Underscore", "Dot", "Dash" so that <c>_id</c> becomes <c>UnderscoreId</c>
    /// rather than <c>Id2</c>.
    /// </summary>
    /// <param name="originalName">The raw property/schema name from the OpenAPI spec.</param>
    /// <param name="basePascalName">The PascalCase name that collided.</param>
    /// <param name="existingNames">Already-used C# names; the result will not be in this set.</param>
    /// <returns>A unique, meaningful name that is NOT yet added to <paramref name="existingNames"/>.</returns>
    public static string ToDifferentiatedName(string originalName, string basePascalName, HashSet<string> existingNames)
    {
        // Step 1: Try expanding special characters at the beginning of the original name
        string? expanded = ExpandSpecialPrefixes(originalName);
        if (expanded != null)
        {
            string candidate = ToPascalCase(expanded);
            candidate = InvalidIdentifierCharsRegex().Replace(candidate, "");
            if (candidate.Length > 0 && !existingNames.Contains(candidate) && candidate != basePascalName)
            {
                return EscapeKeyword(candidate);
            }
        }

        // Step 2: Try expanding all special characters in the name
        string fullyExpanded = ExpandAllSpecialCharacters(originalName);
        if (fullyExpanded != originalName)
        {
            string candidate = ToPascalCase(fullyExpanded);
            candidate = InvalidIdentifierCharsRegex().Replace(candidate, "");
            if (candidate.Length > 0 && !existingNames.Contains(candidate) && candidate != basePascalName)
            {
                return EscapeKeyword(candidate);
            }
        }

        // Step 3: Try preserving separator style as suffix (e.g., "IdSnakeCase", "IdCamelCase")
        string? styleSuffix = DetectNamingStyleSuffix(originalName);
        if (styleSuffix != null)
        {
            string candidate = basePascalName + styleSuffix;
            if (!existingNames.Contains(candidate))
            {
                return candidate;
            }
        }

        // Step 4: Numeric fallback — should be very rare
        int suffix = 2;
        string fallback;
        do
        {
            fallback = basePascalName + suffix;
            suffix++;
        } while (existingNames.Contains(fallback));

        return fallback;
    }

    /// <summary>
    /// Computes a score for how "natural" an original name maps to its PascalCase form.
    /// Lower scores mean the name is more natural. The most natural name in a collision
    /// group should keep the clean PascalCase name; others get differentiated.
    /// </summary>
    public static int NaturalnessScore(string originalName, string pascalName)
    {
        // Exact match is most natural (already PascalCase)
        if (originalName == pascalName)
        {
            return 0;
        }

        // Only casing difference (e.g., "id" → "Id")
        if (string.Equals(originalName, pascalName, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        int specialCount = originalName.Count(c => !char.IsLetterOrDigit(c));
        if (specialCount > 0)
        {
            return 10 + specialCount;
        }

        // Other transformations (camelCase → PascalCase, etc.)
        return 2;
    }

    /// <summary>
    /// Replaces leading special characters with descriptive words.
    /// Returns null if the original has no special prefix to expand.
    /// </summary>
    private static string? ExpandSpecialPrefixes(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var sb = new StringBuilder();
        int i = 0;
        bool anyExpanded = false;

        // Expand leading special characters
        while (i < name.Length && !char.IsLetterOrDigit(name[i]))
        {
            string? word = CharToWord(name[i]);
            if (word != null)
            {
                sb.Append(word).Append(' ');
                anyExpanded = true;
            }
            i++;
        }

        if (!anyExpanded)
        {
            return null;
        }

        sb.Append(name[i..]);
        return sb.ToString();
    }

    /// <summary>
    /// Replaces all special characters in the name with descriptive words,
    /// surrounded by spaces so PascalCase conversion produces proper word boundaries.
    /// </summary>
    private static string ExpandAllSpecialCharacters(string name)
    {
        var sb = new StringBuilder();
        foreach (char ch in name)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else
            {
                string? word = CharToWord(ch);
                if (word != null)
                {
                    sb.Append(' ').Append(word).Append(' ');
                }
                else
                {
                    sb.Append(' '); // use space as separator for unknown chars
                }
            }
        }
        return sb.ToString();
    }

    private static string? CharToWord(char ch)
    {
        return ch switch
        {
            '_' => "Underscore",
            '-' => "Dash",
            '.' => "Dot",
            '@' => "At",
            '#' => "Hash",
            '$' => "Dollar",
            '%' => "Percent",
            '&' => "And",
            '+' => "Plus",
            '~' => "Tilde",
            '!' => "Bang",
            '*' => "Star",
            '/' => "Slash",
            '\\' => "Backslash",
            ':' => "Colon",
            '^' => "Caret",
            '|' => "Pipe",
            _ => null
        };
    }

    /// <summary>
    /// Detects the naming convention of the original name and returns a suffix.
    /// </summary>
    private static string? DetectNamingStyleSuffix(string name)
    {
        if (name.Contains('_', StringComparison.Ordinal))
        {
            return "SnakeCase";
        }

        if (name.Contains('-', StringComparison.Ordinal))
        {
            return "KebabCase";
        }

        if (name.Contains('.', StringComparison.Ordinal))
        {
            return "DotNotation";
        }

        if (name.Length > 0 && char.IsLower(name[0]) && name.Any(char.IsUpper))
        {
            return "CamelCase";
        }

        if (name.Length > 0 && char.IsUpper(name[0]) && name.Any(char.IsLower))
        {
            return "PascalCase";
        }

        if (name.Length > 0 && name.All(c => char.IsLower(c) || !char.IsLetter(c)))
        {
            return "Lowercase";
        }

        if (name.Length > 0 && name.All(c => char.IsUpper(c) || !char.IsLetter(c)))
        {
            return "Uppercase";
        }

        return null;
    }

    private static string EscapeKeyword(string name)
    {
        return CSharpKeywords.Contains(name) ? "@" + name : name;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9_]")]
    private static partial Regex InvalidIdentifierCharsRegex();

    [GeneratedRegex(@"[-_.\s]+|(?<=[a-z])(?=[A-Z])")]
    private static partial Regex SplitWordsRegex();

    [GeneratedRegex(@"(?:^|(?<=\s))-(?=\d)")]
    private static partial Regex LeadingMinusRegex();
}
