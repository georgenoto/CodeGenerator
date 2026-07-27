# Guía del proyecto generado

## Estructura general

El generador produce 5 proyectos .NET dentro de una solución:

```
Solucion.sln
├── Entidades/              # Modelos POCO (1 clase por tabla)
├── Datos/                  # EF Core DbContext + Repositorios genéricos
├── Servicios/              # DTOs, Mappings, Servicios de aplicación
├── Web/                    # ASP.NET Core MVC (vistas, controladores)
└── WebApi/                 # ASP.NET Core Web API (REST, Swagger)
```

Cada proyecto tiene subdirectorios por **esquema** de BD. Ejemplo:

```
Servicios/
├── General/        # tablas schema dbo → "General"
│   ├── DTOs/Cliente/
│   ├── Mappings/
│   └── Services/
└── SegUsuarios/    # tablas schema seg_usuarios → "SegUsuarios"
    ├── DTOs/Usuario/
    ├── Mappings/
    └── Services/
```

---

## Flujo de una petición (lectura)

```
Petición HTTP → Controller → Service → Repository → EF Core → SQL
                    ↓
               DTO (respuesta)
                    ↓
               Vista / JSON
```

1. **Controller** (`Web/Controllers/{Schema}/{Entity}Controller.cs`)
   - Recibe el request, llama al Service
   - Para Create/Edit: también inyecta `I{RefEntity}Service` para poblar dropdowns de FK
2. **Service** (`Servicios/{Schema}/Services/{Entity}Service.cs`)
   - Implementa la lógica, llama al Repository
   - `GetAllAsync()` → repositorio → mapea entidades a DTOs con `ToDto()`
3. **Repository** (`Datos/Repositories/Repository.cs`)
   - Genérico: `IRepository<T>` / `Repository<T>`
   - `GetAllAsync()` incluye automáticamente (`Include`) las navegaciones que EF reconoce vía `[ForeignKey]` o fluent API
4. **Entity** (`Entidades/{Schema}/{Entity}.cs`)
   - POCO con propiedades por columna + navegaciones con `[ForeignKey]`
5. **DTO** (`Servicios/{Schema}/DTOs/{Entity}/{Entity}Dto.cs`)
   - Propiedades planas + propiedades display para FK (ej. `ClienteNombre`)

---

## FK y relaciones

### Detección
El generador lee las FK reales de `sys.foreign_keys`. Para cada columna FK:
- Marca `IsForeignKey = true`
- Guarda `FkReferencedTable`, `FkReferencedColumn`
- Determina una **columna de visualización** (`Nombre`, `Descripcion`, etc.)

### Entidad generada
```csharp
public partial class Parametro
{
    public int IdTipoParametro { get; set; }

    [ForeignKey("IdTipoParametro")]
    public Conceptos.TipoParametro? TipoParametro { get; set; }
}
```

### DTO generado
```csharp
public class ParametroDto
{
    public int IdTipoParametro { get; set; }
    public string? TipoParametroNombre { get; set; } // display de la FK
}
```

### Mapping (ToDto)
```csharp
TipoParametroNombre = entity.TipoParametro?.Nombre
```
EF carga la navegación porque el Repository hace `.Include()` automático.

---

## Vistas y Dropdowns

### Index (`_DataTable.cshtml` parcial compartido)
- Recibe `ColumnHeaders[]` (nombres a mostrar) y `ColumnProperties[]` (propiedades a leer vía reflexión)
- Para FK: header = nombre de tabla referenciada, property = nombre display (ej. `ClienteNombre`)
- Paginación client-side (10/20/50/100 registros)
- Buscador que filtra por cualquier columna

### Create / Edit
- Para propiedades FK: `<select asp-for="IdTipoParametro" asp-items="ViewData["IdTipoParametro"] as SelectList">`
- El Controller puebla `ViewData["IdTipoParametro"]` con todos los registros de la tabla referenciada

### Details
- Muestra `ClienteNombre` en lugar de `ClienteId`

---

## Controller (MVC)

```csharp
public class ParametroController : Controller
{
    private readonly IParametroService _service;
    private readonly ITipoParametroService _tipoParametroService; // para dropdown

    public async Task<IActionResult> Create()
    {
        await PopulateTipoParametroDropdownAsync();
        return View("~/Views/Conceptos/Parametro/Create.cshtml");
    }

    private async Task PopulateTipoParametroDropdownAsync()
    {
        var items = await _tipoParametroService.GetAllAsync();
        ViewData["IdTipoParametro"] = new SelectList(items, nameof(TipoParametroDto.Id), "Nombre");
    }
}
```

---

## Service

```csharp
public class ParametroService : IParametroService
{
    private readonly IRepository<Parametro> _repository;

    public async Task<IEnumerable<ParametroDto>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return entities.Select(e => e.ToDto());
    }

    public async Task<ParametroDto> CreateAsync(CreateParametroDto dto)
    {
        var entity = dto.ToEntity();
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return entity.ToDto();
    }
}
```

---

## Repository

```csharp
public class Repository<T> : IRepository<T> where T : class
{
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        // Incluye navegaciones reconocidas por EF (via [ForeignKey] o fluent)
        return await IncludeNavigations(_dbSet).ToListAsync();
    }

    private IQueryable<T> IncludeNavigations(IQueryable<T> query)
    {
        foreach (var nav in _context.Model.FindEntityType(typeof(T)).GetNavigations())
            query = query.Include(nav.Name);
        return query;
    }
}
```

---

## DI (Program.cs)

```csharp
builder.Services.AddDbContext<ContabilidadContext>(...);
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IParametroService, ParametroService>();
builder.Services.AddScoped<ITipoParametroService, TipoParametroService>();
// ... uno por entidad
builder.Services.AddControllersWithViews();
```

---

## Resumen de capas

| Capa | Proyecto | Contenido |
|------|----------|-----------|
| **Presentación MVC** | `Web` | Controladores + Vistas Razor + `_DataTable` parcial |
| **Presentación API** | `WebApi` | Controladores REST + Swagger |
| **Aplicación** | `Servicios` | DTOs, Mappings, Interfaces/implementaciones de servicios |
| **Dominio** | `Entidades` | POCOs con `[ForeignKey]` para navegaciones |
| **Persistencia** | `Datos` | DbContext, Repositorio genérico con `.Include()` automático |
