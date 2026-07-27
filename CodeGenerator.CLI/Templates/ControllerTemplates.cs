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

        // Unique referenced schemas for using directives
        var refSchemas = fkProps
            .Select(f => SchemaHelper.ToNamespace(f.FkReferencedSchema ?? "dbo"))
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
        foreach (var fk in fkProps)
        {
            var refSchema = SchemaHelper.ToNamespace(fk.FkReferencedSchema ?? "dbo");
            sb.AppendLine($"using {serviciosNamespace}.{refSchema}.DTOs.{fk.FkReferencedTable};");
        }
        sb.AppendLine();
        sb.AppendLine($"namespace {webNamespace}.{entity.SchemaNamespace}.Controllers;");
        sb.AppendLine();
        sb.AppendLine($"public class {controllerName} : Controller");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly I{entity.Name}Service _service;");
        foreach (var fk in fkProps)
        {
            sb.AppendLine($"    private readonly I{fk.FkReferencedTable}Service _{fk.FkReferencedTable}Service;");
        }
        sb.AppendLine();
        sb.AppendLine($"    public {controllerName}(I{entity.Name}Service service");
        foreach (var fk in fkProps)
        {
            sb.AppendLine($"        , I{fk.FkReferencedTable}Service {fk.FkReferencedTable}Service");
        }
        sb.AppendLine("    )");
        sb.AppendLine("    {");
        sb.AppendLine("        _service = service;");
        foreach (var fk in fkProps)
        {
            sb.AppendLine($"        _{fk.FkReferencedTable}Service = {fk.FkReferencedTable}Service;");
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
        foreach (var fk in fkProps)
        {
            sb.AppendLine($"        await Populate{fk.FkReferencedTable}DropdownAsync();");
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
        foreach (var fk in fkProps)
        {
            sb.AppendLine($"        await Populate{fk.FkReferencedTable}DropdownAsync();");
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
        foreach (var fk in fkProps)
        {
            sb.AppendLine($"        await Populate{fk.FkReferencedTable}DropdownAsync();");
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
        foreach (var fk in fkProps)
        {
            sb.AppendLine($"        await Populate{fk.FkReferencedTable}DropdownAsync();");
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
        foreach (var fk in fkProps)
        {
            var displayCol = fk.FkReferencedDisplayColumn ?? "Id";
            sb.AppendLine($"    private async Task Populate{fk.FkReferencedTable}DropdownAsync()");
            sb.AppendLine("    {");
            sb.AppendLine($"        var items = await _{fk.FkReferencedTable}Service.GetAllAsync();");
            sb.AppendLine($"        ViewData[\"{fk.Name}\"] = new SelectList(items, nameof({fk.FkReferencedTable}Dto.{fk.FkReferencedColumn}), \"{displayCol}\");");
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
