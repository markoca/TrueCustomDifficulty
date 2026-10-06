# World Detectors

Version: 0.9.26.44

`World Detectors` is a simple ON/OFF World Loot toggle.

- **ON** preserves vanilla audited world detector spawns.
- **OFF** removes only the proven active non-quest world detector surface.

## Production surface

Base game:

- Echo x2 direct pickups
- Gilka x2 direct pickups
- **4 total**

With DLC1:

- the same 4 base pickups
- Gilka x3 additional direct pickups
- Bear x1 in the active X16 stash
- **8 total**

## Exclusions

The toggle does not touch:

- quest/reward detector generators;
- trader detector stock;
- NPC/named-character detector loadouts;
- player gear presets;
- test-map detector actors;
- the base Gilka actor with `SpawnOnStart=false`;
- deferred/quest stash actors;
- orphan detector stash generators without a proven active world consumer.

The X16 stash Bear is the only proven active, non-quest stash detector reached by the physical WorldMap generator graph.

Status: `runtime_test_required`.
