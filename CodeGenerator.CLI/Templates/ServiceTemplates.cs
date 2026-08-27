using CodeGenerator.CLI.Services;

namespace CodeGenerator.CLI.Templates;

public static class ServiceTemplates
{
    public static string GetGenericServiceInterface(string serviciosNamespace) => $@"namespace {serviciosNamespace}.Services.Common;

public interface IService<TDto, TCreateDto, TUpdateDto, TKey>
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{{
    Task<IEnumerable<TDto>> GetAllAsync();
    Task<TDto?> GetByIdAsync(TKey id);
    Task<TDto> CreateAsync(TCreateDto createDto);
    Task<bool> UpdateAsync(TKey id, TUpdateDto updateDto);
    Task<bool> DeleteAsync(TKey id);
}}
";

    public static string GetEntityServiceInterface(string serviciosNamespace, string entidadesNamespace, EntityInfo entity)
    {
        var keyType = entity.KeyProperty.Type.Replace("?", "");
        var hasParametrosFk = entity.Properties.Any(p => p.IsForeignKey && p.FkReferencedTable == "Parametros");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine($"using {entidadesNamespace}.{entity.SchemaNamespace};");
        sb.AppendLine($"using {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};");
        sb.AppendLine($"using {serviciosNamespace}.Services.Common;");
        sb.AppendLine();
        sb.AppendLine($"namespace {serviciosNamespace}.{entity.SchemaNamespace}.Services;");
        sb.AppendLine();
        sb.AppendLine($"public interface I{entity.Name}Service : IService<{entity.Name}Dto, Create{entity.Name}Dto, Update{entity.Name}Dto, {keyType}>");
        sb.AppendLine("{");
        sb.AppendLine($"    Task<IEnumerable<{entity.Name}Dto>> FindAsync(Expression<Func<{entity.Name}, bool>> predicate);");
        if (entity.Name == "Parametros")
        {
            sb.AppendLine($"    Task<IEnumerable<{entity.Name}Dto>> GetAllAsync(int idTipoParametro);");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string GetEntityServiceImplementation(string serviciosNamespace, string entidadesNamespace, string datosNamespace, EntityInfo entity)
    {
        var keyName = entity.KeyProperty.Name;
        var keyType = entity.KeyProperty.Type.Replace("?", "");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine($"using {entidadesNamespace}.{entity.SchemaNamespace};");
        sb.AppendLine($"using {datosNamespace}.Repositories;");
        sb.AppendLine($"using {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};");
        sb.AppendLine($"using {serviciosNamespace}.{entity.SchemaNamespace}.Mappings;");
        sb.AppendLine();
        sb.AppendLine($"namespace {serviciosNamespace}.{entity.SchemaNamespace}.Services;");
        sb.AppendLine();
        sb.AppendLine($"public class {entity.Name}Service : I{entity.Name}Service");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly IRepository<{entity.Name}> _repository;");
        sb.AppendLine();
        sb.AppendLine($"    public {entity.Name}Service(IRepository<{entity.Name}> repository)");
        sb.AppendLine("    {");
        sb.AppendLine("        _repository = repository;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<IEnumerable<{entity.Name}Dto>> GetAllAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        var entities = await _repository.GetAllAsync();");
        sb.AppendLine("        return entities.Select(e => e.ToDto());");
        sb.AppendLine("    }");
        if (entity.Name == "Parametros")
        {
            sb.AppendLine();
            sb.AppendLine($"    public async Task<IEnumerable<{entity.Name}Dto>> GetAllAsync(int idTipoParametro)");
            sb.AppendLine("    {");
            sb.AppendLine("        var entities = await _repository.FindAsync(p => p.IdTipoParametro == idTipoParametro);");
            sb.AppendLine("        return entities.Select(e => e.ToDto());");
            sb.AppendLine("    }");
        }
        sb.AppendLine();
        sb.AppendLine($"    public async Task<{entity.Name}Dto?> GetByIdAsync({keyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine("        var entity = await _repository.GetByIdAsync(id);");
        sb.AppendLine("        return entity?.ToDto();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<IEnumerable<{entity.Name}Dto>> FindAsync(Expression<Func<{entity.Name}, bool>> predicate)");
        sb.AppendLine("    {");
        sb.AppendLine("        var entities = await _repository.FindAsync(predicate);");
        sb.AppendLine("        return entities.Select(e => e.ToDto());");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<{entity.Name}Dto> CreateAsync(Create{entity.Name}Dto createDto)");
        sb.AppendLine("    {");
        sb.AppendLine("        var entity = createDto.ToEntity();");
        sb.AppendLine("        await _repository.AddAsync(entity);");
        sb.AppendLine("        await _repository.SaveChangesAsync();");
        sb.AppendLine("        return entity.ToDto();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<bool> UpdateAsync({keyType} id, Update{entity.Name}Dto updateDto)");
        sb.AppendLine("    {");
        sb.AppendLine("        var entity = await _repository.GetByIdAsync(id);");
        sb.AppendLine("        if (entity == null) return false;");
        sb.AppendLine();
        sb.AppendLine("        entity.UpdateEntity(updateDto);");
        sb.AppendLine("        _repository.Update(entity);");
        sb.AppendLine("        await _repository.SaveChangesAsync();");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public async Task<bool> DeleteAsync({keyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine("        var entity = await _repository.GetByIdAsync(id);");
        sb.AppendLine("        if (entity == null) return false;");
        sb.AppendLine();
        sb.AppendLine("        _repository.Remove(entity);");
        sb.AppendLine("        await _repository.SaveChangesAsync();");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
