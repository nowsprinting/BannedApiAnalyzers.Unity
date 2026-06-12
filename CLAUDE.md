# BannedApiAnalyzers.Unity — Project Context for Claude

## What This Is

A Roslyn analyzer DLL for Unity 2021.2+, forked from **dotnet/roslyn-analyzers v3.11.0** and adapted to target **Microsoft.CodeAnalysis 3.8.0** (the version bundled in Unity 2021.2+).

Diagnostic rules: RS0030 (SymbolIsBanned), RS0031 (DuplicateBannedSymbol), RS0035 (RestrictedInternalsVisibleTo).

## Build

```bash
dotnet build -c Release        # produces netstandard2.0 DLLs
dotnet test                    # 96 tests, all should pass
```

Place the following two DLLs in your Unity project with the `RoslynAnalyzer` label:
- `Core/bin/Release/netstandard2.0/BannedApiAnalyzers.Unity.dll`
- `CSharp/bin/Release/netstandard2.0/BannedApiAnalyzers.Unity.CSharp.dll`

### NuGet Package

```bash
dotnet build -c Release BannedApiAnalyzers.Unity.slnx   # build CSharp DLL first
dotnet pack Core/BannedApiAnalyzers.Unity.csproj -c Release
```

Output: `Core/bin/Release/BannedApiAnalyzers.Unity.1.0.0.nupkg`

The package places both DLLs under `analyzers/dotnet/cs/` (no `lib/` entry, no declared dependencies) as required for Roslyn analyzer packages. `CSharp.csproj` has `IsPackable=false` and is bundled into this single package.

## Repository Structure

- Core/                                 # analyzer core (netstandard2.0)
- CSharp/                               # C#-specific analyzer (netstandard2.0)
- UnitTests/                            # tests (net10.0)
- Utilities/Compiler/                   # vendored: src/Utilities/Compiler/ from roslyn-analyzers v3.11.0 (115 files)
- nuget/PerformanceSensitiveAnalyzers/  # single file referenced by Analyzer.Utilities.projitems

csproj filenames match their assembly names:
- `Core/BannedApiAnalyzers.Unity.csproj`
- `CSharp/BannedApiAnalyzers.Unity.CSharp.csproj`
- `UnitTests/BannedApiAnalyzers.Unity.Tests.csproj`

## Key Architecture Decisions

### Roslyn version pin
`Directory.Build.props` sets `MicrosoftCodeAnalysisVersion=3.8.0`.
`Analyzer.Utilities.projitems` uses this value to toggle `#if CODEANALYSIS_V3_OR_BETTER` / `CODEANALYSIS_V3_7_OR_BETTER`.
**Never raise this above 3.8.0** — Unity 2021.2 ships Roslyn 3.8, and a higher version will fail to load.

### Analyzer.Utilities.projitems vendoring
The upstream monorepo placed this at `src/Utilities/Compiler/` (3 levels deep). This repo places it at `Utilities/Compiler/` (2 levels deep).
The path to `PerformanceSensitiveAttribute.cs` inside the projitems was adjusted from `..\..\..` to `..\..`.

### ResxSourceGenerator replacement
The Arcade-internal `ResxSourceGenerator` tool is unavailable in a standalone build.
`Core/BannedApiAnalyzerResources.Designer.cs` is a hand-written replacement.

**How the ResourceManager baseName is determined:**
```
{RootNamespace}.{resx filename without extension}
= BannedApiAnalyzers.Unity.BannedApiAnalyzerResources
```
`RootNamespace` defaults to the csproj filename (without extension).
**If you rename the csproj or change `RootNamespace`, you must update the baseName string in `Designer.cs` to match.**

### Namespace and explicit usings
The upstream source used namespace `Microsoft.CodeAnalysis.BannedApiAnalyzers`.
C# namespace nesting made types under `Microsoft.CodeAnalysis.*` accessible without explicit `using` directives.
After renaming to `BannedApiAnalyzers.Unity`, the following `using` directives were added explicitly:
- `using Microsoft.CodeAnalysis;` — Core (4 files), CSharp (2 files), UnitTests (2 files)
- `using Microsoft.CodeAnalysis.CSharp;` — CSharp (2 files)
- `using Microsoft.CodeAnalysis.Text;` — `Core/SymbolIsBannedAnalyzer.cs`

`Text.TextSpan` was also de-qualified to `TextSpan` for the same reason.

### Visual Basic support removed
Unity is C#-only. The VisualBasic project is fully excluded, and all VB tests and helpers have been removed from UnitTests.

## Test Considerations

### Roslyn 3.8 vs 3.11 diagnostic count difference (already handled)
Roslyn 3.11 fires two diagnostics for a banned symbol used in attribute syntax — one for the type reference and one for the constructor call. Roslyn 3.8 fires only one (the type reference).
Six test assertions were adjusted from 2 diagnostics to 1. If new attribute-related tests are added from upstream, apply the same adjustment.

### Minimal test harness vendoring
`UnitTests/TestUtilities/` contains 4 vendored files:
- `CSharpCodeFixVerifier\`2.cs` / `+Test.cs` — test harness wrappers
- `AdditionalMetadataReferences.cs` — `ReferenceAssemblies.Default` + `Microsoft.CodeAnalysis 3.0.0` package
- `WorkItemAttribute.cs`

## What NOT to Do

- Do not raise `MicrosoftCodeAnalysisVersion` above `3.8.0` (breaks Unity load compatibility)
- Do not add `GetPathOfFileAbove` to `Directory.Build.props` (breaks standalone build)
- Do not edit files under `Utilities/Compiler/` without good reason (upstream vendored source)
- If `RootNamespace` changes, update the `ResourceManager` baseName in `Core/BannedApiAnalyzerResources.Designer.cs`

## Source Origin

- Source: dotnet/roslyn-analyzers **v3.11.0** (tag sha `f2384e6`)
- Referenced packages: Microsoft.CodeAnalysis.* **3.8.0**
- Why v3.11.0: last 3.x release; no v3.8.x tag exists (v3.3.4 is followed directly by v3.11.0)
- Why no stable v3.11.0 on nuget.org: the builds are CI rolling artifacts; BannedApiAnalyzers was merged into dotnet/roslyn in March 2025, ending standalone NuGet releases
