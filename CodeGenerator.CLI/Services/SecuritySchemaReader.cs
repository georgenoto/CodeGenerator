using Microsoft.Data.SqlClient;
using CodeGenerator.CLI.Models;

namespace CodeGenerator.CLI.Services;

public class SecuritySchemaReader
{
    /// <summary>
    /// Detecta el esquema de seguridad (Usuarios, Roles, Modulos, Opciones, Permisos, etc.)
    /// y lee los permisos existentes en la BD para poder decorar los controladores.
    /// </summary>
    public static SecuritySchemaInfo ReadSecuritySchema(string connectionString, DatabaseSchema schema)
    {
        var result = new SecuritySchemaInfo();

        // Localizar tablas de seguridad por nombre dentro del esquema que contiene "Usuarios"
        var seguridadSchema = schema.Tables
            .Where(t => t.SchemaName.Equals("Seguridad", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var usuarioTable = seguridadSchema.FirstOrDefault(t => t.TableName.Equals("Usuarios", StringComparison.OrdinalIgnoreCase));
        var rolesTable = seguridadSchema.FirstOrDefault(t => t.TableName.Equals("Roles", StringComparison.OrdinalIgnoreCase));
        var usuarioRolTable = seguridadSchema.FirstOrDefault(t => t.TableName.Equals("UsuarioRol", StringComparison.OrdinalIgnoreCase));
        var modulosTable = seguridadSchema.FirstOrDefault(t => t.TableName.Equals("Modulos", StringComparison.OrdinalIgnoreCase));
        var opcionesTable = seguridadSchema.FirstOrDefault(t => t.TableName.Equals("Opciones", StringComparison.OrdinalIgnoreCase));
        var permisosTable = seguridadSchema.FirstOrDefault(t => t.TableName.Equals("Permisos", StringComparison.OrdinalIgnoreCase));
        var rolPermisoTable = seguridadSchema.FirstOrDefault(t => t.TableName.Equals("RolPermiso", StringComparison.OrdinalIgnoreCase));

        if (usuarioTable == null || rolesTable == null || usuarioRolTable == null ||
            modulosTable == null || opcionesTable == null || permisosTable == null || rolPermisoTable == null)
        {
            return result;
        }

        result.IsEnabled = true;
        result.SchemaName = usuarioTable.SchemaName;
        result.UsuariosTable = usuarioTable.TableName;
        result.RolesTable = rolesTable.TableName;
        result.UsuarioRolTable = usuarioRolTable.TableName;
        result.ModulosTable = modulosTable.TableName;
        result.OpcionesTable = opcionesTable.TableName;
        result.PermisosTable = permisosTable.TableName;
        result.RolPermisoTable = rolPermisoTable.TableName;

        var auditoriaTable = seguridadSchema.FirstOrDefault(t => t.TableName.Equals("Auditoria", StringComparison.OrdinalIgnoreCase));
        var refreshTokensTable = seguridadSchema.FirstOrDefault(t => t.TableName.Equals("RefreshTokens", StringComparison.OrdinalIgnoreCase));
        result.AuditoriaTable = auditoriaTable?.TableName ?? result.AuditoriaTable;
        result.RefreshTokensTable = refreshTokensTable?.TableName ?? result.RefreshTokensTable;

        // Leer permisos existentes: {Opciones.Nombre} + {Accion} -> {Permisos.Descripcion}
        // Ej.: Opciones.Nombre = "Usuarios", Permisos.Codigo = "/Usuarios/Index", Descripcion = "USUARIOS_VER"
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var permisosQuery = $@"
                SELECT o.Nombre AS OpcionNombre, p.Codigo AS Ruta, p.Descripcion AS CodigoPermiso
                FROM [{result.SchemaName}].[{result.PermisosTable}] p
                INNER JOIN [{result.SchemaName}].[{result.OpcionesTable}] o ON p.IdOpcion = o.Id
                WHERE p.Activo = 1 AND o.Activo = 1
                  AND LTRIM(RTRIM(ISNULL(p.Descripcion, ''))) <> ''";

            using (var cmd = new SqlCommand(permisosQuery, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var opcionNombre = reader.GetString(0).Trim();
                    var ruta = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                    var codigoPermiso = reader.GetString(2).Trim();

                    // Parsear ruta "/Usuarios/Index" -> Controller="Usuarios", Action="Index"
                    var partes = ruta.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var controller = partes.Length > 0 ? partes[0] : opcionNombre;
                    var action = partes.Length > 1 ? partes[1] : "Index";

                    result.Permissions.Add(new SecurityPermissionInfo
                    {
                        Controller = controller,
                        Action = action,
                        Code = codigoPermiso
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [AVISO] No se pudieron leer los permisos de seguridad: {ex.Message}");
        }

        return result;
    }
}