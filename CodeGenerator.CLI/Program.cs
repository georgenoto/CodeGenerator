using CodeGenerator.CLI.Configuration;
using CodeGenerator.CLI.Services;

Console.WriteLine(@"
===========================================================
  Generador de Código .NET 8 (Clean Architecture 4-Capas)
  Entidades | Datos | Servicios | Web (API REST + DTOs)
===========================================================
");

var options = new GeneratorOptions();
options.ProjectName = "GNOTO.IntegradorContable";
options.ConnectionString = "Data Source=BBDDPBA02\\INTEGRACION; Initial Catalog=dbContabilidad; User ID=userDesarrollo; Password=desarrollo;TrustServerCertificate=True;";
options.OutputPath = "C:\\D\\Proyecto Generador\\";
options.CascadeConfigPath = "cascades.json";
// Interactivo o por argumentos
if (args.Length > 0)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--connection" && i + 1 < args.Length) options.ConnectionString = args[++i];
        else if (args[i] == "--name" && i + 1 < args.Length) options.ProjectName = args[++i];
        else if (args[i] == "--output" && i + 1 < args.Length) options.OutputPath = args[++i];
        else if (args[i] == "--dbcontext" && i + 1 < args.Length) options.DbContextName = args[++i];
        else if (args[i] == "--cascades" && i + 1 < args.Length) options.CascadeConfigPath = args[++i];
    }
}

if (string.IsNullOrWhiteSpace(options.ProjectName))
{
    Console.Write("Ingrese el Nombre Base del Proyecto (ej. MiEmpresa.Facturacion): ");
    options.ProjectName = Console.ReadLine()?.Trim() ?? "MiProyecto";
    if (string.IsNullOrWhiteSpace(options.ProjectName)) options.ProjectName = "MiProyecto";
}

if (string.IsNullOrWhiteSpace(options.ConnectionString))
{
    Console.Write("Ingrese la Cadena de Conexión SQL Server: ");
    options.ConnectionString = Console.ReadLine()?.Trim() ?? "";
}

if (string.IsNullOrWhiteSpace(options.OutputPath))
{
    var defaultOutput = Path.Combine(Directory.GetCurrentDirectory(), "output", options.ProjectName);
    Console.Write($"Ingrese el Directorio de Salida [Por defecto: {defaultOutput}]: ");
    var inputPath = Console.ReadLine()?.Trim();
    options.OutputPath = string.IsNullOrWhiteSpace(inputPath) ? defaultOutput : inputPath;
}

if (string.IsNullOrWhiteSpace(options.ConnectionString))
{
    Console.WriteLine("\n[ERROR] Debe especificar una Cadena de Conexión SQL válida.");
    return;
}

try
{
    var generator = new SolutionGenerator(options);
    generator.Generate();
}
catch (Exception ex)
{
    Console.WriteLine($"\n[ERROR INESPERADO]: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
