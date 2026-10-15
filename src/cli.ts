#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { redgearDecodeRawStatus, redgearDecodeSettings, REDGEAR_X13_DPI, REDGEAR_X13_RATES } from "./protocol.js";

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
        payloadHex: Buffer.from(payload).toString("hex"), ...redgearDecodeSettings(payload), settingsDecoded: true }, null, 2));
    } catch (error) {
      console.error(error instanceof Error ? error.message : String(error));
      process.exitCode = 1;
    }
  }
} else if (command === "windows-settings") {
  if (process.platform !== "win32") { console.error("Windows settings require Windows."); process.exitCode=1; }
  else {
    const script=fileURLToPath(new URL("../scripts/windows-settings-ui.ps1",import.meta.url));
    const child=spawnSync("powershell.exe",["-NoProfile","-STA","-File",script],{stdio:"inherit"});
    if(child.error)console.error(child.error.message);process.exitCode=child.status??1;
  }
} else if (command === "dpi" || command === "polling" || command === "color") {
  const allowed: readonly number[] = command === "dpi" ? REDGEAR_X13_DPI : REDGEAR_X13_RATES;
  const value = Number(input);
  if (process.platform !== "win32" || !input || (command === "color" ? !/^#[0-9a-f]{6}$/i.test(input) : !allowed.includes(value))) {
    console.error(`This Windows command requires ${command === "color" ? "#rrggbb" : `one of: ${allowed.join(", ")}`}`); process.exitCode = 1;
  } else {
    const script = fileURLToPath(new URL("../scripts/set-setting.ps1", import.meta.url));
    const child = spawnSync("powershell.exe", ["-NoProfile", "-NonInteractive", "-File", script, "-Feature", command, "-Value", command === "color" ? input : String(value)], {stdio:"inherit"});
    if(child.error) console.error(child.error.message);
    process.exitCode = child.status ?? 1;
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
  console.log("Redgear X13 Pro utility\n\nCommands:\n  status            Read report 3 three times (Windows)\n  enumerate         Inspect native HID capabilities (Windows)\n  decode HEX        Decode a 40-byte native report offline\n  dpi VALUE         Select 400/800/1200/2400/3200/4000/4800 DPI (Windows)\n  polling HZ        Select 125/250/500/1000 Hz (Windows)\n  color '#rrggbb'   Set corrected static RGB (Windows)\n  windows-settings  Open native pointer/scroll/double-click settings\n\nDPI/polling/color changes read the live report and verify read-back. Changes may persist. Standard button remapping is available in the OpenMouse driver. Macros, hardware profiles, firmware and calibration writes are unavailable.");
  if (command && command !== "help" && command !== "--help") process.exitCode = 1;
}
