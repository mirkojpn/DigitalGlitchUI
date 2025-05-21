# DigitalGlitchUI

A fullscreen “digital glitch” effect for Unity UI elements, with transparent base and richly configurable noise, displacement, color-shift and animation phases. Ideal for UI transitions, error screens, or sci-fi HUD effects.

---

## Features

* **Per-slice random glitch blocks** over UI, with adjustable block count and intensity
* **Animated in-peak-out phases** with custom durations and easing curve
* **Auto-generated noise textures** or use your own
* **Two alternating “trash” textures** for more varied glitch patterns
* **Displacement & color-shift strength** controls
* **Primary, secondary & tertiary glitch colors** with blend balance
* **Glitch opacity** separate from image alpha
* Playable via simple C# API or on-demand (see **Usage**)

---

## Requirements

* Unity 2020.3 LTS or later
* **Universal RP** (for URP-specific shader variants) or Built-in RP
* UI Toolkit: uses `UnityEngine.UI.Image`, `RawImage`, `Text`, and supports TextMeshPro via reflection
* [Cysharp.Threading.Tasks](https://github.com/Cysharp/UniTask) (for async animation)

---

## Installation

1. **Shader**

   * Copy `DigitalGlitchUIExtended.shader` into your project (e.g. `Assets/Shaders/`)

2. **Scripts**

   * Copy `DigitalGlitchUIVolume.cs` (and any helpers) into `Assets/Scripts/UIEffects/` 

3. **Packages**

   * Ensure **UniTask** is installed via Package Manager (or manually)

---

## Usage

1. **Create a UI Image**

   * In your Canvas (Screen Space – Overlay), add a **UI → Image** (or RawImage/Text).
   * Stretch anchors to fill the screen (or desired area).

2. **Assign Material**

   * Create a new Material using the **DigitalGlitchUIExtended** shader.
   * Assign that Material to your Image/RawImage/Text components.

3. **Add the Effect Component**

   * On the same GameObject, add the `DigitalGlitchUIVolume` component.
   * Configure properties in Inspector:


   | Property                     | Description                                                   |
   | ---------------------------- | ------------------------------------------------------------- |
   | **Intensity**                | Master glitch strength (0–1)                                  |
   | **Displacement Strength**    | How far pixels shift                                          |
   | **Color Shift Strength**     | Amount of RGB channel offset                                  |
   | **Glitch Opacity**           | Alpha of glitch blocks                                        |
   | **Glitch Colors (1–3)**      | RGB tint colors for glitch                                    |
   | **Color Balance**            | Mix between primary/secondary colors                          |
   | **Update Interval**          | Time between noise/trash texture swaps                        |
   | **Noise Update Probability** | Chance to regenerate noise on each update                     |
   | **Animation Durations**      | `glitchInDuration`, `peakGlitchDuration`, `glitchOutDuration` |
   | **Animation Curve**          | Controls easing through phases                                |

4. **Triggering the Effect**

   * **Via Inspector**: Enable “Always Visible” and adjust Intensity to preview.
   * **At runtime**: call from your scripts:

     ```csharp
     var glitch = yourGameObject.GetComponent<DigitalGlitchUIVolume>();
     glitch.PlayEffect();            // default timing
     await glitch.PlayEffect(2f);    // custom total duration
     glitch.CancelEffect();          // stop immediately
     glitch.SetIntensity(0.5f);      // manual control
     ```
   * Additional API:

     ```csharp
     glitch.SetGlitchOpacity(0.8f);
     glitch.SetGlitchColor(Color.cyan);
     glitch.SetGlitchColor2(Color.magenta);
     glitch.SetGlitchColor3(Color.blue);
     glitch.SetColorBalance(0.3f);
     glitch.RefreshNoise();
     glitch.ApplyToUIElement(someOtherGraphic);
     ```

---

## Examples

![Demo Screenshot](docs/demo.gif)

```csharp
// Example: play a quick glitch on button click
public class ButtonGlitchTrigger : MonoBehaviour
{
    public DigitalGlitchUIVolume glitchEffect;

    public void OnClick()
    {
        glitchEffect.PlayEffect(1.2f).Forget();
    }
}
```

---

## License

Distributed under the **MIT License**. See [LICENSE](LICENSE) for details.

---

## Credits

* **DigitalGlitchUIVolume.cs** by Mirko Rossetti 
* Inspired by URPGlitch (saimarei): [https://github.com/saimarei/URPGlitch](https://github.com/saimarei/URPGlitch)
* Inspired by KinoGlitch (Keijiro Takahashi): [https://github.com/keijiro/KinoGlitch](https://github.com/keijiro/KinoGlitch)

