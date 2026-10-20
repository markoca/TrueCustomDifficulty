# True Custom Difficulty — tools

The `tools/` folder serves **two different purposes**. In a published portable installation it holds **user-supplied executables**; in the source repository it also contains **first-party development and audit scripts**. These are not interchangeable.

## Player setup: download the required build tools

**Build Difficulty** requires these two third-party Windows x64 executables for builds containing CFG edits. The project does **not** bundle or download them.

| Tool | Official source | What True Custom Difficulty uses it for |
| --- | --- | --- |
| **repak.exe** | [Project](https://github.com/trumank/repak) · [Releases](https://github.com/trumank/repak/releases) | Pack the generated CFG/BPatch files as a PAK V11 archive, mount point `../../../` |
| **retoc.exe** | [Project](https://github.com/trumank/retoc) · [Releases](https://github.com/trumank/retoc/releases) | Create the matching Unreal Engine 5.5 `.utoc`/`.ucas` IoStore files |

Download the Windows x64 versions from the linked original releases. Extract the actual executables and rename them to the exact filenames above if required.

```text
publish/win-x64/
├── TrueCustomDifficulty.exe
├── tools/
│   ├── README.txt
│   ├── repak.exe          ← supplied by the user
│   └── retoc.exe          ← supplied by the user
├── Presets/
└── Output/
```

The application resolves this directory via `AppContext.BaseDirectory/tools` and invokes `repak pack ... --mount-point ../../../ --version V11` and `retoc to-zen ... --version UE5_5`. When no CFG edits are generated, the build pipeline may finish without either packer. If a required executable is missing for a non-empty build, the app reports its expected path.

**Important:** These tools are for True Custom Difficulty's build workflow only. Unlike Localization Workbench, this application does **not** need `UAssetGUI.exe`, `S2HOCMM.exe`, or `Mappings.usmap`. Workbench's published version baselines do not establish version compatibility with this project. The published `tools/README.txt` contains the same installation information in a plain-text format modeled on [Localization Workbench's tools README](https://github.com/markoca/STALKER2LocalizationTool/blob/main/tools/README.txt).

### Publish behavior

`./publish.sh` publishes a Windows x64 app into `publish/win-x64/`, **preserves** existing `tools/`, `Presets/`, and `Output/`, and copies `tools/README.txt` into the portable tools folder only if absent. An edited README or user-provided binary is not replaced. The repository does not ship, download, or commit third-party binaries.

## Developer tools (source checkout only)

Execute the scripts below **from the repository root**, not from the published `tools/` directory. Use Python 3 for Python-based audits. Many source-oriented checks require the original, independently supplied S.T.A.L.K.E.R. 2 CFG dumps; these archives are not included in the repository.

### Main test and audit entry point

```bash
python3 tools/pre_release.py list
python3 tools/pre_release.py static --output audit/pre-release/static
python3 tools/pre_release.py cfg-routes --output audit/pre-release/cfg-routes
python3 tools/pre_release.py source --output audit/pre-release/source
# On Linux with supported .NET/Windows targeting + Wine:
python3 tools/pre_release.py writer --wine --output audit/pre-release/writer
```

- `static` — source/catalog/UI/preset and project-consistency checks. It does not execute the production C# writer.
- `cfg-routes` — source-to-CFG route/value/ownership mapping. Static evidence is **not** executed BPatch output.
- `source` — source provenance and native CFG checks; some require supplied base-game/DLC CFG archives.
- `writer` — the production C# writer contract projects; `--wine` selects the Linux/Wine harness. A writer PASS still does **not** verify mod loading or gameplay.
- `all` — orchestrated test modes; manual game validation remains separate.

For the detailed test matrix and evidence rules see [release testing](../docs/RELEASE-TESTING.md) and [CFG writer audit scope](../docs/CFG-WRITER-AUDIT-SCOPE.md).

### Useful focused commands

```bash
python3 tools/audit_project_consistency.py
python3 tools/audit_ui_cfg_contract.py
python3 tools/audit_stashes_current.py
python3 tools/audit_stashes_mixer_ui.py
python3 tools/audit_factory_profile_role_coverage.py
python3 tools/audit_publish_directory_contract.py
python3 tools/audit_final_cfg_mapping.py --out audit/final-cfg-mapping
```

The `audit_publish_directory_contract.py` test uses a **fake** .NET publish executable to test repeated publication, safe rollback and preservation of user files. It does not build the WPF application or verify Wine behavior. The other static scripts likewise cannot prove in-game loot probabilities, gameplay effects, or installed mod load order.

### Inventory, generators, and committed evidence

| Area | Source-only tools or evidence | Further documentation |
| --- | --- | --- |
| Spawn-referenced item SIDs | `generate_spawn_item_sids.py`, `audit_spawn_item_sids.py` | [Spawnable item SID inventory](../docs/SPAWNABLE-ITEM-SIDS.md) |
| Ordinary stash actor regions | `generate_stash_actor_regions.py`, `audit_stash_actor_regions.py`, `data/stalker2-community-regions.json` | [Stash regions](../docs/STASH-REGIONS.md) |
| Regular stash provenance | `audit_regular_stash_actor_dump.py`, `audit_regular_stash_source_inventory.py`, `regular-stash-source-inventory.json` | [Generator evidence index](../docs/generators/README.md) |
| Production CFG routing | `audit_final_cfg_mapping.py`, `final_cfg_mapping_inventory.json` | [CFG writer audit scope](../docs/CFG-WRITER-AUDIT-SCOPE.md) |
| Role/preset decisions | `audit_factory_profile_role_coverage.py`, `factory_profile_role_coverage_2026_10_19.json`, `factory_preset_*.json` | [Factory presets](../docs/FACTORY-PRESETS.md) |
| Additional audits | `audit_*.py`, `test_*.py`, `generate_*.py` | [Documentation index](../docs/README.md) |

Example: to regenerate the source-backed spawn SID inventory with locally supplied extracted CFG ZIPs:

```bash
python3 tools/generate_spawn_item_sids.py \
  --game "/path/to/STALKER2-GameData-CFG.zip" \
  --dlc "/path/to/STALKER2-DLC1-CFG.zip"
python3 tools/audit_spawn_item_sids.py
```

Generated audit receipts, logs, fixtures, and reports belong under the ignored `audit/` working directory (unless the specific tool documents a different output). Do not mistake source validation or generated file checks for verified in-game behavior.

## Development rules

Keep `docs/UI-DESIGN-LOCK.md` and `docs/VISUAL-CONTROL-SYSTEM.md` synchronized with live UI controls and the parameter catalog. Treat `main` as the source of truth; code, applicable audits and documentation must change together. Neither repository audits nor `./publish.sh` install third-party binaries.
