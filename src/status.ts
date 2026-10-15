/** Structural subset of OpenMouse MouseStatus; unknown numeric fields stay hidden. */
export interface MouseStatus {
  brand: "Redgear";
  name: string;
  ui: {
    family: string;
    settingsReady: false;
    valuesVerified: false;
    defaultDisplayName: string;
    statusNote: string;
  };
  batteryPercent: null;
  batteryState: "Unknown";
  dpi: number;
  pollingRateHz: number;
  activeProfile: null;
  liftOffDistance: null;
  connectionType: "Wired";
  connectionDetail: string;
  firmware: string[];
}
