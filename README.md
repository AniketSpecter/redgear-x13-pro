# Redgear X13 Pro — open-source read-only utility

Native Windows diagnostics, a TypeScript framing codec, an experimental OpenMouse client, and reviewable OpenMouse integration patches for the **Redgear X-Series X13 Pro**.

**Status: research / read-only.** This project identifies the mouse and reads an opaque status report. It does **not** change DPI, polling, RGB, profiles or buttons. It is not a replacement for the vendor configurator yet.

## Hardware evidence

Tested on one owner-confirmed wired X13 Pro on Windows 11:

| Item | Observed result |
|---|---|
| USB identity | `1B80:B412`, `MECHREV Gaming Mouse`, manufacturer `Jtech` |
| Status collection | Interface 01, collection 03, usage `FF00:0001` |
| Status report | Feature report 3; 40-byte native buffer including ID / 39-byte API payload |
| Native Windows read | Three identical successful reads without preliminary writes |
| Edge direct WebHID | Device opens; feature read fails; driver safely falls back to identity |
| OpenMouse Bridge beta.15 | Three successful matching native reads; local OpenMouse shows BRIDGE and read-only support |
| Firmware version | Unknown; USB revision `0x0713` is not treated as firmware |
| Settings | Opaque and disabled; no write support claimed |

The driver keeps `verified: false`. Recognition and raw reads do not establish full settings support. See [testing](docs/testing.md) and [protocol evidence](docs/protocol.md).

Validation: the standalone package passes its TypeScript build and six targeted tests; its compiled CLI was also exercised against the physical mouse. The full OpenMouse integration passed 2,489 protocol tests and 383 app tests. CI covers Linux/Windows mocked tests and Windows helper compilation without USB access.

## Use the Windows utility

Prerequisites: Node.js 20+ and npm, Windows PowerShell, and the connected mouse. No vendor software, capture driver, Bridge or administrator rights are needed for the direct native status command on the tested machine.

```powershell
git clone https://github.com/AniketSpecter/redgear-x13-pro.git
cd redgear-x13-pro
npm ci
npm run check
npm run status
npm run enumerate
```

Or use the PowerShell helpers without Node:

```powershell
.\scripts\read-feature3.ps1 -Label baseline
.\scripts\enumerate-hid.ps1 -Label baseline
```

The status helper targets only `1B80:B412`, interface 01 / collection 03, report 3, exactly 40 bytes. It refuses ambiguous/missing devices and never calls a report-write API. Local outputs go to ignored `evidence/` directories; enumeration may include other attached devices. Review outputs before sharing them.

Build and decode a recorded report offline:

```powershell
npm run build
node dist/cli.js help
node dist/cli.js decode 0305033100ff00000102030405060700ff000000ff000000ffffff0000ffffff00ffffffff000000
```

`decode` validates framing and preserves the payload; it does not infer settings. Both native framing and WebHID/Bridge payload framing have explicit strict decoders.

## OpenMouse integration

The [patches](patches/README.md) target recorded upstream commits. They register the narrow status collection, hide unsupported settings, and make the limited-support notice visible. The app consumes a local protocol build. Follow the pinned setup instructions before applying the patches; they are not changes to the hosted OpenMouse service.

The reusable client in `src/driver.ts` follows the same read-only logic. It accepts a WebHID-compatible device, validates its identity/report shape, reads only report 3, coalesces reads, and handles timeout/disconnect/failure without issuing writes. Current direct WebHID reads fail on the tested Windows/Edge setup; OpenMouse Bridge works through native HID. Bridge is optional for the standalone PowerShell utility and must be obtained separately from its official project.

## Repository contents

- `src/`: codec, client and small CLI.
- `scripts/`: original read-only Windows HID helpers and offline research tools.
- `test/` and `fixtures/`: mocked transport tests and sanitized physical samples.
- `patches/`: source-only OpenMouse integration patches.
- `docs/`: observed facts, static-analysis references, unknowns and testing limits.

## Contribute

Start with [CONTRIBUTING.md](CONTRIBUTING.md). Settings support needs controlled one-setting captures, independent fixtures, and owner approval before the first write. Do not guess IDs, packet fields or checksums. Firmware, bootloader, DFU and calibration writes are outside this project's scope.

**Not included:** proprietary Redgear/RW01 executables or DLLs, original UI/assets, firmware, raw captures, serial numbers, credentials, or Bridge binaries. The original vendor package is not being relicensed. This repository contains our implementation and sanitized research only.

Licensed **AGPL-3.0-or-later**; see [LICENSE](LICENSE) and [NOTICE](NOTICE) for OpenMouse attribution. Independent community project, not an official Redgear product.
