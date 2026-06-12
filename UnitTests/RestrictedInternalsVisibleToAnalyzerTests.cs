// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers.CSharpRestrictedInternalsVisibleToAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;


namespace Microsoft.CodeAnalysis.BannedApiAnalyzers.UnitTests
{
    public class RestrictedInternalsVisibleToAnalyzerTests
    {
        private static DiagnosticResult GetCSharpResultAt(int markupId, string bannedSymbolName, string restrictedNamespaces) =>
            VerifyCS.Diagnostic()
                .WithLocation(markupId)
                .WithArguments(bannedSymbolName, ApiProviderProjectName, restrictedNamespaces);



        private const string ApiProviderProjectName = nameof(ApiProviderProjectName);
        private const string ApiConsumerProjectName = nameof(ApiConsumerProjectName);

        private const string CSharpApiProviderFileName = "ApiProviderFileName.cs";
        private const string VisualBasicApiProviderFileName = "ApiProviderFileName.vb";

        private const string CSharpRestrictedInternalsVisibleToAttribute = @"
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
    internal class RestrictedInternalsVisibleToAttribute : System.Attribute
    {
        public RestrictedInternalsVisibleToAttribute(string assemblyName, params string[] restrictedNamespaces)
        {
        }
    }
}";
        private async Task VerifyCSharpAsync(string apiProviderSource, string apiConsumerSource, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { apiConsumerSource },
                    AdditionalProjects =
                    {
                        [ApiProviderProjectName] =
                        {
                            Sources =
                            {
                                (CSharpApiProviderFileName, apiProviderSource + CSharpRestrictedInternalsVisibleToAttribute),
                            },
                        },
                    },
                    AdditionalProjectReferences = { ApiProviderProjectName },
                },
                SolutionTransforms =
                {
                    ApplySolutionTransforms,
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }


        private static Solution ApplySolutionTransforms(Solution solution, ProjectId apiConsumerProjectId)
        {
            return solution.WithProjectAssemblyName(apiConsumerProjectId, ApiConsumerProjectName);
        }

        [Fact]
        public async Task CSharp_NoIVT_NoRestrictedIVT_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.{|CS0122:C1|} c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_NoRestrictedIVT_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_NoIVT_RestrictedIVT_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""NonExistentNamespace"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.{|CS0122:C1|} c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_BasicScenario_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_BasicScenario_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M({|#0:N1.C1|} c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_MultipleAttributes_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 { }
}

namespace N2
{
    internal class C2 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c1, N2.C2 c2)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_MultipleAttributes_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 { }
}

namespace N2
{
    internal class C2 { }
}

namespace N3
{
    internal class C3 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c1, N2.C2 c2, {|#0:N3.C3|} c3)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N3.C3", "N1, N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_ProjectNameMismatch_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""XYZ"", ""NonExistentNamespace"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_ProjectNameMismatch_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""XYZ"", ""N1"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M({|#0:N1.C1|} c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_NoRestrictedNamespace_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_QualifiedNamespace_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1.N2"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_QualifiedNamespace_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1.N2"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }

    internal class C3 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c1, {|#0:N1.C3|} c3)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C3", "N1.N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_AncestorNamespace_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_AncestorNamespace_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1.N2"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }

    internal class C2 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c1, {|#0:N1.C2|} c2)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C2", "N1.N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_QualifiedAndAncestorNamespace_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"", ""N1.N2"")]

namespace N1
{
    namespace N2
    {
        internal class C1 { }
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.N2.C1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_NestedType_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 { internal class Nested { } }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c, N1.C1.Nested nested)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_NestedType_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1 { internal class Nested { } }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c, {|#0:N1.C1.Nested|} nested)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1.Nested", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInAttributes_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 : System.Attribute
    {
        public C1(object o) { }
    }
}";

            var apiConsumerSource = @"
[N1.C1(typeof(N1.C1))]
class C2
{
    [N1.C1(typeof(N1.C1))]
    private readonly int field;

    [N1.C1(typeof(N1.C1))]
    private int Property { [N1.C1(typeof(N1.C1))] get; }

    [N1.C1(typeof(N1.C1))]
    private event System.EventHandler X;

    [N1.C1(typeof(N1.C1))]
    [return: N1.C1(typeof(N1.C1))]
    int M([N1.C1(typeof(N1.C1))]object c)
    {
        return 0;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInAttributes_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 : System.Attribute
    {
        public C1(object o) { }
    }
}";

            var apiConsumerSource = @"
[{|#16:{|#0:N1.C1|}(typeof({|#1:N1.C1|}))|}]
class C2
{
    [{|#17:{|#2:N1.C1|}(typeof({|#3:N1.C1|}))|}]
    private readonly int field;

    [{|#18:{|#4:N1.C1|}(typeof({|#5:N1.C1|}))|}]
    private int Property { [{|#19:{|#6:N1.C1|}(typeof({|#7:N1.C1|}))|}] get; }

    [{|#20:{|#8:N1.C1|}(typeof({|#9:N1.C1|}))|}]
    private event System.EventHandler X;

    [{|#21:{|#10:N1.C1|}(typeof({|#11:N1.C1|}))|}]
    [return: {|#22:{|#12:N1.C1|}(typeof({|#13:N1.C1|}))|}]
    int M([{|#23:{|#14:N1.C1|}(typeof({|#15:N1.C1|}))|}]object c)
    {
        return 0;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1", "N2"),
                GetCSharpResultAt(1, "N1.C1", "N2"),
                GetCSharpResultAt(2, "N1.C1", "N2"),
                GetCSharpResultAt(3, "N1.C1", "N2"),
                GetCSharpResultAt(4, "N1.C1", "N2"),
                GetCSharpResultAt(5, "N1.C1", "N2"),
                GetCSharpResultAt(6, "N1.C1", "N2"),
                GetCSharpResultAt(7, "N1.C1", "N2"),
                GetCSharpResultAt(8, "N1.C1", "N2"),
                GetCSharpResultAt(9, "N1.C1", "N2"),
                GetCSharpResultAt(10, "N1.C1", "N2"),
                GetCSharpResultAt(11, "N1.C1", "N2"),
                GetCSharpResultAt(12, "N1.C1", "N2"),
                GetCSharpResultAt(13, "N1.C1", "N2"),
                GetCSharpResultAt(14, "N1.C1", "N2"),
                GetCSharpResultAt(15, "N1.C1", "N2"),
                GetCSharpResultAt(16, "N1.C1.C1", "N2"),
                GetCSharpResultAt(17, "N1.C1.C1", "N2"),
                GetCSharpResultAt(18, "N1.C1.C1", "N2"),
                GetCSharpResultAt(19, "N1.C1.C1", "N2"),
                GetCSharpResultAt(20, "N1.C1.C1", "N2"),
                GetCSharpResultAt(21, "N1.C1.C1", "N2"),
                GetCSharpResultAt(22, "N1.C1.C1", "N2"),
                GetCSharpResultAt(23, "N1.C1.C1", "N2")
                );
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInDeclaration_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"
class C2 : N1.C1
{
    private readonly N1.C1 field;
    private N1.C1 Property { get; }
    private N1.C1 this[N1.C1 index] { get => null; }
    N1.C1 M(N1.C1 c)
    {
        return null;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInDeclaration_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 { }
    internal class C2 { }
    internal class C3 { }
    internal class C4 { }
    internal class C5 { }
    internal class C6 { }
    internal class C7 { }
}";

            var apiConsumerSource = @"
class B : {|#0:N1.C1|}
{
    private readonly {|#1:N1.C2|} field;
    private {|#2:N1.C3|} Property { get; }
    private {|#3:N1.C4|} this[{|#4:N1.C5|} index] { get => null; }
    {|#5:N1.C6|} M({|#6:N1.C7|} c)
    {
        return null;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1", "N2"),
                GetCSharpResultAt(1, "N1.C2", "N2"),
                GetCSharpResultAt(2, "N1.C3", "N2"),
                GetCSharpResultAt(3, "N1.C4", "N2"),
                GetCSharpResultAt(4, "N1.C5", "N2"),
                GetCSharpResultAt(5, "N1.C6", "N2"),
                GetCSharpResultAt(6, "N1.C7", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInExecutableCode_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 : System.Attribute
    {
        public C1(object o) { }
        public C1 GetC() => this;
        public C1 GetC(C1 c) => c;
        public object GetObject() => this;
        public C1 Field;
        public C1 Property { get; set; }
    }
}";

            var apiConsumerSource = @"
class C2
{
    // Field initializer
    private readonly N1.C1 field = new N1.C1(null);

    // Property initializer
    private N1.C1 Property { get; } = new N1.C1(null);

    void M(object c)
    {
        var x = new N1.C1(null);    // Object creation
        N1.C1 y = x.GetC();         // Invocation
        var z = y.GetC(x);          // Parameter type
        _ = (N1.C1)z.GetObject();   // Conversion
        _ = z.Field;                // Field reference
        _ = z.Property;             // Property reference
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalMember_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    public class C1
    {
        internal int Field;
    }
}";

            var apiConsumerSource = @"
class C2
{
    int M(N1.C1 c)
    {
        return c.Field;
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalField_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal int Field;
    }
}";

            var apiConsumerSource = @"
class C2
{
    int M(N1.C1 c)
    {
        return {|#0:c.Field|};
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1.Field", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalMethod_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal int Method() => 0;
    }
}";

            var apiConsumerSource = @"
class C2
{
    int M(N1.C1 c)
    {
        return {|#0:c.Method()|};
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1.Method", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    public class C1
    {
    }

    public static class C1Extensions
    {
        internal static int Method(this C1 c1) => 0;
    }
}";

            var apiConsumerSource = @"using static N1.C1Extensions;
class C2
{
    int M(N1.C1 c)
    {
        return c.Method();
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalExtensionMethod_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
    }

    public static class C1Extensions
    {
        internal static int Method(this C1 c1) => 0;
    }
}";

            var apiConsumerSource = @"using static N1.C1Extensions;
class C2
{
    int M(N1.C1 c)
    {
        return {|#0:c.Method()|};
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1.Method", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalProperty_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal int Property { get => 0; }
    }
}";

            var apiConsumerSource = @"
class C2
{
    int M(N1.C1 c)
    {
        return {|#0:c.Property|};
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1.Property", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalEvent_DiagnosticAsync()
        {
            var apiProviderSource = @"using System;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal event EventHandler Event;
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
        _ = {|#0:c.{|CS0070:Event|}|};
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1.Event", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_PublicTypeInternalConstructor_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    public class C1
    {
        internal C1() { }
    }
}";

            var apiConsumerSource = @"
class C2
{
    void M()
    {
        var c = {|#0:new N1.C1()|};
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1.C1", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_Conversions_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal interface I1 { }
    public class C1 : I1 { }
}";

            var apiConsumerSource = @"
class C2
{
    void M(N1.C1 c)
    {
        _ = ({|#0:N1.I1|})c;       // Explicit
        {|#1:N1.I1|} y = c;        // Implicit
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.I1", "N2"),
                GetCSharpResultAt(1, "N1.I1", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInTypeArgument_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1<T>
    {
        public C1<object> GetC1<U>() => null;
    }

    internal class C3 { }
}";

            var apiConsumerSource = @"
using N1;
class C2 : C1<C3>
{
    void M(C1<C3> c1, C1<object> c2)
    {
        _ = c2.GetC1<C3>();
        _ = c2.GetC1<C1<C3>>();
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsageInTypeArgument_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C3 { }
    internal class C4 { }
    internal class C5 { }
    internal class C6 { }

}

namespace N2
{
    internal class C1<T>
    {
        public C1<object> GetC1<U>() => null;
    }
}
";

            var apiConsumerSource = @"
using N1;
using N2;

class C2 : C1<{|#0:C3|}>
{
    void M(C1<{|#1:C4|}> c1, C1<object> c2)
    {
        _ = c2.GetC1<{|#2:C5|}>();
        _ = c2.GetC1<C1<{|#3:C6|}>>();
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C3", "N2"),
                GetCSharpResultAt(1, "N1.C4", "N2"),
                GetCSharpResultAt(2, "N1.C5", "N2"),
                GetCSharpResultAt(3, "N1.C6", "N2"));
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsingAlias_NoDiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N1"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"using ImportedC1 = N1.C1;
class C2
{
    void M(ImportedC1 c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource);
        }


        [Fact]
        public async Task CSharp_IVT_RestrictedIVT_UsingAlias_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C1 { }
}";

            var apiConsumerSource = @"using ImportedC1 = N1.C1;
class C2
{
    void M({|#0:ImportedC1|} c)
    {
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C1", "N2"));
        }


        [Fact]
        [WorkItem(2655, "https://github.com/dotnet/roslyn-analyzers/issues/2655")]
        public async Task CSharp_IVT_RestrictedIVT_InternalOperators_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal class C
    {
        public static implicit operator C(int i) => new C();
        public static explicit operator C(float f) => new C();
        public static C operator +(C c, int i) => c;
        public static C operator ++(C c) => c;
        public static C operator -(C c) => c;
    }
}";

            var apiConsumerSource = @"
using N1;
class C2
{
    void M()
    {
        {|#0:C|} c = {|#1:0|};        // implicit conversion.
        c = {|#2:({|#3:C|})1.0f|};    // Explicit conversion.
        c = {|#4:c + 1|};      // Binary operator.
        {|#5:c++|};            // Increment or decrement.
        c = {|#6:-c|};         // Unary operator.
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                GetCSharpResultAt(0, "N1.C", "N2"),
                GetCSharpResultAt(1, "N1.C.implicit operator N1.C", "N2"),
                GetCSharpResultAt(2, "N1.C.explicit operator N1.C", "N2"),
                GetCSharpResultAt(3, "N1.C", "N2"),
                GetCSharpResultAt(4, "N1.C.operator +", "N2"),
                GetCSharpResultAt(5, "N1.C.operator ++", "N2"),
                GetCSharpResultAt(6, "N1.C.operator -", "N2"));
        }

        [Fact]
        [WorkItem(2655, "https://github.com/dotnet/roslyn-analyzers/issues/2655")]
        public async Task CSharp_IVT_RestrictedIVT_TypeArgumentUsage_DiagnosticAsync()
        {
            var apiProviderSource = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ApiConsumerProjectName"")]
[assembly: System.Runtime.CompilerServices.RestrictedInternalsVisibleTo(""ApiConsumerProjectName"", ""N2"")]

namespace N1
{
    internal struct C {}
}";

            var apiConsumerSource = @"
using N1;
namespace N2
{
    class G<T>
    {
        class N<U>
        { }

        unsafe void M()
        {
            var b = new G<{|#0:C|}>();
            var c = new G<{|#1:C|}>.N<int>();
            var d = new G<int>.N<{|#2:C|}>();
            var e = new G<G<int>.N<{|#3:C|}>>.N<int>();
            var f = new G<G<{|#4:C|}>.N<int>>.N<int>();
            var g = new {|#5:C|}[42];
            var h = new G<{|#6:C|}[]>();
            fixed ({|#7:C|}* i = &g[0]) { }
        }
    }
}";

            await VerifyCSharpAsync(apiProviderSource, apiConsumerSource,
                // Test0.cs(12,27): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(0, "N1.C", "N2"),
                // Test0.cs(13,27): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(1, "N1.C", "N2"),
                // Test0.cs(14,34): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(2, "N1.C", "N2"),
                // Test0.cs(15,36): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(3, "N1.C", "N2"),
                // Test0.cs(16,29): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(4, "N1.C", "N2"),
                // Test0.cs(17,25): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(5, "N1.C", "N2"),
                // Test0.cs(18,27): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(6, "N1.C", "N2"),
                // Test0.cs(19,20): error RS0035: External access to internal symbol 'N1.C' is prohibited. Assembly 'ApiProviderProjectName' only allows access to internal symbols defined in the following namespace(s): 'N2'
                GetCSharpResultAt(7, "N1.C", "N2"));
        }
    }
}
