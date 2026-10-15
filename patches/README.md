# Apply the OpenMouse integration

These source patches include original AGPL OpenMouse context; see LICENSE and NOTICE. They contain no vendor binary or raw capture.

Tested baselines:

- `mouse-protocol`: `e53af6d57197074bb8afd31f0fc1522ece68c890`
- `openmouse`: `99f6ea450d73ab41ad2e5b61b7a05f9f9509df7f`

Clone the two upstream projects as siblings in a development directory, then use the recorded baseline commits for this reproducible patch set. Set `$patchDir` to this repository's `patches` directory. Work on clean topic branches.

```powershell
# Inside mouse-protocol, at the recorded baseline
git switch -c feat/redgear-x13-pro
git apply --check "$patchDir/mouse-protocol.patch"
git apply "$patchDir/mouse-protocol.patch"
npm ci
npm run check

# Inside sibling openmouse, at its recorded baseline
git switch -c feat/redgear-x13-pro
git apply --check "$patchDir/openmouse.patch"
git apply "$patchDir/openmouse.patch"
npm ci
npm install --no-save --package-lock=false ../mouse-protocol
npm run check
npm run preview -- --host 127.0.0.1 --port 5173 --strictPort
```

This serves a local app only. Select the mouse in its picker. Direct WebHID may fall back to identity on Windows/Edge; obtain OpenMouse Bridge separately if native transport is needed. Review Bridge's startup settings and profiles before running it. Do not rely on a browser protected-collection bypass.

For newer upstream commits, inspect current contracts and rebase the focused changes; do not replace current registry/controller files with stale copies. The hosted supported-devices catalog is generated from another repository and is not changed by this patch.
