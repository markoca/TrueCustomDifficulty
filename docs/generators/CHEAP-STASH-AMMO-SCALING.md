# Cheap Stash Ammo Quantity Scaling Policy

This document closes the integer-scaling design decision for the implemented **Cheap Stash Ammo Quantity** control.

The source ammo block is:

| Ammo | Weight | Vanilla count |
| --- | ---: | ---: |
| 9×18 (`A918D`) | 40 | `2–3` |
| 9×19 (`A919D`) | 30 | `5–7` |
| 12-gauge (`A012D`) | 30 | `2–3` |

The control changes **only `MinCount` / `MaxCount`**. The vanilla selection weights **40 / 30 / 30 stay unchanged**.

## UI range

Use a percentage control with:

- minimum: **50%**
- maximum: **200%**
- step: **25 percentage points**
- default / vanilla: **100%**

A finer step is intentionally avoided. These are small integer counts, so 5% or 10% movement would frequently produce exactly the same written CFG values and give the user fake precision.

## Integer rule

Every endpoint is always calculated from the **vanilla endpoint**, never from the previously scaled value:

```text
scaledEndpoint = max(1, roundHalfUp(vanillaEndpoint × multiplier))
scaledMax      = max(scaledMin, scaledMax)
```

`roundHalfUp` means positive `.5` values round upward (`2.5 -> 3`, `4.5 -> 5`). This is deliberate; banker's rounding is not used.

The minimum clamp of `1` means the quantity slider can reduce an existing ammo result but can never turn an enabled ammo entry into a zero-count pseudo-entry. If we ever want "no ammo", that must be a separate availability/frequency decision rather than hidden inside quantity scaling.

## Exact results

| Scale | 9×18 (`2–3`) | 9×19 (`5–7`) | 12ga (`2–3`) |
| ---: | ---: | ---: | ---: |
| 50% | `1–2` | `3–4` | `1–2` |
| 75% | `2–2` | `4–5` | `2–2` |
| 100% | `2–3` | `5–7` | `2–3` |
| 125% | `3–4` | `6–9` | `3–4` |
| 150% | `3–5` | `8–11` | `3–5` |
| 175% | `4–5` | `9–12` | `4–5` |
| 200% | `4–6` | `10–14` | `4–6` |

## Why this rule

- **Nearest-integer scaling** keeps the result centered around the requested multiplier instead of systematically biasing quantities downward (`floor`) or upward (`ceil`).
- **Half-up** is predictable to a user: exactly `2.5` becomes `3` rather than sometimes rounding to the nearest even number.
- **Scale from vanilla** prevents cumulative drift when the user moves the slider repeatedly or changes presets.
- **25% UI steps** are coarse enough that movement usually has an observable integer result, while still giving useful states below and above vanilla.
- **The 40/30/30 weights never change**, so this is quantity tuning only, not ammo-type rebalance.

## Implementation status

The integer rule, production-only scope and runtime writer are implemented in **v0.2.6**. The writer does not patch `GamePass_Stash_ItemGenerator_Cheap` globally. Instead it emits TCD-owned full-template Cheap roots and applies the audited redirect manifest only to confirmed production-world paths.

```text
SEMANTICS: READY
SCALING:   READY
SCOPE:     READY (production-only manifest)
UI/WRITER: IMPLEMENTED
RUNTIME:   TEST REQUIRED
```
