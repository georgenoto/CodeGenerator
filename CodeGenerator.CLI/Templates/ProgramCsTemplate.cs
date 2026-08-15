using CodeGenerator.CLI.Services;
using System.Text;

namespace CodeGenerator.CLI.Templates;

public static class ProgramCsTemplate
{
    public static string GetProgramCs(string webNamespace, string datosNamespace, string serviciosNamespace, string dbContextName, List<EntityInfo> entities, CodeGenerator.CLI.Models.SecuritySchemaInfo? security = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine($"using {datosNamespace};");
        sb.AppendLine($"using {datosNamespace}.Repositories;");
        foreach (var schemaGroup in entities.GroupBy(e => e.SchemaName))
        {
            if (security is { IsEnabled: true } && schemaGroup.Key == security.SchemaName) continue;
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            sb.AppendLine($"using {serviciosNamespace}.{schemaNs}.Services;");
        }
        if (security is { IsEnabled: true })
        {
            sb.AppendLine($"using {serviciosNamespace}.{security.SchemaNamespace}.Services;");
            sb.AppendLine($"using {serviciosNamespace}.Services.Common;");
            sb.AppendLine("using Microsoft.AspNetCore.Authentication.Cookies;");
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
        if (security is { IsEnabled: true })
        {
            sb.AppendLine($"builder.Services.AddScoped<JwtTokenService>();");
            sb.AppendLine($"builder.Services.AddScoped<IAuthService, AuthService>();");
        }
        sb.AppendLine();
        sb.AppendLine("// 4. Add MVC Controllers and Views");
        sb.AppendLine("builder.Services.AddControllersWithViews();");
        if (security is { IsEnabled: true })
        {
            sb.AppendLine();
            sb.AppendLine("// 5. Add Cookie Authentication");
            sb.AppendLine("builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)");
            sb.AppendLine("    .AddCookie(options =>");
            sb.AppendLine("    {");
            sb.AppendLine("        options.LoginPath = \"/Account/Login\";");
            sb.AppendLine("        options.AccessDeniedPath = \"/Account/AccessDenied\";");
            sb.AppendLine("        options.ExpireTimeSpan = TimeSpan.FromHours(8);");
            sb.AppendLine("        options.SlidingExpiration = true;");
            sb.AppendLine("    });");
        }
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
        if (security is { IsEnabled: true })
        {
            sb.AppendLine();
            sb.AppendLine("app.UseAuthentication();");
        }
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

    public static string GetAppSettingsJson(string connectionString, bool withSecurity = false) => $@"{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*"",
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""{connectionString.Replace("\\", "\\\\").Replace("\"", "\\\"")}""
  }},
  ""Jwt"": {{
    ""Secret"": ""cambia-esta-clave-secreta-de-64-caracteres-minimo-1234567890-abcdef"",
    ""Issuer"": ""GNOTO.Security"",
    ""Audience"": ""GNOTO.Security.Clientes"",
    ""ExpiresMinutes"": ""60""
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

    public static string GetWebApiProgramCs(string webApiNamespace, string datosNamespace, string serviciosNamespace, string dbContextName, List<EntityInfo> entities, CodeGenerator.CLI.Models.SecuritySchemaInfo? security = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine($"using {datosNamespace};");
        sb.AppendLine($"using {datosNamespace}.Repositories;");
        foreach (var schemaGroup in entities.GroupBy(e => e.SchemaName))
        {
            if (security is { IsEnabled: true } && schemaGroup.Key == security.SchemaName) continue;
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            sb.AppendLine($"using {serviciosNamespace}.{schemaNs}.Services;");
        }
        if (security is { IsEnabled: true })
        {
            sb.AppendLine($"using {serviciosNamespace}.{security.SchemaNamespace}.Services;");
            sb.AppendLine($"using {serviciosNamespace}.Services.Common;");
            sb.AppendLine("using Microsoft.AspNetCore.Authentication.JwtBearer;");
            sb.AppendLine("using Microsoft.IdentityModel.Tokens;");
            sb.AppendLine("using Microsoft.OpenApi.Models;");
            sb.AppendLine("using System.Text;");
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
        if (security is { IsEnabled: true })
        {
            sb.AppendLine($"builder.Services.AddScoped<JwtTokenService>();");
            sb.AppendLine($"builder.Services.AddScoped<IAuthService, AuthService>();");
        }
        sb.AppendLine();
        sb.AppendLine("// 4. Add Controllers & Swagger");
        sb.AppendLine("builder.Services.AddControllers();");
        if (security is { IsEnabled: true })
        {
            sb.AppendLine("builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)");
            sb.AppendLine("    .AddJwtBearer(options =>");
            sb.AppendLine("    {");
            sb.AppendLine("        options.TokenValidationParameters = new TokenValidationParameters");
            sb.AppendLine("        {");
            sb.AppendLine("            ValidateIssuer = true,");
            sb.AppendLine("            ValidIssuer = builder.Configuration[\"Jwt:Issuer\"],");
            sb.AppendLine("            ValidateAudience = true,");
            sb.AppendLine("            ValidAudience = builder.Configuration[\"Jwt:Audience\"],");
            sb.AppendLine("            ValidateIssuerSigningKey = true,");
            sb.AppendLine("            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration[\"Jwt:Secret\"])),");
            sb.AppendLine("            ValidateLifetime = true,");
            sb.AppendLine("            ClockSkew = TimeSpan.Zero");
            sb.AppendLine("        };");
            sb.AppendLine("    });");
        }
        sb.AppendLine("builder.Services.AddEndpointsApiExplorer();");
        sb.AppendLine("builder.Services.AddSwaggerGen(c =>");
        sb.AppendLine("{");
        if (security is { IsEnabled: true })
        {
            sb.AppendLine("    c.AddSecurityDefinition(\"Bearer\", new OpenApiSecurityScheme");
            sb.AppendLine("    {");
            sb.AppendLine("        Name = \"Authorization\",");
            sb.AppendLine("        Type = SecuritySchemeType.Http,");
            sb.AppendLine("        Scheme = \"bearer\",");
            sb.AppendLine("        BearerFormat = \"JWT\",");
            sb.AppendLine("        In = ParameterLocation.Header,");
            sb.AppendLine("        Description = \"Ingrese el token JWT (sin la palabra Bearer).\"");
            sb.AppendLine("    });");
            sb.AppendLine("    c.AddSecurityRequirement(new OpenApiSecurityRequirement");
            sb.AppendLine("    {");
            sb.AppendLine("        {");
            sb.AppendLine("            new OpenApiSecurityScheme");
            sb.AppendLine("            {");
            sb.AppendLine("                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = \"Bearer\" }");
            sb.AppendLine("            },");
            sb.AppendLine("            Array.Empty<string>()");
            sb.AppendLine("        }");
            sb.AppendLine("    });");
        }
        sb.AppendLine("});");
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
        if (security is { IsEnabled: true })
        {
            sb.AppendLine();
            sb.AppendLine("app.UseAuthentication();");
        }
        sb.AppendLine("app.UseAuthorization();");
        sb.AppendLine("app.MapControllers();");
        sb.AppendLine();
        sb.AppendLine("app.Run();");

        return sb.ToString();
    }
}
