using Microsoft.Data.SqlClient;
using CodeGenerator.CLI.Models;

namespace CodeGenerator.CLI.Services;

public class SqlSchemaReader
{
    public static DatabaseSchema ReadSchema(string connectionString)
    {
        var schema = new DatabaseSchema();

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        schema.DatabaseName = connection.Database;

        Console.WriteLine($"  Conectado exitosamente a la Base de Datos: {connection.Database}");

        // 1. Obtener Tablas Base
        var tablesQuery = @"
            SELECT TABLE_SCHEMA, TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE' 
              AND TABLE_NAME NOT IN ('sysdiagrams', '__EFMigrationsHistory')
            ORDER BY TABLE_NAME;";

        using (var cmd = new SqlCommand(tablesQuery, connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var table = new TableMetadata
                {
                    SchemaName = reader.GetString(0),
                    TableName = reader.GetString(1)
                };
                schema.Tables.Add(table);
            }
        }

        // 2. Obtener Columnas y Claves Primarias para cada tabla
        foreach (var table in schema.Tables)
        {
            var columnsQuery = @"
                SELECT 
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity,
                    CASE WHEN kcu.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT k.TABLE_SCHEMA, k.TABLE_NAME, k.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE k ON tc.CONSTRAINT_NAME = k.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                ) kcu ON c.TABLE_SCHEMA = kcu.TABLE_SCHEMA 
                     AND c.TABLE_NAME = kcu.TABLE_NAME 
                     AND c.COLUMN_NAME = kcu.COLUMN_NAME
                WHERE c.TABLE_SCHEMA = @SchemaName AND c.TABLE_NAME = @TableName
                ORDER BY c.ORDINAL_POSITION;";

            using var cmd = new SqlCommand(columnsQuery, connection);
            cmd.Parameters.AddWithValue("@SchemaName", table.SchemaName);
            cmd.Parameters.AddWithValue("@TableName", table.TableName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var colName = reader.GetString(0);
                var sqlType = reader.GetString(1);
                var isNullable = reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase);
                var maxLength = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                var isIdentity = !reader.IsDBNull(4) && reader.GetInt32(4) == 1;
                var isPk = !reader.IsDBNull(5) && reader.GetInt32(5) == 1;

                var column = new ColumnMetadata
                {
                    ColumnName = colName,
                    SqlDataType = sqlType,
                    CsDataType = MapSqlTypeToCsType(sqlType, isNullable),
                    IsNullable = isNullable,
                    IsPrimaryKey = isPk,
                    IsIdentity = isIdentity,
                    MaxLength = maxLength
                };

                table.Columns.Add(column);
            }
        }

        // 3. Obtener Foreign Keys y determinar columnas de visualización
        var fkQuery = @"
            SELECT 
                OBJECT_SCHEMA_NAME(fk.parent_object_id) AS ParentSchema,
                OBJECT_NAME(fk.parent_object_id) AS ParentTable,
                cp.name AS ParentColumn,
                OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS ReferencedSchema,
                OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
                cr.name AS ReferencedColumn
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
            INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id";

        var fkList = new List<(string ParentSchema, string ParentTable, string ParentColumn, string RefSchema, string RefTable, string RefColumn)>();

        using (var cmd = new SqlCommand(fkQuery, connection))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                fkList.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)));
            }
        }

        // Asignar FK info a las columnas
        foreach (var fk in fkList)
        {
            var parentTable = schema.Tables.FirstOrDefault(t =>
                t.SchemaName == fk.ParentSchema && t.TableName == fk.ParentTable);
            if (parentTable == null) continue;

            var parentColumn = parentTable.Columns.FirstOrDefault(c =>
                c.ColumnName == fk.ParentColumn);
            if (parentColumn == null) continue;

            parentColumn.IsForeignKey = true;
            parentColumn.FkReferencedSchema = fk.RefSchema;
            parentColumn.FkReferencedTable = fk.RefTable;
            parentColumn.FkReferencedColumn = fk.RefColumn;

            // Encontrar display column en la tabla referenciada
            var refTable = schema.Tables.FirstOrDefault(t =>
                t.SchemaName == fk.RefSchema && t.TableName == fk.RefTable);
            if (refTable != null)
            {
                parentColumn.FkReferencedDisplayColumn = FindDisplayColumn(refTable);
            }
        }

        // Establecer DisplayColumn para cada tabla
        foreach (var table in schema.Tables)
        {
            table.DisplayColumn = FindDisplayColumn(table);
        }

        return schema;
    }

    public static string FindDisplayColumn(TableMetadata table)
    {
        var candidates = new[] { "Nombre", "Name", "Descripcion", "Descripcion", "RazonSocial", "Titulo", "ApellidoYNombre", "NombreCompleto", "Razon_Social", "FullName" };
        foreach (var c in candidates)
        {
            var match = table.Columns.FirstOrDefault(col => col.ColumnName.Equals(c, StringComparison.OrdinalIgnoreCase));
            if (match != null) return c;
        }

        var stringCol = table.Columns.FirstOrDefault(col =>
            !col.IsPrimaryKey && !col.IsForeignKey &&
            (col.CsDataType.StartsWith("string") || col.SqlDataType.Contains("varchar") || col.SqlDataType.Contains("char") || col.SqlDataType.Contains("text")));
        if (stringCol != null) return stringCol.ColumnName;

        return table.KeyColumn.ColumnName;
    }

    public static string MapSqlTypeToCsType(string sqlType, bool isNullable)
    {
        var baseType = sqlType.ToLowerInvariant() switch
        {
            "bigint" => "long",
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => "byte[]",
            "bit" => "bool",
            "char" or "nchar" or "nvarchar" or "varchar" or "text" or "ntext" or "xml" => "string",
            "date" or "datetime" or "datetime2" or "smalldatetime" => "DateTime",
            "datetimeoffset" => "DateTimeOffset",
            "decimal" or "money" or "numeric" or "smallmoney" => "decimal",
            "float" => "double",
            "int" => "int",
            "real" => "float",
            "smallint" => "short",
            "time" => "TimeSpan",
            "tinyint" => "byte",
            "uniqueidentifier" => "Guid",
            _ => "string"
        };

        if (isNullable && baseType != "string" && baseType != "byte[]")
        {
            return $"{baseType}?";
        }

        if (isNullable && baseType == "string")
        {
            return "string?";
        }

        return baseType;
    }
}
