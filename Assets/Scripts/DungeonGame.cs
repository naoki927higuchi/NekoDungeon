using System;
using System.Collections.Generic;
using UnityEngine;

// All connectivity is derived from cardinal grid adjacency, including the return door.
public class DungeonGame : MonoBehaviour
{
    public static readonly Vector2Int[] Rooms = {
        new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(-1,1),
        new Vector2Int(1,1), new Vector2Int(-1,2), new Vector2Int(0,2),
        new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(0,3),
        new Vector2Int(-1,3), new Vector2Int(2,3), new Vector2Int(2,4)
    };
    public static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    readonly HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
    Vector2Int current;
    Vector3 position;
    Transform roomRoot, hero, beacon;
    Camera cam;
    bool complete;
    float elapsed, transition, stride;
    int moves;
    static readonly Color Teal = new Color(.22f,.85f,.76f);
    static readonly Color Gold = new Color(1f,.73f,.31f);
    public static bool HasRoom(Vector2Int cell) { return Array.IndexOf(Rooms, cell) >= 0; }
    public static bool CanTravel(Vector2Int cell, Vector2Int dir) { return Array.IndexOf(Directions, dir) >= 0 && HasRoom(cell) && HasRoom(cell + dir); }

    void Start()
    {
        Application.targetFrameRate = 60;
        cam = new GameObject("Isometric camera", typeof(Camera)).GetComponent<Camera>();
        cam.transform.position = new Vector3(0, 16, -11);
        cam.transform.LookAt(new Vector3(0,0,0.6f));
        cam.orthographic = true; cam.orthographicSize = 8.4f;
        cam.backgroundColor = new Color(.025f,.045f,.065f); cam.clearFlags = CameraClearFlags.SolidColor;
        var light = new GameObject("Moonlight", typeof(Light)).GetComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.3f;
        light.transform.rotation = Quaternion.Euler(48,-28,0); light.shadows = LightShadows.Soft;
        RenderSettings.ambientLight = new Color(.38f,.46f,.55f);
        QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowDistance = 50;
        hero = new GameObject("Explorer").transform;
        Shape("Cloak", PrimitiveType.Capsule, new Vector3(0,.65f,0), new Vector3(.55f,.62f,.55f), Teal, hero);
        Shape("Hood", PrimitiveType.Sphere, new Vector3(0,1.32f,0), Vector3.one*.48f, new Color(.8f,.89f,.84f), hero);
        Shape("Pack", PrimitiveType.Cube, new Vector3(0,.8f,-.26f), new Vector3(.36f,.42f,.2f), new Color(.24f,.27f,.3f), hero);
        Shape("Lantern", PrimitiveType.Sphere, new Vector3(.4f,.8f,.12f), Vector3.one*.18f, Gold, hero, true);
        Restart();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-selftest") >= 0) SelfTest();
    }

    Material Mat(Color color, bool glow = false)
    {
        if (materials.TryGetValue(color, out var existing)) return existing;
        var mat = new Material(Resources.Load<Material>("Stone")); mat.color = color;
        mat.SetFloat("_Glossiness", .15f);
        if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color*.7f); }
        materials[color] = mat; return mat;
    }
    Transform Shape(string label, PrimitiveType kind, Vector3 p, Vector3 scale, Color color, Transform parent, bool glow = false)
    {
        var obj = GameObject.CreatePrimitive(kind); obj.name = label;
        obj.transform.SetParent(parent, false); obj.transform.localPosition = p; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = Mat(color, glow);
        Destroy(obj.GetComponent<Collider>()); return obj.transform;
    }
    void Box(string label, Vector3 p, Vector3 size, Color c) { Shape(label, PrimitiveType.Cube, p, size, c, roomRoot); }
    void Restart()
    {
        current = Rooms[0]; visited.Clear(); visited.Add(current);
        position = new Vector3(0,0,-2.4f); elapsed = 0; moves = 0; complete = false; transition = 0;
        BuildRoom(); hero.position = position;
    }
    void BuildRoom()
    {
        if (roomRoot != null) { roomRoot.gameObject.SetActive(false); Destroy(roomRoot.gameObject); }
        roomRoot = new GameObject("Room " + current).transform; beacon = null;
        Color stone = new Color(.19f,.25f,.29f), edge = new Color(.27f,.34f,.38f);
        Box("Floating foundation", new Vector3(0,-.52f,0), new Vector3(10.8f,.8f,10.8f), new Color(.1f,.15f,.19f));
        for (int x=-4;x<=4;x++) for(int z=-4;z<=4;z++) {
            float tint = ((x*13+z*7+31)%5)*.007f;
            Box("Floor tile",new Vector3(x*1.1f,-.055f,z*1.1f),new Vector3(1.065f,.2f,1.065f),stone+new Color(tint,tint,tint,0));
        }
        foreach (var d in Directions) {
            bool door = CanTravel(current,d);
            bool horizontal = d.y != 0;
            for(int i=-4;i<=4;i++) {
                if (door && Mathf.Abs(i)<=1) continue;
                var p = horizontal ? new Vector3(i*1.1f,.48f,d.y*5.1f) : new Vector3(d.x*5.1f,.48f,i*1.1f);
                Box("Low stone wall",p,horizontal?new Vector3(1.08f,1.05f,.5f):new Vector3(.5f,1.05f,1.08f),edge);
            }
            if (!door) continue;
            var center = new Vector3(d.x*5.1f,0,d.y*5.1f);
            var side = horizontal ? Vector3.right : Vector3.forward;
            foreach(int s in new[]{-1,1}) {
                Box("Door pillar",center+side*s*1.62f+Vector3.up*.85f,new Vector3(.42f,1.8f,.42f),edge);
                Shape("Door light",PrimitiveType.Cube,center+side*s*1.62f+Vector3.up*1.8f,new Vector3(.28f,.12f,.28f),Teal,roomRoot,true);
            }
            Box("Threshold",center+Vector3.up*.055f,horizontal?new Vector3(2.9f,.12f,.6f):new Vector3(.6f,.12f,2.9f),Teal*.6f);
            for(int i=0;i<3;i++) Box("Path markers", center*.68f-new Vector3(d.x,0,d.y)*i*.4f+Vector3.up*.06f,horizontal?new Vector3(.35f,.025f,.13f):new Vector3(.13f,.025f,.35f),Teal);
        }
        for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2) {
            Shape("Corner pedestal",PrimitiveType.Cylinder,new Vector3(x*4.35f,.4f,z*4.35f),new Vector3(.65f,.4f,.65f),edge,roomRoot);
            Shape("Crystal",PrimitiveType.Cube,new Vector3(x*4.35f,1,z*4.35f),Vector3.one*.26f,Teal,roomRoot,true).rotation=Quaternion.Euler(30,45,20);
        }
        bool goal = current == Rooms[Rooms.Length-1];
        Shape("Center medallion",PrimitiveType.Cylinder,new Vector3(0,.065f,0),new Vector3(2.1f,.04f,2.1f),goal?Gold*.6f:edge,roomRoot);
        if(goal) {
            beacon = Shape("Exit crystal",PrimitiveType.Cube,new Vector3(0,1.4f,0),Vector3.one*.65f,Gold,roomRoot,true);
            beacon.rotation = Quaternion.Euler(45,0,45);
        }
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        if(Input.GetKeyDown(KeyCode.R)) Restart();
        if(beacon) { beacon.Rotate(0,45*Time.deltaTime,0,Space.World); beacon.position=new Vector3(0,1.4f+Mathf.Sin(Time.time*2)*.15f,0); }
        transition = Mathf.Max(0,transition-Time.deltaTime);
        if(complete) return;
        elapsed += Time.deltaTime;
        var direction = new Vector3((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),0,
            (Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)).normalized;
        if(direction.sqrMagnitude>0) {
            Move(direction*4.4f*Mathf.Min(Time.deltaTime,.05f));
            hero.rotation=Quaternion.Slerp(hero.rotation,Quaternion.LookRotation(direction),Time.deltaTime*14);
            stride+=Time.deltaTime*12;
        }
        hero.position=position+Vector3.up*(direction.sqrMagnitude>0?Mathf.Abs(Mathf.Sin(stride))*.055f:0);
        if(current==Rooms[Rooms.Length-1] && position.magnitude<1.05f) complete=true;
    }
    void Move(Vector3 delta)
    {
        var p=position+delta;
        // A doorway is wide enough for the player's radius; solid wall edges clamp motion.
        float limitX = Mathf.Abs(p.z)<1.1f && CanTravel(current,p.x>0?Vector2Int.right:Vector2Int.left)?5.65f:4.65f;
        p.x=Mathf.Clamp(p.x,-limitX,limitX);
        float limitZ = Mathf.Abs(p.x)<1.1f && CanTravel(current,p.z>0?Vector2Int.up:Vector2Int.down)?5.65f:4.65f;
        p.z=Mathf.Clamp(p.z,-limitZ,limitZ); position=p;
        if(Mathf.Abs(p.x)>5.5f) Travel(p.x>0?Vector2Int.right:Vector2Int.left);
        else if(Mathf.Abs(p.z)>5.5f) Travel(p.z>0?Vector2Int.up:Vector2Int.down);
    }
    void Travel(Vector2Int dir)
    {
        if(!CanTravel(current,dir)) return;
        current+=dir; moves++; visited.Add(current);
        position=new Vector3(-dir.x*4.15f,0,-dir.y*4.15f); transition=.28f; BuildRoom();
    }
    void Panel(Rect rect, Color c) { GUI.color=c; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=Color.white; }
    void Label(Rect rect,string text,int size,Color color,TextAnchor align=TextAnchor.UpperLeft)
    { GUI.Label(rect,text,new GUIStyle(GUI.skin.label){fontSize=size,normal={textColor=color},alignment=align}); }
    void OnGUI()
    {
        float sx=Screen.width/1280f, sy=Screen.height/800f;
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(sx,sy,1));
        var muted=new Color(.57f,.68f,.73f);
        Panel(new Rect(0,0,1280,95),new Color(.025f,.045f,.065f,.95f));
        Label(new Rect(35,20,600,20),"A  Q U I E T  E X P L O R A T I O N",12,Teal);
        Label(new Rect(33,40,650,45),"THE QUIET VAULT",30,Color.white);
        Label(new Rect(860,31,380,32),"FIND THE GOLDEN EXIT",18,Gold,TextAnchor.MiddleRight);
        Label(new Rect(860,62,380,22),"FLOOR 01  /  NO ENEMIES · NO TRAPS",11,muted,TextAnchor.MiddleRight);
        Panel(new Rect(1010,122,235,306),new Color(.025f,.045f,.065f,.93f));
        Label(new Rect(1028,137,210,25),"EXPLORATION MAP",13,Teal);
        float cell=38, ox=1040, oy=183;
        foreach(var r in Rooms) {
            var rect=new Rect(ox+(r.x+1)*cell,oy+(4-r.y)*cell,25,25);
            foreach(var d in new[]{Vector2Int.up,Vector2Int.right}) if(CanTravel(r,d)) {
                Panel(new Rect(rect.x+10,rect.y+10+(d.y==1?-cell:0),d.x==1?cell+5:5,d.y==1?cell+5:5),new Color(.17f,.23f,.27f));
            }
        }
        foreach(var r in Rooms) {
            var rect=new Rect(ox+(r.x+1)*cell,oy+(4-r.y)*cell,25,25);
            Panel(rect,r==current?Teal:visited.Contains(r)?new Color(.30f,.46f,.48f):new Color(.14f,.20f,.24f));
            string symbol = r==Rooms[0]?"S":r==Rooms[Rooms.Length-1]?"G":"";
            Label(rect,symbol,13,r==current?Color.black:Gold,TextAnchor.MiddleCenter);
            if(r==current) Panel(new Rect(rect.x+10,rect.y+10,5,5),Color.white);
        }
        Label(new Rect(1028,390,210,24),"S  START     G  GOAL",11,muted);
        Label(new Rect(35,125,340,24),"CHAMBER  "+(Array.IndexOf(Rooms,current)+1).ToString("00"),17,Color.white);
        Label(new Rect(35,155,340,25),visited.Count+" / 12 explored   ·   "+moves+" passages",13,muted);
        Panel(new Rect(0,728,1280,72),new Color(.025f,.045f,.065f,.95f));
        Label(new Rect(35,748,920,30),"W A S D  /  ARROWS    Move through doors          R    Restart          ESC    Quit",14,muted);
        Label(new Rect(1000,748,245,30),TimeSpan.FromSeconds(elapsed).ToString(@"mm\:ss"),18,Teal,TextAnchor.MiddleRight);
        if(transition>0) Panel(new Rect(0,95,1000,630),new Color(.02f,.04f,.06f,transition));
        if(complete) {
            Panel(new Rect(0,95,1280,633),new Color(.015f,.025f,.04f,.80f));
            Panel(new Rect(390,254,500,278),new Color(.055f,.09f,.12f));
            Label(new Rect(390,280,500,24),"J O U R N E Y   C O M P L E T E",13,Teal,TextAnchor.MiddleCenter);
            Label(new Rect(390,321,500,60),"EXIT DISCOVERED",32,Gold,TextAnchor.MiddleCenter);
            Label(new Rect(390,391,500,35),visited.Count+" rooms explored  /  "+moves+" passages",16,Color.white,TextAnchor.MiddleCenter);
            if(GUI.Button(new Rect(525,452,230,45),"EXPLORE AGAIN   [ R ]")) Restart();
        }
    }
    void SelfTest()
    {
        try {
            var seen=new HashSet<Vector2Int>{Rooms[0]}; var queue=new Queue<Vector2Int>(); queue.Enqueue(Rooms[0]);
            while(queue.Count>0) { var r=queue.Dequeue(); int degree=0;
                foreach(var d in Directions) if(CanTravel(r,d)) { degree++; if(!CanTravel(r+d,-d)) throw new Exception("Missing return door"); if(seen.Add(r+d)) queue.Enqueue(r+d); }
                if(degree>4) throw new Exception("Too many doors");
            }
            if(seen.Count!=Rooms.Length) throw new Exception("Unreachable room");
            Restart(); Move(new Vector3(10,0,0)); if(current!=Rooms[0]||position.x>4.66f) throw new Exception("Wall bypass");
            Restart(); Move(new Vector3(0,0,10)); if(current!=new Vector2Int(0,1)) throw new Exception("Door crossing failed");
            Move(new Vector3(0,0,-10)); if(current!=Rooms[0]) throw new Exception("Return crossing failed");
            Restart(); foreach(var d in new[]{Vector2Int.up,Vector2Int.up,Vector2Int.right,Vector2Int.right,Vector2Int.up,Vector2Int.up}) Travel(d);
            if(current!=Rooms[Rooms.Length-1]) throw new Exception("Goal unreachable");
            position=Vector3.zero; Update(); if(!complete) throw new Exception("Goal did not complete");
            Restart(); if(complete||moves!=0||visited.Count!=1) throw new Exception("Restart failed");
            Debug.Log("SELFTEST PASS: connectivity, four-door limit, return paths, walls, room crossing, goal, restart"); Application.Quit(0);
        } catch(Exception e) { Debug.LogException(e); Application.Quit(1); }
    }
}
