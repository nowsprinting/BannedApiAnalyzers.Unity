# BannedApiAnalyzers for Unity

[![Build](https://github.com/nowsprinting/BannedApiAnalyzers.Unity/actions/workflows/build.yml/badge.svg)](https://github.com/nowsprinting/BannedApiAnalyzers.Unity/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/BannedApiAnalyzers.Unity)](https://www.nuget.org/packages/BannedApiAnalyzers.Unity)

A Unity-focused fork of [Microsoft.CodeAnalysis.BannedApiAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers) that uses [Unity additional files](https://docs.unity3d.com/Manual/roslyn-analyzers-additional-files.html) instead of `BannedSymbols.txt` — **no `.csproj` edits required**.

> [!NOTE]\
> This package is based on [dotnet/roslyn-analyzers](https://github.com/dotnet/roslyn-analyzers) v3.11.0.

> [!NOTE]\
> Unity additional files require Unity 2021.3 or later.

## Analyzer Rules

### RS0030 — Do not use banned APIs

The symbol has been marked as banned in this project, and an alternate should be used instead.

| Item | Value |
|-|-|
| Category | ApiDesign |
| Enabled | True |
| Severity | Warning |
| CodeFix | False |

---

### RS0031 — The list of banned symbols contains a duplicate

The list of banned symbols contains a duplicate.

| Item | Value |
|-|-|
| Category | ApiDesign |
| Enabled | True |
| Severity | Warning |
| CodeFix | False |

---

### RS0035 — External access to internal symbols outside the restricted namespace(s) is prohibited

`RestrictedInternalsVisibleToAttribute` enables a restricted version of `InternalsVisibleToAttribute` that limits access to internal symbols to those within specified namespaces. Each referencing assembly can only access internal symbols defined in the restricted namespaces that the referenced assembly allows.

| Item | Value |
|-|-|
| Category | ApiDesign |
| Enabled | True |
| Severity | Error |
| CodeFix | False |

---

## Usage

### 1. Install the analyzer package

Install BannedApiAnalyzers.Unity from a package registry using either [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) or [UnityNuGet](https://github.com/bdovaz/UnityNuGet).

#### NuGetForUnity

1. Open the NuGetForUnity window via **NuGet > Manage NuGet Packages**
2. Search "BannedApiAnalyzers.Unity" and click **Install**

#### UnityNuGet (hosted on [OpenUPM](https://openupm.com/))

1. Install the package:

   ```bash
   openupm add org.nuget.BannedApiAnalyzers.Unity
   ```

2. Open the `.asmdef` of each assembly you want the analyzer to apply to, and add `BannedApiAnalyzers.Unity_Unity` to its **Assembly Definition References**.

> [!TIP]\
> Analyzers installed via NuGetForUnity apply to all assemblies in the project (including those in the PackageCache), while analyzers installed via UnityNuGet apply only to the referenced assembly and any assemblies that depend on it.

### 2. Create a banned symbols additional file

Create one or more files named according to the pattern `<Filename>.BannedApiAnalyzers.Unity.additionalfile`
(the `<Filename>` part must not contain a period) and place them anywhere under `Assets/`:

- `BannedSymbols.BannedApiAnalyzers.Unity.additionalfile`
- `Platform.BannedApiAnalyzers.Unity.additionalfile` (one file per concern)

Unity automatically discovers `.additionalfile` files in `Assets/` and passes them to the analyzer — **no `.csproj` edits required**.

For more details on Unity's additional files feature, see
[Additional files for Roslyn analyzers and source generators](https://docs.unity3d.com/Manual/roslyn-analyzers-additional-files.html).

### 3. Add entries to the banned symbols file

To add a symbol to the banned list, just add an entry in the format below to one of the additional files (Description Text will be displayed as description in diagnostics, which is optional):

```txt
{Documentation Comment ID string for the symbol}[;Description Text]
```

Comments can be indicated with `//`, in the same way that they work in C#.

For details on ID string format, please refer to ["ID string format"](https://github.com/dotnet/csharpstandard/blob/standard-v6/standard/documentation-comments.md#d42-id-string-format).

Examples of banned symbols entries for symbols declared in the source below:

```csharp
namespace N
{
    class BannedType
    {
        public BannedType() {}
        public int BannedMethod() {}
        public void BannedMethod(int i) {}
        public void BannedMethod<T>(T t) {}
        public void BannedMethod<T>(Func<T> f) {}
        public string BannedField;
        public string BannedProperty { get; }
        public event EventHandler BannedEvent;
    }

    class BannedType<T> {}
}
```

| Symbol in Source                      | Sample Entry in *.BannedApiAnalyzers.Unity.additionalfile
| -----------                           | -----------
| `class BannedType`                    | `T:N.BannedType;Don't use BannedType`
| `class BannedType<T>`                 | ``T:N.BannedType`1;Don't use BannedType<T>``
| `BannedType()`                        | `M:N.BannedType.#ctor`
| `int BannedMethod()`                  | `M:N.BannedType.BannedMethod`
| `void BannedMethod(int i)`            | `M:N.BannedType.BannedMethod(System.Int32);Don't use BannedMethod`
| `void BannedMethod<T>(T t)`           | ```M:N.BannedType.BannedMethod`1(``0)```
| `void BannedMethod<T>(Func<T> f)`     | ```M:N.BannedType.BannedMethod`1(System.Func{``0})```
| `string BannedField`                  | `F:N.BannedType.BannedField`
| `string BannedProperty { get; }`      | `P:N.BannedType.BannedProperty`
| `event EventHandler BannedEvent;`     | `E:N.BannedType.BannedEvent`
| `namespace N`                         | `N:N`

> [!TIP]\
> An entry without a parameter list (e.g., `M:N.BannedType.BannedMethod`) only matches a member that truly has zero parameters.
> For any overloaded method, write the full parameter list — otherwise the entry silently matches nothing.

## License

MIT — see [License.txt](License.txt).
