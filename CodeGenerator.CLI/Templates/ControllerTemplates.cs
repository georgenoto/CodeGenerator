using CodeGenerator.CLI.Services;
using System.Text;

namespace CodeGenerator.CLI.Templates;

public static class ControllerTemplates
{
    public static string GetEntityController(string webNamespace, string serviciosNamespace, EntityInfo entity)
    {
        var keyType = entity.KeyProperty.Type.Replace("?", "");
        var controllerName = $"{entity.Name}Controller";
        var allProps = entity.Properties.Where(p => !p.IsNavigation).ToList();
        var fkProps = entity.Properties.Where(p => p.IsForeignKey).ToList();
        var viewBase = $"~/Views/{entity.SchemaNamespace}/{entity.Name}";

        // Tablas referenciadas (distintas): una sola inyección de servicio por tabla,
        // aunque existan varias FK hacia la misma tabla (ej. IdLocal/IdVisitante -> Equipos).
        var fkTables = fkProps
            .Select(f => f.FkReferencedTable)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct()
            .ToList();

        // Unique referenced schemas for using directives
        // (excluye el schema de la propia entidad, ya cubierto por el using de Services)
        var refSchemas = fkProps
            .Select(f => SchemaHelper.ToNamespace(f.FkReferencedSchema ?? "dbo"))
            .Where(s => s != SchemaHelper.ToNamespace(entity.SchemaName))
            .Distinct().ToList();

        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc.Rendering;");
        sb.AppendLine($"using {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};");
        sb.AppendLine($"using {serviciosNamespace}.{entity.SchemaNamespace}.Services;");
        sb.AppendLine($"using {serviciosNamespace}.{entity.SchemaNamespace}.Mappings;");
        foreach (var refSchema in refSchemas)
        {
            sb.AppendLine($"using {serviciosNamespace}.{refSchema}.Services;");
        }
        foreach (var fkTable in fkTables)
        {
            var fk = fkProps.First(f => f.FkReferencedTable == fkTable);
            var refSchema = SchemaHelper.ToNamespace(fk.FkReferencedSchema ?? "dbo");
            sb.AppendLine($"using {serviciosNamespace}.{refSchema}.DTOs.{fkTable};");
        }
        sb.AppendLine();
        sb.AppendLine($"namespace {webNamespace}.{entity.SchemaNamespace}.Controllers;");
        sb.AppendLine();
        sb.AppendLine($"public class {controllerName} : Controller");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly I{entity.Name}Service _service;");
        foreach (var fkTable in fkTables)
        {
            sb.AppendLine($"    private readonly I{fkTable}Service _{fkTable}Service;");
        }
        sb.AppendLine();
        sb.AppendLine($"    public {controllerName}(I{entity.Name}Service service");
        foreach (var fkTable in fkTables)
        {
            sb.AppendLine($"        , I{fkTable}Service {fkTable}Service");
        }
        sb.AppendLine("    )");
        sb.AppendLine("    {");
        sb.AppendLine("        _service = service;");
        foreach (var fkTable in fkTables)
        {
            sb.AppendLine($"        _{fkTable}Service = {fkTable}Service;");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    // GET: {entity.Name}");
        sb.AppendLine("    public async Task<IActionResult> Index()");
        sb.AppendLine("    {");
        sb.AppendLine("        var result = await _service.GetAllAsync();");
        sb.AppendLine($"        return View(\"{viewBase}/Index.cshtml\", result);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    // GET: {entity.Name}/Details/{{id}}");
        sb.AppendLine($"    public async Task<IActionResult> Details({keyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine("        var result = await _service.GetByIdAsync(id);");
        sb.AppendLine("        if (result == null) return NotFound();");
        sb.AppendLine($"        return View(\"{viewBase}/Details.cshtml\", result);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    // GET: {entity.Name}/Create");
        sb.AppendLine("    public async Task<IActionResult> Create()");
        sb.AppendLine("    {");
        foreach (var fkTable in fkTables)
        {
            sb.AppendLine($"        await Populate{fkTable}DropdownAsync();");
        }
        sb.AppendLine($"        return View(\"{viewBase}/Create.cshtml\", new Create{entity.Name}Dto());");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    // POST: {entity.Name}/Create");
        sb.AppendLine("    [HttpPost]");
        sb.AppendLine("    [ValidateAntiForgeryToken]");
        sb.AppendLine($"    public async Task<IActionResult> Create(Create{entity.Name}Dto createDto)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ModelState.IsValid)");
        sb.AppendLine("        {");
        sb.AppendLine("            await _service.CreateAsync(createDto);");
        sb.AppendLine("            return RedirectToAction(nameof(Index));");
        sb.AppendLine("        }");
        foreach (var fkTable in fkTables)
        {
            sb.AppendLine($"        await Populate{fkTable}DropdownAsync();");
        }
        sb.AppendLine($"        return View(\"{viewBase}/Create.cshtml\", createDto);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    // GET: {entity.Name}/Edit/{{id}}");
        sb.AppendLine($"    public async Task<IActionResult> Edit({keyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine("        var result = await _service.GetByIdAsync(id);");
        sb.AppendLine("        if (result == null) return NotFound();");
        sb.AppendLine();
        sb.AppendLine($"        var updateDto = new Update{entity.Name}Dto");
        sb.AppendLine("        {");
        foreach (var prop in allProps)
        {
            sb.AppendLine($"            {prop.Name} = result.{prop.Name},");
        }
        sb.AppendLine("        };");
        foreach (var fkTable in fkTables)
        {
            sb.AppendLine($"        await Populate{fkTable}DropdownAsync();");
        }
        sb.AppendLine($"        return View(\"{viewBase}/Edit.cshtml\", updateDto);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    // POST: {entity.Name}/Edit/{{id}}");
        sb.AppendLine("    [HttpPost]");
        sb.AppendLine("    [ValidateAntiForgeryToken]");
        sb.AppendLine($"    public async Task<IActionResult> Edit(Update{entity.Name}Dto updateDto)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ModelState.IsValid)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var success = await _service.UpdateAsync(updateDto.{entity.KeyProperty.Name}, updateDto);");
        sb.AppendLine("            if (!success) return NotFound();");
        sb.AppendLine("            return RedirectToAction(nameof(Index));");
        sb.AppendLine("        }");
        foreach (var fkTable in fkTables)
        {
            sb.AppendLine($"        await Populate{fkTable}DropdownAsync();");
        }
        sb.AppendLine($"        return View(\"{viewBase}/Edit.cshtml\", updateDto);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    // GET: {entity.Name}/Delete/{{id}}");
        sb.AppendLine($"    public async Task<IActionResult> Delete({keyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine("        var result = await _service.GetByIdAsync(id);");
        sb.AppendLine("        if (result == null) return NotFound();");
        sb.AppendLine($"        return View(\"{viewBase}/Delete.cshtml\", result);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    // POST: {entity.Name}/Delete/{{id}}");
        sb.AppendLine("    [HttpPost, ActionName(\"Delete\")]");
        sb.AppendLine("    [ValidateAntiForgeryToken]");
        sb.AppendLine($"    public async Task<IActionResult> DeleteConfirmed({keyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine("        await _service.DeleteAsync(id);");
        sb.AppendLine("        return RedirectToAction(nameof(Index));");
        sb.AppendLine("    }");
        sb.AppendLine();
        foreach (var fk in entity.Properties.Where(p => p.IsForeignKey && p.GenerateGetByEndpoint))
        {
            var cascadeKeyType = fk.Type;
            var displayCol = entity.DisplayColumn;
            sb.AppendLine($"    // GET: {entity.Name}/GetBy{fk.Name}?{fk.Name}=value (cascade)");
            sb.AppendLine("    [HttpGet]");
            sb.AppendLine($"    public async Task<JsonResult> GetBy{fk.Name}({cascadeKeyType} {fk.Name})");
            sb.AppendLine("    {");
            sb.AppendLine($"        var items = await _service.FindAsync(p => p.{fk.Name} == {fk.Name});");
            sb.AppendLine($"        return Json(items.Select(i => new {{ value = i.{entity.KeyProperty.Name}, text = i.{displayCol} }}));");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        foreach (var fkTable in fkTables)
        {
            sb.AppendLine($"    private async Task Populate{fkTable}DropdownAsync()");
            sb.AppendLine("    {");
            sb.AppendLine($"        var items = await _{fkTable}Service.GetAllAsync();");
            foreach (var fk in fkProps.Where(f => f.FkReferencedTable == fkTable))
            {
                var displayCol = fk.FkReferencedDisplayColumn ?? "Id";
                sb.AppendLine($"        ViewData[\"{fk.Name}\"] = new SelectList(items, nameof({fkTable}Dto.{fk.FkReferencedColumn}), \"{displayCol}\");");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string GetHomeController(string webNamespace, string serviciosNamespace) => $@"using Microsoft.AspNetCore.Mvc;

namespace {webNamespace}.Controllers;

public class HomeController : Controller
{{
    public IActionResult Index()
    {{
        return View();
    }}

    public IActionResult Privacy()
    {{
        return View();
    }}
}}
";

    public static string GetEntityApiController(string webNamespace, string serviciosNamespace, EntityInfo entity)
    {
        var keyType = entity.KeyProperty.Type.Replace("?", "");
        var controllerName = $"{entity.Name}Controller";

        return $@"using Microsoft.AspNetCore.Mvc;
using {serviciosNamespace}.{entity.SchemaNamespace}.DTOs.{entity.Name};
using {serviciosNamespace}.{entity.SchemaNamespace}.Services;

namespace {webNamespace}.{entity.SchemaNamespace}.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class {controllerName} : ControllerBase
{{
    private readonly I{entity.Name}Service _service;

    public {controllerName}(I{entity.Name}Service service)
    {{
        _service = service;
    }}

    [HttpGet]
    public async Task<ActionResult<IEnumerable<{entity.Name}Dto>>> GetAll()
    {{
        var result = await _service.GetAllAsync();
        return Ok(result);
    }}

    [HttpGet(""{{id}}"")]
    public async Task<ActionResult<{entity.Name}Dto>> GetById({keyType} id)
    {{
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }}

    [HttpPost]
    public async Task<ActionResult<{entity.Name}Dto>> Create([FromBody] Create{entity.Name}Dto createDto)
    {{
        var result = await _service.CreateAsync(createDto);
        return CreatedAtAction(nameof(GetById), new {{ id = result.{entity.KeyProperty.Name} }}, result);
    }}

    [HttpPut(""{{id}}"")]
    public async Task<IActionResult> Update({keyType} id, [FromBody] Update{entity.Name}Dto updateDto)
    {{
        var success = await _service.UpdateAsync(id, updateDto);
        if (!success) return NotFound();
        return NoContent();
    }}

    [HttpDelete(""{{id}}"")]
    public async Task<IActionResult> Delete({keyType} id)
    {{
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }}
}}
";
    }
}
