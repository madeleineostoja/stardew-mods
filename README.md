# Stardew mods

Independently versioned Stardew Valley mods and patched content packs. Each release ZIP contains one complete mod folder, ready to extract into `Stardew Valley/Mods`. No package depends on another package in this repository; SMAPI, Content Patcher, and any other dependencies listed in that mod's manifest must be installed separately.

## Packages

| Package | Source | Version source | Install folder |
| --- | --- | --- | --- |
| Better Forest Farm | [mods/better-forest-farm](mods/better-forest-farm) | `manifest.json` | `BetterForestFarm` |
| Immersive Dialogue UI | [mods/immersive-dialogue-ui](mods/immersive-dialogue-ui) | `manifest.json` | `ImmersiveDialogueUI` |
| Configurable Rumble | [mods/configurable-rumble](mods/configurable-rumble) | `ConfigurableRumble.csproj` | `ConfigurableRumble` |
| Impactful | [mods/impactful](mods/impactful) | `Impactful.csproj` | `Impactful` |
| Overcast | [mods/overcast](mods/overcast) | `Overcast.csproj` | `Overcast` |
| Overslept | [mods/overslept](mods/overslept) | `Overslept.csproj` | `Overslept` |
| Part of a Saloon Breakfast (patched) | [patches/part-of-a-saloon-breakfast-patched](patches/part-of-a-saloon-breakfast-patched) | `manifest.json` | `PartOfASaloonBreakfast` |

See each package's README for installation, requirements, and configuration. Patched upstream packs retain their authorship and licensing; consult their redistribution permissions before publishing them. The breakfast patch replaces the upstream pack, rather than installing alongside it.

## Build and package

Python 3.12+ is sufficient for content packs and repository tooling. C# mods also require .NET SDK 6.0.428 and a Stardew Valley installation with SMAPI, or compatible reference assemblies. `global.json` pins the SDK; `mise.toml` provides the matching tool installation.

From the repository root:

```sh
mise install
python3 -m unittest discover -s tests -v
python3 scripts/mods.py validate

# Build, run the C# tests, and package every mod.
mise exec -- python3 scripts/mods.py build --game-path "/path/to/Stardew Valley"

# Package one content pack; no game or .NET installation needed.
python3 scripts/mods.py build better-forest-farm

# Build, test, and package one C# mod.
mise exec -- python3 scripts/mods.py build impactful --game-path "/path/to/Stardew Valley"
```

On macOS the Steam game path is usually `~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS`. If `--game-path` is omitted, ModBuildConfig attempts to detect the installation automatically. With .NET already on `PATH`, the `mise exec --` prefix is optional.

Output is `dist/<InstallFolder>-<version>.zip`. Builds do not deploy to the game folder. Packaging includes only the mod's own DLL, manifest, and explicitly listed runtime files and documentation; it excludes game assemblies, other mods, tests, local `config.json`, experiments, and repository metadata. Source `%ProjectVersion%` tokens are resolved in the packaged manifest without modifying the source file.

Store Nexus screenshots, videos, and other promotional assets in each package's `media/` folder. These folders are not included in builds or release ZIPs.

## Local game testing

The machine-local `build-local.sh` helper is ignored by Git:

```sh
./build-local.sh
# Or use a different game installation:
./build-local.sh "/path/to/Stardew Valley"
```

This builds and tests every package, writes stable `dist/<InstallFolder>.zip` archives, and refreshes the unversioned `dist/testing/<InstallFolder>/` folders linked into the game's `Mods` directory. Local configs are preserved, removed runtime files are cleaned up, and versioned ZIPs for those packages are removed from `dist/`. Fully restart the game to load rebuilt DLLs.

Use this command for in-game testing. `scripts/mods.py build` creates versioned release packages only; it does not update the testing installation.

## CI and independent releases

[.github/workflows/build-release.yml](.github/workflows/build-release.yml) is the sole workflow. It validates the package registry and tests the packaging tooling, then builds all registered packages in parallel. C# builds use pinned public Stardew/SMAPI reference assemblies, not a redistributed game installation. Each package has its own downloadable Actions artifact.

- **Pull requests and manual runs:** build, test, and upload ZIPs only.
- **Pushes to `main`:** also publish each version that does not already have a release, once every package build has passed.
- **Versions:** bump only the affected package's version source. C# manifests must retain `%ProjectVersion%`.
- **Tags:** namespaced by package ID, e.g. `impactful/v1.3.4` and `overcast/v1.1.2`. Packages can share a version number without sharing a release.
- **Published versions are immutable:** an existing release is skipped, not overwritten. A version bump is needed to distribute changes. API errors, draft releases, and conflicting existing tags fail rather than silently publishing or replacing content.

The first push to `main` publishes all current package versions under their namespaced tags. Old single-repository tags are not used. Configure this repository's GitHub remote before pushing; no remote is assumed. GitHub update keys pointing to individual repositories are not used for monorepo release discovery; existing Nexus update keys remain intact.

## Adding a mod or patch

1. Place the complete source under `mods/<id>/` or `patches/<id>/`, with no nested `.git` directory.
2. Add its entry to [packages.json](packages.json): source path, install folder name, and the runtime files/directories to package. The manifest is always included. C# mods also specify their project and test project.
3. Keep its version in its own manifest or C# project. Do not introduce a repository-wide version.
4. Run validation, tooling tests, and that package's build. CI rejects mod directories with manifests that are missing from the registry.

## Shared code

Build, validation, release, and packaging tooling are shared. Runtime code is not: Impactful's gameplay-triggered camera shake, Overcast's native cloud-shadow patches, and Overslept's overnight rules have no useful common domain layer.

The only plausible small extraction is a shared Generic Mod Config Menu API **source definition**, compiled separately into each mod. Their option registration differs, so keeping the small local interfaces is preferable to adding shared structure. If this contract grows across more mods, source sharing would preserve standalone DLLs without requiring an installed shared library.
