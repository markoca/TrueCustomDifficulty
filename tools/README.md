# External tools

This directory is intentionally empty in the source/release.

True Custom Difficulty does **not** bundle or download third-party executables. Users supply them manually.

Published layout:

```text
TrueCustomDifficulty.exe
tools/
  repak.exe
  retoc.exe
```

## Required for Build Mod

A non-empty v0.1.51 build requires both tools:

- **repak** packs the generated BPatch CFG files into `TrueCustomDifficulty_P.pak` using PAK V11 and mount point `../../../`.
- **retoc** creates the matching UE5.5 `TrueCustomDifficulty_P.utoc` and `TrueCustomDifficulty_P.ucas` companion container.

The application does not download either executable. If one is missing, Build Mod stops with a clear error and does not publish a partial output.

The same tools may also be used manually for extraction/development/maintenance workflows, but those workflows are outside the normal player build path.

## Override verification harness

`verify_cfg_overrides.py` verifies all visible runtime-pending writer routes from the locked catalog/baseline, including direct edit points, PSY/mutant arrays and generator clone/redirect surfaces. `plan` and `suite` emit field-level plus struct/reference contracts and exact reference BPatch trees; `compare` checks a real isolated production staging tree and fails on missing, unexpected or wrong-value assignments **or structural mode/reference drift**. See `../OVERRIDE-VERIFICATION-HARNESS.md`.

## CFG placement audit

`python tools/audit_cfg_placement.py --output docs/cfg-placement/v0.2.53` verifies all catalog parameters against their concrete CFG writer routes. It is intentionally static and does not launch the application.
