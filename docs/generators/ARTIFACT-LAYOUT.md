# Generator Artifact Layout

## Canonical policy

- `docs/generators/` is the canonical location for generator audit evidence, catalogs, CSV inventories, decision-layer data and runtime-test contracts.
- Root-level generator documents are retained only when they are human-facing master/navigation documents used as release entry points.
- `Data/` and application `*/Data/` copies are runtime/build inputs and are not documentation mirrors. They are intentionally outside this cleanup policy.
- The former top-level `generators/` directory was a byte-for-byte mirror of `docs/generators/` and was removed in v0.2.41.
- Root-level byte-identical copies of generator evidence were removed unless explicitly retained as master documents.

## Rationale

Before v0.2.41 the same generator evidence was commonly stored three times: root, `generators/`, and `docs/generators/`. This made stale-mirror drift possible without adding runtime value. v0.2.41 establishes one evidence source of truth while preserving runtime data copies required by the application.
