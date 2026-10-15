# Roadmap

Completed: exact tested USB identity, parsed capabilities, independent native status fixture, Bridge status fixture, read-only native utility, strict framing codec, timeout/disconnect handling, and limited local OpenMouse integration.

Next milestones:

1. Capture the full wire report descriptor and repeat physical cold-start/unplug/replug acceptance.
2. With owner approval, install an official USB capture tool and observe the original vendor configurator's startup separately. Startup may write cached settings; never assume it is passive.
3. Capture one known setting at a time, record old/new values and timestamps, and restore the original. Obtain approval for the first physical write for each feature. Keep raw captures local and sanitize only the needed evidence.
4. Establish actual status-field meanings, checksums if present, ACK behavior and persistence. Use multiple independent examples; UI labels alone are not packet evidence.
5. Add one proven read/write feature with validation, read-back and independent fixtures. DPI, polling, RGB and buttons are separate claims.
6. Submit focused upstream contributions when each claimed feature is reviewable and physically tested. Keep unverified controls disabled.

Firmware, bootloader, DFU, EEPROM/calibration and speculative commands remain excluded. This repository does not promise support for other Redgear models or different X13 Pro hardware revisions.
