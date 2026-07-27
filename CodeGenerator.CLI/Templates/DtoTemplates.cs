using CodeGenerator.CLI.Services;
using System.Text;

namespace CodeGenerator.CLI.Templates;

public static class DtoTemplates
{
    public static string GetEntityDto(string serviciosNamespace, EntityInfo entity)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};");
        sb.AppendLine();
        sb.AppendLine($"public class {entity.Name}Dto");
        sb.AppendLine("{");

        foreach (var prop in entity.Properties.Where(p => !p.IsNavigation))
        {
            sb.AppendLine($"    public {prop.Type} {prop.Name} {{ get; set; }}");
            if (prop.IsForeignKey && !string.IsNullOrEmpty(prop.FkDisplayPropertyName))
            {
                sb.AppendLine($"    public string? {prop.FkDisplayPropertyName} {{ get; set; }}");
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GetCreateEntityDto(string serviciosNamespace, EntityInfo entity)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};");
        sb.AppendLine();
        sb.AppendLine($"public class Create{entity.Name}Dto");
        sb.AppendLine("{");

        foreach (var prop in entity.Properties.Where(p => !p.IsNavigation && !p.IsKey))
        {
            sb.AppendLine($"    public {prop.Type} {prop.Name} {{ get; set; }}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GetUpdateEntityDto(string serviciosNamespace, EntityInfo entity)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};");
        sb.AppendLine();
        sb.AppendLine($"public class Update{entity.Name}Dto");
        sb.AppendLine("{");

        foreach (var prop in entity.Properties.Where(p => !p.IsNavigation))
        {
            sb.AppendLine($"    public {prop.Type} {prop.Name} {{ get; set; }}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string GetMappingExtensions(string serviciosNamespace, string entidadesNamespace, EntityInfo entity)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"using {entidadesNamespace}.{entity.SchemaNamespace};");
        sb.AppendLine($"using {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};");
        sb.AppendLine();
        sb.AppendLine($"namespace {serviciosNamespace}.{entity.SchemaNamespace}.Mappings;");
        sb.AppendLine();
        sb.AppendLine($"public static class {entity.Name}MappingExtensions");
        sb.AppendLine("{");

        // ToDto
        sb.AppendLine($"    public static {entity.Name}Dto ToDto(this {entity.Name} entity)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (entity == null) return null!;");
        sb.AppendLine($"        return new {entity.Name}Dto");
        sb.AppendLine("        {");
        foreach (var prop in entity.Properties.Where(p => !p.IsNavigation))
        {
            sb.AppendLine($"            {prop.Name} = entity.{prop.Name},");
            if (prop.IsForeignKey && !string.IsNullOrEmpty(prop.FkDisplayPropertyName))
            {
                sb.AppendLine($"            {prop.FkDisplayPropertyName} = entity.{prop.FkReferencedTable}?.{prop.FkReferencedDisplayColumn},");
            }
        }
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        // ToEntity
        sb.AppendLine($"    public static {entity.Name} ToEntity(this Create{entity.Name}Dto dto)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (dto == null) return null!;");
        sb.AppendLine($"        return new {entity.Name}");
        sb.AppendLine("        {");
        foreach (var prop in entity.Properties.Where(p => !p.IsNavigation && !p.IsKey))
        {
            sb.AppendLine($"            {prop.Name} = dto.{prop.Name},");
        }
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        // UpdateEntity
        sb.AppendLine($"    public static void UpdateEntity(this {entity.Name} entity, Update{entity.Name}Dto dto)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (entity == null || dto == null) return;");
        foreach (var prop in entity.Properties.Where(p => !p.IsNavigation && !p.IsKey))
        {
            sb.AppendLine($"        entity.{prop.Name} = dto.{prop.Name};");
        }
        sb.AppendLine("    }");

        sb.AppendLine("}");
        return sb.ToString();
    }
}
