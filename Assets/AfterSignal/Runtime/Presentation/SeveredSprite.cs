using UnityEngine;
namespace AfterSignal
{
    // Separate an atlas region while masking precisely the same region on the corpse.
    // No extra character image or duplicate complete body remains behind.
    public sealed class SeveredSprite:MonoBehaviour
    {
        SpriteRenderer body;Material original,cutMaterial;Sprite piece;GameObject limb;
        public static void Create(WorldActor actor,Vector3 force)
        {
            if(actor.GetComponent<SeveredSprite>())return;
            var sr=actor.GetComponent<SpriteRenderer>();if(!sr||!sr.sprite)return;
            var s=actor.gameObject.AddComponent<SeveredSprite>();s.body=sr;s.original=sr.sharedMaterial;
            int type=Random.Range(0,3);
            Vector4 cut=type==0?new Vector4(0,.78f,1,1):type==1?new Vector4(0,.36f,.31f,.73f):new Vector4(0,0,.49f,.37f);
            var sp=sr.sprite;Rect rect=sp.rect;var tex=sp.texture;
            var shader=Shader.Find("AfterSignal/SeveredSprite");if(!shader){Destroy(s);return;}
            s.cutMaterial=new Material(shader);s.cutMaterial.SetVector("_Atlas",new Vector4(rect.x/tex.width,rect.y/tex.height,rect.width/tex.width,rect.height/tex.height));s.cutMaterial.SetVector("_Cut",cut);sr.sharedMaterial=s.cutMaterial;
            var region=new Rect(rect.x+rect.width*cut.x,rect.y+rect.height*cut.y,rect.width*(cut.z-cut.x),rect.height*(cut.w-cut.y));
            s.piece=Sprite.Create(tex,region,new Vector2(.5f,.5f),sp.pixelsPerUnit,0,SpriteMeshType.FullRect);
            s.limb=new GameObject(type==0?"Detached head":type==1?"Detached arm":"Detached leg",typeof(SpriteRenderer),typeof(DetachedLimb));
            var part=s.limb.GetComponent<SpriteRenderer>();part.sprite=s.piece;part.sharedMaterial=s.original;part.color=sr.color;
            s.limb.transform.position=actor.transform.position+Vector3.up*(type==0?1.8f:type==1?1.1f:.5f);s.limb.transform.localScale=sr.transform.lossyScale;
            s.limb.GetComponent<DetachedLimb>().velocity=force.normalized*Random.Range(3f,7f)+Vector3.up*Random.Range(2f,4f);
        }
        public void Restore(){if(body)body.sharedMaterial=original;if(limb)Destroy(limb);if(piece)Destroy(piece);if(cutMaterial)Destroy(cutMaterial);Destroy(this);}
        void OnDisable(){if(body)body.sharedMaterial=original;if(limb)Destroy(limb);if(piece)Destroy(piece);if(cutMaterial)Destroy(cutMaterial);}
    }
    public sealed class DetachedLimb:MonoBehaviour
    {
        public Vector3 velocity;float age;bool settled;
        void Update(){if(GameDirector.Instance&&GameDirector.Instance.Blocked)return;float dt=Time.deltaTime;age+=dt;
            if(!settled){velocity.y-=18*dt;var step=velocity*dt;if(Physics.Raycast(transform.position,step.normalized,out var hit,step.magnitude+.1f,1,QueryTriggerInteraction.Ignore)){transform.position=hit.point+hit.normal*.12f;velocity=Vector3.zero;settled=true;}else transform.position+=step;}
            if(Camera.main)transform.rotation=Quaternion.Euler(Camera.main.transform.eulerAngles.x,Camera.main.transform.eulerAngles.y,settled?87:age*230);
            if(age>12)Destroy(gameObject);
        }
    }
}
