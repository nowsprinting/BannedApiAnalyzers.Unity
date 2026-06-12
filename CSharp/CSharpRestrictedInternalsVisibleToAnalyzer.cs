// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using BannedApiAnalyzers.Unity;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BannedApiAnalyzers.Unity
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpRestrictedInternalsVisibleToAnalyzer : RestrictedInternalsVisibleToAnalyzer<NameSyntax, SyntaxKind>
    {
        protected override ImmutableArray<SyntaxKind> NameSyntaxKinds =>
            ImmutableArray.Create(
                SyntaxKind.IdentifierName,
                SyntaxKind.GenericName,
                SyntaxKind.QualifiedName,
                SyntaxKind.AliasQualifiedName);

        protected override bool IsInTypeOnlyContext(NameSyntax node)
            => SyntaxFacts.IsInTypeOnlyContext(node);
    }
}