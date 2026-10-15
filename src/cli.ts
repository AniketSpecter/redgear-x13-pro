#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { redgearDecodeRawStatus } from "./protocol.js";

const [command, input] = process.argv.slice(2);
if (command === "decode") {
  if (!input || !/^[0-9a-fA-F]{80}$/.test(input)) {
    console.error("Provide exactly 40 bytes of hex including report ID 03.");
    process.exitCode = 1;
  } else {
    try {
      const report = Uint8Array.from(Buffer.from(input, "hex"));
      const { payload } = redgearDecodeRawStatus(report);
      console.log(JSON.stringify({ reportId: report[0], payloadBytes: payload.length,
        payloadHex: Buffer.from(payload).toString("hex"), settingsDecoded: false }, null, 2));
    } catch (error) {
      console.error(error instanceof Error ? error.message : String(error));
      process.exitCode = 1;
    }
  }
} else if (command === "status" || command === "enumerate") {
  if (process.platform !== "win32") {
    console.error("Native HID utility commands require Windows. Offline decode and tests work cross-platform.");
    process.exitCode = 1;
  } else {
    const script = fileURLToPath(new URL(command === "status"
      ? "../scripts/read-feature3.ps1" : "../scripts/enumerate-hid.ps1", import.meta.url));
    const label = `run-${Date.now()}`;
    const child = spawnSync("powershell.exe", ["-NoProfile", "-NonInteractive", "-File", script, "-Label", label], { stdio: "inherit" });
    if (child.error) console.error(child.error.message);
    process.exitCode = child.status ?? 1;
  }
} else {
  console.log("Redgear X13 Pro read-only utility\n\nCommands:\n  status       Read report 3 three times (Windows)\n  enumerate    Inspect native HID capabilities (Windows)\n  decode HEX   Validate a 40-byte native report offline\n\nNo setting-write, firmware or calibration commands are implemented.");
  if (command && command !== "help" && command !== "--help") process.exitCode = 1;
}
