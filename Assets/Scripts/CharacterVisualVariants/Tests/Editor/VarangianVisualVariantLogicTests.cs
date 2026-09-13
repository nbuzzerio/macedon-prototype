using NUnit.Framework;
using UnityEngine;
using Macedon.CharacterVisualVariants.Editor;

namespace Macedon.CharacterVisualVariants.Tests.Editor
{
    public sealed class VarangianVisualVariantLogicTests
    {
        [Test]
        public void PaletteIndexForSeed_IsDeterministicAndInRange()
        {
            int first = VarangianVisualVariantLogic.PaletteIndexForSeed(12345);
            Assert.That(VarangianVisualVariantLogic.PaletteIndexForSeed(12345), Is.EqualTo(first));
            Assert.That(first, Is.InRange(0, VarangianVisualVariantLogic.Palette.Length - 1));
        }

        [TestCase(0.72f, 0.18f, 0.15f, true)]
        [TestCase(0.72f, 0.55f, 0.43f, false)] // skin-like orange
        [TestCase(0.55f, 0.57f, 0.60f, false)] // metal
        [TestCase(0.10f, 0.22f, 0.48f, false)] // blue cloth/shield paint
        public void FabricMask_OnlyAcceptsRedDominantPixels(float r, float g, float b, bool expected)
        {
            Assert.That(VarangianVisualVariantLogic.IsFabricSourceColor(new Color(r, g, b, 1f)), Is.EqualTo(expected));
        }

        [Test]
        public void RecolorPixel_PreservesUnmaskedPixelsAndAlpha()
        {
            var metal = new Color(0.5f, 0.52f, 0.55f, 0.7f);
            Assert.That(VarangianVisualVariantLogic.RecolorPixel(metal, Color.green), Is.EqualTo(metal));

            var fabric = new Color(0.65f, 0.12f, 0.10f, 0.4f);
            Color recolored = VarangianVisualVariantLogic.RecolorPixel(fabric, Color.green);
            Assert.That(recolored, Is.Not.EqualTo(fabric));
            Assert.That(recolored.a, Is.EqualTo(fabric.a));
        }

        [Test]
        public void ShieldMask_AcceptsRepresentativePaintInsideKnownUvIsland()
        {
            var shieldBlue = new Color(65f / 255f, 81f / 255f, 104f / 255f, 1f);
            Assert.That(VarangianVisualVariantLogic.IsShieldPaintSourceColor(shieldBlue), Is.True);
            Assert.That(VarangianVisualVariantLogic.IsShieldPaintPixel(shieldBlue, 30, 14, 100, 100), Is.True);
            Assert.That(VarangianVisualVariantLogic.IsShieldPaintPixel(shieldBlue, 75, 75, 100, 100), Is.False,
                "Blue colors outside the known shield UV island must be preserved.");
        }

        [TestCase(0.70f, 0.68f, 0.63f)] // off-white
        [TestCase(0.52f, 0.54f, 0.55f)] // metal
        [TestCase(0.72f, 0.48f, 0.32f)] // skin/orange
        [TestCase(0.38f, 0.25f, 0.16f)] // wood
        [TestCase(0.66f, 0.14f, 0.12f)] // red fabric
        [TestCase(0.20f, 0.34f, 0.23f)] // green fabric
        public void ShieldColorMask_RejectsNonPaintColors(float r, float g, float b)
        {
            Assert.That(VarangianVisualVariantLogic.IsShieldPaintSourceColor(new Color(r, g, b, 1f)), Is.False);
        }

        [Test]
        public void ShieldRecolor_PreservesAlpha()
        {
            var shieldBlue = new Color(0.25f, 0.32f, 0.42f, 0.37f);
            Color result = VarangianVisualVariantLogic.RecolorShieldPixel(shieldBlue, new Color(0.50f, 0.16f, 0.14f));
            Assert.That(result, Is.Not.EqualTo(shieldBlue));
            Assert.That(result.a, Is.EqualTo(shieldBlue.a));
        }

        [Test]
        public void FabricAndShieldChannels_DoNotClaimEachOthersSourcePixels()
        {
            var shieldBlue = new Color(0.25f, 0.32f, 0.42f, 1f);
            var redFabric = new Color(0.65f, 0.12f, 0.10f, 1f);
            Assert.That(VarangianVisualVariantLogic.RecolorPixel(shieldBlue, Color.green), Is.EqualTo(shieldBlue));
            Assert.That(VarangianVisualVariantLogic.RecolorShieldPixel(redFabric, Color.green), Is.EqualTo(redFabric));
        }

        [Test]
        public void CombinedRecolor_AppliesBothChannelsAndPreservesUnrelatedPixels()
        {
            const int width = 100;
            const int height = 100;
            var unrelated = new Color32(128, 130, 132, 255);
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = unrelated;
            int fabricIndex = 5 * width + 5;
            int shieldIndex = 14 * width + 30;
            pixels[fabricIndex] = new Color32(166, 31, 26, 211);
            pixels[shieldIndex] = new Color32(65, 81, 104, 177);

            VarangianVisualVariantLogic.RecolorCombined(
                pixels, width, height, Color.green, Color.red, out int fabricChanged, out int shieldChanged);

            Assert.That(fabricChanged, Is.EqualTo(1));
            Assert.That(shieldChanged, Is.EqualTo(1));
            Assert.That(pixels[fabricIndex], Is.Not.EqualTo(new Color32(166, 31, 26, 211)));
            Assert.That(pixels[shieldIndex], Is.Not.EqualTo(new Color32(65, 81, 104, 177)));
            Assert.That(pixels[0], Is.EqualTo(unrelated));
            Assert.That(pixels[fabricIndex].a, Is.EqualTo(211));
            Assert.That(pixels[shieldIndex].a, Is.EqualTo(177));
        }

        [Test]
        public void CombinedRecolor_RegeneratedFromSameSourceHasNoCumulativeDrift()
        {
            var source = new[]
            {
                new Color32(166, 31, 26, 255),
                new Color32(65, 81, 104, 255),
                new Color32(128, 130, 132, 255),
                new Color32(128, 130, 132, 255)
            };
            Color32[] first = (Color32[])source.Clone();
            Color32[] second = (Color32[])source.Clone();

            VarangianVisualVariantLogic.RecolorCombined(first, 2, 2, Color.green, Color.red, out _, out _);
            VarangianVisualVariantLogic.RecolorCombined(second, 2, 2, Color.green, Color.red, out _, out _);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ShieldPaletteIndexForSeed_IsDeterministicAndInRange()
        {
            int first = VarangianVisualVariantLogic.ShieldPaletteIndexForSeed(6789);
            Assert.That(VarangianVisualVariantLogic.ShieldPaletteIndexForSeed(6789), Is.EqualTo(first));
            Assert.That(first, Is.InRange(0, VarangianVisualVariantLogic.ShieldPalette.Length - 1));
        }
    }
}
