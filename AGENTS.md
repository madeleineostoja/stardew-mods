# Repository contracts

- Keep installed packages standalone. Do not introduce runtime dependencies between packages in this repository. Shared source must compile into each mod independently.
- Packaging uses an explicit allowlist in `packages.json`. Update it when adding runtime files or assets; unlisted files do not ship. Do not include local configuration, tests, repository metadata, or game assemblies.
- Keep versions independent: content packs use `manifest.json`; C# mods use their `.csproj` version and retain `%ProjectVersion%` in source manifests. Bump only the affected package when preparing a release.
- Preserve upstream attribution, mod IDs, licensing, and JSON-with-comments content in patched packs. Avoid unrelated normalization or reformatting of upstream files.
- Place mods under `mods/` and patched packs under `patches/`. Register each package in `packages.json`. Do not add nested Git repositories.

# Verification

Run the repository tooling tests and package validation, then build the affected package. Inspect ZIP contents when changing packaging to verify that each archive contains one complete mod folder and no unrelated files.

See [README.md](README.md) for build commands, prerequisites, packaging configuration, and release behaviour.
