using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace AfterSignal.Tests
{
    public class ProjectContractTests
    {
        [Test] public void AllTwentyFiveScenesPreserveTheOriginalFourSaveIds()
        {
            Assert.AreEqual(25,EditorBuildSettings.scenes.Length);Assert.AreEqual(0,(int)StageId.Station);Assert.AreEqual(1,(int)StageId.Carriage);Assert.AreEqual(2,(int)StageId.Roof);Assert.AreEqual(3,(int)StageId.Haven);
            for(int i=0;i<25;i++){Assert.IsTrue(EditorBuildSettings.scenes[i].enabled);Assert.That(EditorBuildSettings.scenes[i].path,Does.EndWith(CampaignRules.Scenes[i]+".unity"));Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[i].path));}
        }
        [Test] public void RenderPipelineIsUniversal3D()
        {
            var pipeline=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            Assert.NotNull(pipeline);Assert.That(pipeline.GetType().Name,Is.EqualTo("UniversalRenderPipelineAsset"));
        }
        [Test] public void AllOriginalFramesImportAsPointFilteredSprites()
        {
            var hero=System.Array.FindAll(Resources.LoadAll<Sprite>("Art/Hero"),s=>s.name.StartsWith("seo-"));Assert.AreEqual(96,hero.Length);
            foreach(var sprite in hero){Assert.AreEqual(FilterMode.Point,sprite.texture.filterMode);Assert.AreEqual(49,sprite.pixelsPerUnit);Assert.That(sprite.pivot.y,Is.EqualTo(12).Within(.1));}
            Assert.AreEqual(48,Resources.LoadAll<Sprite>("Art/Enemies").Length);
            Assert.AreEqual(21,Resources.LoadAll<Sprite>("Art/NPC").Length);
        }
        [Test] public void WASDAndMouseBindingsAreNativeInputActions()
        {
            var asset=Resources.Load<InputActionAsset>("Controls");Assert.NotNull(asset);
            Assert.AreEqual("<Mouse>/leftButton",asset.FindAction("Attack").bindings[0].path);Assert.AreEqual("<Mouse>/rightButton",asset.FindAction("Grapple").bindings[0].path);
            var bindings=asset.FindAction("Move").bindings;
            Assert.IsTrue(bindings[0].isComposite);Assert.AreEqual("<Keyboard>/w",bindings[1].path);Assert.AreEqual("<Keyboard>/s",bindings[2].path);Assert.AreEqual("<Keyboard>/a",bindings[3].path);Assert.AreEqual("<Keyboard>/d",bindings[4].path);
        }
        [TestCase(false,false,false)] [TestCase(true,false,false)] [TestCase(false,true,false)] [TestCase(true,true,true)]
        public void BoardingRequiresPowerAndClearance(bool power,bool clear,bool expected){Assert.AreEqual(expected,CampaignRules.CanBoard(power,clear));}
        [TestCase(true,true,false,false)] [TestCase(false,true,true,false)] [TestCase(true,false,true,false)] [TestCase(true,true,true,true)]
        public void HatchRequiresReleaseGlassAndClearance(bool release,bool clear,bool glass,bool expected){Assert.AreEqual(expected,CampaignRules.CanOpenHatch(release,clear,glass));}
        [Test] public void OrdinaryEnemiesTakeOneToThreeWeaponHits()
        {
            var tuning=Resources.Load<GameTuning>("GameTuning");
            foreach(float health in new[]{60f,64f,72f})foreach(WeaponId weapon in System.Enum.GetValues(typeof(WeaponId))){int hits=Mathf.CeilToInt(health/CampaignRules.Damage(weapon,0,tuning));Assert.That(hits,Is.InRange(1,3),weapon+" vs "+health);}
            Assert.AreEqual(1080,tuning.bossCoreDamage*3);
        }
        [Test] public void QualityPosesAndSoundsAreRealImportedAssets()
        {
            var poses=Resources.LoadAll<Sprite>("Art/Hero/Quality");Assert.AreEqual(6,poses.Length);foreach(var s in poses){Assert.AreEqual(49,s.pixelsPerUnit);Assert.AreEqual(FilterMode.Point,s.texture.filterMode);Assert.AreEqual(224,s.rect.width);Assert.That(s.pivot.y,Is.EqualTo(12).Within(.1));}
            var audio=Resources.LoadAll<AudioClip>("Audio/Quality");Assert.AreEqual(103,audio.Length);foreach(var clip in audio){Assert.Greater(clip.samples,1000);Assert.AreEqual(clip.name.StartsWith("urban_")?48000:44100,clip.frequency);}
        }
    }
}

