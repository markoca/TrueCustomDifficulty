True Custom Difficulty - user-supplied build tools
=================================================

True Custom Difficulty does not include, download, or redistribute third-party
tool executables. To build a non-empty mod, provide BOTH Windows x64 programs
listed below, using the exact names shown:

    tools/repak.exe
    tools/retoc.exe

These files are resolved relative to TrueCustomDifficulty.exe. The application's
Build Difficulty action uses the locally supplied executables, not copies from
your PATH. On Linux, use Windows builds because the application runs under Wine.

DOWNLOAD SOURCES
================

repak.exe
---------
Original project:
    https://github.com/trumank/repak

Releases:
    https://github.com/trumank/repak/releases

Download the Windows x64 executable. If the release is archived, extract the
actual executable and name it repak.exe. Place it in:

    tools/repak.exe

True Custom Difficulty invokes repak with:
    pack ... --mount-point ../../../ --version V11

It creates the .pak archive containing the generated CFG/BPatch files.


retoc.exe
---------
Original project:
    https://github.com/trumank/retoc

Releases:
    https://github.com/trumank/retoc/releases

Download the Windows x64 executable. If the release is archived, extract the
actual executable and name it retoc.exe. Place it in:

    tools/retoc.exe

True Custom Difficulty invokes retoc with:
    to-zen ... --version UE5_5

It creates the matching .utoc and .ucas IoStore companion files.

The version numbers that may be listed for repak/retoc in Localization
Workbench documentation are baselines for THAT project, not independently
verified compatibility guarantees for True Custom Difficulty. Use a release
whose command-line interface supports the options above.


EXPECTED PORTABLE LAYOUT
========================

TrueCustomDifficulty.exe
tools/
    README.txt
    repak.exe
    retoc.exe
Presets/
Output/

You do NOT need UAssetGUI, S2HOCMM, or Mappings.usmap for this application's
normal Build Difficulty workflow. Those belong to Localization Workbench's
different extraction/localization workflow.


USAGE & NOTES
=============

1. Put both .exe files into the tools folder BESIDE the published app.
2. Start TrueCustomDifficulty.exe and configure your difficulty.
3. Select Build Difficulty. If the chosen settings produce CFG edits, the
   application uses repak and retoc and writes the generated .pak/.utoc/.ucas
   triplet to the adjacent Output/ folder.
4. If no CFG edits are needed, the app may clear the old Output/ without
   invoking either executable.
5. If a program is missing, the app reports its expected location and will
   not produce an incomplete mod. If a supplied tool fails, inspect the
   application's error details and check its CLI/version compatibility.

The republish script preserves user-supplied executables and files in tools/.
It installs this README only when missing; it does not overwrite your edited
copy. Do not place third-party binaries in Git or share them as part of this
project. Download them from the original authors.

For developer audits/scripts, see tools/README.md in the SOURCE repository.
