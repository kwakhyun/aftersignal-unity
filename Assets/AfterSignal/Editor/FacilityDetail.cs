using UnityEngine;
namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        static void CivicChair(float x,float y,float z)
        {
            Box("Chair / upholstered seat",x,y+.57f,z,1.12f,.17f,.95f,"SoftCloth");
            Box("Chair / back cushion",x,y+1.08f,z-.42f,1.05f,.66f,.13f,"SoftCloth");
            foreach(float dx in new[]{-.44f,.44f}){
                Box("Chair / tubular back",x+dx,y+.92f,z-.43f,.055f,1.12f,.055f,"Chrome");
                foreach(float dz in new[]{-.37f,.37f})Box("Chair / tubular leg",x+dx,y+.27f,z+dz,.055f,.54f,.055f,"Chrome");
            }
        }
        static void FacilityDetail(StageId stage)
        {
            // The near floor is set dressing outside the existing travel bounds.
            // It closes the camera cutaway; navigation and interactable positions stay intact.
            Box("Facility foreground foundation",26,-.4f,-17,63,.6f,14,"DarkMetal");
            for(int row=0;row<12;row++)for(int col=0;col<21;col++){
                float z=-22.5f+row*2.8f;string material=stage==StageId.School?"HomeWood"+((row+col)%4):stage==StageId.Clinic?(row%3==0?"Enamel":"CivicStone"):((row+col)%5==0?"Metal":"Concrete");
                Box("Facility floor slab",-3.5f+col*3,.012f,z,2.975f,.02f,2.775f,material);
            }
            Box("Inlaid wayfinding line",26,.033f,-7.8f,62,.012f,.075f,stage==StageId.Clinic?"DistrictBlue":"Gold");
            foreach(float x in new[]{-4.6f,12f,29f,46f,57.6f}){
                Box("Facility wall pilaster",x,4.5f,9.25f,.3f,9,.52f,"CivicBronze");
                Box("Facility ceiling beam",x,8.5f,0,.3f,.23f,18,"DarkMetal");
            }
            Box("Wall skirting",26,.25f,9.4f,63,.5f,.3f,"WarmWood");
            foreach(var r in world.GetComponentsInChildren<MeshRenderer>()){
                if(r.name=="Desk top"||r.name=="Reception desk")r.sharedMaterial=Mat("WarmWood");
                if(r.name=="Mattress"||r.name=="Pillow")r.sharedMaterial=Mat("Enamel");
                if(r.name=="Folded blanket"||r.name=="Privacy divider")r.sharedMaterial=Mat("SoftCloth");
            }
            if(stage==StageId.School){
                foreach(float x in new[]{21f,28f,35f,42f}){
                    Box("Classroom bookcase back",x,2,8.55f,3.4f,4,.65f,"WarmWood");
                    for(int row=0;row<4;row++){float y=.5f+row*.9f;Box("Classroom shelf",x,y,8.1f,3.5f,.1f,1,"WarmWood");for(int b=0;b<10;b++){float h=.42f+b%3*.09f;Box("Student reference book",x-1.43f+b*.3f,y+h*.5f+.1f,8.02f,.22f,h,.58f,b%3==0?"DistrictRed":b%3==1?"Seat":"Enamel");}}
                }
                Box("Lesson board frame",17,3.5f,9.08f,5.5f,2.2f,.22f,"WarmWood");Box("Lesson slate",17,3.5f,8.93f,5.2f,1.95f,.05f,"DarkMetal");
                Sign("기억은 누구의 것일까?",new Vector3(17,3.6f,8.87f),4.6f,.55f,"Amber");
            }else if(stage==StageId.Clinic){
                for(int i=0;i<4;i++){float x=22+i*8;
                    for(int pleat=0;pleat<8;pleat++)Box("Curtain fold",x+3.42f,1.38f,2.3f+pleat*.48f,.1f,2.3f,.2f,"Enamel");
                    foreach(float z in new[]{3.03f,4.97f})Box("Hospital bedside rail",x,.98f,z,2.9f,.07f,.07f,"Chrome");
                    Box("Bedside medication cabinet",x-2.4f,.54f,4,1.1f,1.08f,.9f,"Enamel");FurnitureFace("Cabinet",new Vector3(x-2.4f,.54f,3.53f),1.05f,1,Quaternion.identity);
                }
            }else{
                for(int i=0;i<4;i++){float x=21+i*8;CivicChair(x,0,5.4f);Box("Console keyboard",x,.98f,6.35f,1.35f,.045f,.4f,"DarkMetal");}
                foreach(float x in new[]{23f,27f,31f})foreach(float z in new[]{-1.8f,3.8f})CivicChair(x,0,z);
                for(int i=0;i<5;i++){float x=38+i*3.8f;Box("Archive server cabinet",x,2.15f,8.6f,2.2f,4.3f,1.1f,"DarkMetal");for(int r=0;r<12;r++){Box("Server drawer",x,.35f+r*.3f,7.99f,1.92f,.24f,.05f,"Metal");Box("Server status",x+.7f,.35f+r*.3f,7.94f,.07f,.045f,.015f,r%4==0?"Amber":"Cyan");}}
            }
        }
    }
}
