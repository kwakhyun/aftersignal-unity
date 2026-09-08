using UnityEngine;
namespace AfterSignal
{
    public sealed partial class HighriseInterior
    {
        void Amenities(Transform p,float w,float d,float h)
        {
            float x=w*.5f-3,z=-d*.5f+3;
            Box(p,"Restroom privacy wall",new(x-2,1.4f,z),new(.14f,2.8f,4.5f),"Cladding");
            Box(p,"Restroom entry left",new(x-1.65f,1.4f,z+2.3f),new(.7f,2.8f,.14f),"Cladding");
            Box(p,"Restroom entry right",new(x+1.65f,1.4f,z+2.3f),new(.7f,2.8f,.14f),"Cladding");
            Sign(p,"RESTROOM",new(x,2.5f,z+2.2f),.08f);
            Box(p,"Wash basin vanity",new(x+1,.5f,z+.8f),new(1.2f,1,.65f),"FutureCeramic");
            Box(p,"Mirror",new(x+1,1.65f,z+1.15f),new(1.15f,.9f,.035f),"Glass",false);
            Box(p,"Restroom cubicle screen",new(x,1.1f,z-.8f),new(.1f,2.2f,1.9f),"Cladding");
            for(int s=-1;s<=1;s+=2)
            {
                Box(p,"Toilet bowl",new(x+s,.3f,z-1.1f),new(.6f,.6f,.75f),"FutureCeramic");
                Box(p,"Toilet cistern",new(x+s,.7f,z-1.45f),new(.65f,.9f,.2f),"FutureCeramic");
            }
            // A separate quiet room is reachable through a 2.4 m opening from the central aisle.
            float roomZ=d*.5f-5;
            Box(p,"Meeting room glazed partition",new(-w*.17f,1.45f,roomZ-2.7f),new(w*.48f,2.9f,.065f),"Glass");
            Box(p,"Meeting room side",new(w*.08f,1.45f,roomZ+.3f),new(.08f,2.9f,3.6f),"Glass");
            Sign(p,"MEETING / FOCUS",new(-w*.17f,2.6f,roomZ-2.75f),.085f);
            Box(p,"Shared wall display",new(-w*.18f,1.8f,d*.5f-.2f),new(3.2f,1.5f,.12f),"FutureCarbon");
            Box(p,"Shared display content",new(-w*.18f,1.8f,d*.5f-.28f),new(3,1.3f,.02f),"NeonCyan",false);
        }
    }
}
