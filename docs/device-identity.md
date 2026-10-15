# Device identity

Observed on connected hardware 2026-10-16 via Windows PnP + descriptor-only HID APIs. User confirmed that the Redgear X13 Pro is connected and is the only external mouse.

- VID:PID 1B80:B412, USB product MECHREV Gaming Mouse, manufacturer Jtech.
- HidD_GetAttributes VersionNumber: 0x0713. This is the USB device revision field; do not present it as a decoded firmware version.
- MI_00 is the mouse-pointer interface. Opening a zero-access descriptor handle on this collection returned Win32 error 5; PnP still identifies it as a mouse. No privilege change attempted.
- MI_01 contains keyboard, consumer-control and vendor configuration collections.

| MI_01 collection | Usage page:usage | Report type/ID | Windows buffer bytes (including ID) |
|---|---|---|---:|
| COL01 | 0001:0006 | input 1 / output 1 | 8 / 2 |
| COL02 | 000C:0001 | input 2 | 4 |
| COL03 | FF00:0001 | feature 3 | 40 |
| COL04 | FF00:0001 | feature 4 | 2049 |
| COL05 | FF00:0001 | feature 5 | 6 |
| COL06 | FF00:0001 | feature 6 | 8 |

Only report 3 was read. Feature 4/5/6 have not been requested or written. Both native capabilities and report IDs come from HidP_GetCaps/GetValueCaps/GetButtonCaps. They are not the original wire report descriptor. Full descriptor, browser collection shape and cold reconnect tests remain pending.

Sanitized summaries: evidence/sanitized/hid-baseline.json and pnp-baseline.json. Original interface paths/instance suffixes are kept only under evidence/raw. Generic pointer-handle access failures are preserved, not hidden.
