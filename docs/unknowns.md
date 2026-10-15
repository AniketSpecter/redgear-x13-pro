# Hypotheses and unknowns

Observed: exact USB identity; vendor-defined feature IDs/lengths; static API wrappers; three identical successful GET_FEATURE(3) replies. User identifies the physical product as X13 Pro.

Inferred: the RW01 program family uses separate feature collections for status/configuration/macro data. Their precise roles are not established except for the 40-byte read path. MECHREV/Jtech naming suggests OEM reuse but is not evidence of compatibility with any existing OpenMouse codec. Source searches found no current Redgear/RW01/1B80/B412 match.

Unverified: meaning of every payload field, DPI values/stage, polling rate, lighting, profiles, report-4 contents, checksums, ACK behavior, reconnect persistence, firmware identity, browser collection access, Bridge requirement. Byte changes must be tied to controlled known settings before assigning semantics.

Do not use skin labels as packet constants or infer firmware from USB revision 0x0713. Do not request report 4 merely to explore it; it may contain user macro data.
