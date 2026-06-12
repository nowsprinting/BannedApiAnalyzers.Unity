# BannedApiAnalyzers.Unity

A Roslyn analyzer that detects usages of banned APIs, forked from [dotnet/roslyn-analyzers v3.11.0](https://github.com/dotnet/roslyn-analyzers) and adapted for Unity 2021.2+ (Microsoft.CodeAnalysis 3.8).

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

### 1. Add the analyzer DLLs to your Unity project

Place both DLLs under `Assets/` (or a subdirectory) and assign the `RoslynAnalyzer` label to each:

- `BannedApiAnalyzers.Unity.dll`
- `BannedApiAnalyzers.Unity.CSharp.dll`

### 2. Create a `BannedSymbols.txt` configuration file

Add one or more of the following files to your project:

- `BannedSymbols.txt`
- `BannedSymbols.*.txt` (e.g. `BannedSymbols.Platform.txt`)

In Visual Studio, right-click the project in Solution Explorer and choose **Add → New Item**, then select **Text File**. Or create the file manually and reference it in your `.csproj`:

```xml
<ItemGroup>
  <AdditionalFiles Include="BannedSymbols.txt" />
</ItemGroup>
```

### 3. Add entries to the banned symbols file

Each entry uses the [Documentation Comment ID](https://github.com/dotnet/csharpstandard/blob/standard-v6/standard/documentation-comments.md#d42-id-string-format) format, with an optional description after `;`. Lines starting with `//` are treated as comments.

```txt
{Documentation Comment ID}[;Description Text]
```

#### Examples

Given the following source:

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

    class BannedType<T> { }
}
```

| Symbol                            | BannedSymbols.txt entry                                            |
|-----------------------------------|--------------------------------------------------------------------|
| `class BannedType`                | `T:N.BannedType;Don't use BannedType`                              |
| `class BannedType<T>`             | `` T:N.BannedType`1;Don't use BannedType<T> ``                     |
| `BannedType()`                    | `M:N.BannedType.#ctor`                                             |
| `int BannedMethod()`              | `M:N.BannedType.BannedMethod`                                      |
| `void BannedMethod(int i)`        | `M:N.BannedType.BannedMethod(System.Int32);Don't use BannedMethod` |
| `void BannedMethod<T>(T t)`       | `` M:N.BannedType.BannedMethod`1(``0) ``                           |
| `void BannedMethod<T>(Func<T> f)` | `` M:N.BannedType.BannedMethod`1(System.Func{``0}) ``              |
| `string BannedField`              | `F:N.BannedType.BannedField`                                       |
| `string BannedProperty { get; }`  | `P:N.BannedType.BannedProperty`                                    |
| `event EventHandler BannedEvent`  | `E:N.BannedType.BannedEvent`                                       |
| `namespace N`                     | `N:N`                                                              |

## License

MIT — see [License.txt](License.txt).
