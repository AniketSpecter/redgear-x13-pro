# Remaining coverage

The tested driver is ready for upstream review with explicit feature limits.
Future work includes DPI indicator colours, other lighting effects/brightness,
macro/M-Macro encoding and vendor host-profile interoperability. These require
independent evidence and feature-specific physical acceptance before enabling.

Windows host controls are provided by a native companion. The browser cannot
change Windows user32 settings through a mouse HID report; integrating those
controls into the official Bridge needs a separately reviewed upstream API.

Official OpenMouse availability depends on maintainer review, merge and deployment.
Publishing this community repository or opening review requests does not deploy
changes to the official app or its supported-devices catalog.
