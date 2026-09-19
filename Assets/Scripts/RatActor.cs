using UnityEngine;

// Uses the game's original faceted mesh, with a distinct mouse silhouette and palette.
public sealed class RatActor : MonoBehaviour
{
    public RatBrain Brain { get; private set; }
    Transform body, tail, alert;
    readonly Transform[] feet=new Transform[4];
    float phase, defeatTime;

    public void Initialize(RatBrain brain,Material fur,Material pink,Material dark,Material gold)
    {
        Brain=brain;
        var mesh=Resources.Load<Mesh>("CatModel/Cat faceted ellipsoid");
        body=Pivot("Mouse body",transform,new Vector3(0,.25f,0));
        Part("Brown fur",mesh,body,Vector3.zero,new Vector3(.43f,.36f,.63f),fur);
        Part("Tapered snout",mesh,body,new Vector3(0,.01f,.29f),new Vector3(.26f,.25f,.37f),fur);
        if(brain.Aggressive) Part("Red hunter crest",mesh,body,new Vector3(0,.17f,0),new Vector3(.28f,.12f,.32f),gold);
        Part("Pink nose",mesh,body,new Vector3(0,.015f,.46f),Vector3.one*.075f,pink);
        foreach(int side in new[]{-1,1}) {
            Part("Round ear",mesh,body,new Vector3(side*.17f,.18f,.19f),new Vector3(.23f,.25f,.085f),fur);
            Part("Inner ear",mesh,body,new Vector3(side*.17f,.19f,.231f),new Vector3(.155f,.17f,.035f),pink);
            Part("Black eye",mesh,body,new Vector3(side*.115f,.07f,.325f),new Vector3(.068f,.075f,.05f),dark);
        }
        for(int i=0;i<4;i++) feet[i]=Part("Paw",mesh,transform,new Vector3(i%2==0?-.17f:.17f,.075f,i<2?.19f:-.21f),new Vector3(.10f,.10f,.18f),pink);
        tail=Pivot("Long tail",transform,new Vector3(0,.18f,-.26f));
        var points=new[]{Vector3.zero,new Vector3(.06f,-.05f,-.20f),new Vector3(.16f,-.07f,-.37f),new Vector3(.28f,-.05f,-.46f)};
        for(int i=0;i<points.Length-1;i++) {
            var delta=points[i+1]-points[i];
            var piece=Part("Tail",mesh,tail,(points[i]+points[i+1])*.5f,new Vector3(.055f-i*.01f,delta.magnitude+.045f,.055f-i*.01f),pink);
            piece.localRotation=Quaternion.FromToRotation(Vector3.up,delta);
        }
        alert=Pivot("Spotted the cat",transform,new Vector3(0,.95f,0));
        Part("Alert stem",mesh,alert,new Vector3(0,.09f,0),new Vector3(.075f,.22f,.075f),gold);
        Part("Alert dot",mesh,alert,new Vector3(0,-.08f,0),Vector3.one*.08f,gold);
        Sync(0);
    }
    public void Sync(float dt)
    {
        if(Brain.Defeated) {
            defeatTime+=dt; float t=Mathf.Clamp01(defeatTime/.35f);
            alert.gameObject.SetActive(false);
            transform.localPosition=new Vector3(Brain.Position.x,Mathf.Sin(t*Mathf.PI)*.4f,Brain.Position.y);
            body.localRotation=Quaternion.Euler(0,0,t*150);
            transform.localScale=Vector3.one*(1-t);
            if(t>=1) gameObject.SetActive(false);
            return;
        }
        phase+=dt*(Brain.Speed>0?18:2);
        transform.localPosition=new Vector3(Brain.Position.x,0,Brain.Position.y);
        var rotation=Quaternion.LookRotation(new Vector3(Brain.Forward.x,0,Brain.Forward.y));
        transform.localRotation=dt>0?Quaternion.Slerp(transform.localRotation,rotation,dt*18):rotation;
        body.localPosition=new Vector3(0,.25f+(Brain.Speed>0?Mathf.Abs(Mathf.Sin(phase))*.035f:Mathf.Sin(phase)*.009f),0);
        body.localRotation=Quaternion.Euler(Brain.WindingUp?-20:0,0,0);
        for(int i=0;i<4;i++) feet[i].localRotation=Quaternion.Euler(Brain.Speed>0?Mathf.Sin(phase+(i==0||i==3?0:Mathf.PI))*30:0,0,0);
        tail.localRotation=Quaternion.Euler(0,Mathf.Sin(phase*.6f)*18,0);
        alert.gameObject.SetActive(Brain.Fleeing || Brain.Chasing);
        alert.localScale=Vector3.one*(Brain.WindingUp?1.35f+Mathf.Sin(phase*8)*.15f:1);
    }
    static Transform Pivot(string name,Transform parent,Vector3 p)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
    static Transform Part(string name,Mesh mesh,Transform parent,Vector3 p,Vector3 scale,Material mat)
    {
        var t=Pivot(name,parent,p);t.localScale=scale;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t;
    }
}
