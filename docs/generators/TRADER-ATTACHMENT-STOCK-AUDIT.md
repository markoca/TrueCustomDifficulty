# Trader Attachment Stock Audit

## Decision

**CLOSED NO UI — mixed availability/amount surface.**

A global `Trader Attachment Stock Amount` or `Trader Attachment Availability` control would not map 1:1 to one vanilla mechanism.

## Exact audited surface

- **37 direct attachment rows across 11 trader-local roots.**
- All 37 direct attachment rows in the audited source are base-game rows; **0 DLC1 direct attachment rows** were found.
- **29** rows use `MinCount=1, MaxCount=1`.
- **4** rows use `1..2`.
- **4** rows have no count fields.
- Every direct attachment row uses `Chance`; audited values range from **0.1 to 1.0**.
- No direct attachment row in this audited surface uses `Weight`.

Full row evidence: [`trader-attachment-stock-surface.csv`](trader-attachment-stock-surface.csv).

## Shared trader-only attachment pools

There is no T1 shared attachment pool in the audited source. The reusable pools begin at T2:

| Pool | Rows | Unique items | Item Chance values | Count | Consumer links | Link Chance |
| --- | ---: | ---: | --- | --- | ---: | --- |
| `Trader_Attachments_T2_ItemGenerator` | 5 | 5 | 0.1 / 0.5 / 1.0 | 1/1 | 31 | 1 |
| `Trader_Attachments_T3_ItemGenerator` | 4 | 4 | 0.5 / 1.0 | 1/1 | 17 | 1 |
| `Trader_Attachments_T4_ItemGenerator` | 6 | 5 | 0.5 / 0.7 / 1 | 1/1 | 9 | 1 |

All **57** audited links into T2/T3/T4 attachment pools use parent-link `Chance=1`. That makes the shared pools cleaner than weapons: the parent link is effectively just wiring, while per-item `Chance` is the active availability mechanism inside those pools.

However, the shared pools cover only **15 direct rows** (14 unique item SIDs because T4 contains a duplicate `EN_X4Scope_1` row). The other direct trader attachment rows live in special trader-specific roots and do not all share the same count model.

## Per-root semantics

| Root | Rows | Model | Item Chance values | Count surface | Rank handling |
| --- | ---: | --- | --- | --- | --- |
| `EgerTrader_TradeItemGenerator` | 2 | direct/special | 0.5 | 1/1 | rank-gated |
| `MalakhitTrader_TradeItemGenerator` | 3 | direct/special | 0.5 / 1 | 1/1 | rank-gated |
| `RC_TraderNPC_ItemGenerator` | 4 | direct/special | 1.0 | 1/2 | no rank field |
| `RostokTraderNato_TradeItemGenerator` | 7 | direct/special | 0.2 / 0.5 / 1 | 1/1 | rank-gated |
| `RostokTraderSoviet_TradeItemGenerator` | 1 | direct/special | 0.5 | 1/1 | rank-gated |
| `SultanskTrader_TradeItemGenerator` | 1 | direct/special | 1 | 1/1 | rank-gated |
| `TraderChemicalPlant_TradeItemGenerator` | 1 | direct/special | 1 | ∅/∅ | no rank field |
| `TraderZalesie_TradeItemGenerator` | 3 | direct/special | 1 | ∅/∅ | rank-gated |
| `Trader_Attachments_T2_ItemGenerator` | 5 | shared tier pool | 0.1 / 0.5 / 1.0 | 1/1 | rank-gated |
| `Trader_Attachments_T3_ItemGenerator` | 4 | shared tier pool | 0.5 / 1.0 | 1/1 | rank-gated |
| `Trader_Attachments_T4_ItemGenerator` | 6 | shared tier pool | 0.5 / 0.7 / 1 | 1/1 | rank-gated |

## Why an Amount slider is wrong

`MinCount/MaxCount` is not complete across the domain. Four rows have no count pair, and four RC trader rows use `1..2` while most rows use `1..1`. Scaling only the count-bearing rows would create a partial control. Scaling `1..1` upward would also create duplicate copies of the same attachment rather than increasing the variety of available attachments.

## Why a global Availability slider is still wrong

The shared T2/T3/T4 pools do have a clean per-item `Chance` axis, but they are only one part of trader attachment stock. Direct/special trader roots also carry attachment rows with their own `Chance`, count behavior and rank handling. A single percentage multiplier over every `Chance` would technically be writable, but it would merge multiple authored stock models and would not correspond to one named vanilla control.

That violates the project rule that normal UI controls must represent a complete, observable vanilla semantic surface rather than a synthetic cross-model abstraction.

## Candidate controls considered

| Proposed control | Decision | Reason |
| --- | --- | --- |
| `Trader Attachment Stock Amount` | **REJECT** | Incomplete count surface: 29 rows are 1/1, 4 are 1/2, 4 have no count pair. |
| `Trader Attachment Availability` over all direct rows | **REJECT** | Would synthetically scale several independently authored direct/special trader models. |
| T2/T3/T4 shared-pool attachment availability | **TECHNICALLY CLEAN, NO NORMAL UI** | The 15 shared-pool rows are trader-only and Chance-based, but this covers only shared tier pools, not global attachment stock. |
| Parent pool Chance | **NO VALUE AS CONTROL** | All 57 audited T2/T3/T4 pool links are already Chance=1; the link is wiring, not a useful stock-scaling surface. |

## Final policy

Keep attachment stock vanilla and expose no normal UI control.

If a future advanced/debug control is ever added, the only clean candidate is explicitly scoped **Shared T2–T4 Trader Attachment Availability**. It must state that direct/special trader attachment rows are unaffected. It must not be labeled as global attachment stock.

## DLC note

The audited trader-local source contains no DLC1 direct attachment rows and no DLC1 links to the base T2/T3/T4 attachment pools. This is an evidence statement about the current extracted dataset, not a claim that future DLC revisions cannot add such rows.
