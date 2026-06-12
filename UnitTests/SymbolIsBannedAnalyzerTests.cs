// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.BannedApiAnalyzers.CSharpSymbolIsBannedAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;


namespace Microsoft.CodeAnalysis.BannedApiAnalyzers.UnitTests
{
    // For specification of document comment IDs see https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/documentation-comments#processing-the-documentation-file

    public class SymbolIsBannedAnalyzerTests
    {
        private const string BannedSymbolsFileName = "BannedSymbols.txt";

        private static DiagnosticResult GetCSharpResultAt(int markupKey, DiagnosticDescriptor descriptor, string bannedMemberName, string message)
            => VerifyCS.Diagnostic(descriptor)
                .WithLocation(markupKey)
                .WithArguments(bannedMemberName, message);


        private static async Task VerifyCSharpAnalyzerAsync(string source, string bannedApiText, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { (BannedSymbolsFileName, bannedApiText) },
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        #region Diagnostic tests

        [Fact]
        public async Task NoDiagnosticForNoBannedTextAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync("class C { }");
        }

        [Fact]
        public async Task NoDiagnosticReportedForEmptyBannedTextAsync()
        {
            var source = @"";

            var bannedText = @"";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

        [Fact]
        public async Task NoDiagnosticReportedForMultilineBlankBannedTextAsync()
        {
            var source = @"


";

            var bannedText = @"";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

        [Fact]
        public async Task NoDiagnosticReportedForMultilineMessageOnlyBannedTextAsync()
        {
            var source = @"";
            var bannedText = @"
;first
  ;second
;third // comment";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesAsync()
        {
            var source = @"";
            var bannedText = @"
{|#0:T:System.Console|}
{|#1:T:System.Console|}";

            var expected = new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                .WithLocation(1)
                .WithLocation(0)
                .WithArguments("System.Console");
            await VerifyCSharpAnalyzerAsync(source, bannedText, expected);
        }

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesWithDifferentIdsAsync()
        {
            // The colon in the documentation ID is optional.
            // Verify that it doesn't cause exceptions when building look ups.

            var source = @"";
            var bannedText = @"
{|#0:T:System.Console;Message 1|}
{|#1:TSystem.Console;Message 2|}";

            var expected = new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                .WithLocation(1)
                .WithLocation(0)
                .WithArguments("System.Console");
            await VerifyCSharpAnalyzerAsync(source, bannedText, expected);
        }

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesInDifferentFiles()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { @"" },
                    AdditionalFiles =
                    {
                        ("BannedSymbols.txt", @"{|#0:T:System.Console|}") ,
                        ("BannedSymbols.Other.txt", @"{|#1:T:System.Console|}")
                    },
                },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                        .WithLocation(0)
                        .WithLocation(1)
                        .WithArguments("System.Console")
                }
            };

            await test.RunAsync();
        }

        #region Comments in BannedSymbols.txt tests

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesWithCommentsAsync()
        {
            var source = @"";
            var bannedText = @"
{|#0:T:System.Console|} " + '\t' + @" //comment here with whitespace before it
{|#1:T:System.Console|}//should not affect the reported duplicate";

            var expected = new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                .WithLocation(1)
                .WithLocation(0)
                .WithArguments("System.Console");
            await VerifyCSharpAnalyzerAsync(source, bannedText, expected);
        }

        [Fact]
        public async Task DiagnosticReportedForDuplicateBannedApiLinesWithCommentsInDifferentFiles()
        {
            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { @"" },
                    AdditionalFiles =
                    {
                        ("BannedSymbols.txt", @"{|#0:T:System.Console|} //ignore this") ,
                        ("BannedSymbols.Other.txt", @"{|#1:T:System.Console|} //comment will not affect result")
                    },
                },
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule)
                        .WithLocation(0)
                        .WithLocation(1)
                        .WithArguments("System.Console")
                }
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task NoDiagnosticReportedForCommentLine()
        {
            var source = @"";
            var bannedText = @"// this comment line will be ignored";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

        [Fact]
        public async Task NoDiagnosticReportedForCommentedOutBannedApiLineAsync()
        {
            var bannedText = @"//T:N.Banned";

            var csharpSource = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = new Banned();
        }
    }
}";

            await VerifyCSharpAnalyzerAsync(csharpSource, bannedText);


        }

        [Fact]
        public async Task NoDiagnosticReportedForCommentLineThatBeginsWithWhitespaceAsync()
        {
            var bannedText = @"  // comment here";

            var csharpSource = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = new Banned();
        }
    }
}";

            await VerifyCSharpAnalyzerAsync(csharpSource, bannedText);


        }

        [Fact]
        public async Task NoDiagnosticReportedForCommentedOutDuplicateBannedApiLinesAsync()
        {
            var source = @"";
            var bannedText = @"
//T:System.Console
//T:System.Console";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

        [Fact]
        public async Task DiagnosticReportedForBannedApiLineWithCommentAtTheEndAsync()
        {
            var bannedText = @"T:N.Banned//comment here";

            var csharpSource = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = {|#0:new Banned()|};
        }
    }
}";

            await VerifyCSharpAnalyzerAsync(csharpSource, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));


        }

        [Fact]
        public async Task DiagnosticReportedForInterfaceImplementationAsync()
        {
            var bannedText = "T:N.IBanned//comment here";

            var csharpSource = @"
namespace N
{
    interface IBanned { }
    class C : {|#0:IBanned|}
    {
    }
}";

            await VerifyCSharpAnalyzerAsync(csharpSource, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "IBanned", ""));



        }

        [Fact]
        public async Task DiagnosticReportedForClassInheritanceAsync()
        {
            var bannedText = "T:N.Banned//comment here";

            var csharpSource = @"
namespace N
{
    class Banned { }
    class C : {|#0:Banned|}
    {
    }
}";

            await VerifyCSharpAnalyzerAsync(csharpSource, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));



        }

        [Fact]
        public async Task DiagnosticReportedForBannedApiLineWithWhitespaceThenCommentAtTheEndAsync()
        {
            var bannedText = "T:N.Banned \t //comment here";

            var csharpSource = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = {|#0:new Banned()|};
        }
    }
}";

            await VerifyCSharpAnalyzerAsync(csharpSource, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));


        }

        [Fact]
        public async Task DiagnosticReportedForBannedApiLineWithMessageThenWhitespaceFollowedByCommentAtTheEndMustNotIncludeWhitespaceInMessageAsync()
        {
            var bannedText = "T:N.Banned;message \t //comment here";

            var csharpSource = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = {|#0:new Banned()|};
        }
    }
}";

            await VerifyCSharpAnalyzerAsync(csharpSource, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ": message"));


        }

        #endregion Comments in BannedSymbols.txt tests

        [Fact]
        public async Task CSharp_BannedApiFile_MessageIncludedInDiagnosticAsync()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = {|#0:new Banned()|};
        }
    }
}";

            var bannedText = @"T:N.Banned;Use NonBanned instead";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ": Use NonBanned instead"));
        }

        [Fact]
        public async Task CSharp_BannedApiFile_WhiteSpaceAsync()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = {|#0:new Banned()|};
        }
    }
}";

            var bannedText = @"
  T:N.Banned  ";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact]
        public async Task CSharp_BannedApiFile_WhiteSpaceWithMessageAsync()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = {|#0:new Banned()|};
        }
    }
}";

            var bannedText = @"T:N.Banned ; Use NonBanned instead ";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ": Use NonBanned instead"));
        }

        [Fact]
        public async Task CSharp_BannedApiFile_EmptyMessageAsync()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = {|#0:new Banned()|};
        }
    }
}";

            var bannedText = @"T:N.Banned;";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact]
        public async Task CSharp_BannedApi_MultipleFiles()
        {
            var source = @"
namespace N
{
    class BannedA { }
    class BannedB { }
    class NotBanned { }
    class C
    {
        void M()
        {
            var a = {|#0:new BannedA()|};
            var b = {|#1:new BannedB()|};
            var c = new NotBanned();
        }
    }
}";

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles =
                    {
                        ("BannedSymbols.txt", @"T:N.BannedA") ,
                        ("BannedSymbols.Other.txt", @"T:N.BannedB"),
                        ("OtherFile.txt", @"T:N.NotBanned")
                    },
                },
                ExpectedDiagnostics =
                {
                    GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedA", ""),
                    GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedB", "")
                }
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task CSharp_BannedType_ConstructorAsync()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = {|#0:new Banned()|};
        }
    }
}";

            var bannedText = @"
T:N.Banned";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact]
        public async Task CSharp_BannedNamespace_ConstructorAsync()
        {
            var source = @"
namespace N
{
    class C
    {
        void M()
        {
            var c = {|#0:new N.C()|};
        }
    }
}
";

            var bannedText = @"
N:N";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));
        }

        [Fact]
        public async Task CSharp_BannedNamespace_Parent_ConstructorAsync()
        {
            var source = @"
namespace N.NN
{
    class C
    {
        void M()
        {
            var a = {|#0:new N.NN.C()|};
        }
    }
}
";

            var bannedText = @"
N:N";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));
        }

        [Fact]
        public async Task CSharp_BannedNamespace_MethodGroupAsync()
        {
            var source = @"
namespace N
{
    delegate void D();
    class C
    {
        void M()
        {
            D d = {|#0:M|};
        }
    }
}
";

            var bannedText = @"
N:N";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));
        }

        [Fact]
        public async Task CSharp_BannedNamespace_PropertyAsync()
        {
            var source = @"
namespace N
{
    class C
    {
        public int P { get; set; }
        void M()
        {
            {|#0:P|} = {|#1:P|};
        }
    }
}
";

            var bannedText = @"
N:N";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));
        }

        [Fact]
        public async Task CSharp_BannedNamespace_MethodAsync()
        {
            var source = @"
namespace N
{
    interface I
    {
        void M();
    }
}

class C
{
    void M()
    {
        N.I i = null;
        {|#0:i.M()|};
    }
}";
            var bannedText = @"N:N";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));
        }

        [Fact]
        public async Task CSharp_BannedNamespace_TypeOfArgument()
        {
            var source = @"
namespace N
{
    class Banned {  }
}
class C
{
    void M()
    {
        var type = {|#0:typeof(N.Banned)|};
    }
}
";
            var bannedText = @"N:N";
            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "N", ""));
        }

        [Fact]
        public async Task CSharp_BannedNamespace_Constituent()
        {
            var source = @"
class C
{
    void M()
    {
        var thread = {|#0:new System.Threading.Thread((System.Threading.ThreadStart)null)|};
    }
}
";
            var bannedText = @"N:System.Threading";
            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "System.Threading", ""));
        }

        [Fact]
        public async Task CSharp_BannedGenericType_ConstructorAsync()
        {
            var source = @"
class C
{
    void M()
    {
        var c = {|#0:new System.Collections.Generic.List<string>()|};
    }
}";

            var bannedText = @"
T:System.Collections.Generic.List`1";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "List<T>", ""));
        }

        [Fact]
        public async Task CSharp_BannedType_AsTypeArgumentAsync()
        {
            var source = @"
struct C {}

class G<T>
{
    class N<U>
    { }

    unsafe void M()
    {
        var b = {|#0:new G<C>()|};
        var c = {|#1:new G<C>.N<int>()|};
        var d = {|#2:new G<int>.N<C>()|};
        var e = {|#3:new G<G<int>.N<C>>.N<int>()|};
        var f = {|#4:new G<G<C>.N<int>>.N<int>()|};
        var g = {|#5:new C[42]|};
        var h = {|#6:new G<C[]>()|};
        fixed (C* i = {|#7:&g[0]|}) { }
    }
}";

            var bannedText = @"
T:C";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(3, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(4, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(5, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(6, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(7, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public async Task CSharp_BannedNestedType_ConstructorAsync()
        {
            var source = @"
class C
{
    class Nested { }
    void M()
    {
        var n = {|#0:new Nested()|};
    }
}";

            var bannedText = @"
T:C.Nested";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Nested", ""));
        }

        [Fact]
        public async Task CSharp_BannedType_MethodOnNestedTypeAsync()
        {
            var source = @"
class C
{
    public static class Nested
    {
        public static void M() { }
    }
}

class D
{
    void M2()
    {
        {|#0:C.Nested.M()|};
    }
}";
            var bannedText = @"
T:C";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public async Task CSharp_BannedInterface_MethodAsync()
        {
            var source = @"
interface I
{
    void M();
}

class C
{
    void M()
    {
        I i = null;
        {|#0:i.M()|};
    }
}";
            var bannedText = @"T:I";

            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "I", ""));
        }

        [Fact]
        public async Task CSharp_BannedClass_OperatorsAsync()
        {
            var source = @"
class C
{
    public static implicit operator C(int i) => {|#0:new C()|};
    public static explicit operator C(float f) => {|#1:new C()|};
    public static C operator +(C c, int i) => c;
    public static C operator ++(C c) => c;
    public static C operator -(C c) => c;

    void M()
    {
        C c = {|#2:0|};        // implicit conversion.
        c = {|#3:(C)1.0f|};    // Explicit conversion.
        c = {|#4:c + 1|};      // Binary operator.
        {|#5:c++|};            // Increment or decrement.
        c = {|#6:-c|};         // Unary operator.
    }
}";
            var bannedText = @"T:C";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(3, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(4, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(5, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(6, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public async Task CSharp_BannedClass_PropertyAsync()
        {
            var source = @"
class C
{
    public int P { get; set; }
    void M()
    {
        {|#0:P|} = {|#1:P|};
    }
}";
            var bannedText = @"T:C";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public async Task CSharp_BannedClass_FieldAsync()
        {
            var source = @"
class C
{
    public int F;
    void M()
    {
        {|#0:F|} = {|#1:F|};
    }
}";
            var bannedText = @"T:C";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public async Task CSharp_BannedClass_EventAsync()
        {
            var source = @"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        {|#0:E|} += null;
        {|#1:E|} -= null;
        {|#2:E|}(null, EventArgs.Empty);
    }
}";
            var bannedText = @"T:C";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""),
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public async Task CSharp_BannedClass_MethodGroupAsync()
        {
            var source = @"
delegate void D();
class C
{
    void M()
    {
        D d = {|#0:M|};
    }
}
";
            var bannedText = @"T:C";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public async Task CSharp_BannedClass_DocumentationReferenceAsync()
        {
            var source = @"
class C { }

/// <summary><see cref=""{|#0:C|}"" /></summary>
class D { }
";
            var bannedText = @"T:C";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C", ""));
        }

        [Fact]
        public async Task CSharp_BannedAttribute_UsageOnTypeAsync()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, Inherited = true)]
class BannedAttribute : Attribute { }

[{|#0:Banned|}]
class C { }
class D : C { }
";
            var bannedText = @"T:BannedAttribute";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public async Task CSharp_BannedAttribute_UsageOnMemberAsync()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, Inherited = true)]
class BannedAttribute : Attribute { }

class C 
{
    [{|#0:Banned|}]
    public int SomeProperty { get; }
}
";
            var bannedText = @"T:BannedAttribute";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public async Task CSharp_BannedAttribute_UsageOnAssemblyAsync()
        {
            var source = @"
using System;

[assembly: {|#0:BannedAttribute|}]

[AttributeUsage(AttributeTargets.All, Inherited = true)]
class BannedAttribute : Attribute { }
";

            var bannedText = @"T:BannedAttribute";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public async Task CSharp_BannedAttribute_UsageOnModuleAsync()
        {
            var source = @"
using System;

[module: {|#0:BannedAttribute|}]

[AttributeUsage(AttributeTargets.All, Inherited = true)]
class BannedAttribute : Attribute { }
";

            var bannedText = @"T:BannedAttribute";

            await VerifyCSharpAnalyzerAsync(source, bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute", ""));
        }

        [Fact]
        public async Task CSharp_BannedConstructorAsync()
        {
            var source = @"
namespace N
{
    class Banned
    {
        public Banned() {}
        public Banned(int i) {}
    }
    class C
    {
        void M()
        {
            var c = {|#0:new Banned()|};
            var d = {|#1:new Banned(1)|};
        }
    }
}";

            var bannedText1 = @"M:N.Banned.#ctor";
            var bannedText2 = @"M:N.Banned.#ctor(System.Int32)";

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText1,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned.Banned()", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText2,
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned.Banned(int)", ""));
        }

        [Fact]
        public async Task Csharp_BannedConstructor_AttributeAsync()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, Inherited = true)]
class BannedAttribute : Attribute
{
    public BannedAttribute() {}
    public BannedAttribute(int banned) {}
    public BannedAttribute(string notBanned) {}
}

class C
{
    [{|#0:Banned|}]
    public int SomeProperty { get; }

    [{|#1:Banned(1)|}]
    public void SomeMethod() {}

    [Banned("""")]
    class D {}
}
";
            var bannedText1 = @"M:BannedAttribute.#ctor";
            var bannedText2 = @"M:BannedAttribute.#ctor(System.Int32)";

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText1,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute.BannedAttribute()", ""),
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute.BannedAttribute()", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText2,
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute.BannedAttribute(int)", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "BannedAttribute.BannedAttribute(int)", ""));
        }

        [Fact]
        public async Task CSharp_BannedMethodAsync()
        {
            var source = @"
namespace N
{
    class C
    {
        public void Banned() {}
        public void Banned(int i) {}
        public void Banned<T>(T t) {}

        void M()
        {
            {|#0:Banned()|};
            {|#1:Banned(1)|};
            {|#2:Banned<string>("""")|};
        }
    }

    class D<T>
    {
        public void Banned() {}
        public void Banned(int i) {}
        public void Banned<U>(U u) {}

        void M()
        {
            {|#3:Banned()|};
            {|#4:Banned(1)|};
            {|#5:Banned<string>("""")|};
        }
    }
}";

            var bannedText1 = @"M:N.C.Banned";
            var bannedText2 = @"M:N.C.Banned(System.Int32)";
            var bannedText3 = @"M:N.C.Banned``1(``0)";
            var bannedText4 = @"M:N.D`1.Banned()";
            var bannedText5 = @"M:N.D`1.Banned(System.Int32)";
            var bannedText6 = @"M:N.D`1.Banned``1(``0)";

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText1,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned()", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText2,
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned(int)", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText3,
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned<T>(T)", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText4,
                GetCSharpResultAt(3, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "D<T>.Banned()", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText5,
                GetCSharpResultAt(4, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "D<T>.Banned(int)", ""));

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText6,
                GetCSharpResultAt(5, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "D<T>.Banned<U>(U)", ""));
        }

        [Fact]
        public async Task CSharp_BannedPropertyAsync()
        {
            var source = @"
namespace N
{
    class C
    {
        public int Banned { get; set; }

        void M()
        {
            {|#0:Banned|} = {|#1:Banned|};
        }
    }
}";

            var bannedText = @"P:N.C.Banned";

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""));
        }

        [Fact]
        public async Task CSharp_BannedFieldAsync()
        {
            var source = @"
namespace N
{
    class C
    {
        public int Banned;

        void M()
        {
            {|#0:Banned|} = {|#1:Banned|};
        }
    }
}";

            var bannedText = @"F:N.C.Banned";

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""));
        }

        [Fact]
        public async Task CSharp_BannedEventAsync()
        {
            var source = @"
namespace N
{
    class C
    {
        public event System.Action Banned;

        void M()
        {
            {|#0:Banned|} += null;
            {|#1:Banned|} -= null;
            {|#2:Banned|}();
        }
    }
}";

            var bannedText = @"E:N.C.Banned";

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(1, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""),
                GetCSharpResultAt(2, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned", ""));
        }

        [Fact]
        public async Task CSharp_BannedMethodGroupAsync()
        {
            var source = @"
namespace N
{
    class C
    {
        public void Banned() {}

        void M()
        {
            System.Action b = {|#0:Banned|};
        }
    }
}";
            var bannedText = @"M:N.C.Banned";

            await VerifyCSharpAnalyzerAsync(
                source,
                bannedText,
                GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Banned()", ""));
        }

        [Fact]
        public async Task CSharp_NoDiagnosticClass_TypeOfArgument()
        {
            var source = @"
class Banned {  }
class C
{
    void M()
    {
        var type = {|#0:typeof(C)|};
    }
}
";
            var bannedText = @"T:Banned";
            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

        [Fact]
        public async Task CSharp_BannedClass_TypeOfArgument()
        {
            var source = @"
class Banned {  }
class C
{
    void M()
    {
        var type = {|#0:typeof(Banned)|};
    }
}
";
            var bannedText = @"T:Banned";
            await VerifyCSharpAnalyzerAsync(source, bannedText, GetCSharpResultAt(0, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned", ""));
        }

        [Fact, WorkItem(3295, "https://github.com/dotnet/roslyn-analyzers/issues/3295")]
        public async Task CSharp_BannedAbstractVirtualMemberAlsoBansOverrides_RootLevelIsBannedAsync()
        {
            var source = @"
using System;

namespace N
{
    public abstract class C1
    {
        public abstract void Method1();
        public abstract int Property1 { get; set; }
        public abstract event Action Event1;

        public virtual void Method2() {}
        public virtual int Property2 { get; set; }
        public virtual event Action Event2;
    }

    public class C2 : C1
    {
        public override void Method1() {}
        public override int Property1 { get; set; }
        public override event Action Event1;

        public override void Method2()
        {
            {|RS0030:base.Method2()|};
        }

        public override int Property2 { get; set; }
        public override event Action Event2;

        void M1()
        {
            {|RS0030:Method1()|};
            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

            {|RS0030:Method2()|};
            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
        }
    }

    public class C3 : C2
    {
        public override void Method1()
        {
            {|RS0030:base.Method1()|};
        }

        public override int Property1 { get; set; }
        public override event Action Event1;

        public override void Method2()
        {
            {|RS0030:base.Method2()|};
        }

        public override int Property2 { get; set; }
        public override event Action Event2;

        void M2()
        {
            {|RS0030:Method1()|};
            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

            {|RS0030:Method2()|};
            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
        }
    }
}";

            var bannedText = @"M:N.C1.Method1
P:N.C1.Property1
E:N.C1.Event1
M:N.C1.Method2
P:N.C1.Property2
E:N.C1.Event2";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

        [Fact, WorkItem(3295, "https://github.com/dotnet/roslyn-analyzers/issues/3295")]
        public async Task CSharp_BannedAbstractVirtualMemberBansCorrectOverrides_MiddleLevelIsBannedAsync()
        {
            var source = @"
using System;

namespace N
{
    public abstract class C1
    {
        public abstract void Method1();
        public abstract int Property1 { get; set; }
        public abstract event Action Event1;

        public virtual void Method2() {}
        public virtual int Property2 { get; set; }
        public virtual event Action Event2;
    }

    public class C2 : C1
    {
        public override void Method1() {}
        public override int Property1 { get; set; }
        public override event Action Event1;

        public override void Method2()
        {
            base.Method2();
        }

        public override int Property2 { get; set; }
        public override event Action Event2;

        void M1()
        {
            {|RS0030:Method1()|};
            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

            {|RS0030:Method2()|};
            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
        }
    }

    public class C3 : C2
    {
        public override void Method1()
        {
            {|RS0030:base.Method1()|};
        }

        public override int Property1 { get; set; }
        public override event Action Event1;

        public override void Method2()
        {
            {|RS0030:base.Method2()|};
        }

        public override int Property2 { get; set; }
        public override event Action Event2;

        void M2()
        {
            {|RS0030:Method1()|};
            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

            {|RS0030:Method2()|};
            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
        }
    }
}";

            var bannedText = @"M:N.C2.Method1
P:N.C2.Property1
E:N.C2.Event1
M:N.C2.Method2
P:N.C2.Property2
E:N.C2.Event2";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

        [Fact, WorkItem(3295, "https://github.com/dotnet/roslyn-analyzers/issues/3295")]
        public async Task CSharp_BannedAbstractVirtualMemberBansCorrectOverrides_LeafLevelIsBannedAsync()
        {
            var source = @"
using System;

namespace N
{
    public abstract class C1
    {
        public abstract void Method1();
        public abstract int Property1 { get; set; }
        public abstract event Action Event1;

        public virtual void Method2() {}
        public virtual int Property2 { get; set; }
        public virtual event Action Event2;
    }

    public class C2 : C1
    {
        public override void Method1() {}
        public override int Property1 { get; set; }
        public override event Action Event1;

        public override void Method2()
        {
            base.Method2();
        }

        public override int Property2 { get; set; }
        public override event Action Event2;

        void M1()
        {
            Method1();
            if (Property1 == 42 && Event1 != null) {}

            Method2();
            if (Property2 == 42 && Event2 != null) {}
        }
    }

    public class C3 : C2
    {
        public override void Method1()
        {
            base.Method1();
        }

        public override int Property1 { get; set; }
        public override event Action Event1;

        public override void Method2()
        {
            base.Method2();
        }

        public override int Property2 { get; set; }
        public override event Action Event2;

        void M2()
        {
            {|RS0030:Method1()|};
            if ({|RS0030:Property1|} == 42 && {|RS0030:Event1|} != null) {}

            {|RS0030:Method2()|};
            if ({|RS0030:Property2|} == 42 && {|RS0030:Event2|} != null) {}
        }
    }
}";

            var bannedText = @"M:N.C3.Method1
P:N.C3.Property1
E:N.C3.Event1
M:N.C3.Method2
P:N.C3.Property2
E:N.C3.Event2";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

        [Fact]
        public async Task CSharp_InvalidOverrideDefinitionAsync()
        {
            var source = @"
using System;

namespace N
{
    public class C1
    {
        public void Method1() {}
    }

    public class C2 : C1
    {
        public override void {|CS0506:Method1|}() {}

        void M1()
        {
            Method1();
        }
    }
}";

            var bannedText = @"M:N.C1.Method1";

            await VerifyCSharpAnalyzerAsync(source, bannedText);
        }

























        #endregion
    }
}
