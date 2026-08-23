# Versioning

Releases follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes to CLI or library API
- **MINOR**: New features, backward-compatible
- **PATCH**: Bug fixes, backward-compatible

## Changelog

`CHANGELOG.md` uses [Keep a Changelog](https://keepachangelog.com/) format with `Added`, `Changed`, and `Fixed` sections per version. Update it when making a new release.

## Release Process

Releases are published to NuGet as `Nikcio.OpenApiCodeGen`. The release commit follows the pattern `chore(release): Version X.Y.Z`. The GitHub Release triggers the `release.yml` workflow which builds, tests, packs, and pushes to NuGet.
