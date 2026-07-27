using CodeGenerator.CLI.Services;
using System.Text;

namespace CodeGenerator.CLI.Templates;

public static class ViewTemplates
{
    public static string GetIndexView(string webNamespace, string serviciosNamespace, EntityInfo entity)
    {
        var allProps = entity.Properties.Where(p => !p.IsNavigation).ToList();
        var headers = new List<string>();
        var props = new List<string>();
        foreach (var p in allProps)
        {
            if (p.IsForeignKey)
            {
                headers.Add(p.FkReferencedTable ?? p.Name);
                props.Add(p.FkDisplayPropertyName ?? p.Name);
            }
            else
            {
                headers.Add(p.Name);
                props.Add(p.Name);
            }
        }
        var headersArray = string.Join(", ", headers.Select(h => $"\"{h}\""));
        var propsArray = string.Join(", ", props.Select(p => $"\"{p}\""));

        var sb = new StringBuilder();
        sb.AppendLine($"@model IEnumerable<{serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name}.{entity.Name}Dto>");
        sb.AppendLine();
        sb.AppendLine("@{");
        sb.AppendLine($"    ViewData[\"Title\"] = \"{entity.Name}\";");
        sb.AppendLine($"    ViewData[\"ColumnHeaders\"] = new[] {{ {headersArray} }};");
        sb.AppendLine($"    ViewData[\"ColumnProperties\"] = new[] {{ {propsArray} }};");
        sb.AppendLine($"    ViewData[\"KeyName\"] = \"{entity.KeyProperty.Name}\";");
        sb.AppendLine("    ViewData[\"PageSize\"] = 20;");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("<h1 class=\"mb-4\">@ViewData[\"Title\"]</h1>");
        sb.AppendLine();
        sb.AppendLine($"<p><a asp-action=\"Create\" class=\"btn btn-primary\"><i class=\"bi bi-plus-circle\"></i> Crear Nuevo</a></p>");
        sb.AppendLine();
        sb.AppendLine("@await Html.PartialAsync(\"_DataTable\", Model)");
        sb.AppendLine();
        sb.AppendLine("@section Scripts {");
        sb.AppendLine("    <script>initDataTables();</script>");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string GetDetailsView(string webNamespace, string serviciosNamespace, EntityInfo entity)
    {
        var allProps = entity.Properties.Where(p => !p.IsNavigation).ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"@model {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name}.{entity.Name}Dto");
        sb.AppendLine();
        sb.AppendLine("@{");
        sb.AppendLine($"    ViewData[\"Title\"] = \"Detalles de {entity.Name}\";");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("<h1 class=\"mb-4\">@ViewData[\"Title\"]</h1>");
        sb.AppendLine();
        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("    <div class=\"card-body\">");
        sb.AppendLine("        <dl class=\"row\">");
        foreach (var prop in allProps)
        {
            var displayName = prop.IsForeignKey ? (prop.FkReferencedTable ?? prop.Name) : prop.Name;
            var displayProp = prop.IsForeignKey && prop.FkDisplayPropertyName != null ? prop.FkDisplayPropertyName : prop.Name;
            sb.AppendLine($"            <dt class=\"col-sm-3\">{displayName}</dt>");
            sb.AppendLine($"            <dd class=\"col-sm-9\">@Model.{displayProp}</dd>");
        }
        sb.AppendLine("        </dl>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"mt-3\">");
        sb.AppendLine($"    <a asp-action=\"Edit\" asp-route-id=\"@Model.{entity.KeyProperty.Name}\" class=\"btn btn-warning\"><i class=\"bi bi-pencil\"></i> Editar</a>");
        sb.AppendLine("    <a asp-action=\"Index\" class=\"btn btn-secondary\">Volver a la lista</a>");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    public static string GetCreateView(string webNamespace, string serviciosNamespace, EntityInfo entity)
    {
        var props = entity.Properties.Where(p => !p.IsNavigation && !p.IsKey).ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"@model {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name}.Create{entity.Name}Dto");
        sb.AppendLine();
        sb.AppendLine("@{");
        sb.AppendLine($"    ViewData[\"Title\"] = \"Crear {entity.Name}\";");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("<h1 class=\"mb-4\">@ViewData[\"Title\"]</h1>");
        sb.AppendLine();
        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("    <div class=\"card-body\">");
        sb.AppendLine("        <form asp-action=\"Create\">");
        sb.AppendLine("            <div asp-validation-summary=\"ModelOnly\" class=\"text-danger\"></div>");
        foreach (var prop in props)
        {
            sb.AppendLine("            <div class=\"mb-3\">");
            sb.AppendLine($"                <label asp-for=\"{prop.Name}\" class=\"form-label\"></label>");
            if (prop.IsForeignKey)
            {
                if (!string.IsNullOrEmpty(prop.CascadeParentProperty))
                {
                    var cascadeUrl = $"@Url.Action(\"GetBy{prop.CascadeFilterProperty}\", \"{prop.FkReferencedTable}\")";
                    sb.AppendLine($"                <select asp-for=\"{prop.Name}\" class=\"form-select cascade-select\" data-cascade-parent=\"{prop.CascadeParentProperty}\" data-cascade-url=\"{cascadeUrl}\" data-current-value=\"@Model.{prop.Name}\">");
                    sb.AppendLine("                    <option value=\"\">-- Seleccione --</option>");
                    sb.AppendLine("                </select>");
                }
                else
                {
                    sb.AppendLine($"                <select asp-for=\"{prop.Name}\" class=\"form-select\" asp-items=\"@(ViewData[\"{prop.Name}\"] as SelectList)\">");
                    sb.AppendLine("                    <option value=\"\">-- Seleccione --</option>");
                    sb.AppendLine("                </select>");
                }
            }
            else
            {
                sb.AppendLine(GetInputForProperty(prop.Name, prop.Type));
            }
            sb.AppendLine($"                <span asp-validation-for=\"{prop.Name}\" class=\"text-danger\"></span>");
            sb.AppendLine("            </div>");
        }
        sb.AppendLine("            <div class=\"d-flex gap-2\">");
        sb.AppendLine("                <button type=\"submit\" class=\"btn btn-primary\"><i class=\"bi bi-save\"></i> Guardar</button>");
        sb.AppendLine("                <a asp-action=\"Index\" class=\"btn btn-secondary\">Cancelar</a>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </form>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine();
        sb.AppendLine("@section Scripts {");
        sb.AppendLine("    @{await Html.RenderPartialAsync(\"_ValidationScriptsPartial\");}");
        if (entity.Properties.Any(p => !string.IsNullOrEmpty(p.CascadeParentProperty)))
        {
            sb.AppendLine("    <script>initCascadeSelects();</script>");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string GetEditView(string webNamespace, string serviciosNamespace, EntityInfo entity)
    {
        var allProps = entity.Properties.Where(p => !p.IsNavigation).ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"@model {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name}.Update{entity.Name}Dto");
        sb.AppendLine();
        sb.AppendLine("@{");
        sb.AppendLine($"    ViewData[\"Title\"] = \"Editar {entity.Name}\";");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("<h1 class=\"mb-4\">@ViewData[\"Title\"]</h1>");
        sb.AppendLine();
        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("    <div class=\"card-body\">");
        sb.AppendLine($"        <form asp-action=\"Edit\">");
        sb.AppendLine("            <div asp-validation-summary=\"ModelOnly\" class=\"text-danger\"></div>");
        foreach (var prop in allProps)
        {
            if (prop.IsKey)
            {
                sb.AppendLine($"            <input type=\"hidden\" asp-for=\"{prop.Name}\" />");
            }
            else
            {
                sb.AppendLine("            <div class=\"mb-3\">");
                sb.AppendLine($"                <label asp-for=\"{prop.Name}\" class=\"form-label\"></label>");
                if (prop.IsForeignKey)
                {
                if (!string.IsNullOrEmpty(prop.CascadeParentProperty))
                {
                    var cascadeUrl = $"@Url.Action(\"GetBy{prop.CascadeFilterProperty}\", \"{prop.FkReferencedTable}\")";
                    sb.AppendLine($"                <select asp-for=\"{prop.Name}\" class=\"form-select cascade-select\" data-cascade-parent=\"{prop.CascadeParentProperty}\" data-cascade-url=\"{cascadeUrl}\" data-current-value=\"@Model.{prop.Name}\">");
                    sb.AppendLine("                    <option value=\"\">-- Seleccione --</option>");
                    sb.AppendLine("                </select>");
                }
                else
                {
                    sb.AppendLine($"                <select asp-for=\"{prop.Name}\" class=\"form-select\" asp-items=\"@(ViewData[\"{prop.Name}\"] as SelectList)\">");
                    sb.AppendLine("                    <option value=\"\">-- Seleccione --</option>");
                    sb.AppendLine("                </select>");
                }
            }
            else
            {
                sb.AppendLine(GetInputForProperty(prop.Name, prop.Type));
            }
            sb.AppendLine($"                <span asp-validation-for=\"{prop.Name}\" class=\"text-danger\"></span>");
            sb.AppendLine("            </div>");
            }
        }
        sb.AppendLine("            <div class=\"d-flex gap-2\">");
        sb.AppendLine("                <button type=\"submit\" class=\"btn btn-primary\"><i class=\"bi bi-save\"></i> Guardar</button>");
        sb.AppendLine("                <a asp-action=\"Index\" class=\"btn btn-secondary\">Cancelar</a>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </form>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine();
        sb.AppendLine("@section Scripts {");
        sb.AppendLine("    @{await Html.RenderPartialAsync(\"_ValidationScriptsPartial\");}");
        if (entity.Properties.Any(p => !string.IsNullOrEmpty(p.CascadeParentProperty)))
        {
            sb.AppendLine("    <script>initCascadeSelects();</script>");
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string GetDeleteView(string webNamespace, string serviciosNamespace, EntityInfo entity)
    {
        var allProps = entity.Properties.Where(p => !p.IsNavigation).ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"@model {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name}.{entity.Name}Dto");
        sb.AppendLine();
        sb.AppendLine("@{");
        sb.AppendLine($"    ViewData[\"Title\"] = \"Eliminar {entity.Name}\";");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("<h1 class=\"mb-4\">@ViewData[\"Title\"]</h1>");
        sb.AppendLine();
        sb.AppendLine("<div class=\"alert alert-danger\">");
        sb.AppendLine("    <i class=\"bi bi-exclamation-triangle\"></i> ¿Está seguro de que desea eliminar este registro?");
        sb.AppendLine("</div>");
        sb.AppendLine();
        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("    <div class=\"card-body\">");
        sb.AppendLine("        <dl class=\"row\">");
        foreach (var prop in allProps)
        {
            sb.AppendLine($"            <dt class=\"col-sm-3\">@{"Html"}.DisplayNameFor(model => model.{prop.Name})</dt>");
                sb.AppendLine($"            <dd class=\"col-sm-9\">@{"Html"}.DisplayFor(model => model.{prop.Name})</dd>");
        }
        sb.AppendLine("        </dl>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<div class=\"mt-3 d-flex gap-2\">");
        sb.AppendLine($"    <form asp-action=\"Delete\" method=\"post\">");
        sb.AppendLine($"        <input type=\"hidden\" name=\"id\" value=\"@Model.{entity.KeyProperty.Name}\" />");
        sb.AppendLine("        <button type=\"submit\" class=\"btn btn-danger\"><i class=\"bi bi-trash\"></i> Eliminar</button>");
        sb.AppendLine("    </form>");
        sb.AppendLine("    <a asp-action=\"Index\" class=\"btn btn-secondary\">Volver a la lista</a>");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    public static string GetLayoutView(string webNamespace, List<EntityInfo> entities)
    {
        var entitiesBySchema = entities.GroupBy(e => e.SchemaName).OrderBy(g => g.Key);
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"es\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"utf-8\" />");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        sb.AppendLine("    <title>@ViewData[\"Title\"] - " + webNamespace + "</title>");
        sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css\" />");
        sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css\" />");
        sb.AppendLine("    <link rel=\"stylesheet\" href=\"~/css/site.css\" asp-append-version=\"true\" />");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"d-flex vh-100\">");
        sb.AppendLine("        <!-- Sidebar izquierdo -->");
        sb.AppendLine("        <div class=\"sidebar bg-dark text-white d-flex flex-column flex-shrink-0 p-3\">");
        sb.AppendLine("            <a class=\"navbar-brand text-white text-decoration-none mb-4 fs-4\" asp-controller=\"Home\" asp-action=\"Index\">");
        sb.AppendLine("                <i class=\"bi bi-building\"></i> " + webNamespace);
        sb.AppendLine("            </a>");
        sb.AppendLine("            <hr class=\"text-secondary\" />");
        sb.AppendLine("            <ul class=\"nav nav-pills flex-column mb-auto\" id=\"sidebarMenu\">");
        sb.AppendLine("                <li class=\"nav-item\">");
        sb.AppendLine("                    <a class=\"nav-link text-white\" asp-controller=\"Home\" asp-action=\"Index\">");
        sb.AppendLine("                        <i class=\"bi bi-house\"></i> Inicio");
        sb.AppendLine("                    </a>");
        sb.AppendLine("                </li>");
        sb.AppendLine("            </ul>");
        sb.AppendLine("            <hr class=\"text-secondary\" />");
        var groupIndex = 0;
        foreach (var schemaGroup in entitiesBySchema)
        {
            var schemaNs = SchemaHelper.ToNamespace(schemaGroup.Key);
            var displayName = schemaNs == "Dbo" ? "General" : schemaNs;
            var collapseId = $"collapse{schemaNs}";
            sb.AppendLine($"            <ul class=\"nav nav-pills flex-column mb-auto\">");
            sb.AppendLine("                <li class=\"nav-item\">");
            sb.AppendLine($"                    <a class=\"nav-link text-secondary small text-uppercase fw-bold px-2 d-flex justify-content-between align-items-center\"");
            sb.AppendLine($"                       data-bs-toggle=\"collapse\" href=\"#{collapseId}\" role=\"button\" aria-expanded=\"{(groupIndex == 0 ? "true" : "false")}\">");
            sb.AppendLine($"                        <span>{displayName}</span>");
            sb.AppendLine("                        <i class=\"bi bi-chevron-down\"></i>");
            sb.AppendLine("                    </a>");
            var firstItem = groupIndex == 0 ? " show" : "";
            sb.AppendLine($"                    <div class=\"collapse{firstItem}\" id=\"{collapseId}\">");
            sb.AppendLine("                        <ul class=\"nav nav-pills flex-column ms-2\">");
            foreach (var entity in schemaGroup.OrderBy(e => e.Name))
            {
                sb.AppendLine("                            <li class=\"nav-item\">");
                sb.AppendLine($"                                <a class=\"nav-link text-white\" asp-controller=\"{entity.Name}\" asp-action=\"Index\">");
                sb.AppendLine($"                                    <i class=\"bi bi-table\"></i> {entity.Name}");
                sb.AppendLine("                                </a>");
                sb.AppendLine("                            </li>");
            }
            sb.AppendLine("                        </ul>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                </li>");
            sb.AppendLine("            </ul>");
            groupIndex++;
        }
        sb.AppendLine("            <hr class=\"text-secondary\" />");
        sb.AppendLine("            <ul class=\"nav nav-pills flex-column\">");
        sb.AppendLine("                <li class=\"nav-item\">");
        sb.AppendLine("                    <a class=\"nav-link text-white\" asp-controller=\"Home\" asp-action=\"Privacy\">");
        sb.AppendLine("                        <i class=\"bi bi-shield-lock\"></i> Privacidad");
        sb.AppendLine("                    </a>");
        sb.AppendLine("                </li>");
        sb.AppendLine("            </ul>");
        sb.AppendLine("        </div>");
        sb.AppendLine();
        sb.AppendLine("        <!-- Contenido principal -->");
        sb.AppendLine("        <div class=\"main-content flex-grow-1 d-flex flex-column\">");
        sb.AppendLine("            <main class=\"container-fluid p-4 flex-grow-1\">");
        sb.AppendLine("                @RenderBody()");
        sb.AppendLine("            </main>");
        sb.AppendLine("            <footer class=\"bg-light border-top text-muted text-center py-2\">");
        sb.AppendLine("                &copy; @DateTime.Now.Year - " + webNamespace);
        sb.AppendLine("            </footer>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js\"></script>");
        sb.AppendLine("    <script src=\"~/js/site.js\" asp-append-version=\"true\"></script>");
        sb.AppendLine("    @await RenderSectionAsync(\"Scripts\", required: false)");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    public static string GetViewStart() => @"@{
    Layout = ""_Layout"";
}
";

    public static string GetViewImports(string webNamespace, string serviciosNamespace) => $@"@using {webNamespace}
@using {serviciosNamespace}
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
";

    public static string GetHomeIndexView(string webNamespace)
    {
        return $@"@{{

    ViewData[""Title""] = ""Inicio"";
}}

<div class=""text-center"">
    <h1 class=""display-4"">Bienvenido a {webNamespace}</h1>
    <p class=""lead"">Sistema de gestión generado automáticamente.</p>
</div>
";
    }

    public static string GetHomePrivacyView()
    {
        return @"@{
    ViewData[""Title""] = ""Privacidad"";
}

<h1>@ViewData[""Title""]</h1>
<p>Esta aplicación respeta la privacidad de sus usuarios.</p>
";
    }

    public static string GetValidationScriptsPartial()
    {
        return @"<script src=""https://cdn.jsdelivr.net/npm/jquery@3.7.1/dist/jquery.min.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/jquery-validation@1.21.0/dist/jquery.validate.min.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/jquery-validation-unobtrusive@4.1.0/dist/jquery.validate.unobtrusive.min.js""></script>
";
    }

    public static string GetDataTablePartial()
    {
        return @"@model IEnumerable<dynamic>
@{
    var headers = ViewData[""ColumnHeaders""] as string[] ?? ViewData[""Columns""] as string[] ?? System.Array.Empty<string>();
    var props = ViewData[""ColumnProperties""] as string[] ?? headers;
    var keyName = ViewData[""KeyName""] as string ?? ""Id"";
    var pageSize = ViewData[""PageSize""] is int ps ? ps : 20;
}

<div class=""data-table-wrapper"" data-page-size=""@pageSize"">
    <div class=""d-flex flex-wrap justify-content-between align-items-center mb-2 gap-2"">
        <div>
            <label class=""me-2"">Mostrar</label>
            <select class=""form-select form-select-sm d-inline-block w-auto page-size-select"">
                    <option value=""10"" selected=""@(pageSize == 10)"">10</option>
                    <option value=""20"" selected=""@(pageSize == 20)"">20</option>
                    <option value=""50"" selected=""@(pageSize == 50)"">50</option>
                    <option value=""100"" selected=""@(pageSize == 100)"">100</option>
            </select>
            <label class=""ms-1"">registros</label>
        </div>
        <div class=""d-flex align-items-center gap-2"">
            <span class=""text-muted small data-table-info""></span>
            <div class=""input-group input-group-sm"" style=""max-width: 260px;"">
                <span class=""input-group-text bg-white""><i class=""bi bi-search text-muted""></i></span>
                <input type=""text"" class=""form-control data-table-search"" placeholder=""Buscar en tabla..."" />
            </div>
        </div>
    </div>

    <div class=""table-responsive"">
        <table class=""table table-striped table-hover data-table"">
            <thead class=""table-dark"">
                <tr>
                    @foreach (var h in headers)
                    {
                        <th scope=""col"">@h</th>
                    }
                    <th scope=""col"" class=""text-center"">Acciones</th>
                </tr>
            </thead>
            <tbody>
                @if (Model != null && Model.Any())
                {
                    foreach (var item in Model)
                    {
                        <tr>
                            @foreach (var p in props)
                            {
                                var val = item.GetType().GetProperty(p)?.GetValue(item);
                                <td>@(val?.ToString() ?? """")</td>
                            }
                            <td class=""text-center text-nowrap"">
                                @{
                                    var keyVal = item.GetType().GetProperty(keyName)?.GetValue(item);
                                }
                                <a asp-action=""Edit"" asp-route-id=""@keyVal"" class=""btn btn-sm btn-warning"" title=""Editar""><i class=""bi bi-pencil""></i></a>
                                <a asp-action=""Details"" asp-route-id=""@keyVal"" class=""btn btn-sm btn-info"" title=""Detalles""><i class=""bi bi-eye""></i></a>
                                <a asp-action=""Delete"" asp-route-id=""@keyVal"" class=""btn btn-sm btn-danger"" title=""Eliminar""><i class=""bi bi-trash""></i></a>
                            </td>
                        </tr>
                    }
                }
                else
                {
                    <tr>
                        <td colspan=""@(headers.Length + 1)"" class=""text-center text-muted py-4"">
                            <i class=""bi bi-inbox""></i> No hay registros disponibles.
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>

    <nav aria-label=""Paginación"">
        <ul class=""pagination pagination-sm justify-content-center data-table-pagination""></ul>
    </nav>
</div>
";
    }

    public static string GetSiteCss() => @"html, body {
  height: 100%;
  margin: 0;
  font-size: 14px;
}

.sidebar {
  width: 280px;
  min-width: 280px;
  overflow-y: auto;
}

.sidebar .nav-link {
  border-radius: 0.375rem;
  padding: 0.5rem 0.75rem;
  margin-bottom: 2px;
}

.sidebar .nav-link:hover {
  background-color: rgba(255, 255, 255, 0.1);
}

.sidebar .nav-link i {
  margin-right: 8px;
  width: 18px;
  text-align: center;
}

.sidebar .nav-link[data-bs-toggle=""collapse""] .bi-chevron-down {
  transition: transform 0.3s ease;
}

.sidebar .nav-link[data-bs-toggle=""collapse""]:not(.collapsed) .bi-chevron-down {
  transform: rotate(180deg);
}

.main-content {
  overflow-y: auto;
  min-height: 100vh;
}

.data-table-wrapper .page-size-select {
  width: auto !important;
  display: inline-block;
}

.data-table-wrapper .data-table-pagination {
  margin-bottom: 0;
}
";

    public static string GetSiteJs() => @"// DataTable pagination and search
function initDataTables() {
    var wrappers = document.querySelectorAll('.data-table-wrapper');
    wrappers.forEach(function (wrapper) {
        var table = wrapper.querySelector('.data-table');
        if (!table) return;
        var tbody = table.querySelector('tbody');
        if (!tbody) return;

        var allRows = Array.from(tbody.querySelectorAll('tr'));
        if (allRows.length === 0) return;
        if (allRows.length === 1 && allRows[0].querySelector('td[colspan]')) return;

        var pageSize = parseInt(wrapper.getAttribute('data-page-size')) || 20;
        var pagination = wrapper.querySelector('.data-table-pagination');
        var info = wrapper.querySelector('.data-table-info');
        var pageSizeSelect = wrapper.querySelector('.page-size-select');
        var searchInput = wrapper.querySelector('.data-table-search');
        var currentPage = 1;
        var filteredRows = allRows.slice();
        var debounceTimer = null;

        function getFilteredRows() {
            var query = searchInput ? searchInput.value.toLowerCase().trim() : '';
            if (!query) return allRows;
            return allRows.filter(function (row) {
                var cells = row.querySelectorAll('td');
                for (var i = 0; i < cells.length - 1; i++) {
                    if (cells[i].textContent.toLowerCase().includes(query)) {
                        return true;
                    }
                }
                return false;
            });
        }

        function renderPage(page) {
            filteredRows = getFilteredRows();

            var totalPages = Math.ceil(filteredRows.length / pageSize) || 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;
            currentPage = page;

            allRows.forEach(function (row) {
                row.style.display = 'none';
            });

            filteredRows.forEach(function (row, index) {
                var pageNum = Math.floor(index / pageSize) + 1;
                row.style.display = pageNum === page ? '' : 'none';
            });

            if (info) {
                if (filteredRows.length === 0) {
                    info.textContent = '0 resultados';
                } else {
                    var start = (page - 1) * pageSize + 1;
                    var end = Math.min(page * pageSize, filteredRows.length);
                    info.textContent = start + '-' + end + ' de ' + filteredRows.length;
                }
            }

            if (!pagination) return;

            var html = '';
            html += '<li class=""page-item' + (page === 1 ? ' disabled' : '') + '"">';
            html += '<a class=""page-link"" href=""#"" data-page=""' + (page - 1) + '"" tabindex=""-1"">&laquo;</a></li>';

            var maxVisible = 5;
            var startPage = Math.max(1, page - Math.floor(maxVisible / 2));
            var endPage = Math.min(totalPages, startPage + maxVisible - 1);
            if (endPage - startPage < maxVisible - 1) {
                startPage = Math.max(1, endPage - maxVisible + 1);
            }

            if (startPage > 1) {
                html += '<li class=""page-item""><a class=""page-link"" href=""#"" data-page=""1"">1</a></li>';
                if (startPage > 2) html += '<li class=""page-item disabled""><span class=""page-link"">...</span></li>';
            }

            for (var i = startPage; i <= endPage; i++) {
                html += '<li class=""page-item' + (i === page ? ' active' : '') + '"">';
                html += '<a class=""page-link"" href=""#"" data-page=""' + i + '"">' + i + '</a></li>';
            }

            if (endPage < totalPages) {
                if (endPage < totalPages - 1) html += '<li class=""page-item disabled""><span class=""page-link"">...</span></li>';
                html += '<li class=""page-item""><a class=""page-link"" href=""#"" data-page=""' + totalPages + '"">' + totalPages + '</a></li>';
            }

            html += '<li class=""page-item' + (page === totalPages ? ' disabled' : '') + '"">';
            html += '<a class=""page-link"" href=""#"" data-page=""' + (page + 1) + '"">&raquo;</a></li>';

            pagination.innerHTML = html;

            pagination.querySelectorAll('.page-link[data-page]').forEach(function (link) {
                link.addEventListener('click', function (e) {
                    e.preventDefault();
                    var p = parseInt(this.getAttribute('data-page'));
                    if (!isNaN(p) && p >= 1 && p <= totalPages) renderPage(p);
                });
            });
        }

        if (pageSizeSelect) {
            pageSizeSelect.value = pageSize;
            pageSizeSelect.addEventListener('change', function () {
                pageSize = parseInt(this.value);
                wrapper.setAttribute('data-page-size', pageSize);
                renderPage(1);
            });
        }

        if (searchInput) {
            searchInput.addEventListener('input', function () {
                if (debounceTimer) clearTimeout(debounceTimer);
                debounceTimer = setTimeout(function () {
                    renderPage(1);
                }, 300);
            });
        }

        renderPage(1);
    });
}

// Cascade selects
function initCascadeSelects() {
    document.querySelectorAll('.cascade-select').forEach(function (child) {
        var parentName = child.getAttribute('data-cascade-parent');
        var parentSelect = document.querySelector('[name=""' + parentName + '""]');
        if (!parentSelect) return;
        var cascadeUrl = child.getAttribute('data-cascade-url');
        function loadOptions() {
            var parentVal = parentSelect.value;
            var currentVal = child.value || child.getAttribute('data-current-value');
            if (!parentVal) {
                child.innerHTML = '<option value="">-- Seleccione --</option>';
                return;
            }
            fetch(cascadeUrl + '?' + parentName + '=' + encodeURIComponent(parentVal))
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    var html = '<option value="">-- Seleccione --</option>';
                    data.forEach(function (item) {
                        html += '<option value=""' + item.value + '"">' + item.text + '</option>';
                    });
                    child.innerHTML = html;
                    child.value = currentVal;
                    child.removeAttribute('data-current-value');
                });
        }
        parentSelect.addEventListener('change', loadOptions);
        if (parentSelect.value) loadOptions();
    });
}
";

    private static string GetInputForProperty(string propName, string propType)
    {
        var baseType = propType.Replace("?", "").Trim();

        return baseType switch
        {
            "string" => $"                <input asp-for=\"{propName}\" class=\"form-control\" />",
            "int" or "long" or "short" or "byte" or "sbyte" or "uint" or "ulong" or "ushort"
                => $"                <input asp-for=\"{propName}\" class=\"form-control\" type=\"number\" />",
            "decimal" or "double" or "float"
                => $"                <input asp-for=\"{propName}\" class=\"form-control\" type=\"number\" step=\"any\" />",
            "bool" => $"                <input asp-for=\"{propName}\" class=\"form-check-input\" />",
            "DateTime" or "DateTimeOffset"
                => $"                <input asp-for=\"{propName}\" class=\"form-control\" type=\"datetime-local\" />",
            "Guid"
                => $"                <input asp-for=\"{propName}\" class=\"form-control\" />",
            "TimeSpan"
                => $"                <input asp-for=\"{propName}\" class=\"form-control\" type=\"time\" />",
            _ => $"                <input asp-for=\"{propName}\" class=\"form-control\" />"
        };
    }
}
