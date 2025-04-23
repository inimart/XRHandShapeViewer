# XR HandShape Viewer – User Guide

## Opening the Preview Window

* **Inspector button** – Select an `XRHandShape` asset and click **Open 3D Preview** at the bottom of its Inspector (added by `XRHandShapeEditor`).
* **Menu** – `Window ▸ XR ▸ Hand Shape Preview`.

The window automatically shows the currently selected `XRHandShape`.

---

## UI Overview

| Control              | Description                                                           |
| -------------------- | --------------------------------------------------------------------- |
| **Animation Toggle** | Enables sinusoidal interpolation through each finger‑shape tolerance. |
| **Speed Slider**     | Controls how fast the interpolation runs (0.1 – 3×).                  |
| **Reset View**       | Resets rotation, pan, and zoom.                                       |

Mouse shortcuts inside the preview:

* **Left‑drag** – rotate model
* **Middle‑drag** – pan
* **Scroll wheel** – zoom

---

## ## Troubleshooting

| Issue                       | Solution                                                                           |
| --------------------------- | ---------------------------------------------------------------------------------- |
| *Hand model not found*      | Ensure the prefab paths are correct and the package is installed in **Packages/**. |
| *Nothing renders*           | Confirm the selected asset is of type `XRHandShape`.                               |
| *Animation too fast / slow* | Adjust the **Speed** slider.                                                       |




