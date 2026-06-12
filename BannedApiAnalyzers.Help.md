# How to use BannedApiAnalyzers.Unity

BannedApiAnalyzers.Unity is a Unity-focused fork of [Microsoft.CodeAnalysis.BannedApiAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers)
uses [Unity additional files](https://docs.unity3d.com/Manual/roslyn-analyzers-additional-files.html)
instead of `BannedSymbols.txt` configuration files.

Create one or more files named according to the pattern `<Filename>.BannedApiAnalyzers.Unity.additionalfile`
(the `<Filename>` part must not contain a period) and place them anywhere under `Assets/`:

- `BannedSymbols.BannedApiAnalyzers.Unity.additionalfile`
- `Platform.BannedApiAnalyzers.Unity.additionalfile` (one file per concern)

Unity automatically discovers `.additionalfile` files in `Assets/` and passes them to the analyzer — **no `.csproj` edits required**.

For more details on Unity's additional files feature, see
[Additional files for Roslyn analyzers and source generators](https://docs.unity3d.com/Manual/roslyn-analyzers-additional-files.html).

To add a symbol to the banned list, just add an entry in the format below to one of the additional files (Description Text will be displayed as description in diagnostics, which is optional):

```txt
{Documentation Comment ID string for the symbol}[;Description Text]
```

Comments can be indicated with `//`, in the same way that they work in C#.

For details on ID string format, please refer to ["ID string format"](https://github.com/dotnet/csharpstandard/blob/standard-v6/standard/documentation-comments.md#d42-id-string-format).

Examples of banned symbols entries for symbols declared in the source below:

```cs
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
