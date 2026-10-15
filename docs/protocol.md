# X13 Pro settings report

Observed on owner-confirmed wired USB 1B80:B412, Windows 11, 2026-10-16.
Interface 01 / collection 03, FF00:0001, feature report 3. Windows buffers
include the report ID and are 40 bytes; WebHID/Bridge payloads are 39 bytes.
No handshake, checksum or extra commit report was needed for the tested writes.

| Native byte | API payload byte | Evidence-backed interpretation |
|---|---|---|
| 0 | separate argument | Report ID 3 |
| 1 | 0 | Zero-based DPI selection: 400, 800, 1200, 2400, 3200, 4000, 4800 |
| 2 | 1 | Polling selection: 0=125, 1=250, 2=500, 3=1000 Hz |
| 3 | 2 | Vendor lighting mode in high nibble; static=0, Circle Stream=3; static cycle bit 0 |
| 5..7 | 4..6 | Physical R,B,G channels; vendor UI incorrectly assumes R,G,B |
| Other bytes | corresponding offset minus 1 | Preserved unchanged; no write semantics claimed |

Evidence sources are distinct:

- Native GET_FEATURE(3) reads of the physical device.
- Original vendor HidD_SetFeature arguments intercepted before transmission,
  with every feature write denied. These are API traces, not USB wire captures.
  Independent 800/2400/4000 DPI and 125/250/500/1000 Hz examples establish mappings.
- Explicitly approved native DPI 4000->2400->4000 and polling 1000->500->1000
  writes. Every target and restoration read back exactly, including unknown bytes.
- The same two changes and restoration were exercised in the real local OpenMouse
  UI through official Bridge beta.15, independently cross-checked by native reads.

Important: the vendor's unchanged Apply packet has lighting byte 0x30 while the
actual original mouse reads 0x31. Never replay vendor defaults to change DPI/rate.
The setters read fresh, replace only one byte, send report 3, and verify the entire
reply. Invalid inputs fail before USB access; a transport timeout/failure or
verification mismatch prevents further writes until reconnect.

Static RGB: vendor red/green fixtures expose three bytes, and controlled physical
one-channel trials establish actual R,B,G order. The owner observed native byte 5
as red, byte 6 as blue/bluish-purple, byte 7 as green. The public encoder swaps the
last two channels; decoder reports corrected #rrggbb. Only static mode is enabled.

## Button report 4

Native buffers are 2049 bytes with ID 4; API payloads are 2048 bytes. The first seven
little-endian pointers are offsets relative to the payload, excluding ID. Standard
records are 10 bytes. Physical layout by slot: Left, Right, Middle, DPI, Forward,
Back, and an extra unexposed slot. Captured standard records use tag 0x20|(slot+1),
one mouse-button mask (left=1/right=2/middle=4/back=8/forward=16), six zeros and ffff.
DPI cycle uses tag 0x40|(slot+1) and nine zeros. The formatter at hid.exe 0x40426e
corroborates tags/masks. Controlled Forward->Back and Forward->Right vendor calls
provide independent fixtures. Only same-length recognized standard records are
writable. Unknown/macro slots and the seventh slot are preserved unchanged.

Native and real OpenMouse/Bridge Forward->Back->Forward trials passed complete
report SHA-256 read-back/restoration. Code prevents removal of the last standard
primary click. Report-4 content can contain private macros; never publish complete
raw contents. Shared fixtures contain minimal standard records and full-report
hashes only. Firmware/version and report-5/6 semantics remain unknown.
