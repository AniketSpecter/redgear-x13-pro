/**
 * X13 Pro identity and raw status framing. Evidence: docs/testing.md.
 * No settings encoders or speculative field interpretations are provided.
 */
export const REDGEAR_X13_VENDOR_ID = 0x1b80;
export const REDGEAR_X13_PRODUCT_ID = 0xb412;
export const REDGEAR_X13_USAGE_PAGE = 0xff00;
export const REDGEAR_X13_USAGE = 1;
export const REDGEAR_X13_STATUS_REPORT_ID = 3;
export const REDGEAR_X13_STATUS_BYTES = 40;

export const REDGEAR_X13_SUPPORT = {
  name: "Redgear X13 Pro",
  verified: false,
  nativeStatusReadTested: true,
  bridgeStatusReadTested: true,
  settingsReady: false,
} as const;

/** Accept a complete native Windows feature buffer, including its report ID. */
export function redgearDecodeRawStatus(report: Uint8Array): { payload: Uint8Array } {
  if (report.length !== REDGEAR_X13_STATUS_BYTES) {
    throw new RangeError(`X13 Pro status must be ${REDGEAR_X13_STATUS_BYTES} bytes including its report ID`);
  }
  if (report[0] !== REDGEAR_X13_STATUS_REPORT_ID) {
    throw new Error("Unexpected X13 Pro status report ID");
  }
  // Ownership is separate from the input buffer; all settings fields remain opaque.
  return { payload: report.slice(1) };
}

/** WebHID/Bridge return feature data without the separately requested report ID. */
export function redgearDecodeStatusPayload(payload: Uint8Array): { payload: Uint8Array } {
  if (payload.length !== REDGEAR_X13_STATUS_BYTES - 1) {
    throw new RangeError("X13 Pro status payload must be 39 bytes without its report ID");
  }
  return { payload: payload.slice() };
}
