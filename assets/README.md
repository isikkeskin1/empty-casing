# Character animation frames

The app intentionally ships **without Capri character artwork**. Add frames you created or are authorized to use in:

%LOCALAPPDATA%\CapriDesktop\character\

Use subfolders named:

- idle
- think
- reply
- error

Put sequential transparent PNG frames in each folder, for example idle/0001.png, idle/0002.png and so on. Frames are sorted by filename; use zero-padded numbering. They are shown at a fixed preview rate for now. Missing states fall back to idle, and if no frames exist a placeholder hologram is displayed.

In the app tray menu choose **Open character art folder** to open the directory, then choose **Reload character art** after changes. Keep similar frame dimensions and consistent alignment so animations do not jump. Do not commit assets you lack the right to redistribute.
