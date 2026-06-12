// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

// Hand-written replacement for the ResxSourceGenerator output (which is an Arcade/Roslyn-internal
// tool not available in a standalone build).  The other partial is BannedApiAnalyzerResources.cs.
//
// The ResourceManager baseName must match the EmbeddedResource logical name (minus the ".resources"
// suffix).  The Microsoft.NET.Sdk embeds a *.resx in the project directory as
//   "{AssemblyName}.{FileNameWithoutExtension}.resources"
// which for this project (assembly name "Microsoft.CodeAnalysis.BannedApiAnalyzers") becomes:
//   "Microsoft.CodeAnalysis.BannedApiAnalyzers.BannedApiAnalyzerResources.resources"

using System.Resources;

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers
{
    internal partial class BannedApiAnalyzerResources
    {
        private static ResourceManager? s_resourceManager;

        internal static ResourceManager ResourceManager
            => s_resourceManager ??= new ResourceManager(
                "Microsoft.CodeAnalysis.BannedApiAnalyzers.BannedApiAnalyzerResources",
                typeof(BannedApiAnalyzerResources).Assembly);

        private static string GetResourceString(string key)
            => ResourceManager.GetString(key, null) ?? key;

        // --- Resource keys (9 entries matching BannedApiAnalyzerResources.resx) ---
        // Used via nameof() in SymbolIsBannedAnalyzer and RestrictedInternalsVisibleToAnalyzer.

        internal static string DuplicateBannedSymbolDescription
            => GetResourceString(nameof(DuplicateBannedSymbolDescription));

        internal static string DuplicateBannedSymbolMessage
            => GetResourceString(nameof(DuplicateBannedSymbolMessage));

        internal static string DuplicateBannedSymbolTitle
            => GetResourceString(nameof(DuplicateBannedSymbolTitle));

        internal static string SymbolIsBannedDescription
            => GetResourceString(nameof(SymbolIsBannedDescription));

        internal static string SymbolIsBannedMessage
            => GetResourceString(nameof(SymbolIsBannedMessage));

        internal static string SymbolIsBannedTitle
            => GetResourceString(nameof(SymbolIsBannedTitle));

        internal static string RestrictedInternalsVisibleToDescription
            => GetResourceString(nameof(RestrictedInternalsVisibleToDescription));

        internal static string RestrictedInternalsVisibleToMessage
            => GetResourceString(nameof(RestrictedInternalsVisibleToMessage));

        internal static string RestrictedInternalsVisibleToTitle
            => GetResourceString(nameof(RestrictedInternalsVisibleToTitle));
    }
}
