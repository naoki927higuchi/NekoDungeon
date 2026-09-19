using System.Collections.Generic;
using UnityEngine;

// Original low-poly cat. +Z is forward; the root stays at the gameplay position.
public sealed class CatAvatar : MonoBehaviour
{
    [SerializeField] Transform body, head, tail;
    [SerializeField] Transform[] legs;
    float phase, blend, punchTime;
    public void Punch() { punchTime=.25f; }

    public void Animate(float speed, float dt)
    {
        blend = Mathf.MoveTowards(blend, Mathf.Clamp01(speed), dt * 8);
        phase += dt * Mathf.Lerp(2, 12, blend);
        body.localPosition = new Vector3(0, .48f + Mathf.Sin(phase * 2) * .022f * blend, -.08f);
        head.localRotation = Quaternion.Euler(Mathf.Sin(phase) * 2 * blend, Mathf.Sin(phase * .3f) * 4 * (1-blend), 0);
        for (int i=0; i<legs.Length; i++) {
            float offset = i==0 || i==3 ? 0 : Mathf.PI;
            legs[i].localRotation = Quaternion.Euler(Mathf.Sin(phase+offset)*26*blend,0,0);
        }
        tail.localRotation = Quaternion.Euler(0, Mathf.Sin(phase*.45f)*17, Mathf.Sin(phase*.35f)*7);
        punchTime=Mathf.Max(0,punchTime-dt);
        float punch=Mathf.Sin((1-punchTime/.25f)*Mathf.PI);
        legs[1].localPosition=new Vector3(.20f,.38f,.24f+punch*.24f);
        if(punchTime>0) legs[1].localRotation=Quaternion.Euler(-110*punch,0,-18*punch);
    }
    public void ResetPose()
    {
        phase=0; blend=0; punchTime=0; Animate(0,0);
    }

    // Called in the editor to bake an editable prefab, mesh assets and materials.
    public void BuildModel(Material template)
    {
        transform.localScale=Vector3.one*1.2f;
        var fur = Material(template,"Ivory fur",new Color(.82f,.80f,.70f));
        var dark = Material(template,"Slate markings",new Color(.22f,.27f,.31f));
        var cream = Material(template,"Warm white paws",new Color(.97f,.94f,.82f));
        var pink = Material(template,"Rose ears and nose",new Color(.78f,.40f,.42f));
        var teal = Material(template,"Jade scarf",new Color(.13f,.67f,.59f));
        var gold = Material(template,"Amber eyes and charm",new Color(1f,.68f,.19f));
        var pupil = Material(template,"Dark pupils",new Color(.035f,.055f,.065f));
        var round = RoundMesh();
        var ear = EarMesh();
        body = Pivot("Body",transform,new Vector3(0,.48f,-.08f));
        Part("Torso",round,body,Vector3.zero,new Vector3(.59f,.50f,.86f),fur);
        Part("Back saddle",round,body,new Vector3(0,.19f,-.17f),new Vector3(.48f,.22f,.49f),dark);
        Part("Chest",round,body,new Vector3(0,.035f,.30f),new Vector3(.48f,.48f,.39f),cream);
        head = Pivot("Head",transform,new Vector3(0,.82f,.34f));
        Part("Cat head",round,head,Vector3.zero,new Vector3(.66f,.54f,.55f),fur);
        // Broad, pointed ears remain legible from the elevated game camera.
        foreach (int side in new[]{-1,1}) {
            var earPart = Part(side<0?"Left ear":"Right ear",ear,head,new Vector3(side*.23f,.19f,-.015f),new Vector3(.25f,.38f,.22f),dark);
            earPart.localRotation=Quaternion.Euler(0,0,-side*12);
            Part("Inner ear",ear,earPart,new Vector3(0,.10f,.22f),new Vector3(.60f,.66f,.58f),pink);
            Part("Cheek",round,head,new Vector3(side*.105f,-.08f,.225f),new Vector3(.24f,.18f,.18f),cream);
            Part("Amber eye",round,head,new Vector3(side*.19f,.045f,.236f),new Vector3(.13f,.14f,.072f),gold);
            Part("Vertical pupil",round,head,new Vector3(side*.19f,.05f,.273f),new Vector3(.036f,.10f,.022f),pupil);
            Part("Eye glint",round,head,new Vector3(side*.19f-.022f,.082f,.282f),Vector3.one*.025f,cream);
        }
        Part("Forehead blaze",round,head,new Vector3(0,.19f,.16f),new Vector3(.16f,.16f,.18f),cream);
        Part("Pink nose",round,head,new Vector3(0,-.045f,.322f),new Vector3(.09f,.065f,.065f),pink);
        Part("Scarf collar",round,transform,new Vector3(0,.61f,.28f),new Vector3(.52f,.18f,.42f),teal);
        Part("Scarf knot",round,transform,new Vector3(.29f,.59f,.22f),Vector3.one*.15f,teal);
        var scarfEnd=Part("Scarf end",ear,transform,new Vector3(.24f,.47f,.12f),new Vector3(.18f,.30f,.10f),teal);
        scarfEnd.localRotation=Quaternion.Euler(0,0,155);
        Part("Explorer charm",round,transform,new Vector3(0,.49f,.43f),new Vector3(.105f,.13f,.07f),gold);
        legs=new Transform[4];
        for(int i=0;i<4;i++) {
            float x=i%2==0?-.20f:.20f, z=i<2?.24f:-.36f;
            legs[i]=Pivot("Leg "+i,transform,new Vector3(x,.38f,z));
            Part("Lower leg",round,legs[i],new Vector3(0,-.12f,0),new Vector3(.17f,.36f,.19f),fur);
            Part("White paw",round,legs[i],new Vector3(0,-.25f,.04f),new Vector3(.21f,.17f,.28f),cream);
        }
        tail=Pivot("Tail",transform,new Vector3(0,.51f,-.47f));
        var points=new[]{Vector3.zero,new Vector3(0,.12f,-.19f),new Vector3(0,.35f,-.30f),new Vector3(0,.59f,-.30f),new Vector3(.04f,.78f,-.25f),new Vector3(.13f,.85f,-.15f)};
        for(int i=0;i<points.Length-1;i++) {
            var delta=points[i+1]-points[i];
            var part=Part("Tail segment "+i,round,tail,(points[i]+points[i+1])*.5f,new Vector3(.13f-i*.009f,delta.magnitude+.10f,.13f-i*.009f),i>=3?dark:fur);
            part.localRotation=Quaternion.FromToRotation(Vector3.up,delta);
        }
        ResetPose();
    }
    static Material Material(Material template,string name,Color color)
    {
        var result=new Material(template){name=name,color=color}; result.SetFloat("_Glossiness",.12f); return result;
    }
    static Transform Pivot(string name,Transform parent,Vector3 position)
    {
        var result=new GameObject(name).transform; result.SetParent(parent,false); result.localPosition=position; return result;
    }
    static Transform Part(string name,Mesh mesh,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var result=Pivot(name,parent,position); result.localScale=scale;
        result.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        result.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material; return result;
    }
    static Mesh RoundMesh()
    {
        var vertices=new List<Vector3>(); var indices=new List<int>();
        const int rings=6, sides=10;
        for(int y=0;y<rings;y++) for(int x=0;x<sides;x++) {
            Vector3 a=SpherePoint(y,x,rings,sides), b=SpherePoint(y+1,x,rings,sides), c=SpherePoint(y+1,x+1,rings,sides), d=SpherePoint(y,x+1,rings,sides);
            if(y>0) Triangle(vertices,indices,a,d,b);
            if(y<rings-1) Triangle(vertices,indices,d,c,b);
        }
        return MakeMesh("Cat faceted ellipsoid",vertices,indices);
    }
    static Vector3 SpherePoint(int y,int x,int rings,int sides)
    {
        float pitch=Mathf.PI*y/rings, angle=2*Mathf.PI*x/sides;
        return new Vector3(Mathf.Sin(pitch)*Mathf.Cos(angle),Mathf.Cos(pitch),Mathf.Sin(pitch)*Mathf.Sin(angle))*.5f;
    }
    static Mesh EarMesh()
    {
        var vertices=new List<Vector3>(); var indices=new List<int>();
        var a=new Vector3(-.5f,0,.5f); var b=new Vector3(.5f,0,.5f); var c=new Vector3(0,1,0); var d=new Vector3(0,0,-.5f);
        Triangle(vertices,indices,a,b,c); Triangle(vertices,indices,b,d,c); Triangle(vertices,indices,d,a,c); Triangle(vertices,indices,a,d,b);
        return MakeMesh("Cat pointed ear",vertices,indices);
    }
    static void Triangle(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c)
    { int n=v.Count; v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2); }
    static Mesh MakeMesh(string name,List<Vector3> vertices,List<int> indices)
    { var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh; }
}
