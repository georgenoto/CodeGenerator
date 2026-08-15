using System.Text.Json;
using CodeGenerator.CLI.Configuration;
using CodeGenerator.CLI.Models;
using CodeGenerator.CLI.Templates;

namespace CodeGenerator.CLI.Services;

public class SolutionGenerator
{
    private readonly GeneratorOptions _options;

    public SolutionGenerator(GeneratorOptions options)
    {
        _options = options;
    }

    public bool Generate()
    {
        Console.WriteLine($"\n=======================================================");
        Console.WriteLine($" Iniciando Generador para el Proyecto: {_options.ProjectName}");
        Console.WriteLine($" Destino: {_options.OutputPath}");
        Console.WriteLine($" Target: {_options.TargetFramework}");
        Console.WriteLine($"=======================================================\n");

        // 1. Crear directorios base
        Directory.CreateDirectory(_options.OutputPath);
        Directory.CreateDirectory(_options.EntidadesPath);
        Directory.CreateDirectory(_options.DatosPath);
        Directory.CreateDirectory(_options.ServiciosPath);
        Directory.CreateDirectory(_options.WebPath);
        Directory.CreateDirectory(_options.WebApiPath);

        // 2. Crear archivos de proyectos (.csproj)
        Console.WriteLine("[1/9] Creando archivos de proyecto (.csproj)...");
        File.WriteAllText(
            Path.Combine(_options.EntidadesPath, $"{_options.EntidadesProjectName}.csproj"),
            CsProjTemplates.GetEntidadesCsProj(_options.TargetFramework));

        File.WriteAllText(
            Path.Combine(_options.DatosPath, $"{_options.DatosProjectName}.csproj"),
            CsProjTemplates.GetDatosCsProj(_options.TargetFramework, _options.EntidadesProjectName));

        File.WriteAllText(
            Path.Combine(_options.ServiciosPath, $"{_options.ServiciosProjectName}.csproj"),
            CsProjTemplates.GetServiciosCsProj(_options.TargetFramework, _options.EntidadesProjectName, _options.DatosProjectName));

        File.WriteAllText(
            Path.Combine(_options.WebPath, $"{_options.WebProjectName}.csproj"),
            CsProjTemplates.GetWebCsProj(_options.TargetFramework, _options.ServiciosProjectName, _options.DatosProjectName, _options.EntidadesProjectName));

        File.WriteAllText(
            Path.Combine(_options.WebApiPath, $"{_options.WebApiProjectName}.csproj"),
            CsProjTemplates.GetWebApiCsProj(_options.TargetFramework, _options.ServiciosProjectName, _options.DatosProjectName, _options.EntidadesProjectName));

        // 3. Crear archivo de soluciÃ³n .sln
        Console.WriteLine("[2/9] Generando soluciÃ³n .NET (.sln)...");
        ProcessRunner.RunCommand("dotnet", "new sln -n " + _options.ProjectName, _options.OutputPath);
        
        var entidadesCsproj = Path.Combine(_options.EntidadesProjectName, $"{_options.EntidadesProjectName}.csproj");
        var datosCsproj = Path.Combine(_options.DatosProjectName, $"{_options.DatosProjectName}.csproj");
        var serviciosCsproj = Path.Combine(_options.ServiciosProjectName, $"{_options.ServiciosProjectName}.csproj");
        var webCsproj = Path.Combine(_options.WebProjectName, $"{_options.WebProjectName}.csproj");
        var webApiCsproj = Path.Combine(_options.WebApiProjectName, $"{_options.WebApiProjectName}.csproj");

        ProcessRunner.RunCommand("dotnet", $"sln add \"{entidadesCsproj}\" \"{datosCsproj}\" \"{serviciosCsproj}\" \"{webCsproj}\" \"{webApiCsproj}\"", _options.OutputPath);

        // 4. Extraer Esquema de la Base de Datos nativamente
        Console.WriteLine("[3/9] Leyendo tablas, columnas y tipos desde SQL Server...");
        DatabaseSchema schema;
        try
        {
            schema = SqlSchemaReader.ReadSchema(_options.ConnectionString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] No se pudo leer el esquema de la Base de Datos: {ex.Message}");
            return false;
        }

        Console.WriteLine($"  Se encontraron {schema.Tables.Count} tablas en la base de datos.");

        // Detectar esquema de seguridad (Usuarios, Roles, Permisos, etc.)
        Console.WriteLine("[4/9] Detectando esquema de seguridad...");
        var security = SecuritySchemaReader.ReadSecuritySchema(_options.ConnectionString, schema);
        if (security.IsEnabled)
        {
            Console.WriteLine($"  Esquema de seguridad detectado en '{security.SchemaName}': {security.Permissions.Count} permisos encontrados.");
        }
        else
        {
            Console.WriteLine("  No se detectÃ³ esquema de seguridad (se generarÃ¡ sin mÃ³dulo de login).");
        }

        // 5. Cargar configuraciÃ³n manual de cascadas (opcional)
        List<CascadeConfig>? manualCascades = null;
        if (!string.IsNullOrWhiteSpace(_options.CascadeConfigPath))
        {
            var cascadePath = _options.CascadeConfigPath;
            if (!Path.IsPathRooted(cascadePath))
            {
                var candidate1 = Path.Combine(Directory.GetCurrentDirectory(), cascadePath);
                var candidate2 = Path.Combine(AppContext.BaseDirectory, cascadePath);
                if (File.Exists(candidate1))
                    cascadePath = candidate1;
                else if (File.Exists(candidate2))
                    cascadePath = candidate2;
            }

            if (File.Exists(cascadePath))
            {
                Console.WriteLine($"  Cargando configuraciÃ³n de cascadas desde: {cascadePath}");
                var json = File.ReadAllText(cascadePath);
                manualCascades = JsonSerializer.Deserialize<List<CascadeConfig>>(json);
                Console.WriteLine($"  Se encontraron {manualCascades?.Count ?? 0} configuraciones de cascada manual.");
            }
            else
            {
                Console.WriteLine($"  [AVISO] Archivo de cascadas no encontrado: {_options.CascadeConfigPath}");
                Console.WriteLine($"  (buscado en: {Directory.GetCurrentDirectory()} y {AppContext.BaseDirectory})");
            }
        }

        // 6. Generar Capa de Entidades
        Console.WriteLine("[5/9] Generando modelos POCO en la capa de Entidades...");
        var entities = EntityGenerator.GenerateEntities(_options.EntidadesProjectName, _options.EntidadesPath, schema, manualCascades);

        // 7. Generar Capa de Datos (DbContext + Repositorios)
        Console.WriteLine("[6/9] Generando DbContext y Repositorios en la capa de Datos...");
        DbContextGenerator.GenerateDbContext(_options.DatosProjectName, _options.EntidadesProjectName, _options.DatosPath, _options.DbContextName, schema);
        
        var reposDir = Path.Combine(_options.DatosPath, "Repositories");
        Directory.CreateDirectory(reposDir);
        File.WriteAllText(Path.Combine(reposDir, "IRepository.cs"), RepositoryTemplates.GetIRepositoryInterface(_options.DatosProjectName));
        File.WriteAllText(Path.Combine(reposDir, "Repository.cs"), RepositoryTemplates.GetRepositoryImplementation(_options.DatosProjectName, _options.DbContextName));

        // 7. Generar DTOs, Mappings y Servicios en la capa de Servicios (agrupado por esquema)
        Console.WriteLine("[6/9] Generando DTOs, Extensiones de Mapeo y Servicios...");
        var serviciosCommonDir = Path.Combine(_options.ServiciosPath, "Services", "Common");
        Directory.CreateDirectory(serviciosCommonDir);
        File.WriteAllText(Path.Combine(serviciosCommonDir, "IService.cs"), ServiceTemplates.GetGenericServiceInterface(_options.ServiciosProjectName));

        if (security.IsEnabled)
        {
            // MÃ³dulo de seguridad en la capa de Servicios
            Console.WriteLine("  Generando servicios de seguridad (PasswordHasher, JWT, AuthService)...");
            File.WriteAllText(Path.Combine(serviciosCommonDir, "PasswordHasher.cs"),
                SecurityTemplates.GetPasswordHasher(_options.ServiciosProjectName));
            File.WriteAllText(Path.Combine(serviciosCommonDir, "JwtTokenService.cs"),
                SecurityTemplates.GetJwtTokenService(_options.ServiciosProjectName));

            var authDtoDir = Path.Combine(_options.ServiciosPath, security.SchemaNamespace, "DTOs", "Auth");
            Directory.CreateDirectory(authDtoDir);
            File.WriteAllText(Path.Combine(authDtoDir, "LoginDto.cs"),
                SecurityTemplates.GetLoginDto(_options.ServiciosProjectName, security));
            File.WriteAllText(Path.Combine(authDtoDir, "LoginResultDto.cs"),
                SecurityTemplates.GetLoginResultDto(_options.ServiciosProjectName, security));
            File.WriteAllText(Path.Combine(authDtoDir, "RefreshTokenDto.cs"),
                SecurityTemplates.GetRefreshTokenDto(_options.ServiciosProjectName, security));
            File.WriteAllText(Path.Combine(authDtoDir, "MenuDto.cs"),
                SecurityTemplates.GetMenuDtos(_options.ServiciosProjectName, security));

            var authServiceDir = Path.Combine(_options.ServiciosPath, security.SchemaNamespace, "Services");
            Directory.CreateDirectory(authServiceDir);
            File.WriteAllText(Path.Combine(authServiceDir, "IAuthService.cs"),
                SecurityTemplates.GetIAuthService(_options.ServiciosProjectName, _options.EntidadesProjectName, security));
            File.WriteAllText(Path.Combine(authServiceDir, "AuthService.cs"),
                SecurityTemplates.GetAuthService(_options.ServiciosProjectName, _options.EntidadesProjectName, _options.DatosProjectName, security));
        }

        var entitiesBySchema = entities.GroupBy(e => e.SchemaName);
        foreach (var schemaGroup in entitiesBySchema)
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            foreach (var entity in schemaGroup)
            {
                // DTOs
                var dtoDir = Path.Combine(_options.ServiciosPath, schemaNs, "DTOs", entity.Name);
                Directory.CreateDirectory(dtoDir);
                File.WriteAllText(Path.Combine(dtoDir, $"{entity.Name}Dto.cs"), DtoTemplates.GetEntityDto(_options.ServiciosProjectName, entity));
                File.WriteAllText(Path.Combine(dtoDir, $"Create{entity.Name}Dto.cs"), DtoTemplates.GetCreateEntityDto(_options.ServiciosProjectName, entity));
                File.WriteAllText(Path.Combine(dtoDir, $"Update{entity.Name}Dto.cs"), DtoTemplates.GetUpdateEntityDto(_options.ServiciosProjectName, entity));

                // Mappings
                var mappingDir = Path.Combine(_options.ServiciosPath, schemaNs, "Mappings");
                Directory.CreateDirectory(mappingDir);
                File.WriteAllText(Path.Combine(mappingDir, $"{entity.Name}MappingExtensions.cs"), DtoTemplates.GetMappingExtensions(_options.ServiciosProjectName, _options.EntidadesProjectName, entity));

                // Services
                var serviceDir = Path.Combine(_options.ServiciosPath, schemaNs, "Services");
                Directory.CreateDirectory(serviceDir);
                File.WriteAllText(Path.Combine(serviceDir, $"I{entity.Name}Service.cs"), ServiceTemplates.GetEntityServiceInterface(_options.ServiciosProjectName, _options.EntidadesProjectName, entity));
                File.WriteAllText(Path.Combine(serviceDir, $"{entity.Name}Service.cs"), ServiceTemplates.GetEntityServiceImplementation(_options.ServiciosProjectName, _options.EntidadesProjectName, _options.DatosProjectName, entity));
            }
        }

        // 8. Generar Controladores MVC, Vistas, Layout y Program.cs en la capa Web
        Console.WriteLine("[8/9] Generando proyecto Web MVC (Controladores, Vistas, Layout)...");
        var rootControllersDir = Path.Combine(_options.WebPath, "Controllers");
        Directory.CreateDirectory(rootControllersDir);

        // Controladores para cada entidad (agrupado por esquema)
        foreach (var schemaGroup in entitiesBySchema)
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            var schemaControllersDir = Path.Combine(rootControllersDir, schemaNs);
            Directory.CreateDirectory(schemaControllersDir);

            foreach (var entity in schemaGroup)
            {
                var permisos = BuildPermisoMap(entity.Name, security);
                File.WriteAllText(
                    Path.Combine(schemaControllersDir, $"{entity.Name}Controller.cs"),
                    ControllerTemplates.GetEntityController(_options.WebProjectName, _options.ServiciosProjectName, entity, permisos));
            }
        }

        // HomeController (raÃ­z, sin esquema)
        File.WriteAllText(
            Path.Combine(rootControllersDir, "HomeController.cs"),
            ControllerTemplates.GetHomeController(_options.WebProjectName, _options.ServiciosProjectName));

        // MÃ³dulo de seguridad (Web MVC)
        if (security.IsEnabled)
        {
            Console.WriteLine("  Generando mÃ³dulo de seguridad Web (Login, Logout, Permisos, MenÃº)...");

            // AccountController
            File.WriteAllText(
                Path.Combine(rootControllersDir, "AccountController.cs"),
                SecurityTemplates.GetAccountController(_options.WebProjectName, _options.ServiciosProjectName, security));

            // PermisoAttribute (MVC)
            var segAttrDir = Path.Combine(_options.WebPath, "Seguridad");
            Directory.CreateDirectory(segAttrDir);
            File.WriteAllText(Path.Combine(segAttrDir, "PermisoAttribute.cs"),
                SecurityTemplates.GetPermisoAttribute(_options.WebProjectName));

            // Vistas de Account
            var accountViewsDir = Path.Combine(_options.WebPath, "Views", "Account");
            Directory.CreateDirectory(accountViewsDir);
            File.WriteAllText(Path.Combine(accountViewsDir, "Login.cshtml"),
                SecurityTemplates.GetLoginView(_options.WebProjectName, _options.ServiciosProjectName, security));
            File.WriteAllText(Path.Combine(accountViewsDir, "AccessDenied.cshtml"),
                SecurityTemplates.GetAccessDeniedView());

            // ViewComponent de menÃº
            var componentsDir = Path.Combine(_options.WebPath, "Components");
            Directory.CreateDirectory(componentsDir);
            File.WriteAllText(Path.Combine(componentsDir, "MenuViewComponent.cs"),
                SecurityTemplates.GetMenuViewComponent(_options.WebProjectName, _options.ServiciosProjectName, security));

            var menuViewDir = Path.Combine(_options.WebPath, "Views", "Shared", "Components", "Menu");
            Directory.CreateDirectory(menuViewDir);
            File.WriteAllText(Path.Combine(menuViewDir, "Default.cshtml"),
                SecurityTemplates.GetMenuViewComponentView(_options.ServiciosProjectName, security));
        }

        // Vistas para cada entidad (agrupado por esquema)
        foreach (var schemaGroup in entitiesBySchema)
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            foreach (var entity in schemaGroup)
            {
                var viewsDir = Path.Combine(_options.WebPath, "Views", schemaNs, entity.Name);
                Directory.CreateDirectory(viewsDir);

                File.WriteAllText(Path.Combine(viewsDir, "Index.cshtml"),
                    ViewTemplates.GetIndexView(_options.WebProjectName, _options.ServiciosProjectName, entity));
                File.WriteAllText(Path.Combine(viewsDir, "Details.cshtml"),
                    ViewTemplates.GetDetailsView(_options.WebProjectName, _options.ServiciosProjectName, entity));
                File.WriteAllText(Path.Combine(viewsDir, "Create.cshtml"),
                    ViewTemplates.GetCreateView(_options.WebProjectName, _options.ServiciosProjectName, entity));
                File.WriteAllText(Path.Combine(viewsDir, "Edit.cshtml"),
                    ViewTemplates.GetEditView(_options.WebProjectName, _options.ServiciosProjectName, entity));
                File.WriteAllText(Path.Combine(viewsDir, "Delete.cshtml"),
                    ViewTemplates.GetDeleteView(_options.WebProjectName, _options.ServiciosProjectName, entity));
            }
        }

        // Vistas compartidas (Layout, _DataTable, _ViewStart, _ViewImports, _ValidationScriptsPartial)
        var sharedViewsDir = Path.Combine(_options.WebPath, "Views", "Shared");
        Directory.CreateDirectory(sharedViewsDir);

        File.WriteAllText(Path.Combine(sharedViewsDir, "_Layout.cshtml"),
            ViewTemplates.GetLayoutView(_options.WebProjectName, entities, security));
        File.WriteAllText(Path.Combine(sharedViewsDir, "_DataTable.cshtml"),
            ViewTemplates.GetDataTablePartial());
        File.WriteAllText(Path.Combine(sharedViewsDir, "_ValidationScriptsPartial.cshtml"),
            ViewTemplates.GetValidationScriptsPartial());

        // Home views
        var homeViewsDir = Path.Combine(_options.WebPath, "Views", "Home");
        Directory.CreateDirectory(homeViewsDir);

        File.WriteAllText(Path.Combine(homeViewsDir, "Index.cshtml"),
            ViewTemplates.GetHomeIndexView(_options.WebProjectName));
        File.WriteAllText(Path.Combine(homeViewsDir, "Privacy.cshtml"),
            ViewTemplates.GetHomePrivacyView());

        // _ViewStart.cshtml y _ViewImports.cshtml
        File.WriteAllText(
            Path.Combine(_options.WebPath, "Views", "_ViewStart.cshtml"),
            ViewTemplates.GetViewStart());
        File.WriteAllText(
            Path.Combine(_options.WebPath, "Views", "_ViewImports.cshtml"),
            ViewTemplates.GetViewImports(_options.WebProjectName, _options.ServiciosProjectName));

        // wwwroot (archivos estÃ¡ticos)
        var cssDir = Path.Combine(_options.WebPath, "wwwroot", "css");
        Directory.CreateDirectory(cssDir);
        File.WriteAllText(Path.Combine(cssDir, "site.css"), ViewTemplates.GetSiteCss());

        var jsDir = Path.Combine(_options.WebPath, "wwwroot", "js");
        Directory.CreateDirectory(jsDir);
        File.WriteAllText(Path.Combine(jsDir, "site.js"), ViewTemplates.GetSiteJs());

        // Program.cs
        File.WriteAllText(
            Path.Combine(_options.WebPath, "Program.cs"),
            ProgramCsTemplate.GetProgramCs(_options.WebProjectName, _options.DatosProjectName, _options.ServiciosProjectName, _options.DbContextName, entities, security));

        // appsettings.json
        File.WriteAllText(
            Path.Combine(_options.WebPath, "appsettings.json"),
            ProgramCsTemplate.GetAppSettingsJson(_options.ConnectionString));

        // Properties/launchSettings.json
        var propertiesDir = Path.Combine(_options.WebPath, "Properties");
        Directory.CreateDirectory(propertiesDir);
        File.WriteAllText(
            Path.Combine(propertiesDir, "launchSettings.json"),
            ProgramCsTemplate.GetLaunchSettingsJson());

        // 9. Generar proyecto Web API (REST)
        Console.WriteLine("[9/9] Generando proyecto Web API REST (Controladores, Swagger)...");
        var apiControllersDir = Path.Combine(_options.WebApiPath, "Controllers");
        Directory.CreateDirectory(apiControllersDir);

        foreach (var schemaGroup in entitiesBySchema)
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            var schemaControllersDir = Path.Combine(apiControllersDir, schemaNs);
            Directory.CreateDirectory(schemaControllersDir);

            foreach (var entity in schemaGroup)
            {
                var permisos = BuildPermisoMap(entity.Name, security);
                File.WriteAllText(
                    Path.Combine(schemaControllersDir, $"{entity.Name}Controller.cs"),
                    ControllerTemplates.GetEntityApiController(_options.WebApiProjectName, _options.ServiciosProjectName, entity, permisos));
            }
        }

        // AuthController (JWT) y PermisoAttribute para WebApi
        if (security.IsEnabled)
        {
            Console.WriteLine("  Generando mÃ³dulo de autenticaciÃ³n WebApi (JWT, RefreshToken)...");
            File.WriteAllText(
                Path.Combine(apiControllersDir, "AuthController.cs"),
                SecurityTemplates.GetAuthApiController(_options.WebApiProjectName, _options.ServiciosProjectName, security));

            var segAttrApiDir = Path.Combine(_options.WebApiPath, "Seguridad");
            Directory.CreateDirectory(segAttrApiDir);
            File.WriteAllText(Path.Combine(segAttrApiDir, "PermisoAttribute.cs"),
                SecurityTemplates.GetApiPermisoAttribute(_options.WebApiProjectName));
        }

        // Program.cs
        File.WriteAllText(
            Path.Combine(_options.WebApiPath, "Program.cs"),
            ProgramCsTemplate.GetWebApiProgramCs(_options.WebApiProjectName, _options.DatosProjectName, _options.ServiciosProjectName, _options.DbContextName, entities, security));

        // appsettings.json
        File.WriteAllText(
            Path.Combine(_options.WebApiPath, "appsettings.json"),
            ProgramCsTemplate.GetAppSettingsJson(_options.ConnectionString));

        // Properties/launchSettings.json
        var apiPropertiesDir = Path.Combine(_options.WebApiPath, "Properties");
        Directory.CreateDirectory(apiPropertiesDir);
        File.WriteAllText(
            Path.Combine(apiPropertiesDir, "launchSettings.json"),
            ProgramCsTemplate.GetLaunchSettingsJson("swagger"));

        Console.WriteLine($"\n=======================================================");
        Console.WriteLine($" Â¡GeneraciÃ³n completada exitosamente!");
        Console.WriteLine($" UbicaciÃ³n de la SoluciÃ³n: {_options.OutputPath}");
        Console.WriteLine($"=======================================================\n");

        return true;
    }

    /// <summary>
    /// Construye el mapa de permisos por acciÃ³n para un controlador (solo permisos existentes en BD).
    /// Ej.: Usuarios -> Index="USUARIOS_VER", Create="USUARIOS_CREAR", Edit="USUARIOS_EDITAR", Delete="USUARIOS_ELIMINAR", Details="USUARIOS_DETALLE"
    /// </summary>
    private static IReadOnlyDictionary<string, string>? BuildPermisoMap(string controllerName, SecuritySchemaInfo? security)
    {
        if (security is not { IsEnabled: true }) return null;

        var mapa = new Dictionary<string, string>();
        foreach (var perm in security.Permissions)
        {
            if (!perm.Controller.Equals(controllerName, StringComparison.OrdinalIgnoreCase)) continue;
            if (!mapa.ContainsKey(perm.Action))
            {
                mapa[perm.Action] = perm.Code;
            }
        }
        return mapa.Count > 0 ? mapa : null;
    }
}
