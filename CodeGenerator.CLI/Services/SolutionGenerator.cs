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
        Console.WriteLine("[1/7] Creando archivos de proyecto (.csproj)...");
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

        // 3. Crear archivo de solución .sln
        Console.WriteLine("[2/7] Generando solución .NET (.sln)...");
        ProcessRunner.RunCommand("dotnet", "new sln -n " + _options.ProjectName, _options.OutputPath);
        
        var entidadesCsproj = Path.Combine(_options.EntidadesProjectName, $"{_options.EntidadesProjectName}.csproj");
        var datosCsproj = Path.Combine(_options.DatosProjectName, $"{_options.DatosProjectName}.csproj");
        var serviciosCsproj = Path.Combine(_options.ServiciosProjectName, $"{_options.ServiciosProjectName}.csproj");
        var webCsproj = Path.Combine(_options.WebProjectName, $"{_options.WebProjectName}.csproj");
        var webApiCsproj = Path.Combine(_options.WebApiProjectName, $"{_options.WebApiProjectName}.csproj");

        ProcessRunner.RunCommand("dotnet", $"sln add \"{entidadesCsproj}\" \"{datosCsproj}\" \"{serviciosCsproj}\" \"{webCsproj}\" \"{webApiCsproj}\"", _options.OutputPath);

        // 4. Extraer Esquema de la Base de Datos nativamente
        Console.WriteLine("[3/8] Leyendo tablas, columnas y tipos desde SQL Server...");
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

        // 5. Cargar configuración manual de cascadas (opcional)
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
                Console.WriteLine($"  Cargando configuración de cascadas desde: {cascadePath}");
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
        Console.WriteLine("[4/8] Generando modelos POCO en la capa de Entidades...");
        var entities = EntityGenerator.GenerateEntities(_options.EntidadesProjectName, _options.EntidadesPath, schema, manualCascades);

        // 7. Generar Capa de Datos (DbContext + Repositorios)
        Console.WriteLine("[5/8] Generando DbContext y Repositorios en la capa de Datos...");
        DbContextGenerator.GenerateDbContext(_options.DatosProjectName, _options.EntidadesProjectName, _options.DatosPath, _options.DbContextName, schema);
        
        var reposDir = Path.Combine(_options.DatosPath, "Repositories");
        Directory.CreateDirectory(reposDir);
        File.WriteAllText(Path.Combine(reposDir, "IRepository.cs"), RepositoryTemplates.GetIRepositoryInterface(_options.DatosProjectName));
        File.WriteAllText(Path.Combine(reposDir, "Repository.cs"), RepositoryTemplates.GetRepositoryImplementation(_options.DatosProjectName, _options.DbContextName));

        // 7. Generar DTOs, Mappings y Servicios en la capa de Servicios (agrupado por esquema)
        Console.WriteLine("[6/8] Generando DTOs, Extensiones de Mapeo y Servicios...");
        var serviciosCommonDir = Path.Combine(_options.ServiciosPath, "Services", "Common");
        Directory.CreateDirectory(serviciosCommonDir);
        File.WriteAllText(Path.Combine(serviciosCommonDir, "IService.cs"), ServiceTemplates.GetGenericServiceInterface(_options.ServiciosProjectName));

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
        Console.WriteLine("[7/8] Generando proyecto Web MVC (Controladores, Vistas, Layout)...");
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
                File.WriteAllText(
                    Path.Combine(schemaControllersDir, $"{entity.Name}Controller.cs"),
                    ControllerTemplates.GetEntityController(_options.WebProjectName, _options.ServiciosProjectName, entity));
            }
        }

        // HomeController (raíz, sin esquema)
        File.WriteAllText(
            Path.Combine(rootControllersDir, "HomeController.cs"),
            ControllerTemplates.GetHomeController(_options.WebProjectName, _options.ServiciosProjectName));

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
            ViewTemplates.GetLayoutView(_options.WebProjectName, entities));
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

        // wwwroot (archivos estáticos)
        var cssDir = Path.Combine(_options.WebPath, "wwwroot", "css");
        Directory.CreateDirectory(cssDir);
        File.WriteAllText(Path.Combine(cssDir, "site.css"), ViewTemplates.GetSiteCss());

        var jsDir = Path.Combine(_options.WebPath, "wwwroot", "js");
        Directory.CreateDirectory(jsDir);
        File.WriteAllText(Path.Combine(jsDir, "site.js"), ViewTemplates.GetSiteJs());

        // Program.cs
        File.WriteAllText(
            Path.Combine(_options.WebPath, "Program.cs"),
            ProgramCsTemplate.GetProgramCs(_options.WebProjectName, _options.DatosProjectName, _options.ServiciosProjectName, _options.DbContextName, entities));

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
        Console.WriteLine("[8/8] Generando proyecto Web API REST (Controladores, Swagger)...");
        var apiControllersDir = Path.Combine(_options.WebApiPath, "Controllers");
        Directory.CreateDirectory(apiControllersDir);

        foreach (var schemaGroup in entitiesBySchema)
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            var schemaControllersDir = Path.Combine(apiControllersDir, schemaNs);
            Directory.CreateDirectory(schemaControllersDir);

            foreach (var entity in schemaGroup)
            {
                File.WriteAllText(
                    Path.Combine(schemaControllersDir, $"{entity.Name}Controller.cs"),
                    ControllerTemplates.GetEntityApiController(_options.WebApiProjectName, _options.ServiciosProjectName, entity));
            }
        }

        // Program.cs
        File.WriteAllText(
            Path.Combine(_options.WebApiPath, "Program.cs"),
            ProgramCsTemplate.GetWebApiProgramCs(_options.WebApiProjectName, _options.DatosProjectName, _options.ServiciosProjectName, _options.DbContextName, entities));

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
        Console.WriteLine($" ¡Generación completada exitosamente!");
        Console.WriteLine($" Ubicación de la Solución: {_options.OutputPath}");
        Console.WriteLine($"=======================================================\n");

        return true;
    }
}
