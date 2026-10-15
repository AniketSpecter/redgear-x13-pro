# Contributing

Run `npm ci` and `npm run check`. CI checks the portable codec/client using independent captured fixtures and mocked devices; it does not claim hardware testing. Native Windows helpers are exercised manually on the exact identified device.

Keep observations, hypotheses and hardware-tested features separate. A USB revision is not necessarily firmware. Three identical reads of one state do not establish field meanings, persistence or settings support.

Before adding a setter, collect several controlled before/after examples, validate framing and ranges, preserve unknown bytes, and obtain owner approval for the first hardware write. Record read-back, original-value restoration and reconnect behavior. Never send speculative commands, firmware updates, bootloader/DFU or calibration operations.

Do not commit vendor binaries/assets, firmware, raw USB captures, serial numbers, device paths, user macros, access tokens or personal information. New shared fixtures should contain only the minimal allowlisted bytes and provenance needed to support a claim.

OpenMouse patches must keep upstream licensing and match current source contracts. Do not mark `verified` true until the claimed features work on the exact device and connection mode. Do not advertise read-only research as a complete configurator.
