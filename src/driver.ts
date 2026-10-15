import "./webhid.js";
import type { MouseStatus } from "./status.js";
import {
  REDGEAR_X13_VENDOR_ID, REDGEAR_X13_PRODUCT_ID, REDGEAR_X13_USAGE_PAGE,
  REDGEAR_X13_USAGE, REDGEAR_X13_STATUS_REPORT_ID, REDGEAR_X13_STATUS_BYTES,
  REDGEAR_X13_SUPPORT, redgearDecodeStatusPayload,
} from "./protocol.js";

function hasStatusCollection(collections: readonly HIDCollectionInfo[]): boolean {
  return collections.some((collection) => (
    collection.usagePage === REDGEAR_X13_USAGE_PAGE
    && collection.usage === REDGEAR_X13_USAGE
    && collection.featureReports.some((report) => report.reportId === REDGEAR_X13_STATUS_REPORT_ID
      && report.items.reduce((bits, item) => bits + item.reportSize * item.reportCount, 0)
        === (REDGEAR_X13_STATUS_BYTES - 1) * 8)
  ) || hasStatusCollection(collection.children));
}

/** Identity + opaque status only. This class cannot send output/feature writes. */
export class RedgearX13HidClient {
  readonly device: HIDDevice;
  private pending: Promise<MouseStatus> | null = null;
  private generation = 0;
  private readFailed = false;
  private rawStatus: Uint8Array | null = null;

  constructor(device: HIDDevice) { this.device = device; }

  static isSupported(device: HIDDevice): boolean {
    return device.vendorId === REDGEAR_X13_VENDOR_ID
      && device.productId === REDGEAR_X13_PRODUCT_ID
      && hasStatusCollection(device.collections);
  }

  getDpiOptions(): number[] { return []; }

  async open(): Promise<void> {
    if (!RedgearX13HidClient.isSupported(this.device)) throw new Error("Unsupported X13 Pro HID interface");
    if (!this.device.opened) await this.device.open();
  }

  async close(): Promise<void> {
    this.generation++;
    this.rawStatus = null;
    this.readFailed = false;
    if (this.device.opened) await this.device.close();
  }

  /** For local protocol research; no setting semantics or identifiers are inferred. */
  getRawStatusReport(): Uint8Array | null { return this.rawStatus?.slice() ?? null; }

  readStatus(): Promise<MouseStatus> {
    if (this.pending) return this.pending;
    const task = this.readStatusNow();
    this.pending = task;
    void task.finally(() => { if (this.pending === task) this.pending = null; }).catch(() => undefined);
    return task;
  }

  private async readStatusNow(): Promise<MouseStatus> {
    const generation = this.generation;
    await this.open();
    if (generation !== this.generation) throw new Error("X13 Pro disconnected while opening");
    let note = "Read-only research support. DPI, polling, profiles and lighting are not decoded.";
    if (!this.readFailed) {
      let timer: ReturnType<typeof setTimeout> | undefined;
      try {
        const view = await Promise.race([
          this.device.receiveFeatureReport(REDGEAR_X13_STATUS_REPORT_ID),
          new Promise<never>((_, reject) => { timer = setTimeout(() => reject(new Error("Status read timed out")), 2000); }),
        ]);
        if (generation !== this.generation) throw new Error("X13 Pro disconnected during status read");
        const bytes = new Uint8Array(view.buffer, view.byteOffset, view.byteLength);
        const { payload } = redgearDecodeStatusPayload(bytes);
        this.rawStatus = new Uint8Array(REDGEAR_X13_STATUS_BYTES);
        this.rawStatus[0] = REDGEAR_X13_STATUS_REPORT_ID;
        this.rawStatus.set(payload, 1);
      } catch (error) {
        if (generation !== this.generation) throw error;
        // A timed-out transfer cannot be cancelled by WebHID. Do not issue
        // another transfer on this connection; closing/reopening resets it.
        this.readFailed = true;
        this.rawStatus = null;
      } finally { if (timer !== undefined) clearTimeout(timer); }
    }
    if (generation !== this.generation) throw new Error("X13 Pro disconnected during status read");
    if (this.readFailed) note = "Identity only; status read unavailable. Reconnect to retry. Settings are disabled.";
    return {
      brand: "Redgear", name: REDGEAR_X13_SUPPORT.name,
      ui: { family: "redgear-x13", settingsReady: false, valuesVerified: false,
        defaultDisplayName: REDGEAR_X13_SUPPORT.name, statusNote: note },
      batteryPercent: null, batteryState: "Unknown",
      // Required shared fields use hidden sentinels, never claimed measurements.
      dpi: 0, pollingRateHz: 0, activeProfile: null, liftOffDistance: null,
      connectionType: "Wired", connectionDetail: "USB", firmware: [],
    };
  }
}
