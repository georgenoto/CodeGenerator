using System.Text;
using System.Text.RegularExpressions;

namespace CodeGenerator.CLI.Services;

public class EntityProperty
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsKey { get; set; }
    public bool IsNavigation { get; set; }
    public bool IsNullable { get; set; }
    public bool IsForeignKey { get; set; }
    public string? FkReferencedSchema { get; set; }
    public string? FkReferencedTable { get; set; }
    public string? FkReferencedColumn { get; set; }
    public string? FkReferencedDisplayColumn { get; set; }
    public string? FkDisplayPropertyName { get; set; }
    public string? CascadeParentProperty { get; set; }
    public string? CascadeFilterProperty { get; set; }
    public bool GenerateGetByEndpoint { get; set; }
}

public class EntityInfo
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public List<EntityProperty> Properties { get; set; } = new();
    public string DisplayColumn { get; set; } = "Id";
    
    public string SchemaNamespace => SchemaHelper.ToNamespace(SchemaName);

    public EntityProperty KeyProperty => 
        Properties.FirstOrDefault(p => p.IsKey) 
        ?? Properties.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
        ?? Properties.FirstOrDefault(p => p.Name.Equals($"{Name}Id", StringComparison.OrdinalIgnoreCase))
        ?? Properties.FirstOrDefault(p => !p.IsNavigation) 
        ?? new EntityProperty { Name = "Id", Type = "int", IsKey = true };
}

public static class SchemaHelper
{
    public static string ToNamespace(string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName)) return "Dbo";
        var parts = schemaName.Split(new[] { '_', '-', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length > 0)
                sb.Append(char.ToUpper(part[0]) + part.Substring(1).ToLower());
        }
        var result = sb.ToString();
        return string.IsNullOrEmpty(result) ? "Dbo" : result;
    }
}

public class CodeParser
{
    public static List<EntityInfo> ParseEntities(string entidadesPath, string dbContextName)
    {
        var entities = new List<EntityInfo>();

        if (!Directory.Exists(entidadesPath))
            return entities;

        var csFiles = Directory.GetFiles(entidadesPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            // Skip DbContext or non-entity files
            if (fileName.Equals(dbContextName, StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith("Context", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("I") && char.IsUpper(fileName[1]))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            
            // Basic verification if it's a class definition
            if (!content.Contains($"public partial class {fileName}") && 
                !content.Contains($"public class {fileName}"))
            {
                continue;
            }

            var entity = new EntityInfo
            {
                Name = fileName,
                FilePath = file,
                Properties = ExtractProperties(content, fileName)
            };

            entities.Add(entity);
        }

        return entities;
    }

    private static List<EntityProperty> ExtractProperties(string content, string entityName)
    {
        var properties = new List<EntityProperty>();
        
        // Match C# property patterns: public [type] [propertyName] { get; set; }
        var propRegex = new Regex(@"public\s+([\w\<\>\?\.\,\s]+)\s+([\w]+)\s*\{\s*get;\s*set;\s*\}", RegexOptions.Compiled);
        var matches = propRegex.Matches(content);

        foreach (Match match in matches)
        {
            var rawType = match.Groups[1].Value.Trim();
            var propName = match.Groups[2].Value.Trim();

            // Skip DbContext navigation collection initializers or internal methods
            if (propName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                propName.Equals($"{entityName}Id", StringComparison.OrdinalIgnoreCase))
            {
                properties.Add(new EntityProperty
                {
                    Name = propName,
                    Type = rawType,
                    IsKey = true,
                    IsNavigation = false,
                    IsNullable = rawType.EndsWith("?")
                });
                continue;
            }

            // Check if navigation property (e.g., ICollection<Foo>, virtual Bar, or custom class)
            bool isNavigation = rawType.StartsWith("ICollection<") ||
                                rawType.StartsWith("List<") ||
                                rawType.StartsWith("virtual ") ||
                                (!IsPrimitiveOrCommonType(rawType.Replace("?", "").Trim()));

            properties.Add(new EntityProperty
            {
                Name = propName,
                Type = rawType.Replace("virtual ", "").Trim(),
                IsKey = false,
                IsNavigation = isNavigation,
                IsNullable = rawType.EndsWith("?")
            });
        }

        return properties;
    }

    private static bool IsPrimitiveOrCommonType(string typeName)
    {
        var commonTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "int", "long", "short", "byte", "sbyte", "uint", "ulong", "ushort",
            "float", "double", "decimal", "bool", "char", "string", "DateTime",
            "DateTimeOffset", "TimeSpan", "Guid", "byte[]"
        };

        return commonTypes.Contains(typeName);
    }
}
