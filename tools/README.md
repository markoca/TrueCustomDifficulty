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

Override mapping evidence is documented in `../OVERRIDE-VERIFICATION-HARNESS.md`; there is no automated verification harness in this repository.

## WPF aggregate / toggle visibility regression

UI aggregate bindings are maintained directly in WPF source and the UI documentation.

## Mixer fader pilot regression

The mixer contract is described in the UI design documentation; publishing does not run any UI test scripts.

## CFG placement audit

`python tools/audit_cfg_placement.py --output docs/cfg-placement/v0.2.53` verifies all catalog parameters against their concrete CFG writer routes. It is intentionally static and does not launch the application.
