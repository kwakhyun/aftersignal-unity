using NUnit.Framework;
using UnityEngine;
using UnityEditor;
namespace AfterSignal.Tests
{
    public class ResidenceAssetTests
    {
        [Test] public void EveryPlayableSceneHasAnImportedBackgroundAndActionSheetsHaveStablePivots()
        {
            foreach(StageId stage in System.Enum.GetValues(typeof(StageId)))if((int)stage<23)Assert.NotNull(Resources.Load<Texture2D>("Art/Environment/"+stage),stage.ToString());
            // The open city uses four-sided 3D facades instead of a single flat backdrop.
            for(int i=0;i<8;i++){var image=Resources.Load<Texture2D>("Art/Urban/Facade-"+i.ToString("00"));Assert.NotNull(image);Assert.GreaterOrEqual(image.width,490);Assert.AreEqual(FilterMode.Trilinear,image.filterMode);var material=Resources.Load<Material>("Materials/Urban-Facade-"+i.ToString("00"));Assert.NotNull(material);Assert.AreEqual(image,material.GetTexture("_BaseMap"));}
            for(int i=0;i<6;i++)Assert.NotNull(Resources.Load<Texture2D>("Art/Urban/Surface-"+i.ToString("00")));
            foreach(WeaponId weapon in System.Enum.GetValues(typeof(WeaponId))){var poses=Resources.LoadAll<Sprite>("Art/Hero/Actions/"+weapon);Assert.AreEqual(weapon==WeaponId.Pistol?24:16,poses.Length);foreach(var sprite in poses){Assert.AreEqual(288,sprite.rect.width);Assert.That(sprite.pivot.y,Is.EqualTo(16).Within(.1));Assert.AreEqual(49,sprite.pixelsPerUnit);Assert.AreEqual(FilterMode.Point,sprite.texture.filterMode);}}
        }
        [Test] public void SceneComponentsResolveToSeparateScripts()
        {
            foreach(var type in new[]{typeof(HingedDoor),typeof(ResidentWalker),typeof(TravellingCut)}){
                var script=AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/AfterSignal/Runtime/"+type.Name+".cs");Assert.NotNull(script);Assert.AreEqual(type,script.GetClass());
            }
        }
        [Test] public void EveryWeaponHasFrontAndBackWalkFramesWithIdenticalFootAnchors()
        {
            foreach(WeaponId weapon in System.Enum.GetValues(typeof(WeaponId))){var poses=Resources.LoadAll<Sprite>("Art/Hero/Depth/"+weapon);Assert.AreEqual(8,poses.Length);foreach(var sprite in poses){Assert.AreEqual(new Vector2(288,288),sprite.rect.size);Assert.AreEqual(new Vector2(144,16),sprite.pivot);Assert.AreEqual(49,sprite.pixelsPerUnit);Assert.AreEqual(FilterMode.Point,sprite.texture.filterMode);}}
        }
    }
}
