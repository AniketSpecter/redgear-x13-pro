# Minimal observed protocol surface

Public sanitized samples are under `fixtures/`. Native and Bridge reads matched on the tested unit; see [testing.md](testing.md). Raw evidence mentioned below is intentionally not distributed.

Read-only framing, not a settings protocol specification.

- Device: USB 1B80:B412, MI_01 COL03, FF00:0001.
- Operation: HidD_GetFeature / WebHID receiveFeatureReport(3). No preliminary SetFeature or output command is used.
- Full native Windows buffer: 40 bytes, byte 0 is report ID 3; remaining 39 bytes are opaque and copied without interpretation. WebHID/Bridge API data excludes the ID and is exactly 39 bytes. Separate decoder entry points enforce each convention.
- Three direct native reads succeeded with identical replies, separated by approximately 100 ms. This is one state sampled three times, not three distinct setting cases or a USBPcap capture.
- Sanitized reply SHA-256: b8141d45a6823d836b7b9e41a6dbbcf6e96fb3d8932aabc9d3d0f701d62d9595.
- Unknown: all payload semantics, checksum/ACK rules, firmware, safe setters and persistence. No encoder exists.

The native C# helper uses a zero-access shared descriptor handle and implements only GET_FEATURE(3). The codec rejects any other report ID or length. Browser/native buffer conventions must remain explicit; do not silently strip bytes based on a coincidental leading 03.
