namespace CodeGenerator.CLI.Models;

public class ColumnMetadata
{
    public string ColumnName { get; set; } = string.Empty;
    public string SqlDataType { get; set; } = string.Empty;
    public string CsDataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsForeignKey { get; set; }
    public string? FkReferencedSchema { get; set; }
    public string? FkReferencedTable { get; set; }
    public string? FkReferencedColumn { get; set; }
    public string? FkReferencedDisplayColumn { get; set; }
    public int? MaxLength { get; set; }
}

public class TableMetadata
{
    public string TableName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public string EntityClassName => SanitizeIdentifier(TableName);
    public List<ColumnMetadata> Columns { get; set; } = new();
    public string DisplayColumn { get; set; } = "Id";

    public ColumnMetadata KeyColumn =>
        Columns.FirstOrDefault(c => c.IsPrimaryKey)
        ?? Columns.FirstOrDefault(c => c.ColumnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
        ?? Columns.FirstOrDefault(c => c.ColumnName.Equals($"{TableName}Id", StringComparison.OrdinalIgnoreCase))
        ?? Columns.FirstOrDefault()
        ?? new ColumnMetadata { ColumnName = "Id", CsDataType = "int", IsPrimaryKey = true };

    private static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Entity";
        
        // Remove spaces, brackets or non-alphanumeric chars
        var clean = System.Text.RegularExpressions.Regex.Replace(name, @"[^\w]", "");
        
        // Capitalize first letter
        if (clean.Length > 0 && char.IsLower(clean[0]))
        {
            clean = char.ToUpper(clean[0]) + clean.Substring(1);
        }
        
        return clean;
    }
}

public class DatabaseSchema
{
    public string DatabaseName { get; set; } = string.Empty;
    public List<TableMetadata> Tables { get; set; } = new();
}
