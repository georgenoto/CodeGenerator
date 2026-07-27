namespace CodeGenerator.CLI.Templates;

public static class CsProjTemplates
{
    public static string GetEntidadesCsProj(string framework) => $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
";

    public static string GetDatosCsProj(string framework, string entidadesProjectName) => $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Tools"" Version=""8.0.0"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""8.0.0"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{entidadesProjectName}\{entidadesProjectName}.csproj"" />
  </ItemGroup>

</Project>
";

    public static string GetServiciosCsProj(string framework, string entidadesProjectName, string datosProjectName) => $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{entidadesProjectName}\{entidadesProjectName}.csproj"" />
    <ProjectReference Include=""..\{datosProjectName}\{datosProjectName}.csproj"" />
  </ItemGroup>

</Project>
";

    public static string GetWebCsProj(string framework, string serviciosProjectName, string datosProjectName, string entidadesProjectName) => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""8.0.0"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{entidadesProjectName}\{entidadesProjectName}.csproj"" />
    <ProjectReference Include=""..\{datosProjectName}\{datosProjectName}.csproj"" />
    <ProjectReference Include=""..\{serviciosProjectName}\{serviciosProjectName}.csproj"" />
  </ItemGroup>

</Project>
";

    public static string GetWebApiCsProj(string framework, string serviciosProjectName, string datosProjectName, string entidadesProjectName) => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""8.0.0"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{entidadesProjectName}\{entidadesProjectName}.csproj"" />
    <ProjectReference Include=""..\{datosProjectName}\{datosProjectName}.csproj"" />
    <ProjectReference Include=""..\{serviciosProjectName}\{serviciosProjectName}.csproj"" />
  </ItemGroup>

</Project>
";
}
