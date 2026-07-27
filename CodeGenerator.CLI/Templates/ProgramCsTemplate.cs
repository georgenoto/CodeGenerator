using CodeGenerator.CLI.Services;
using System.Text;

namespace CodeGenerator.CLI.Templates;

public static class ProgramCsTemplate
{
    public static string GetProgramCs(string webNamespace, string datosNamespace, string serviciosNamespace, string dbContextName, List<EntityInfo> entities)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine($"using {datosNamespace};");
        sb.AppendLine($"using {datosNamespace}.Repositories;");
        foreach (var schemaGroup in entities.GroupBy(e => e.SchemaName))
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            sb.AppendLine($"using {serviciosNamespace}.{schemaNs}.Services;");
        }
        sb.AppendLine();
        sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
        sb.AppendLine();
        sb.AppendLine("// 1. Add DbContext");
        sb.AppendLine($"var connectionString = builder.Configuration.GetConnectionString(\"DefaultConnection\")");
        sb.AppendLine($"    ?? throw new InvalidOperationException(\"ConnectionString 'DefaultConnection' not found.\");");
        sb.AppendLine();
        sb.AppendLine($"builder.Services.AddDbContext<{dbContextName}>(options =>");
        sb.AppendLine("    options.UseSqlServer(connectionString));");
        sb.AppendLine();
        sb.AppendLine("// 2. Register Repositories");
        sb.AppendLine("builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));");
        sb.AppendLine();
        sb.AppendLine("// 3. Register Application Services");
        foreach (var entity in entities)
        {
            sb.AppendLine($"builder.Services.AddScoped<I{entity.Name}Service, {entity.Name}Service>();");
        }
        sb.AppendLine();
        sb.AppendLine("// 4. Add MVC Controllers and Views");
        sb.AppendLine("builder.Services.AddControllersWithViews();");
        sb.AppendLine();
        sb.AppendLine("var app = builder.Build();");
        sb.AppendLine();
        sb.AppendLine("// Configure HTTP Request Pipeline");
        sb.AppendLine("if (!app.Environment.IsDevelopment())");
        sb.AppendLine("{");
        sb.AppendLine("    app.UseExceptionHandler(\"/Home/Error\");");
        sb.AppendLine("    app.UseHsts();");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("app.UseHttpsRedirection();");
        sb.AppendLine("app.UseStaticFiles();");
        sb.AppendLine();
        sb.AppendLine("app.UseRouting();");
        sb.AppendLine();
        sb.AppendLine("app.UseAuthorization();");
        sb.AppendLine();
        sb.AppendLine("app.MapControllerRoute(");
        sb.AppendLine("    name: \"default\",");
        sb.AppendLine("    pattern: \"{controller=Home}/{action=Index}/{id?}\");");
        sb.AppendLine();
        sb.AppendLine("app.Run();");

        return sb.ToString();
    }

    public static string GetAppSettingsJson(string connectionString) => $@"{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*"",
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""{connectionString.Replace("\\", "\\\\").Replace("\"", "\\\"")}""
  }}
}}
";

    public static string GetLaunchSettingsJson(string? launchUrl = null)
    {
        var url = launchUrl ?? "";
        return @"{
  ""$schema"": ""http://json.schemastore.org/launchsettings.json"",
  ""profiles"": {
    ""http"": {
      ""commandName"": ""Project"",
      ""dotnetRunMessages"": true,
      ""launchBrowser"": true,
      ""launchUrl"": """ + url + @""",
      ""applicationUrl"": ""http://localhost:5000"",
      ""environmentVariables"": {
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }
    },
    ""https"": {
      ""commandName"": ""Project"",
      ""dotnetRunMessages"": true,
      ""launchBrowser"": true,
      ""launchUrl"": """ + url + @""",
      ""applicationUrl"": ""https://localhost:7001;http://localhost:5000"",
      ""environmentVariables"": {
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }
    }
  }
}
";
    }

    public static string GetWebApiProgramCs(string webApiNamespace, string datosNamespace, string serviciosNamespace, string dbContextName, List<EntityInfo> entities)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine($"using {datosNamespace};");
        sb.AppendLine($"using {datosNamespace}.Repositories;");
        foreach (var schemaGroup in entities.GroupBy(e => e.SchemaName))
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            sb.AppendLine($"using {serviciosNamespace}.{schemaNs}.Services;");
        }
        sb.AppendLine();
        sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
        sb.AppendLine();
        sb.AppendLine("// 1. Add DbContext");
        sb.AppendLine($"var connectionString = builder.Configuration.GetConnectionString(\"DefaultConnection\")");
        sb.AppendLine($"    ?? throw new InvalidOperationException(\"ConnectionString 'DefaultConnection' not found.\");");
        sb.AppendLine();
        sb.AppendLine($"builder.Services.AddDbContext<{dbContextName}>(options =>");
        sb.AppendLine("    options.UseSqlServer(connectionString));");
        sb.AppendLine();
        sb.AppendLine("// 2. Register Repositories");
        sb.AppendLine("builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));");
        sb.AppendLine();
        sb.AppendLine("// 3. Register Application Services");
        foreach (var entity in entities)
        {
            sb.AppendLine($"builder.Services.AddScoped<I{entity.Name}Service, {entity.Name}Service>();");
        }
        sb.AppendLine();
        sb.AppendLine("// 4. Add Controllers & Swagger");
        sb.AppendLine("builder.Services.AddControllers();");
        sb.AppendLine("builder.Services.AddEndpointsApiExplorer();");
        sb.AppendLine("builder.Services.AddSwaggerGen();");
        sb.AppendLine();
        sb.AppendLine("var app = builder.Build();");
        sb.AppendLine();
        sb.AppendLine("// Configure HTTP Request Pipeline");
        sb.AppendLine("if (app.Environment.IsDevelopment())");
        sb.AppendLine("{");
        sb.AppendLine("    app.UseSwagger();");
        sb.AppendLine("    app.UseSwaggerUI();");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("app.UseHttpsRedirection();");
        sb.AppendLine("app.UseAuthorization();");
        sb.AppendLine("app.MapControllers();");
        sb.AppendLine();
        sb.AppendLine("app.Run();");

        return sb.ToString();
    }
}
