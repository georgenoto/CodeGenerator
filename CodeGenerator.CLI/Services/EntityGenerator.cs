using System.Text;
using CodeGenerator.CLI.Models;

namespace CodeGenerator.CLI.Services;

public class EntityGenerator
{
    public static List<EntityInfo> GenerateEntities(string entidadesNamespace, string entidadesPath, DatabaseSchema schema, List<CascadeConfig>? manualCascades = null)
    {
        var entityInfos = new List<EntityInfo>();

        var tablesBySchema = schema.Tables.GroupBy(t => t.SchemaName);

        foreach (var schemaGroup in tablesBySchema)
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            var schemaDir = Path.Combine(entidadesPath, schemaNs);
            Directory.CreateDirectory(schemaDir);

            foreach (var table in schemaGroup)
            {
                var entityInfo = new EntityInfo
                {
                    Name = table.EntityClassName,
                    SchemaName = table.SchemaName,
                    FilePath = Path.Combine(schemaDir, $"{table.EntityClassName}.cs"),
                    DisplayColumn = table.DisplayColumn
                };

                var sb = new StringBuilder();
                sb.AppendLine("#nullable enable");
                sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
                sb.AppendLine($"namespace {entidadesNamespace}.{schemaNs};");
                sb.AppendLine();
                sb.AppendLine($"public partial class {table.EntityClassName}");
                sb.AppendLine("{");

                foreach (var col in table.Columns)
                {
                    sb.AppendLine($"    public {col.CsDataType} {col.ColumnName} {{ get; set; }}");

                    var ep = new EntityProperty
                    {
                        Name = col.ColumnName,
                        Type = col.CsDataType,
                        IsKey = col.IsPrimaryKey,
                        IsNavigation = false,
                        IsNullable = col.IsNullable,
                        IsForeignKey = col.IsForeignKey
                    };

                    if (col.IsForeignKey)
                    {
                        ep.FkReferencedSchema = col.FkReferencedSchema;
                        ep.FkReferencedTable = col.FkReferencedTable;
                        ep.FkReferencedColumn = col.FkReferencedColumn;
                        ep.FkReferencedDisplayColumn = col.FkReferencedDisplayColumn;
                        ep.FkDisplayPropertyName = $"{col.FkReferencedTable}{col.FkReferencedDisplayColumn}";

                        var refSchemaNs = SchemaHelper.ToNamespace(col.FkReferencedSchema ?? "dbo");
                        sb.AppendLine();
                        sb.AppendLine($"    [ForeignKey(\"{col.ColumnName}\")]");                       
                        sb.AppendLine($"    public {refSchemaNs}.{col.FkReferencedTable}? {col.FkReferencedTable} {{ get; set; }}");
                    }

                    entityInfo.Properties.Add(ep);
                }

                sb.AppendLine("}");

                File.WriteAllText(entityInfo.FilePath, sb.ToString());
                entityInfos.Add(entityInfo);
            }
        }

        DetectCascadeDependencies(entityInfos, schema);
        ApplyManualCascades(entityInfos, schema, manualCascades);
        MarkCascadeEndpoints(entityInfos);
        return entityInfos;
    }

    private static void MarkCascadeEndpoints(List<EntityInfo> entities)
    {
        // Collect all (ReferencedTable, CascadeFilterProperty) pairs from cascades
        var neededEndpoints = new HashSet<(string Table, string FilterFk)>();
        foreach (var entity in entities)
        {
            foreach (var prop in entity.Properties)
            {
                if (!string.IsNullOrEmpty(prop.CascadeFilterProperty) && !string.IsNullOrEmpty(prop.FkReferencedTable))
                {
                    neededEndpoints.Add((prop.FkReferencedTable, prop.CascadeFilterProperty));
                }
            }
        }

        // Mark GenerateGetByEndpoint on matching FKs
        foreach (var entity in entities)
        {
            foreach (var prop in entity.Properties.Where(p => p.IsForeignKey))
            {
                if (neededEndpoints.Contains((entity.Name, prop.Name)))
                {
                    prop.GenerateGetByEndpoint = true;
                }
            }
        }
    }

    private static void DetectCascadeDependencies(List<EntityInfo> entities, DatabaseSchema schema)
    {
        foreach (var entity in entities)
        {
            var fkProps = entity.Properties.Where(p => p.IsForeignKey).ToList();
            if (fkProps.Count < 2) continue;
            foreach (var child in fkProps)
            {
                var childRefTable = schema.Tables.FirstOrDefault(t =>
                    t.EntityClassName == child.FkReferencedTable);
                if (childRefTable == null) continue;

                var childRefFks = childRefTable.Columns.Where(c => c.IsForeignKey).ToList();
                foreach (var refFk in childRefFks)
                {
                    var parent = fkProps.FirstOrDefault(p =>
                        p.Name != child.Name &&
                        SchemaHelper.ToNamespace(p.FkReferencedSchema ?? "dbo") == SchemaHelper.ToNamespace(refFk.FkReferencedSchema ?? "dbo") &&
                        p.FkReferencedTable == refFk.FkReferencedTable);
                    if (parent != null)
                    {
                        child.CascadeParentProperty = parent.Name;
                        child.CascadeFilterProperty = refFk.ColumnName;
                        break;
                    }
                }
            }
        }
    }

    private static void ApplyManualCascades(List<EntityInfo> entities, DatabaseSchema schema, List<CascadeConfig>? configs)
    {
        if (configs == null || configs.Count == 0) return;

        foreach (var cfg in configs)
        {
            var entity = entities.FirstOrDefault(e =>
                string.Equals(e.Name, cfg.Entity, StringComparison.OrdinalIgnoreCase));
            if (entity == null)
            {
                Console.WriteLine($"[WARN] CascadeConfig: entity '{cfg.Entity}' not found, skipping.");
                continue;
            }

            var child = entity.Properties.FirstOrDefault(p =>
                string.Equals(p.Name, cfg.ChildProperty, StringComparison.OrdinalIgnoreCase));
            if (child == null || !child.IsForeignKey)
            {
                Console.WriteLine($"[WARN] CascadeConfig: property '{cfg.ChildProperty}' not found or is not a FK in entity '{cfg.Entity}', skipping.");
                continue;
            }

            var parent = entity.Properties.FirstOrDefault(p =>
                string.Equals(p.Name, cfg.ParentProperty, StringComparison.OrdinalIgnoreCase));
            if (parent == null || !parent.IsForeignKey)
            {
                Console.WriteLine($"[WARN] CascadeConfig: parent property '{cfg.ParentProperty}' not found or is not a FK in entity '{cfg.Entity}', skipping.");
                continue;
            }

            var childRefTable = schema.Tables.FirstOrDefault(t =>
                t.EntityClassName == child.FkReferencedTable);
            if (childRefTable == null)
            {
                Console.WriteLine($"[WARN] CascadeConfig: referenced table '{child.FkReferencedTable}' for '{cfg.ChildProperty}' not found in schema.");
                continue;
            }

            var filterFk = childRefTable.Columns.FirstOrDefault(c =>
                c.IsForeignKey &&
                SchemaHelper.ToNamespace(c.FkReferencedSchema ?? "dbo") == SchemaHelper.ToNamespace(parent.FkReferencedSchema ?? "dbo") &&
                c.FkReferencedTable == parent.FkReferencedTable);

            if (filterFk == null)
            {
                Console.WriteLine($"[WARN] CascadeConfig: no FK in '{child.FkReferencedTable}' references the same table as '{cfg.ParentProperty}', skipping.");
                continue;
            }

            child.CascadeParentProperty = parent.Name;
            child.CascadeFilterProperty = filterFk.ColumnName;
            Console.WriteLine($"[CASCADE] {cfg.Entity}: {child.Name} depends on {parent.Name} (filter by {child.FkReferencedTable}.{filterFk.ColumnName})");
        }
    }
}
