namespace CodeGenerator.CLI.Models;

public class CascadeConfig
{
    public string Entity { get; set; } = string.Empty;
    public string ChildProperty { get; set; } = string.Empty;
    public string ParentProperty { get; set; } = string.Empty;
}
