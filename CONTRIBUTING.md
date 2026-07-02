# Contributing

Thanks for your interest in improving the BACnet .NET library!

## Building

```sh
dotnet build BACnet.slnx -c Release
dotnet test  BACnet.slnx -c Release
```

The whole solution builds on Windows; on Linux/macOS the `net48` target and the two WinForms
examples are skipped. The examples build as their own solution (`Examples/BACnet.Examples.slnx`).

Package versions are centralised in `Directory.Packages.props` (Central Package Management) — add or
bump versions there, not in individual `.csproj` files.

## Pull requests

- Target the active integration branch (`v4` for the 4.0 line; `master` afterwards).
- Keep changes focused; add or extend tests under `Tests/` for behavioural changes.
- CI (GitHub Actions `build`) must be green.

## Commit messages

Short, imperative subject with a type prefix:

- `fix:` a bug fix
- `new:` a new feature (additive)
- `chore:` / `ci:` / `test:` tooling, CI, tests
- append **`!`** for a breaking change, e.g. `new!: …`

## Versioning & releases

Versioning is tag-driven via [MinVer](https://github.com/adamralph/minver): **the git tag is the
version**. Untagged commits build as `4.0.x-alpha.0.<height>`.

To cut a release (maintainers):

```sh
git tag v4.0.0
git push origin v4.0.0
```

Pushing a `v*` tag triggers `release.yml`, which packs every package and publishes to nuget.org
(via Trusted Publishing) and GitHub Packages. Pre-releases use explicit tags, e.g. `v4.0.0-rc.1`.
