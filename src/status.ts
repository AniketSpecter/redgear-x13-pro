/** Structural subset of OpenMouse MouseStatus; unknown numeric fields stay hidden. */
export interface MouseLighting {
  zone: string; modes: readonly string[]; mode: string | null; color: string | null; color2: string | null;
  colorModes: readonly string[]; dualColorModes: readonly string[]; reactiveModes: readonly string[];
  speeds: readonly number[]; speed: number | null; brightness?: number | null;
}
export interface MouseStatus {
  brand: "Redgear";
  name: string;
  ui: {
    family: string;
    settingsReady: boolean;
    valuesVerified: boolean;
    hideUnsupportedPollingRates: boolean;
    hideProcessingCard: boolean;
    hideSignalCard: boolean;
    hideSleepCard: boolean;
    dpiStageEditor: { maxStages: number; countEditable: boolean; minDpi: number; maxDpi: number; stepDpi: number };
    defaultDisplayName: string;
    statusNote: string;
  };
  batteryPercent: null;
  batteryState: "Unknown";
  dpi: number;
  pollingRateHz: number;
  supportedPollingRates: number[];
  dpiStages?: number[];
  activeDpiStage?: number;
  lighting?: MouseLighting;
  buttonMappings?: Record<string,string>;
  buttonOptions?: string[];
  fixedButtons?: string[];
  activeProfile: null;
  liftOffDistance: null;
  connectionType: "Wired";
  connectionDetail: string;
  firmware: string[];
}
