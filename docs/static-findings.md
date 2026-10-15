# Static binary findings

During the static phase vendor files were parsed as bytes. Later, separately approved vendor execution was observed with feature writes blocked; see [testing](testing.md). Full strings/disassembly stay local and ignored. Virtual addresses below assume the PE preferred image base and are static references, not live process addresses. Linear disassembly can include data; conclusions use nearby call/argument flow and hardware corroboration.

hid.exe SHA-256: ef665c1dd608656729c78d1cc003ad44ef50c15c7370e2a499f1b40fb87d808a.

| Observation | Reference | Confidence |
|---|---|---|
| Uses VID 1B80 / PID B412 | VA 00404973/00404978, file offsets 003d73/003d78; object assignments VA 00406add/00406ae7 | High; matches connected hardware |
| HID feature-write wrapper | function VA 00402260; call VA 0040226f -> thunk 0051eb88 -> HidD_SetFeature | Static flow; later vendor API attempts intercepted before transmission |
| HID feature-read wrapper | function VA 00402280; call VA 0040228f -> thunk 0051eb8e -> HidD_GetFeature | High static API flow |
| Reads report 3 with length 0x28 | VA 0040588e pushes length; 0040589d sets report ID 3; 004058a4 calls read wrapper | High; repeated native read succeeds |
| Reads report 4 with length 0x801 | VA 00405c2f length, 00405c41 report ID 4, 00405c48 read wrapper | Static only; not requested |
| Writes buffers of 8, 40 and 2049 bytes | wrapper callers VA 00403595, 00403faf, 004045ad | Static only; purposes/persistence unverified |

hid.exe imports HID.DLL, SetupAPI enumeration and ReadFile/WriteFile. Gaming.exe has UI/config resources and file/process/library APIs but no direct imported HidD_GetFeature/SetFeature entries. A generic WriteFile import is not proof of a USB report path. hid.exe has a path referring to Gaming.exe and a shared RW01 identifier; how the two processes coordinate is not fully mapped. Keyboard/mouse hook DLL functions may support macros; none were installed or activated.

DPI and polling now have independent mapped fields and physical read-back/restoration proof; see [protocol](protocol.md). Static RGB remains a captured candidate pending physical acceptance. Do not replay complete vendor defaults or infer a shared OEM protocol merely from RW01/Jtech naming.
