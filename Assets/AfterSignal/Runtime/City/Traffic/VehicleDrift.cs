using UnityEngine;
namespace AfterSignal
{
    public sealed partial class CityVehicle
    {
        public int designVariant=-1;
        public bool Drifting{get;private set;}
        public float SlipAngle{get;private set;}
        public float DriftDistance{get;private set;}
        Vector3 roadVelocity;TrailRenderer[] skidTrails;ParticleSystem tyreSmoke;
        Vector3 DriftStep(ControlFrame input,bool brake,float dt)
        {
            bool sliding=brake&&Mathf.Abs(input.move.x)>.2f&&Mathf.Abs(speed)>7;
            float grip=sliding?(IsHeavy?2.3f:1.1f):9;
            if(roadVelocity.sqrMagnitude<.1f)roadVelocity=Forward*speed;
            roadVelocity=Vector3.Lerp(roadVelocity,Forward*speed,1-Mathf.Exp(-grip*dt));
            roadVelocity=Vector3.ClampMagnitude(roadVelocity,Mathf.Abs(speed)+2);
            SlipAngle=Mathf.Abs(Vector3.SignedAngle(Forward*Mathf.Sign(speed),roadVelocity,Vector3.up));
            Drifting=sliding&&SlipAngle>5;
            if(Drifting)DriftDistance+=roadVelocity.magnitude*dt;
            if(Drifting&&skidTrails==null)CreateTyreEffects();
            if(skidTrails!=null)foreach(var trail in skidTrails)trail.emitting=Drifting;
            if(tyreSmoke){var e=tyreSmoke.emission;e.enabled=Drifting;e.rateOverTime=Drifting?7:0;}
            return roadVelocity*dt;
        }
        void CreateTyreEffects()
        {
            skidTrails=new TrailRenderer[type==CityVehicleType.Motorcycle?1:2];
            for(int i=0;i<skidTrails.Length;i++)
            {
                var go=new GameObject("Drifting tyre trail");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(-HalfLength*.62f,.035f,skidTrails.Length==1?0:(i==0?-1:1)*HalfWidth*.91f);
                var t=go.AddComponent<TrailRenderer>();t.time=9;t.minVertexDistance=.25f;t.startWidth=.16f;t.endWidth=.11f;t.startColor=new Color(.02f,.02f,.025f,.65f);t.endColor=new Color(.02f,.02f,.025f,0);t.sharedMaterial=CityGeometry.Material("Rubber");t.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;skidTrails[i]=t;
            }
            tyreSmoke=VehicleDamagePresentation.Emitter(transform,"Drift tyre smoke",new Vector3(-HalfLength*.65f,.18f,0),.48f,false);
        }
    }
}
