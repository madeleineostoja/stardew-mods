# Repository contracts

- Keep installed packages standalone. Do not introduce runtime dependencies between packages in this repository. Shared source must compile into each mod independently.
- Packaging uses an explicit allowlist in `packages.json`. Update it when adding runtime files or assets; unlisted files do not ship. Do not include local configuration, tests, repository metadata, or game assemblies.
- Keep versions independent: content packs use `manifest.json`; C# mods use their `.csproj` version and retain `%ProjectVersion%` in source manifests. Bump only the affected package when preparing a release.
- Preserve upstream attribution, mod IDs, licensing, and JSON-with-comments content in patched packs. Avoid unrelated normalization or reformatting of upstream files.
- Place mods under `mods/` and patched packs under `patches/`. Register each package in `packages.json`. Do not add nested Git repositories.

# Verification

Run the repository tooling tests and package validation, then build the affected package. Inspect ZIP contents when changing packaging to verify that each archive contains one complete mod folder and no unrelated files.

For in-game testing, run `./build-local.sh` to refresh the stable `dist/testing/` folders linked into the game and preserve local configs. `scripts/mods.py build` only creates release packages; it does not deploy them. Local ZIPs use unversioned names; release ZIPs remain versioned.

See [README.md](README.md) for build commands, prerequisites, packaging configuration, and release behaviour.

## Sandboxed worktree builds

If `mise exec` fails because the sandbox blocks writes to mise trust metadata, omit that prefix when the pinned .NET SDK is already on `PATH` (`dotnet --version` should match `global.json`). Do not change permissions outside the worktree.

For compiler-state permission errors in a sandboxed worktree, isolate .NET CLI state and disable shared compilation while preserving the existing NuGet cache. From the repository root, for example:

```sh
DOTNET_CLI_HOME="$PWD/tmp/dotnet-home" \
DOTNET_CLI_TELEMETRY_OPTOUT=1 \
NUGET_PACKAGES="$HOME/.nuget/packages" \
UseSharedCompilation=false \
python3 scripts/mods.py build better-journal
```

Replace the package ID as needed and add `--game-path` if auto-detection is unavailable. If the package cache is configured elsewhere, use that path instead. Setting `DOTNET_CLI_HOME` without preserving `NUGET_PACKAGES` changes the default cache location and can force a restore that fails without network access. These environment settings apply to both the build and C# tests invoked by the packaging command.

## Headless game probes

- On Apple Silicon, probes loading the installed x64 game assembly need an x64 host/runtime; `PlatformTarget=x64` alone isn’t enough.
- Keep probe setup in a fresh temporary directory; never modify the game installation. The game’s bundled runtime can be reused with a copied, retargeted, ad-hoc-signed apphost and combined game/probe dependency metadata.
- Copy—not symlink—the probe’s main DLL so runtime configuration resolves beside it. Other runtime files may be symlinked.
