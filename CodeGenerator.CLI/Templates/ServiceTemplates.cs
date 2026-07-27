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
        return $@"using System.Linq.Expressions;
using {entidadesNamespace}.{entity.SchemaNamespace};
using {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};
using {serviciosNamespace}.Services.Common;

namespace {serviciosNamespace}.{entity.SchemaNamespace}.Services;

public interface I{entity.Name}Service : IService<{entity.Name}Dto, Create{entity.Name}Dto, Update{entity.Name}Dto, {keyType}>
{{
    Task<IEnumerable<{entity.Name}Dto>> FindAsync(Expression<Func<{entity.Name}, bool>> predicate);
}}
";
    }

    public static string GetEntityServiceImplementation(string serviciosNamespace, string entidadesNamespace, string datosNamespace, EntityInfo entity)
    {
        var keyName = entity.KeyProperty.Name;
        var keyType = entity.KeyProperty.Type.Replace("?", "");

        return $@"using System.Linq.Expressions;
using {entidadesNamespace}.{entity.SchemaNamespace};
using {datosNamespace}.Repositories;
using {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};
using {serviciosNamespace}.{entity.SchemaNamespace}.Mappings;

namespace {serviciosNamespace}.{entity.SchemaNamespace}.Services;

public class {entity.Name}Service : I{entity.Name}Service
{{
    private readonly IRepository<{entity.Name}> _repository;

    public {entity.Name}Service(IRepository<{entity.Name}> repository)
    {{
        _repository = repository;
    }}

    public async Task<IEnumerable<{entity.Name}Dto>> GetAllAsync()
    {{
        var entities = await _repository.GetAllAsync();
        return entities.Select(e => e.ToDto());
    }}

    public async Task<{entity.Name}Dto?> GetByIdAsync({keyType} id)
    {{
        var entity = await _repository.GetByIdAsync(id);
        return entity?.ToDto();
    }}

    public async Task<IEnumerable<{entity.Name}Dto>> FindAsync(Expression<Func<{entity.Name}, bool>> predicate)
    {{
        var entities = await _repository.FindAsync(predicate);
        return entities.Select(e => e.ToDto());
    }}

    public async Task<{entity.Name}Dto> CreateAsync(Create{entity.Name}Dto createDto)
    {{
        var entity = createDto.ToEntity();
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return entity.ToDto();
    }}

    public async Task<bool> UpdateAsync({keyType} id, Update{entity.Name}Dto updateDto)
    {{
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return false;

        entity.UpdateEntity(updateDto);
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return true;
    }}

    public async Task<bool> DeleteAsync({keyType} id)
    {{
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return false;

        _repository.Remove(entity);
        await _repository.SaveChangesAsync();
        return true;
    }}
}}
";
    }
}
