# MapperAnalyzer Setup Guide

The MapperAnalyzer enforces SimpleMapper's design principles at compile time, preventing common anti-patterns.

## Quick Setup

### 1. Create Analyzer Project

```bash
# Create analyzer project
dotnet new classlib -n YourProject.Analyzers
cd YourProject.Analyzers

# Add analyzer packages
dotnet add package Microsoft.CodeAnalysis.Analyzers --version 3.3.4
dotnet add package Microsoft.CodeAnalysis.CSharp --version 4.5.0
```

### 2. Configure Analyzer Project

Update `YourProject.Analyzers.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    
    <!-- Analyzer-specific settings -->
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageId>YourProject.Analyzers</PackageId>
    <PackageVersion>1.0.0</PackageVersion>
    <Authors>Your Name</Authors>
    <Description>Code analyzers for SimpleMapper best practices</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.5.0" PrivateAssets="all" />
  </ItemGroup>

  <!-- Include analyzers in package -->
  <ItemGroup>
    <Analyzer Include="**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <None Include="tools\install.ps1" Pack="true" PackagePath="tools\install.ps1" />
    <None Include="tools\uninstall.ps1" Pack="true" PackagePath="tools\uninstall.ps1" />
  </ItemGroup>
</Project>
```

### 3. Copy the Analyzer Code

Copy the analyzer from SimpleMapper:

```csharp
// YourProject.Analyzers/MapperAnalyzer.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MapperAnalyzer : DiagnosticAnalyzer
{
    // Copy the complete analyzer code from SimpleMapper/Analyzers/MapperAnalyzer.cs
    // (Remove the #if ANALYZER wrapper)
}
```

### 4. Reference in Your Main Project

```xml
<!-- In your main project file -->
<Project Sdk="Microsoft.NET.Sdk">
  <!-- ... other content ... -->
  
  <ItemGroup>
    <Analyzer Include="../YourProject.Analyzers/bin/Debug/netstandard2.0/YourProject.Analyzers.dll" />
  </ItemGroup>
  
  <!-- Or via package reference -->
  <ItemGroup>
    <PackageReference Include="YourProject.Analyzers" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

## What You'll See

### In Visual Studio

When you violate mapper rules, you'll see:

1. **Red squiggly lines** under violating code
2. **Error messages** in the Error List
3. **Build failures** until violations are fixed

### Error Examples

```csharp
// SM001 Error
public class BadMapper : BaseMapper<User, UserDto>
{
    public BadMapper(IService service) // ← Red squiggly here
    //                ^^^^^^^^^^^^^^^^
    // Error SM001: Mapper 'BadMapper' should not have constructor parameters
}

// SM002 Error  
public async Task<UserDto> MapAsync(User user) // ← Red squiggly here
//     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
// Error SM002: Mapper 'BadMapper' contains async method 'MapAsync'

// SM003 Error
var result = asyncMethod.Result; // ← Red squiggly here
//                      ^^^^^^
// Error SM003: Mapper 'BadMapper' contains blocking async call '.Result'
```

## Configuration

### Disable Specific Rules

```xml
<!-- In .editorconfig or project file -->
<PropertyGroup>
  <WarningsAsErrors />
  <WarningsNotAsErrors>SM001;SM002;SM003</WarningsNotAsErrors>
</PropertyGroup>
```

### Rule Severity

```xml
<!-- Make rules warnings instead of errors -->
<PropertyGroup>
  <MSBuildTreatWarningsAsErrors>false</MSBuildTreatWarningsAsErrors>
</PropertyGroup>
```

## Benefits

✅ **Catch Issues Early** - At compile time, not runtime  
✅ **Enforce Best Practices** - Team consistency  
✅ **Prevent Deadlocks** - No blocking async calls  
✅ **Performance** - No service injection overhead  
✅ **Testability** - Pure mappers are easier to test  

## Integration with CI/CD

The analyzer works automatically in:
- **Visual Studio** - Live error checking
- **VS Code** - With C# extension  
- **dotnet build** - Command line builds
- **GitHub Actions** - CI/CD pipelines
- **Azure DevOps** - Build pipelines

Your builds will fail if mapper anti-patterns are detected, ensuring code quality! 🛡️ 