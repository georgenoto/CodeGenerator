namespace CodeGenerator.CLI.Configuration;

public class GeneratorOptions
{
    public string ProjectName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string DbContextName { get; set; } = "AppDbContext";
    public string TargetFramework { get; set; } = "net8.0";
    public string? CascadeConfigPath { get; set; }

    public string EntidadesProjectName => $"{ProjectName}.Entidades";
    public string DatosProjectName => $"{ProjectName}.Datos";
    public string ServiciosProjectName => $"{ProjectName}.Servicios";
    public string WebProjectName => $"{ProjectName}.Web";
    public string WebApiProjectName => $"{ProjectName}.WebApi";

    public string EntidadesPath => Path.Combine(OutputPath, EntidadesProjectName);
    public string DatosPath => Path.Combine(OutputPath, DatosProjectName);
    public string ServiciosPath => Path.Combine(OutputPath, ServiciosProjectName);
    public string WebPath => Path.Combine(OutputPath, WebProjectName);
    public string WebApiPath => Path.Combine(OutputPath, WebApiProjectName);
}
