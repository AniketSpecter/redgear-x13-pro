# Testing checkpoint — 2026-10-16

Owner-confirmed wired X13 Pro on Windows 11, VID:PID `1B80:B412`, product `MECHREV Gaming Mouse`, manufacturer `Jtech`.

## Physical observations

1. Native descriptor-only enumeration identified the status collection as MI_01/COL03, usage FF00:0001, feature report 3 with a 40-byte Windows buffer.
2. The original vendor executable was inspected statically, without execution. It calls HidD_GetFeature for report 3 with length 40. Reference offsets and hash are in [static findings](static-findings.md).
3. Our native helper read that report three times. All 40-byte buffers matched [the sanitized native fixture](../fixtures/feature3-baseline.json).
4. A patched local OpenMouse app in Edge identified/opened the device. Direct WebHID GET_FEATURE(3) failed at the browser API, before decoding. The app visibly fell back to identity only.
5. With owner approval, official portable OpenMouse Bridge v1.0.0-beta.15 was run using an isolated config, no saved profiles, updates off, and startup registration disabled. The download matched its published SHA-256. No Bridge binary is distributed here.
6. The local app displayed a BRIDGE badge and its successful read-only research notice. Three additional GET_FEATURE(3) reads through Bridge returned 39-byte payloads matching the native fixture after the native report-ID prefix was removed. See [Bridge fixture](../fixtures/bridge-feature3.json).

No vendor executable or setting-write operation was needed. No firmware command or capture driver was installed. The cause of Edge's direct read failure is unproven; a browser/Windows collection-routing or buffer issue is a hypothesis.

## Automated checks

The OpenMouse integration baseline passed 2,483 protocol tests and 383 app tests. With this driver, full checks passed 2,489 protocol tests and 383 app tests. Six targeted driver/codec tests cover physical framing, strict identity/collection matching, nonzero DataView offsets, no report writes, read failures/malformed frames, cache ownership, concurrent reads, timeout and disconnect invalidation.

The standalone package runs those six targeted tests and a TypeScript build/package inspection. CI uses mocked transport, not physical USB hardware.

## Still unverified

Payload field meanings, DPI/rate/RGB/profile/button settings, persistence, restoration, firmware identity, raw wire report descriptor, physical unplug/replug/cold start, other units/PIDs/OSes, and every write feature. `verified` remains false.
