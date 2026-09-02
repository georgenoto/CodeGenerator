using CodeGenerator.CLI.Models;
using CodeGenerator.CLI.Services;
using System.Text;

namespace CodeGenerator.CLI.Templates;

public static class SecurityTemplates
{
    // =====================================================================
    //  Capa Servicios
    // =====================================================================

    public static string GetPasswordHasher(string serviciosNamespace) => $@"namespace {serviciosNamespace}.Services.Common;

using System.Security.Cryptography;
using System.Text;

public static class PasswordHasher
{{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string HashPassword(string password)
    {{
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return string.Join('$', ""PBKDF2"", Iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }}

    public static bool VerifyPassword(string password, string stored, out bool requiresUpgrade)
    {{
        requiresUpgrade = false;
        if (string.IsNullOrEmpty(stored)) return false;

        // Hash legado en texto plano -> validar y marcar migración
        if (!stored.StartsWith(""PBKDF2$"", StringComparison.Ordinal))
        {{
            var ok = string.Equals(password, stored, StringComparison.Ordinal);
            requiresUpgrade = ok;
            return ok;
        }}

        var parts = stored.Split('$');
        if (parts.Length != 4) return false;
        if (!int.TryParse(parts[1], out var iterations)) return false;

        try
        {{
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            requiresUpgrade = iterations < Iterations;
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }}
        catch
        {{
            return false;
        }}
    }}
}}
";

    public static string GetJwtTokenService(string serviciosNamespace) => $@"namespace {serviciosNamespace}.Services.Common;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class JwtTokenService
{{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {{
        _config = config;
    }}

    public string CreateAccessToken(int userId, string usuario, string nombreCompleto, IEnumerable<string> roles, IEnumerable<string> permisos)
    {{
        var secret = _config[""Jwt:Secret""] ?? ""ChangeThisSecretKey123!ChangeThisSecretKey123!"";
        var issuer = _config[""Jwt:Issuer""] ?? """";
        var audience = _config[""Jwt:Audience""] ?? """";
        var expiresMinutes = int.TryParse(_config[""Jwt:ExpiresMinutes""], out var em) ? em : 60;

        var claims = new List<Claim>
        {{
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, usuario),
            new(""NombreCompleto"", nombreCompleto)
        }};
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permisos.Select(p => new Claim(""Permiso"", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }}

    public string GenerateRefreshToken() => Guid.NewGuid().ToString(""N"");
}}
";

    public static string GetLoginDto(string serviciosNamespace, SecuritySchemaInfo security) => $@"namespace {serviciosNamespace}.{security.SchemaNamespace}.DTOs.Auth;

using System.ComponentModel.DataAnnotations;

public class LoginDto
{{
    [Required(ErrorMessage = ""El usuario es obligatorio."")]
    public string? Usuario {{ get; set; }}

    [Required(ErrorMessage = ""La contraseña es obligatoria."")]
    [DataType(DataType.Password)]
    public string? Password {{ get; set; }}
}}
";

    public static string GetLoginResultDto(string serviciosNamespace, SecuritySchemaInfo security) => $@"namespace {serviciosNamespace}.{security.SchemaNamespace}.DTOs.Auth;

public class LoginResultDto
{{
    public bool Success {{ get; set; }}
    public string? Error {{ get; set; }}
    public int UsuarioId {{ get; set; }}
    public string? Usuario {{ get; set; }}
    public string? NombreCompleto {{ get; set; }}
    public List<string> Roles {{ get; set; }} = new();
    public List<string> Permisos {{ get; set; }} = new();
    public string? AccessToken {{ get; set; }}
    public string? RefreshToken {{ get; set; }}
}}
";

    public static string GetRefreshTokenDto(string serviciosNamespace, SecuritySchemaInfo security) => $@"namespace {serviciosNamespace}.{security.SchemaNamespace}.DTOs.Auth;

using System.ComponentModel.DataAnnotations;

public class RefreshTokenDto
{{
    [Required]
    public string? RefreshToken {{ get; set; }}
}}
";

    public static string GetMenuDtos(string serviciosNamespace, SecuritySchemaInfo security) => $@"namespace {serviciosNamespace}.{security.SchemaNamespace}.DTOs.Auth;

public class MenuDto
{{
    public string Modulo {{ get; set; }} = """";
    public string Codigo {{ get; set; }} = """";
    public string Icono {{ get; set; }} = """";
    public string Ruta {{ get; set; }} = """";
    public List<OpcionMenuDto> Opciones {{ get; set; }} = new();
}}

public class OpcionMenuDto
{{
    public string Nombre {{ get; set; }} = """";
    public string Codigo {{ get; set; }} = """";
    public string Ruta {{ get; set; }} = """";
    public string Icono {{ get; set; }} = """";
    public string Controller {{ get; set; }} = """";
    public string Action {{ get; set; }} = ""Index"";
}}
";

    public static string GetIAuthService(string serviciosNamespace, string entidadesNamespace, SecuritySchemaInfo security)
    {
        var ns = $"{serviciosNamespace}.{security.SchemaNamespace}";
        return $@"namespace {ns}.Services;

using {entidadesNamespace}.{security.SchemaNamespace};
using {ns}.DTOs.Auth;

public interface IAuthService
{{
    Task<LoginResultDto> LoginAsync(LoginDto dto);
    Task<LoginResultDto> RefreshTokenAsync(string refreshToken);
    Task<List<string>> GetRolesAsync(int userId);
    Task<List<string>> GetPermisosAsync(int userId);
    Task<List<MenuDto>> GetMenuAsync(int userId);
}}
";
    }

    public static string GetAuthService(string serviciosNamespace, string entidadesNamespace, string datosNamespace, SecuritySchemaInfo security)
    {
        var ns = $"{serviciosNamespace}.{security.SchemaNamespace}";
        var ent = $"{entidadesNamespace}.{security.SchemaNamespace}";
        var u = security.UsuariosTable;
        var r = security.RolesTable;
        var ur = security.UsuarioRolTable;
        var rp = security.RolPermisoTable;
        var p = security.PermisosTable;
        var m = security.ModulosTable;
        var o = security.OpcionesTable;
        var rt = security.RefreshTokensTable;

        return $@"namespace {ns}.Services;

using {ent};
using {datosNamespace}.Repositories;
using {ns}.DTOs.Auth;
using {serviciosNamespace}.Services.Common;

public class AuthService : IAuthService
{{
    private readonly IRepository<{u}> _usuarios;
    private readonly IRepository<{r}> _roles;
    private readonly IRepository<{ur}> _usuarioRoles;
    private readonly IRepository<{rp}> _rolPermisos;
    private readonly IRepository<{p}> _permisos;
    private readonly IRepository<{m}> _modulos;
    private readonly IRepository<{o}> _opciones;
    private readonly IRepository<{rt}> _refreshTokens;
    private readonly JwtTokenService _jwt;

    public AuthService(
        IRepository<{u}> usuarios,
        IRepository<{r}> roles,
        IRepository<{ur}> usuarioRoles,
        IRepository<{rp}> rolPermisos,
        IRepository<{p}> permisos,
        IRepository<{m}> modulos,
        IRepository<{o}> opciones,
        IRepository<{rt}> refreshTokens,
        JwtTokenService jwt)
    {{
        _usuarios = usuarios;
        _roles = roles;
        _usuarioRoles = usuarioRoles;
        _rolPermisos = rolPermisos;
        _permisos = permisos;
        _modulos = modulos;
        _opciones = opciones;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
    }}

    public async Task<LoginResultDto> LoginAsync(LoginDto dto)
    {{
        if (dto == null || string.IsNullOrWhiteSpace(dto.Usuario) || string.IsNullOrWhiteSpace(dto.Password))
            return new LoginResultDto {{ Success = false, Error = ""Ingrese usuario y contraseña."" }};

        var usuario = (await _usuarios.FindAsync(u =>
            (u.usuario == dto.Usuario || u.correo == dto.Usuario) && u.activo)).FirstOrDefault();

        if (usuario == null)
            return new LoginResultDto {{ Success = false, Error = ""Usuario o contraseña incorrectos."" }};

        if (!PasswordHasher.VerifyPassword(dto.Password, usuario.passwordHash, out var requiresUpgrade))
            return new LoginResultDto {{ Success = false, Error = ""Usuario o contraseña incorrectos."" }};

        if (requiresUpgrade)
        {{
            usuario.passwordHash = PasswordHasher.HashPassword(dto.Password);
            _usuarios.Update(usuario);
            await _usuarios.SaveChangesAsync();
        }}

        usuario.ultimoAcceso = DateTime.UtcNow;
        _usuarios.Update(usuario);
        await _usuarios.SaveChangesAsync();

        var roles = await GetRolesAsync(usuario.id);
        var permisos = await GetPermisosAsync(usuario.id);

        return new LoginResultDto
        {{
            Success = true,
            UsuarioId = usuario.id,
            Usuario = usuario.usuario,
            NombreCompleto = usuario.nombreCompleto,
            Roles = roles,
            Permisos = permisos,
            AccessToken = _jwt.CreateAccessToken(usuario.id, usuario.usuario, usuario.nombreCompleto, roles, permisos),
            RefreshToken = await CreateRefreshTokenAsync(usuario.id)
        }};
    }}

    public async Task<LoginResultDto> RefreshTokenAsync(string refreshToken)
    {{
        if (string.IsNullOrWhiteSpace(refreshToken))
            return new LoginResultDto {{ Success = false, Error = ""Token de refresco requerido."" }};

        var stored = (await _refreshTokens.FindAsync(t =>
            t.token == refreshToken && !t.revocado && t.fechaExpiracion > DateTime.UtcNow)).FirstOrDefault();

        if (stored == null)
            return new LoginResultDto {{ Success = false, Error = ""Token de refresco inválido o expirado."" }};

        var usuario = await _usuarios.GetByIdAsync(stored.idUsuario);
        if (usuario == null || !usuario.activo)
            return new LoginResultDto {{ Success = false, Error = ""Usuario no válido."" }};

        stored.revocado = true;
        stored.fechaRevocacion = DateTime.UtcNow;
        _refreshTokens.Update(stored);
        await _refreshTokens.SaveChangesAsync();

        var roles = await GetRolesAsync(usuario.id);
        var permisos = await GetPermisosAsync(usuario.id);

        return new LoginResultDto
        {{
            Success = true,
            UsuarioId = usuario.id,
            Usuario = usuario.usuario,
            NombreCompleto = usuario.nombreCompleto,
            Roles = roles,
            Permisos = permisos,
            AccessToken = _jwt.CreateAccessToken(usuario.id, usuario.usuario, usuario.nombreCompleto, roles, permisos),
            RefreshToken = await CreateRefreshTokenAsync(usuario.id)
        }};
    }}

    public async Task<List<string>> GetRolesAsync(int userId)
    {{
        var rolIds = (await _usuarioRoles.FindAsync(ur => ur.idUsuario == userId))
            .Select(ur => ur.idRol).ToList();
        if (rolIds.Count == 0) return new List<string>();

        var roles = await _roles.FindAsync(r => rolIds.Contains(r.id));
        return roles.Select(r => r.nombre).ToList();
    }}

    public async Task<List<string>> GetPermisosAsync(int userId)
    {{
        var rolIds = (await _usuarioRoles.FindAsync(ur => ur.idUsuario == userId))
            .Select(ur => ur.idRol).ToList();
        if (rolIds.Count == 0) return new List<string>();

        var permisoIds = (await _rolPermisos.FindAsync(rp => rolIds.Contains(rp.idRol)))
            .Select(rp => rp.idPermiso).Distinct().ToList();
        if (permisoIds.Count == 0) return new List<string>();

        var permisos = await _permisos.FindAsync(p => permisoIds.Contains(p.id) && p.activo);
        return permisos.Select(p => p.descripcion).Where(d => !string.IsNullOrEmpty(d)).ToList()!;
    }}

    public async Task<List<MenuDto>> GetMenuAsync(int userId)
    {{
        var rolIds = (await _usuarioRoles.FindAsync(ur => ur.idUsuario == userId))
            .Select(ur => ur.idRol).ToList();
        if (rolIds.Count == 0) return new List<MenuDto>();

        var permisoIds = (await _rolPermisos.FindAsync(rp => rolIds.Contains(rp.idRol)))
            .Select(rp => rp.idPermiso).Distinct().ToList();
        if (permisoIds.Count == 0) return new List<MenuDto>();

        // Opciones accesibles por el usuario (FK directa Permisos.idOpcion -> Opciones.id)
        var opcionIds = (await _permisos.FindAsync(p => permisoIds.Contains(p.id) && p.activo))
            .Select(p => p.idOpcion).Distinct().ToList();
        if (opcionIds.Count == 0) return new List<MenuDto>();

        var modulos = (await _modulos.FindAsync(x => x.activo && x.visibleMenu))
            .OrderBy(x => x.ordenMenu).ToList();
        var opciones = (await _opciones.FindAsync(x => opcionIds.Contains(x.id) && x.activo && x.visibleMenu))
            .OrderBy(x => x.ordenMenu).ToList();

        var menu = new List<MenuDto>();
        foreach (var modulo in modulos)
        {{
            var items = opciones
                .Where(x => x.idModulo == modulo.id)
                .Select(x => new OpcionMenuDto
                {{
                    Nombre = x.nombre,
                    Codigo = x.codigo ?? """",
                    Ruta = x.ruta ?? """",
                    Icono = x.icono ?? """",
                    Controller = GetControllerFromRuta(x.ruta),
                    Action = ""Index""
                }})
                .ToList();

            if (items.Count == 0) continue;

            menu.Add(new MenuDto
            {{
                Modulo = modulo.nombre,
                Codigo = modulo.codigo ?? """",
                Icono = modulo.icono ?? """",
                Ruta = modulo.ruta ?? """",
                Opciones = items
            }});
        }}
        return menu;
    }}

    private async Task<string> CreateRefreshTokenAsync(int userId)
    {{
        var token = _jwt.GenerateRefreshToken();
        var entity = new {rt}
        {{
            idUsuario = userId,
            token = token,
            fechaCreacion = DateTime.UtcNow,
            fechaExpiracion = DateTime.UtcNow.AddDays(7),
            revocado = false
        }};
        await _refreshTokens.AddAsync(entity);
        await _refreshTokens.SaveChangesAsync();
        return token;
    }}

    private static string GetControllerFromRuta(string? ruta)
    {{
        if (string.IsNullOrWhiteSpace(ruta)) return """";
        var partes = ruta.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length > 0 ? partes[0] : """";
    }}
}}
";
    }

    // =====================================================================
    //  Capa Web MVC
    // =====================================================================

    public static string GetPermisoAttribute(string webNamespace) => $@"namespace {webNamespace}.Seguridad;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class PermisoAttribute : Attribute, IAuthorizationFilter
{{
    public string Permiso {{ get; }}

    public PermisoAttribute(string permiso)
    {{
        Permiso = permiso;
    }}

    public void OnAuthorization(AuthorizationFilterContext context)
    {{
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {{
            context.Result = new RedirectToActionResult(""Login"", ""Account"", new {{ ReturnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString }});
            return;
        }}

        if (!user.HasClaim(""Permiso"", Permiso))
        {{
            context.Result = new RedirectToActionResult(""AccessDenied"", ""Account"", null);
            return;
        }}
    }}
}}
";

    public static string GetApiPermisoAttribute(string webApiNamespace) => $@"namespace {webApiNamespace}.Seguridad;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class PermisoAttribute : Attribute, IAuthorizationFilter
{{
    public string Permiso {{ get; }}

    public PermisoAttribute(string permiso)
    {{
        Permiso = permiso;
    }}

    public void OnAuthorization(AuthorizationFilterContext context)
    {{
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {{
            context.Result = new UnauthorizedResult();
            return;
        }}

        if (!user.HasClaim(""Permiso"", Permiso))
        {{
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }}
    }}
}}
";

    public static string GetAccountController(string webNamespace, string serviciosNamespace, SecuritySchemaInfo security)
    {
        var ns = $"{serviciosNamespace}.{security.SchemaNamespace}";
        return $@"namespace {webNamespace}.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {ns}.DTOs.Auth;
using {ns}.Services;

public class AccountController : Controller
{{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {{
        _authService = authService;
    }}

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {{
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(""Index"", ""Home"");
        ViewData[""ReturnUrl""] = returnUrl;
        return View(new LoginDto());
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto, string? returnUrl = null)
    {{
        if (!ModelState.IsValid)
        {{
            ViewData[""ReturnUrl""] = returnUrl;
            return View(dto);
        }}

        var result = await _authService.LoginAsync(dto);
        if (!result.Success)
        {{
            ModelState.AddModelError("""", result.Error ?? ""Credenciales inválidas."");
            ViewData[""ReturnUrl""] = returnUrl;
            return View(dto);
        }}

        var claims = new List<Claim>
        {{
            new(ClaimTypes.NameIdentifier, result.UsuarioId.ToString()),
            new(ClaimTypes.Name, result.Usuario ?? """")
        }};
        if (!string.IsNullOrEmpty(result.NombreCompleto))
            claims.Add(new Claim(""NombreCompleto"", result.NombreCompleto));
        claims.AddRange(result.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(result.Permisos.Select(p => new Claim(""Permiso"", p)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {{
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            }});

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(""Index"", ""Home"");
    }}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {{
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(""Login"", ""Account"");
    }}

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {{
        return View();
    }}
}}
";
    }

    public static string GetLoginView(string webNamespace, string serviciosNamespace, SecuritySchemaInfo security)
    {
        var ns = $"{serviciosNamespace}.{security.SchemaNamespace}";
        return $@"@model {ns}.DTOs.Auth.LoginDto

@{{
    ViewData[""Title""] = ""Iniciar Sesión"";
    var returnUrl = ViewData[""ReturnUrl""] as string;
}}

<div class=""row justify-content-center align-items-center"" style=""min-height: 80vh;"">
    <div class=""col-md-5 col-lg-4"">
        <div class=""card shadow"">
            <div class=""card-body p-4"">
                <h1 class=""h3 text-center mb-4"">Iniciar Sesión</h1>
                <form asp-controller=""Account"" asp-action=""Login"" method=""post"" role=""form"">
                    <div asp-validation-summary=""ModelOnly"" class=""text-danger mb-3""></div>
                    <input type=""hidden"" name=""returnUrl"" value=""@returnUrl"" />
                    <div class=""mb-3"">
                        <label asp-for=""Usuario"" class=""form-label""></label>
                        <input asp-for=""Usuario"" class=""form-control"" autocomplete=""username"" autofocus=""autofocus"" />
                        <span asp-validation-for=""Usuario"" class=""text-danger""></span>
                    </div>
                    <div class=""mb-3"">
                        <label asp-for=""Password"" class=""form-label""></label>
                        <input asp-for=""Password"" class=""form-control"" autocomplete=""current-password"" />
                        <span asp-validation-for=""Password"" class=""text-danger""></span>
                    </div>
                    <button type=""submit"" class=""btn btn-primary w-100""><i class=""bi bi-box-arrow-in-right""></i> Entrar</button>
                </form>
            </div>
        </div>
    </div>
</div>

@section Scripts {{
    @{{await Html.RenderPartialAsync(""_ValidationScriptsPartial"");}}
}}
";
    }

    public static string GetAccessDeniedView() => @"@{
    ViewData[""Title""] = ""Acceso Denegado"";
}

<div class=""text-center mt-5"">
    <i class=""bi bi-shield-lock text-danger"" style=""font-size: 4rem;""></i>
    <h1 class=""mt-3"">Acceso Denegado</h1>
    <p class=""lead"">No tiene los permisos necesarios para acceder a este recurso.</p>
    <a asp-controller=""Home"" asp-action=""Index"" class=""btn btn-primary"">Volver al Inicio</a>
</div>
";

    public static string GetMenuViewComponent(string webNamespace, string serviciosNamespace, SecuritySchemaInfo security)
    {
        var ns = $"{serviciosNamespace}.{security.SchemaNamespace}";
        return $@"namespace {webNamespace}.Components;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using {ns}.Services;

public class MenuViewComponent : ViewComponent
{{
    private readonly IAuthService _authService;

    public MenuViewComponent(IAuthService authService)
    {{
        _authService = authService;
    }}

    public async Task<IViewComponentResult> InvokeAsync()
    {{
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Content(string.Empty);
        var menu = await _authService.GetMenuAsync(int.Parse(userId));
        return View(menu);
    }}
}}
";
    }

    public static string GetMenuViewComponentView(string serviciosNamespace, SecuritySchemaInfo security)
    {
        var ns = $"{serviciosNamespace}.{security.SchemaNamespace}";
        return $@"@model IEnumerable<{ns}.DTOs.Auth.MenuDto>

@foreach (var modulo in Model)
{{
    var collapseId = ""menu-"" + modulo.Codigo;
    <ul class=""nav nav-pills flex-column mb-auto"">
        <li class=""nav-item"">
            <a class=""nav-link text-secondary small text-uppercase fw-bold px-2 d-flex justify-content-between align-items-center""
               data-bs-toggle=""collapse"" href=""#{{collapseId}}"" role=""button"" aria-expanded=""true"">
                <span><i class=""@(modulo.Icono ?? ""bi bi-folder"")""></i> @modulo.Modulo</span>
                <i class=""bi bi-chevron-down""></i>
            </a>
            <div class=""collapse show"" id=""@collapseId"">
                <ul class=""nav nav-pills flex-column ms-2"">
                    @foreach (var opcion in modulo.Opciones)
                    {{
                        <li class=""nav-item"">
                            <a class=""nav-link text-white"" asp-controller=""@opcion.Controller"" asp-action=""@opcion.Action"" asp-area="""">
                                <i class=""@(opcion.Icono ?? ""bi bi-table"")""></i> @opcion.Nombre
                            </a>
                        </li>
                    }}
                </ul>
            </div>
        </li>
    </ul>
}}
";
    }

    // =====================================================================
    //  Capa Web API
    // =====================================================================

    public static string GetAuthApiController(string webApiNamespace, string serviciosNamespace, SecuritySchemaInfo security)
    {
        var ns = $"{serviciosNamespace}.{security.SchemaNamespace}";
        return $@"namespace {webApiNamespace}.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using {ns}.DTOs.Auth;
using {ns}.Services;

[ApiController]
[Route(""api/[controller]"")]
[AllowAnonymous]
public class AuthController : ControllerBase
{{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {{
        _authService = authService;
    }}

    [HttpPost(""login"")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {{
        var result = await _authService.LoginAsync(dto);
        if (!result.Success) return Unauthorized(new {{ error = result.Error }});
        return Ok(result);
    }}

    [HttpPost(""refresh"")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {{
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken ?? """");
        if (!result.Success) return Unauthorized(new {{ error = result.Error }});
        return Ok(result);
    }}
}}
";
    }
}