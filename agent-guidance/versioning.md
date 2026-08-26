# Versioning

Releases follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes to CLI or library API
- **MINOR**: New features, backward-compatible
- **PATCH**: Bug fixes, backward-compatible

## Changelog

`CHANGELOG.md` uses [Keep a Changelog](https://keepachangelog.com/) format with `Added`, `Changed`, and `Fixed` sections per version. It is maintained automatically by [Release Please](https://github.com/googleapis/release-please) based on Conventional Commits.

## Release Process

Releases are published to NuGet as `Nikcio.OpenApiCodeGen`.

1. **Each merged PR** (using Conventional Commits) updates the Release Please pull request, which accumulates version bumps and changelog entries.
2. **Merge the Release Please PR** when ready to release. Release Please then:
   - Updates `CHANGELOG.md` and the version in the CLI `.csproj`
   - Creates a `vX.Y.Z` git tag and a GitHub Release
3. **The GitHub Release** triggers the `release.yml` workflow which builds, tests, packs, and pushes to NuGet.

Commit messages are linted in CI via `commitlint` (see `.commitlintrc.json`). PRs must use Conventional Commits to pass the `commit-lint` job.
