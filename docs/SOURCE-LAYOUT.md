# Source Layout

The canonical application implementation lives only in:

`src/TrueCustomDifficulty/`

The solution and both publish scripts target that project directly. Historical duplicate C#/XAML trees were removed in v0.3.0. v0.3.1 also removes root-level documentation/data mirrors so the repository root stays intentionally small.

## Repository root

Only build/navigation entry points live in the root:

```text
.gitignore
README.md
TrueCustomDifficulty.sln
audit_project_consistency.py
publish.sh
publish.ps1
```

Canonical content lives in directories:

- `src/TrueCustomDifficulty/` — the only production C#/XAML project.
- `Data/` — canonical audit/runtime data mirror used by Python CFG tooling.
- `docs/` — all architecture, mapping, audit, policy and generated evidence documentation.
- `tools/` — project audit/verification scripts only. It is **not** the runtime third-party tools directory.

There are no standalone root copies of `parameters.json`, `cfg-writer-baseline.json`, manifests, CSV evidence or audit Markdown.

## Published runtime layout

Both publish scripts emit the portable application under:

```text
publish/win-x64/
  TrueCustomDifficulty.exe
  tools/
    repak.exe
    retoc.exe
  Presets/
    README.md
  Output/
```

`repak.exe` and `retoc.exe` remain user-supplied. Keeping the published runtime under `publish/win-x64/` prevents source audit tooling and runtime third-party tools from sharing the same `tools/` directory.

## Source-tree rules

- Do not recreate root-level or `TrueCustomDifficulty/` C#/XAML mirrors.
- Do not recreate root documentation/data mirrors; update `docs/` or `Data/` directly.
- Do not write publish/runtime artifacts into the repository root.
