using UnityEngine;
namespace AfterSignal
{
    public sealed class VehicleDetails:MonoBehaviour
    {
        CityVehicle car;Transform[] wheels;Vector3 previous;
        public static void Install(CityVehicle vehicle)
        {
            if(vehicle.GetComponent<VehicleDetails>()||vehicle.GetComponent<AuthoredCraft>())return;
            var detail=vehicle.gameObject.AddComponent<VehicleDetails>();detail.car=vehicle;
            if(!vehicle.GetComponent<AuthoredCraft>())
            {
                var taxi=vehicle.GetComponent<CityTaxiService>();string modelName=taxi?(taxi.Air?"AirTaxi":"WaterTaxi"):FleetDesign.Model(vehicle);
                var asset=Resources.Load<GameObject>("WorldAssets/"+modelName+"/"+modelName);if(!asset)return;
                Material basePaint=Resources.Load<Material>("Materials/SedanIvory");
                foreach(var renderer in vehicle.GetComponentsInChildren<MeshRenderer>())if(renderer.name=="Sculpted chassis"&&renderer.sharedMaterial)basePaint=renderer.sharedMaterial;
                foreach(var r in vehicle.GetComponentsInChildren<MeshRenderer>())
                {
                    string n=r.name.ToLowerInvariant();
                    if(n.Contains("police")||n.Contains("siren")||n.Contains("lightbar")||n.Contains("taxi")||n.Contains("plate"))continue;
                    r.enabled=false;
                }
                var model=Instantiate(asset,vehicle.transform,false);model.name="Detailed vehicle coachwork";
                // FBX converts Blender +X into Unity -X. Align every authored nose with Drive.Forward.
                model.transform.localRotation=Quaternion.Euler(0,180,0);
                foreach(var r in model.GetComponentsInChildren<MeshRenderer>())
                {
                    string key=r.sharedMaterial?r.sharedMaterial.name:r.name;
                    string mat=key.StartsWith("BodyPaint")?vehicle.GetComponent<PoliceCar>()?"DistrictIvory":vehicle.type==CityVehicleType.Taxi?"TaxiPaint":"SedanRed":key.StartsWith("TrimRubber")?"Rubber":key.StartsWith("BrushedAlloy")?"Chrome":key.StartsWith("Glazing")?"Glass":key.StartsWith("Headlamp")?"CyanFX":"RedFX";
                    r.sharedMaterial=Resources.Load<Material>("Materials/"+mat)??Resources.Load<Material>("Materials/Chrome");
                    if(key.StartsWith("BodyPaint")){r.name="Sculpted chassis";if(taxi)r.sharedMaterial=Resources.Load<Material>("Materials/SedanIvory");else if(!vehicle.GetComponent<PoliceCar>()&&vehicle.type!=CityVehicleType.Taxi)r.sharedMaterial=VehiclePaint.For(vehicle.type,basePaint);}
                    if(key.StartsWith("Headlamp")||key.StartsWith("Taillamp"))r.sharedMaterial=VehiclePaint.Lens(key.StartsWith("Taillamp"));
                    if(key.StartsWith("Glazing"))r.name="Windshield and side windows";
                }
                var list=new System.Collections.Generic.List<Transform>();foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Wheel assembly"))list.Add(t);detail.wheels=list.ToArray();foreach(var w in detail.wheels){var motion=w.gameObject.AddComponent<VehicleWheel>();motion.Initialize(vehicle); }
                if(taxi){var lod=model.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.008f,model.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();}
            }
            else
            {
                for(int s=-1;s<=1;s+=2)
                {
                    var front=vehicle.HalfLength;
                    WorldGeometry.Part(vehicle.transform,"Large vehicle mirror",new Vector3(front-.35f,2.25f,s*1.53f),new Vector3(.32f,.55f,.18f),"Chrome");
                    WorldGeometry.Part(vehicle.transform,"Mirror support",new Vector3(front-.5f,2.4f,s*1.34f),new Vector3(.12f,.12f,.65f),"DarkMetal");
                    for(int i=0;i<9;i++)WorldGeometry.Part(vehicle.transform,"Rear ventilation louver",new Vector3(-front+.025f,.9f+i*.07f,s*.65f),new Vector3(.07f,.025f,.75f),"Chrome");
                    WorldGeometry.Part(vehicle.transform,"Front LED strip",new Vector3(front+.03f,.82f,s*.77f),new Vector3(.07f,.06f,.4f),"CyanFX");
                    for(int i=0;i<5;i++)WorldGeometry.Part(vehicle.transform,"Body panel seam",new Vector3(-front+.6f+i*1.7f,1.03f,s*1.24f),new Vector3(.018f,.5f,.02f),"DarkMetal");
                }
            }
            detail.previous=vehicle.transform.position;
        }
        
    }
}
