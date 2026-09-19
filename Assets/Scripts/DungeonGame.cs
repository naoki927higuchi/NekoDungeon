using System;
using System.Collections.Generic;
using UnityEngine;

// All connectivity is derived from cardinal grid adjacency, including the return door.
public class DungeonGame : MonoBehaviour
{
    DungeonLayout layout;
    readonly ExplorationMap exploration = new ExplorationMap();
    HashSet<Vector2Int> visited => exploration.Visited;
    static Vector2Int[] Directions => DungeonLayout.Directions;
    readonly System.Random seeds = new System.Random();
    bool choosing = true;
    int difficulty;
    int selectedDifficulty;
    readonly MenuNavigation menuNavigation = new MenuNavigation();
    Font uiFont;
    readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
    Vector2Int current;
    Vector3 position;
    Transform roomRoot, hero, beacon;
    CatAvatar cat;
    Camera cam;
    bool complete;
    float elapsed, transition;
    int moves;
    static readonly Color Teal = new Color(.22f,.85f,.76f);
    static readonly Color Gold = new Color(1f,.73f,.31f);
    bool CanTravel(Vector2Int cell, Vector2Int dir) { return layout != null && layout.CanTravel(cell, dir); }

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
        hero = Instantiate(Resources.Load<GameObject>("ExplorerCat")).transform;
        cat = hero.GetComponent<CatAvatar>();
        uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo", "Arial" }, 18);
        hero.gameObject.SetActive(false);
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
        current = Vector2Int.zero; exploration.Reset();
        position = new Vector3(0,0,-2.4f); elapsed = 0; moves = 0; complete = false; transition = 0;
        BuildRoom(); hero.position = position; hero.rotation = Quaternion.Euler(0,180,0); cat.ResetPose();
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
        bool goal = current == layout.Goal;
        Shape("Center medallion",PrimitiveType.Cylinder,new Vector3(0,.065f,0),new Vector3(2.1f,.04f,2.1f),goal?Gold*.6f:edge,roomRoot);
        if(goal) {
            beacon = Shape("Exit crystal",PrimitiveType.Cube,new Vector3(0,1.4f,0),Vector3.one*.65f,Gold,roomRoot,true);
            beacon.rotation = Quaternion.Euler(45,0,45);
        }
    }
    void Update()
    {
        Tick(DungeonInput.Read(), Mathf.Min(Time.deltaTime,.05f));
    }
    void Tick(DungeonInputFrame input, float deltaTime)
    {
        if(input.Quit || (choosing && input.Back)) { Application.Quit(); return; }
        if (choosing) {
            selectedDifficulty = Mathf.Clamp(selectedDifficulty + menuNavigation.Step(input.Move.x, Time.unscaledTime), 0, 2);
            if (input.DifficultyKey > 0) BeginGame(input.DifficultyKey - 1);
            else if (input.Confirm) BeginGame(selectedDifficulty);
            return;
        }
        if(input.Menu || input.Back || (complete && input.Confirm)) { ShowDifficulty(); return; }
        if(input.Restart) { Restart(); return; }
        if(beacon) { beacon.Rotate(0,45*Time.deltaTime,0,Space.World); beacon.position=new Vector3(0,1.4f+Mathf.Sin(Time.time*2)*.15f,0); }
        transition = Mathf.Max(0,transition-Time.deltaTime);
        if(complete) { cat.Animate(0,deltaTime); return; }
        elapsed += Time.deltaTime;
        var direction = new Vector3(input.Move.x,0,input.Move.y);
        var beforeMove = position;
        if(direction.sqrMagnitude>0) {
            Move(direction*4.4f*deltaTime);
            hero.rotation=Quaternion.Slerp(hero.rotation,Quaternion.LookRotation(direction),Time.deltaTime*14);
        }
        hero.position=position;
        cat.Animate((position-beforeMove).sqrMagnitude>.000001f ? direction.magnitude : 0,deltaTime);
        if(current==layout.Goal && position.magnitude<1.05f) complete=true;
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
        if(choosing || complete || !CanTravel(current,dir)) return;
        current+=dir; moves++; exploration.Visit(current);
        position=new Vector3(-dir.x*4.15f,0,-dir.y*4.15f); transition=.28f; BuildRoom();
    }
    void Panel(Rect rect, Color c) { GUI.color=c; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=Color.white; }
    void Label(Rect rect,string text,int size,Color color,TextAnchor align=TextAnchor.UpperLeft)
    { GUI.Label(rect,text,new GUIStyle(GUI.skin.label){fontSize=size,normal={textColor=color},alignment=align}); }
    void OnGUI()
    {
        float sx=Screen.width/1280f, sy=Screen.height/800f;
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(sx,sy,1));
        GUI.skin.font = uiFont;
        var muted=new Color(.57f,.68f,.73f);
        Panel(new Rect(0,0,1280,95),new Color(.025f,.045f,.065f,.95f));
        Label(new Rect(35,20,600,20),"A  Q U I E T  E X P L O R A T I O N",12,Teal);
        Label(new Rect(33,40,650,45),"THE QUIET VAULT",30,Color.white);
        Label(new Rect(860,31,380,32),"FIND THE GOLDEN EXIT",18,Gold,TextAnchor.MiddleRight);
        Label(new Rect(860,62,380,22),"FLOOR 01  /  NO ENEMIES · NO TRAPS",11,muted,TextAnchor.MiddleRight);
        if (choosing) { DrawDifficulty(); return; }
        DrawMap(muted);
        Label(new Rect(35,125,340,24),"探索済み  " + visited.Count + " 部屋",17,Color.white);
        Label(new Rect(35,155,340,25),DifficultyName(difficulty) + "  ·  " + moves + " passages",13,muted);
        Panel(new Rect(0,728,1280,72),new Color(.025f,.045f,.065f,.95f));
        Label(new Rect(35,738,980,25),"WASD / 矢印 : 移動     R : 同じ地図でやり直す     N : 難易度選択 / 新しい地図     Esc : 終了",14,muted);
        Label(new Rect(35,766,980,25),"パッド : 左スティック / 方向キーで移動   Y : やり直す   Start / B : 難易度選択",13,muted);
        Label(new Rect(1000,748,245,30),TimeSpan.FromSeconds(elapsed).ToString(@"mm\:ss"),18,Teal,TextAnchor.MiddleRight);
        if(transition>0) Panel(new Rect(0,95,1000,630),new Color(.02f,.04f,.06f,transition));
        if(complete) {
            Panel(new Rect(0,95,1280,633),new Color(.015f,.025f,.04f,.80f));
            Panel(new Rect(390,254,500,278),new Color(.055f,.09f,.12f));
            Label(new Rect(390,280,500,24),"J O U R N E Y   C O M P L E T E",13,Teal,TextAnchor.MiddleCenter);
            Label(new Rect(390,321,500,60),"EXIT DISCOVERED",32,Gold,TextAnchor.MiddleCenter);
            Label(new Rect(390,391,500,35),visited.Count+" rooms explored  /  "+moves+" passages",16,Color.white,TextAnchor.MiddleCenter);
            if(GUI.Button(new Rect(525,452,230,45),"新しい探索   [ N / A ]")) ShowDifficulty();
        }
    }
    static string DifficultyName(int level) { return new[] { "初級", "中級", "上級" }[level]; }
    void BeginGame(int level)
    {
        difficulty = level; selectedDifficulty = level; menuNavigation.Reset(); layout = DungeonLayout.Generate(level, seeds.Next());
        choosing = false; hero.gameObject.SetActive(true); Restart();
    }
    void ShowDifficulty()
    {
        choosing = true; menuNavigation.Reset(); hero.gameObject.SetActive(false);
        if(roomRoot) roomRoot.gameObject.SetActive(false);
    }
    void DrawDifficulty()
    {
        Label(new Rect(240,180,800,55),"未知のダンジョンへ",32,Color.white,TextAnchor.MiddleCenter);
        Label(new Rect(240,244,800,55),"訪れた部屋と、そこで見つけた通路を地図に記録します。\nゴールの場所は、到達するまで分かりません。",17,Teal,TextAnchor.MiddleCenter);
        var style = new GUIStyle(GUI.skin.button) { fontSize = 23 };
        for(int i=0; i<3; i++) {
            float x=225+i*285;
            if(i==selectedDifficulty) Panel(new Rect(x-3,347,266,216),Teal);
            Panel(new Rect(x,350,260,210),new Color(.055f,.09f,.12f));
            Label(new Rect(x,374,260,28),DungeonLayout.RoomCounts[i] + " 部屋",17,Teal,TextAnchor.MiddleCenter);
            Label(new Rect(x+10,412,240,30),new[]{"短い道のりを気軽に探索","分岐をたどって奥へ","広い迷宮をじっくり探索"}[i],14,Color.white,TextAnchor.MiddleCenter);
            if(GUI.Button(new Rect(x+20,465,220,62),DifficultyName(i)+"   [ "+(i+1)+" ]",style)) BeginGame(i);
        }
        Label(new Rect(200,565,880,32),"← → / 左スティック / 方向キー : 選択    A / Enter : 決定    B / Esc : 終了",16,Teal,TextAnchor.MiddleCenter);
        Label(new Rect(240,605,800,40),"選ぶたびに新しい地図を生成  /  敵・罠なし",15,new Color(.57f,.68f,.73f),TextAnchor.MiddleCenter);
    }
    void DrawMap(Color muted)
    {
        Panel(new Rect(1010,122,235,306),new Color(.025f,.045f,.065f,.93f));
        Label(new Rect(1028,137,210,25),"EXPLORATION MAP",13,Teal);
        // Fit only discovered cells; hidden layout and goal never influence the bounds.
        int minX=0,maxX=0,minY=0,maxY=0;
        foreach(var r in visited) { minX=Math.Min(minX,r.x); maxX=Math.Max(maxX,r.x); minY=Math.Min(minY,r.y); maxY=Math.Max(maxY,r.y); }
        float cell=Mathf.Min(36, 185f/(maxX-minX+2), 200f/(maxY-minY+2));
        float size=cell*.57f;
        Vector2 center=new Vector2(1127,275);
        foreach(var r in visited) {
            Vector2 p=center+new Vector2(r.x-(minX+maxX)*.5f,(minY+maxY)*.5f-r.y)*cell;
            foreach(var d in exploration.KnownExits(layout,r)) {
                // A short stub signals a visible exit, without revealing the unseen room.
                float length=cell*(visited.Contains(r+d)?1f:.48f);
                Vector2 end=p+new Vector2(d.x,-d.y)*length;
                Panel(new Rect(Mathf.Min(p.x,end.x)-1,Mathf.Min(p.y,end.y)-1,Mathf.Abs(p.x-end.x)+2,Mathf.Abs(p.y-end.y)+2),muted);
            }
        }
        foreach(var r in visited) {
            Vector2 p=center+new Vector2(r.x-(minX+maxX)*.5f,(minY+maxY)*.5f-r.y)*cell;
            var rect=new Rect(p.x-size/2,p.y-size/2,size,size);
            Panel(rect,r==current?Teal:new Color(.30f,.46f,.48f));
            string symbol=r==Vector2Int.zero?"S":r==layout.Goal && exploration.GoalVisible(layout)?"G":"";
            if(symbol.Length>0) Label(rect,symbol,Mathf.Max(8,(int)(size*.72f)),r==current?Color.black:Gold,TextAnchor.MiddleCenter);
            else if(r==current) Panel(new Rect(p.x-2,p.y-2,4,4),Color.white);
            if(r==current) Panel(new Rect(rect.x,rect.yMax+2,size,2),Teal);
        }
        Label(new Rect(1028,389,210,24),exploration.GoalVisible(layout)?"S 開始地点   G ゴール":"S 開始地点   ─ 発見した通路",11,muted);
    }
    void TestControllerFlow()
    {
        ShowDifficulty(); selectedDifficulty=0;
        Tick(new DungeonInputFrame { Move=Vector2.right },0);
        if(selectedDifficulty!=1) throw new Exception("Controller difficulty selection failed");
        Tick(new DungeonInputFrame { Confirm=true },0);
        if(choosing || difficulty!=1) throw new Exception("Controller confirm failed");
        var before=position;
        Tick(new DungeonInputFrame { Move=Vector2.up },.05f);
        if(position.z<=before.z) throw new Exception("Controller movement failed");
        Tick(new DungeonInputFrame { Restart=true },0);
        if(position!=before || moves!=0) throw new Exception("Controller restart failed");
        Tick(new DungeonInputFrame { Menu=true },0);
        if(!choosing) throw new Exception("Controller menu failed");
        Tick(new DungeonInputFrame { DifficultyKey=3 },0);
        if(choosing || difficulty!=2) throw new Exception("Keyboard difficulty shortcut failed");
        complete=true;
        Tick(new DungeonInputFrame { Confirm=true },0);
        if(!choosing) throw new Exception("Controller clear-screen confirm failed");
    }
    void SelfTest()
    {
        try {
            DungeonTests.Run();
            DungeonInputTests.Run();
            TestControllerFlow();
            for(int level=0;level<3;level++) {
                BeginGame(level);
                if(choosing || visited.Count!=1 || exploration.GoalVisible(layout)) throw new Exception("Initial state leaked map");
                var start=Vector2Int.zero;
                foreach(var d in Directions) if(CanTravel(start,d)) {
                    position=Vector3.zero; Move(new Vector3(d.x*10,0,d.y*10));
                    if(current!=start+d || visited.Count!=2) throw new Exception("Door crossing failed");
                    Move(new Vector3(-d.x*10,0,-d.y*10));
                    if(current!=start || visited.Count!=2) throw new Exception("Return crossing failed");
                    break;
                }
                foreach(var room in layout.Rooms) {
                    bool tested=false;
                    foreach(var d in Directions) if(!CanTravel(room,d)) {
                        current=room; position=Vector3.zero; Move(new Vector3(d.x*10,0,d.y*10));
                        if(current!=room || Mathf.Max(Mathf.Abs(position.x),Mathf.Abs(position.z))>4.66f) throw new Exception("Wall bypass");
                        tested=true; break;
                    }
                    if(tested) break;
                }
                Restart();
                var distance=layout.Distances(); var route=new List<Vector2Int>(); var cursor=layout.Goal;
                while(cursor!=start) foreach(var d in Directions) if(layout.CanTravel(cursor,d) && distance[cursor+d]==distance[cursor]-1) {
                    route.Add(-d); cursor+=d; break;
                }
                route.Reverse();
                for(int i=0;i<route.Count;i++) {
                    if(exploration.GoalVisible(layout)) throw new Exception("Goal revealed before arrival");
                    Travel(route[i]);
                }
                if(current!=layout.Goal || !exploration.GoalVisible(layout)) throw new Exception("Goal discovery failed");
                position=Vector3.zero; Tick(default,0); if(!complete) throw new Exception("Goal did not complete");
                var original=layout; Restart();
                if(layout!=original || complete || moves!=0 || visited.Count!=1 || exploration.GoalVisible(layout)) throw new Exception("Restart failed");
                ShowDifficulty(); var before=position; Tick(default,0);
                if(!choosing || position!=before || hero.gameObject.activeSelf) throw new Exception("Menu did not pause");
            }
            Debug.Log("SELFTEST PASS: virtual gamepad controls, dead zone, disconnect/reconnect, keyboard mixing, menu repeat, controller game flow; 600 generated layouts; fog of exploration; hidden goal; all difficulties; walls; door crossing; return; goal; restart; menu");
            Application.Quit(0);
        } catch(Exception e) { Debug.LogException(e); Application.Quit(1); }
    }
}
