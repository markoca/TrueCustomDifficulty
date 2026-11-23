"""Single source of truth for the pinned catalog census used by the audits.

Adding a control is a deliberate change: update these numbers here once
(together with the CURRENT doc entry) instead of in a dozen audits.
"""

CATALOG = 497          # parameters.json entries == baseline supportedParameterCount
VISIBLE = 216          # exposeInUi != false
HIDDEN = CATALOG - VISIBLE
EDIT_POINTS = 2871     # cfg-writer-baseline.json editPoints
DIRECT_IDS = 272       # distinct parameterIds with direct edit points
