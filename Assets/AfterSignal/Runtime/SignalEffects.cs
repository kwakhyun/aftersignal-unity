using UnityEngine;

namespace AfterSignal
{
    public static class SignalEffects
    {
        public static readonly Color Cyan=new Color(.15f,.92f,1),Gold=new Color(1,.67f,.2f),Red=new Color(1,.18f,.34f);
        static Material Material(Color c)=>Resources.Load<Material>(c.r>.8f?(c.g>.4f?"Materials/GoldFX":"Materials/RedFX"):"Materials/CyanFX");
        public static void Beam(Vector3 from,Vector3 to,Color color,float width,float life)
        {
            if(PresentationSettings.Effects<=0||EffectBudget.Active>=64)return;
            var go=new GameObject("Energy trail");var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Material(color);line.positionCount=2;
            line.SetPosition(0,from);line.SetPosition(1,to);line.widthMultiplier=width;line.numCapVertices=3;line.startColor=color;line.endColor=color;
            go.AddComponent<EffectBudget>();var fade=go.AddComponent<TransientEffect>();fade.life=life;fade.line=line;
        }
        public static void Ring(Vector3 center,Color color,float radius,float life)
        {
            if(PresentationSettings.Effects<=0||EffectBudget.Active>=64)return;
            var go=new GameObject("Resonance ring");var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Material(color);line.positionCount=65;line.widthMultiplier=.045f;
            line.startColor=line.endColor=color;
            for(int i=0;i<65;i++){float a=i/64f*Mathf.PI*2;line.SetPosition(i,center+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius);}
            go.AddComponent<EffectBudget>();var fx=go.AddComponent<TransientEffect>();fx.life=life;fx.line=line;fx.expansionCenter=center;fx.expand=true;
        }
        public static void Slash(Vector3 center,float facing,float radius,Color color,int combo)
        {
            if(PresentationSettings.Effects<=0||EffectBudget.Active>=54)return;
            if(radius<3){Beam(center+Vector3.right*facing*.5f,center+Vector3.right*facing*radius,color,.035f,.1f);return;}
            var go=new GameObject("Blade arc");var line=go.AddComponent<LineRenderer>();line.sharedMaterial=Material(color);line.positionCount=25;line.widthMultiplier=.16f;line.numCapVertices=3;
            line.startColor=line.endColor=color;
            line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.6f,1),new Keyframe(1,0));
            for(int i=0;i<25;i++){float a=Mathf.Lerp(-1.2f,1.4f,i/24f);if(combo==1)a=-a;line.SetPosition(i,center+new Vector3(Mathf.Cos(a)*facing,Mathf.Sin(a)*.72f,-.12f)*radius);}
            go.AddComponent<EffectBudget>();var fx=go.AddComponent<TransientEffect>();fx.line=line;fx.life=.17f;
        }
        public static void Burst(Vector3 center,Color color,int count,float speed)
        {
            if(PresentationSettings.Effects<=0||EffectBudget.Active>=48)return;
            count=Mathf.Min(32,Mathf.CeilToInt(count*PresentationSettings.Effects));
            var go=new GameObject("Signal sparks");go.transform.position=center;
            go.AddComponent<EffectBudget>();
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=.7f;main.startLifetime=new ParticleSystem.MinMaxCurve(.16f,.6f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.4f,speed);main.startSize=new ParticleSystem.MinMaxCurve(.025f,.095f);main.startColor=color;main.gravityModifier=.45f;main.maxParticles=80;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.14f;
            var render=ps.GetComponent<ParticleSystemRenderer>();render.sharedMaterial=Material(color);render.renderMode=ParticleSystemRenderMode.Stretch;render.lengthScale=2;
            ps.Emit(count);Object.Destroy(go,1.1f);
        }
        public static void Impact(Vector3 point,Vector3 direction,Color color,float strength)
        {
            if(PresentationSettings.Effects<=0)return;
            direction.z=0;if(direction.sqrMagnitude<.01f)direction=Vector3.right;direction.Normalize();
            var tangent=new Vector3(-direction.y,direction.x,0);float s=strength*.36f;
            Beam(point-direction*s*.55f,point+direction*s,color,.09f,.08f);
            Beam(point-tangent*s*.65f,point+tangent*s*.65f,color,.05f,.07f);
            for(int i=0;i<4;i++){Vector3 ray=(direction+ tangent*(i-1.5f)*.32f).normalized;Beam(point+ray*.06f,point+ray*(.45f+strength*.4f),color,.024f,.14f);}
        }
        public static void Dust(Vector3 point,Vector3 direction,float strength)
        {
            if(PresentationSettings.Effects<=0||EffectBudget.Active>=40)return;
            for(int i=0;i<3;i++)Beam(point+new Vector3((i-1)*.16f,.06f,-.03f),point+new Vector3((i-1)*.33f,.08f,-.03f)+direction*strength*.18f,new Color(.48f,.57f,.57f),.035f,.16f);
        }
        public static void Afterimage(PixelActor actor,float life)
        {
            if(PresentationSettings.Effects<=0||EffectBudget.Active>=48)return;
            var source=actor.Visual.GetComponent<SpriteRenderer>();var go=new GameObject("Dash silhouette");go.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);go.transform.localScale=source.transform.lossyScale;
            var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=source.sprite;sprite.flipX=source.flipX;sprite.sharedMaterial=Resources.Load<Material>("Materials/GhostFX");sprite.color=new Color(.25f,.6f,.63f,.3f);go.AddComponent<EffectBudget>();Object.Destroy(go,life);
        }
        public static void Glass(Vector3 center,Vector3 size)
        {
            if(PresentationSettings.Effects<=0)return;
            for(int i=0;i<22;i++){
                var shard=GameObject.CreatePrimitive(PrimitiveType.Cube);shard.name="Glass shard";
                shard.transform.position=center+new Vector3(Random.Range(-.15f,.15f),Random.Range(-size.y/2,size.y/2),Random.Range(-size.z/2,size.z/2));
                shard.transform.localScale=new Vector3(.035f,Random.Range(.06f,.24f),Random.Range(.08f,.28f));shard.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Materials/Glass");
                Object.Destroy(shard.GetComponent<Collider>());var fx=shard.AddComponent<TransientEffect>();fx.life=1.8f;fx.velocity=new Vector3(Random.Range(-4f,4f),Random.Range(1f,6f),Random.Range(-3f,3f));fx.fall=true;
            }
            Burst(center,Cyan,35,6);
        }
    }
}
