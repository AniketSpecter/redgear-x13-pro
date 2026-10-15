import assert from "node:assert/strict";
import test from "node:test";
import { redgearDecodeRawStatus, redgearDecodeStatusPayload } from "../src/protocol.js";
import { RedgearX13HidClient } from "../src/driver.js";
import { X13_NATIVE_STATUS_HEX } from "./status-fixture.js";

const fixture = Uint8Array.from(Buffer.from(X13_NATIVE_STATUS_HEX, "hex"));
function mock() {
  const calls: string[] = [];
  const state = {
    vendorId: 0x1b80, productId: 0xb412, productName: "MECHREV Gaming Mouse", opened: false,
    collections: [{ usagePage: 0xff00, usage: 1, children: [], inputReports: [], outputReports: [],
      featureReports: [{ reportId: 3, items: [{ reportSize: 8, reportCount: 39 }] }] }],
    async open() { state.opened = true; calls.push("open"); },
    async close() { state.opened = false; calls.push("close"); },
    async receiveFeatureReport(id: number) {
      calls.push(`read:${id}`);
      // Exercise nonzero DataView offsets too.
      const bytes = new Uint8Array(44); bytes.set(fixture.subarray(1), 2);
      return new DataView(bytes.buffer, 2, 39);
    },
    async sendFeatureReport() { assert.fail("Driver must never write a feature report"); },
    async sendReport() { assert.fail("Driver must never write an output report"); },
  };
  return { state, device: state as unknown as HIDDevice, calls };
}

test("raw framing validates independent native fixture without interpreting settings", () => {
  const bytes = fixture.slice();
  const parsed = redgearDecodeRawStatus(bytes);
  assert.equal(parsed.payload.length, 39);
  assert.deepEqual(parsed.payload, fixture.slice(1));
  parsed.payload[0] = 0;
  assert.equal(bytes[1], 5);
  for (const length of [0, 1, 39, 41]) assert.throws(() => redgearDecodeRawStatus(new Uint8Array(length)));
  const wrong = fixture.slice(); wrong[0] = 4;
  assert.throws(() => redgearDecodeRawStatus(wrong), /report ID/);
  assert.deepEqual(redgearDecodeStatusPayload(fixture.subarray(1)).payload, fixture.subarray(1));
  for (const length of [0, 38, 40]) assert.throws(() => redgearDecodeStatusPayload(new Uint8Array(length)));
  // Payload byte 0 may itself equal 3; it must never be mistaken for an ID.
  const payload = fixture.slice(1); payload[0] = 3;
  assert.equal(redgearDecodeStatusPayload(payload).payload[0], 3);
});

test("identity matcher accepts only the status collection on the exact VID/PID", () => {
  const { device, state } = mock();
  assert.equal(RedgearX13HidClient.isSupported(device), true);
  state.productId = 0xb413; assert.equal(RedgearX13HidClient.isSupported(device), false);
  state.productId = 0xb412; state.vendorId = 0x1b81; assert.equal(RedgearX13HidClient.isSupported(device), false);
  state.vendorId = 0x1b80; state.collections[0]!.featureReports[0]!.reportId = 4;
  assert.equal(RedgearX13HidClient.isSupported(device), false);
  state.collections[0]!.featureReports[0]!.reportId = 3;
  state.collections[0]!.featureReports[0]!.items[0]!.reportCount = 2048;
  assert.equal(RedgearX13HidClient.isSupported(device), false);
});

test("client performs only GET_FEATURE(3), coalesces reads, and hides unknown settings", async () => {
  const { device, calls } = mock(); const client = new RedgearX13HidClient(device);
  const pending = client.readStatus(); assert.equal(client.readStatus(), pending);
  const status = await pending;
  assert.deepEqual(calls, ["open", "read:3"]);
  assert.equal(status.brand, "Redgear"); assert.equal(status.ui?.settingsReady, false);
  assert.equal(status.ui?.valuesVerified, false); assert.deepEqual(status.firmware, []);
  assert.deepEqual(client.getDpiOptions(), []);
  assert.deepEqual(client.getRawStatusReport(), fixture);
  const copy = client.getRawStatusReport()!; copy[0] = 0;
  assert.equal(client.getRawStatusReport()![0], 3);
  await client.close(); assert.equal(client.getRawStatusReport(), null);
});

test("failed or malformed reads degrade to identity and do not retry until reconnect", async () => {
  const { device, state, calls } = mock();
  state.receiveFeatureReport = async () => { calls.push("read:3"); throw new Error("unavailable"); };
  const client = new RedgearX13HidClient(device);
  assert.match((await client.readStatus()).ui!.statusNote!, /Identity only/);
  await client.readStatus(); assert.deepEqual(calls, ["open", "read:3"]);
  await client.close();
  state.receiveFeatureReport = async () => { calls.push("malformed"); return new DataView(new ArrayBuffer(2)); };
  assert.match((await client.readStatus()).ui!.statusNote!, /Identity only/);
  assert.equal(client.getRawStatusReport(), null);
});

test("disconnect prevents an outstanding reply from repopulating cached status", async () => {
  const { device, state } = mock();
  let resolve!: (value: DataView) => void;
  state.receiveFeatureReport = () => new Promise<DataView>((r) => { resolve = r; });
  const client = new RedgearX13HidClient(device);
  const pending = client.readStatus();
  await new Promise((r) => setImmediate(r));
  await client.close(); resolve(new DataView(fixture.slice(1).buffer));
  await assert.rejects(pending, /disconnected/);
  assert.equal(client.getRawStatusReport(), null);
});

test("timeout falls back to identity without issuing overlapping retries", async () => {
  const { device, state, calls } = mock();
  state.receiveFeatureReport = () => { calls.push("pending-read"); return new Promise<DataView>(() => {}); };
  const client = new RedgearX13HidClient(device);
  assert.match((await client.readStatus()).ui!.statusNote!, /Identity only/);
  await client.readStatus();
  assert.deepEqual(calls, ["open", "pending-read"]);
  await client.close();
});
