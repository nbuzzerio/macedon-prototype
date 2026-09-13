using System;
using UnityEngine;

namespace Macedon.CharacterVisualVariants.Editor
{
    public static class VarangianVisualVariantLogic
    {
        public readonly struct PaletteEntry
        {
            public readonly string Name;
            public readonly Color Color;

            public PaletteEntry(string name, string html)
            {
                Name = name;
                if (!ColorUtility.TryParseHtmlString(html, out Color color))
                    throw new ArgumentException($"Invalid palette color: {html}", nameof(html));
                Color = color;
            }
        }

        public static readonly PaletteEntry[] Palette =
        {
            new PaletteEntry("Muted Red", "#8B3F38"),
            new PaletteEntry("Burgundy", "#572B32"),
            new PaletteEntry("Dark Green", "#33483B"),
            new PaletteEntry("Olive", "#65633B"),
            new PaletteEntry("Brown", "#604637"),
            new PaletteEntry("Tan", "#9A7957"),
            new PaletteEntry("Muted Blue", "#465B6C"),
            new PaletteEntry("Gray", "#5B5D5C"),
            new PaletteEntry("Off-White", "#B7AD91")
        };

        public static readonly PaletteEntry[] ShieldPalette =
        {
            new PaletteEntry("Muted Blue", "#465B6C"),
            new PaletteEntry("Dark Blue", "#283A50"),
            new PaletteEntry("Muted Red", "#8B3F38"),
            new PaletteEntry("Burgundy", "#572B32"),
            new PaletteEntry("Dark Green", "#33483B"),
            new PaletteEntry("Olive", "#65633B"),
            new PaletteEntry("Brown", "#604637"),
            new PaletteEntry("Charcoal", "#404445")
        };

        public static int PaletteIndexForSeed(int seed)
        {
            // A small integer mixer avoids UnityEngine.Random global-state changes.
            unchecked
            {
                uint value = (uint)seed;
                value ^= value >> 16;
                value *= 0x7feb352d;
                value ^= value >> 15;
                value *= 0x846ca68b;
                value ^= value >> 16;
                return (int)(value % Palette.Length);
            }
        }

        public static int ShieldPaletteIndexForSeed(int seed)
        {
            // Salt the same deterministic mixer so one seed produces an independent pair.
            return PaletteIndexForSeed(seed ^ unchecked((int)0x5f356495u), ShieldPalette.Length);
        }

        private static int PaletteIndexForSeed(int seed, int paletteLength)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= value >> 16;
                value *= 0x7feb352d;
                value ^= value >> 15;
                value *= 0x846ca68b;
                value ^= value >> 16;
                return (int)(value % paletteLength);
            }
        }

        public static bool IsFabricSourceColor(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            bool redHue = hue <= 0.055f || hue >= 0.97f;
            return color.a > 0.01f && redHue && saturation >= 0.28f && value >= 0.10f &&
                   color.r > color.g * 1.10f && color.r > color.b * 1.06f;
        }

        public static Color RecolorPixel(Color source, Color target)
        {
            if (!IsFabricSourceColor(source)) return source;

            Color.RGBToHSV(source, out _, out float sourceSaturation, out float sourceValue);
            Color.RGBToHSV(target, out float targetHue, out float targetSaturation, out float targetValue);
            float saturation = Mathf.Lerp(sourceSaturation, targetSaturation, 0.82f);
            float valueScale = Mathf.Lerp(0.78f, 1.18f, targetValue);
            Color result = Color.HSVToRGB(targetHue, saturation, Mathf.Clamp01(sourceValue * valueScale));
            result.a = source.a;
            return result;
        }

        public static bool IsShieldPaintSourceColor(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return color.a > 0.01f && hue >= 0.54f && hue <= 0.68f &&
                   saturation >= 0.18f && value >= 0.10f && value <= 0.72f &&
                   color.b > color.r * 1.08f && color.b > color.g * 1.06f;
        }

        public static bool IsShieldPaintPixel(Color color, int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0 || !IsShieldPaintSourceColor(color)) return false;

            // The only painted round shield occupies this UV island. A color-only mask would also
            // catch blue garments elsewhere in the atlas, so keep the asset-specific spatial guard.
            float u = (x + 0.5f) / width;
            float v = (y + 0.5f) / height;
            float dx = (u - 0.303f) / 0.155f;
            float dy = (v - 0.140f) / 0.155f;
            return dx * dx + dy * dy <= 1f;
        }

        public static Color RecolorShieldPixel(Color source, Color target)
        {
            if (!IsShieldPaintSourceColor(source)) return source;
            return RecolorMaskedPixel(source, target);
        }

        public static Color RecolorShieldPixel(Color source, Color target, int x, int y, int width, int height)
        {
            if (!IsShieldPaintPixel(source, x, y, width, height)) return source;
            return RecolorMaskedPixel(source, target);
        }

        public static void RecolorCombined(
            Color32[] sourcePixels,
            int width,
            int height,
            Color fabricTarget,
            Color shieldTarget,
            out int fabricChanged,
            out int shieldChanged)
        {
            if (sourcePixels == null) throw new ArgumentNullException(nameof(sourcePixels));
            if (width <= 0 || height <= 0 || sourcePixels.Length != width * height)
                throw new ArgumentException("Pixel dimensions do not match the source array.");

            fabricChanged = 0;
            shieldChanged = 0;
            for (int i = 0; i < sourcePixels.Length; i++)
            {
                Color source = sourcePixels[i];
                Color result = RecolorPixel(source, fabricTarget);
                if (result != source)
                {
                    sourcePixels[i] = result;
                    fabricChanged++;
                    continue;
                }

                int x = i % width;
                int y = i / width;
                result = RecolorShieldPixel(source, shieldTarget, x, y, width, height);
                if (result != source)
                {
                    sourcePixels[i] = result;
                    shieldChanged++;
                }
            }
        }

        private static Color RecolorMaskedPixel(Color source, Color target)
        {
            Color.RGBToHSV(source, out _, out float sourceSaturation, out float sourceValue);
            Color.RGBToHSV(target, out float targetHue, out float targetSaturation, out float targetValue);
            float saturation = Mathf.Lerp(sourceSaturation, targetSaturation, 0.82f);
            float valueScale = Mathf.Lerp(0.78f, 1.18f, targetValue);
            Color result = Color.HSVToRGB(targetHue, saturation, Mathf.Clamp01(sourceValue * valueScale));
            result.a = source.a;
            return result;
        }

        public static int RecolorPixels(Color32[] pixels, Color target)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            int changed = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color source = pixels[i];
                Color result = RecolorPixel(source, target);
                if (result != source)
                {
                    pixels[i] = result;
                    changed++;
                }
            }
            return changed;
        }
    }
}
