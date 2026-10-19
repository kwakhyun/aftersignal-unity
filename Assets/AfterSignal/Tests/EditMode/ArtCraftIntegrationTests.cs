using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace AfterSignal.Tests
{
    public class ArtCraftIntegrationTests
    {
        [Test]
        public void EveryVenueHasItsOwnSymbolOnOneNonReadableTexture()
        {
            var texture = Resources.Load<Texture2D>(FacilityGlyphs.ResourcePath);
            Assert.NotNull(texture, "Restore the private ArtCraft UI asset pack.");
            Assert.AreEqual(1024, texture.width);
            Assert.AreEqual(640, texture.height);
            Assert.IsFalse(texture.isReadable);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
            foreach (VenueKind kind in Enum.GetValues(typeof(VenueKind)))
            {
                var sprite = FacilityGlyphs.For(kind);
                Assert.NotNull(sprite, kind.ToString());
                Assert.AreEqual(kind.ToString(), sprite.name);
                Assert.AreSame(texture, sprite.texture);
                Assert.AreEqual(new Vector2(128, 128), sprite.rect.size);
            }
        }

        [TestCase(VenueKind.Police)]
        [TestCase(VenueKind.FireStation)]
        [TestCase(VenueKind.Bank)]
        public void PublicServicesRemainVisibleInLifeCategory(VenueKind kind)
        {
            var go = new GameObject("Filtered atlas", typeof(RectTransform), typeof(AtlasViewport));
            try
            {
                Assert.NotNull(go.GetComponent<CanvasRenderer>(), "Procedural atlas must render without relying on automatic UI setup.");
                var map = go.GetComponent<AtlasViewport>();
                map.category = 6;
                var venue = new CityVenue("test", "test", 0, kind, 0, 0, 0, 40, 40, 0, "");
                Assert.IsTrue(map.Visible(venue));
                map.category = 1;
                Assert.IsFalse(map.Visible(venue));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void TransportFilterDoesNotConfuseDistrictWithStation()
        {
            Assert.AreEqual(4, AtlasViewport.CategoryForName("애프터라이트 공항"));
            Assert.AreEqual(4, AtlasViewport.CategoryForName("노바 항구"));
            Assert.AreEqual(4, AtlasViewport.CategoryForName("중앙역"));
            Assert.AreEqual(6, AtlasViewport.CategoryForName("연구 구역"));
            Assert.AreEqual(3, AtlasViewport.CategoryForName("노바 호텔"));
            Assert.AreEqual("Hospital", FacilityGlyphs.ForName("항만 진료소").name);
            Assert.AreEqual("Police", FacilityGlyphs.ForName("해양경찰").name);
            Assert.AreEqual("Garden", FacilityGlyphs.ForName("오로라 전망공원").name);
        }
    }
}
