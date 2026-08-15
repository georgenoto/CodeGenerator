namespace CodeGenerator.CLI.Models;

using CodeGenerator.CLI.Services;

public class SecurityPermissionInfo
{
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class SecuritySchemaInfo
{
    public bool IsEnabled { get; set; }
    public string SchemaName { get; set; } = "Seguridad";
    public string SchemaNamespace => SchemaHelper.ToNamespace(SchemaName);

    public string UsuariosTable { get; set; } = "Usuarios";
    public string RolesTable { get; set; } = "Roles";
    public string UsuarioRolTable { get; set; } = "UsuarioRol";
    public string ModulosTable { get; set; } = "Modulos";
    public string OpcionesTable { get; set; } = "Opciones";
    public string PermisosTable { get; set; } = "Permisos";
    public string RolPermisoTable { get; set; } = "RolPermiso";
    public string AuditoriaTable { get; set; } = "Auditoria";
    public string RefreshTokensTable { get; set; } = "RefreshTokens";

    public List<SecurityPermissionInfo> Permissions { get; set; } = new();
}