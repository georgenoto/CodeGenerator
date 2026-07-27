using System.Text;
using CodeGenerator.CLI.Models;

namespace CodeGenerator.CLI.Services;

public class DbContextGenerator
{
    public static void GenerateDbContext(string datosNamespace, string entidadesNamespace, string datosPath, string dbContextName, DatabaseSchema schema)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        foreach (var schemaGroup in schema.Tables.GroupBy(t => t.SchemaName))
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            sb.AppendLine($"using {entidadesNamespace}.{schemaNs};");
        }
        sb.AppendLine();
        sb.AppendLine($"namespace {datosNamespace};");
        sb.AppendLine();
        sb.AppendLine($"public partial class {dbContextName} : DbContext");
        sb.AppendLine("{");

        // Constructors
        sb.AppendLine($"    public {dbContextName}()");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public {dbContextName}(DbContextOptions<{dbContextName}> options) : base(options)");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine();

        // DbSets
        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"    public virtual DbSet<{table.EntityClassName}> {table.EntityClassName}s {{ get; set; }} = null!;");
        }

        sb.AppendLine();

        // OnModelCreating
        sb.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
        sb.AppendLine("    {");
        sb.AppendLine("        base.OnModelCreating(modelBuilder);");
        sb.AppendLine();

        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"        modelBuilder.Entity<{table.EntityClassName}>(entity =>");
            sb.AppendLine("        {");
            sb.AppendLine($"            entity.ToTable(\"{table.TableName}\", \"{table.SchemaName}\");");
            
            var keyCols = table.Columns.Where(c => c.IsPrimaryKey).ToList();
            if (keyCols.Count == 1)
            {
                sb.AppendLine($"            entity.HasKey(e => e.{keyCols[0].ColumnName});");
            }
            else if (keyCols.Count > 1)
            {
                var keysStr = string.Join(", ", keyCols.Select(k => $"e.{k.ColumnName}"));
                sb.AppendLine($"            entity.HasKey(e => new {{ {keysStr} }});");
            }

            foreach (var col in table.Columns)
            {
                if (col.IsIdentity)
                {
                    sb.AppendLine($"            entity.Property(e => e.{col.ColumnName}).UseIdentityColumn();");
                }
                if (col.MaxLength.HasValue && col.MaxLength.Value > 0 && col.CsDataType.StartsWith("string"))
                {
                    sb.AppendLine($"            entity.Property(e => e.{col.ColumnName}).HasMaxLength({col.MaxLength.Value});");
                }
            }

            sb.AppendLine("        });");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        var filePath = Path.Combine(datosPath, $"{dbContextName}.cs");
        File.WriteAllText(filePath, sb.ToString());
    }
}
