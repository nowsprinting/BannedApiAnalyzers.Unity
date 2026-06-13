# Publishing to nuget.org

Pushing a `vX.Y.Z` tag triggers the `publish` job in `.github/workflows/build.yml`, which automatically publishes the package to nuget.org via NuGet Trusted Publishing (OIDC).

## Prerequisites

- The `<Version>` in `Core/BannedApiAnalyzers.Unity.csproj` must match the tag (without the `v` prefix).  
  The CI job verifies this and fails with an error if they differ.
- The GitHub repository must be configured for NuGet Trusted Publishing.  
  The `publish` job uses `NuGet/login` with `secrets.NUGET_USER` to obtain an OIDC-based API key.

## Steps

1. Update the version in `Core/BannedApiAnalyzers.Unity.csproj`:
   ```xml
   <Version>X.Y.Z</Version>
   ```

2. Commit and push the change to `master`.

3. Push a tag that matches the version:
   ```bash
   git tag vX.Y.Z
   git push origin vX.Y.Z
   ```

The CI will:
1. Run the `build` job (build → test → pack → upload artifact).
2. Run the `publish` job only if `build` succeeds:
   - Download the `.nupkg` artifact.
   - Verify the tag version equals the package version extracted from the `.nuspec` inside the archive.
   - Authenticate with nuget.org via OIDC.
   - Push the package (`--skip-duplicate`, so re-pushing an existing version is a no-op rather than an error).

> [!CAUTION]\
> Pushing a tag without updating `<Version>` first will fail the version-check step and block the publish.
