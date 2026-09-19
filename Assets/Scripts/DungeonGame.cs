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
    readonly CatHealth health = new CatHealth();
    readonly CatPunch punch = new CatPunch();
    LineRenderer swipe;
    float swipeTime, hitMessageTime;
    int defeatedMice;
    string hitMessage;
    readonly Dictionary<Vector2Int, List<RatBrain>> roomRats = new Dictionary<Vector2Int, List<RatBrain>>();
    readonly List<RatActor> rats = new List<RatActor>();
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
        var swipeObject=new GameObject("Paw swipe"); swipeObject.transform.SetParent(hero,false);
        swipe=swipeObject.AddComponent<LineRenderer>(); swipe.useWorldSpace=false;
        swipe.sharedMaterial=Mat(Teal,true); swipe.startWidth=.085f;swipe.endWidth=.025f;
        swipe.positionCount=13;
        for(int i=0;i<13;i++) {
            float angle=Mathf.Lerp(-60,60,i/12f)*Mathf.Deg2Rad;
            swipe.SetPosition(i,new Vector3(Mathf.Sin(angle),.45f,Mathf.Cos(angle))*1.04f);
        }
        swipe.enabled=false;
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
        current = Vector2Int.zero; exploration.Reset(); roomRats.Clear(); defeatedMice=0; CancelPunch(); health.Reset();cat.SetVisible(true);
        position = new Vector3(0,0,-2.4f); elapsed = 0; moves = 0; complete = false; transition = 0;
        BuildRoom(); hero.position = position; hero.rotation = Quaternion.Euler(0,180,0); cat.ResetPose();
    }
    void BuildRoom()
    {
        if (roomRoot != null) { roomRoot.gameObject.SetActive(false); Destroy(roomRoot.gameObject); }
        roomRoot = new GameObject("Room " + current).transform; beacon = null; rats.Clear();
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
        SpawnRats();
    }
    void SpawnRats()
    {
        if(!roomRats.TryGetValue(current,out var states)) {
            states = new List<RatBrain> { new RatBrain(new Vector2(1.6f,.35f)) };
            if(current!=Vector2Int.zero) states.Add(new RatBrain(new Vector2(-2.0f,1.5f),true));
            roomRats[current]=states;
        }
        foreach(var state in states) {
            if(state.Defeated) continue;
            var obj=new GameObject(state.Aggressive?"Attacking mouse":"Fleeing mouse");obj.transform.SetParent(roomRoot,false);
            var rat=obj.AddComponent<RatActor>();
            rat.Initialize(state,Mat(state.Aggressive?new Color(.60f,.18f,.15f):new Color(.52f,.36f,.24f)),Mat(new Color(.85f,.51f,.49f)),Mat(new Color(.04f,.03f,.025f)),Mat(state.Aggressive?new Color(1f,.25f,.10f):Gold,true));
            rats.Add(rat);
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
        if(input.Restart || (health.Dead && input.Confirm)) { Restart(); return; }
        if(beacon) { beacon.Rotate(0,45*Time.deltaTime,0,Space.World); beacon.position=new Vector3(0,1.4f+Mathf.Sin(Time.time*2)*.15f,0); }
        transition = Mathf.Max(0,transition-Time.deltaTime);
        if(complete || health.Dead) { cat.SetVisible(true);cat.Animate(0,deltaTime); return; }
        health.Tick(deltaTime);
        punch.Tick(deltaTime);
        swipeTime=Mathf.Max(0,swipeTime-deltaTime); swipe.enabled=swipeTime>0;
        hitMessageTime=Mathf.Max(0,hitMessageTime-deltaTime);
        elapsed += Time.deltaTime;
        var direction = new Vector3(input.Move.x,0,input.Move.y);
        var beforeMove = position;
        var beforeRoom = current;
        if(direction.sqrMagnitude>0) {
            Move(direction*4.4f*deltaTime);
            hero.rotation=Quaternion.Slerp(hero.rotation,Quaternion.LookRotation(direction),Time.deltaTime*14);
        }
        hero.position=position;
        if(input.Attack && current==beforeRoom) {
            // Aim immediately in the requested direction when moving; otherwise use the
            // visible facing direction. Movement and attack work on the same frame.
            if(direction.sqrMagnitude>0) hero.rotation=Quaternion.LookRotation(direction);
            int hits=punch.Strike(new Vector2(position.x,position.z),new Vector2(hero.forward.x,hero.forward.z),roomRats[current]);
            if(hits>=0) {
                cat.Punch();swipeTime=.20f;swipe.enabled=true;
                defeatedMice+=hits;hitMessageTime=.65f;
                hitMessage=hits>0?"猫パンチ！  ネズミを倒した":"猫パンチ！";
            }
        }
        cat.Animate((position-beforeMove).sqrMagnitude>.000001f ? direction.magnitude : 0,deltaTime);
        foreach(var rat in rats) {
            rat.Brain.Tick(new Vector2(position.x,position.z),deltaTime);
            rat.Sync(deltaTime);
            if(rat.Brain.AttackLanded && health.Hurt()) {
                hitMessage="攻撃を受けた！  HP -1";hitMessageTime=.8f;
            }
        }
        cat.SetVisible(!health.Invulnerable || (int)(Time.unscaledTime*12)%2==0);
        if(health.Dead) { CancelPunch();cat.SetVisible(true);return; }
        if(current==layout.Goal && position.magnitude<1.05f) { complete=true; CancelPunch(); }
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
        if(choosing || complete || health.Dead || !CanTravel(current,dir)) return;
        CancelPunch(); current+=dir; moves++; exploration.Visit(current);
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
        Label(new Rect(860,62,380,22),"FLOOR 01  /  CAT & MICE",11,muted,TextAnchor.MiddleRight);
        if (choosing) { DrawDifficulty(); return; }
        DrawMap(muted);
        Label(new Rect(1028,455,210,28),"猫のHP  "+health.HP+" / "+CatHealth.MaxHP,18,health.HP<=2?new Color(1,.4f,.3f):Teal);
        for(int i=0;i<CatHealth.MaxHP;i++) Panel(new Rect(1028+i*40,491,32,14),i<health.HP?(health.HP<=2?new Color(1,.4f,.3f):Teal):new Color(.15f,.20f,.24f));
        Label(new Rect(1028,527,215,50),"茶色 : 逃げるネズミ\n赤色 : 攻撃するネズミ",12,muted);
        Label(new Rect(35,125,340,24),"探索済み  " + visited.Count + " 部屋",17,Color.white);
        Label(new Rect(35,155,340,25),DifficultyName(difficulty) + "  ·  " + moves + " passages",13,muted);
        Label(new Rect(35,185,540,25),"倒したネズミ  " + defeatedMice + " 匹",13,muted);
        if(hitMessageTime>0) Label(new Rect(35,215,540,25),hitMessage,17,Gold);
        Panel(new Rect(0,728,1280,72),new Color(.025f,.045f,.065f,.95f));
        Label(new Rect(35,738,980,25),"WASD / 矢印 : 移動   Space / J : 猫パンチ   R : やり直す   N : 難易度選択   Esc : 終了",14,muted);
        Label(new Rect(35,766,980,25),"パッド : 左スティック / 方向キーで移動   X : 猫パンチ   Y : やり直す   Start / B : 難易度選択",13,muted);
        Label(new Rect(1000,748,245,30),TimeSpan.FromSeconds(elapsed).ToString(@"mm\:ss"),18,Teal,TextAnchor.MiddleRight);
        if(transition>0) Panel(new Rect(0,95,1000,630),new Color(.02f,.04f,.06f,transition));
        if(health.Dead) {
            Panel(new Rect(0,95,1280,633),new Color(.025f,.01f,.02f,.85f));
            Panel(new Rect(365,240,550,310),new Color(.10f,.055f,.075f));
            Label(new Rect(365,267,550,62),"GAME OVER",36,new Color(1,.4f,.3f),TextAnchor.MiddleCenter);
            Label(new Rect(365,344,550,35),"HPがなくなりました",19,Color.white,TextAnchor.MiddleCenter);
            if(GUI.Button(new Rect(425,414,430,45),"同じ地図で再挑戦  [ R / Y / A / Enter ]")) Restart();
            if(GUI.Button(new Rect(425,478,430,42),"難易度選択へ  [ N / Start / B ]")) ShowDifficulty();
        }
        else if(complete) {
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
        choosing = true; CancelPunch(); menuNavigation.Reset(); hero.gameObject.SetActive(false);
        if(roomRoot) roomRoot.gameObject.SetActive(false);
    }
    void DrawDifficulty()
    {
        Label(new Rect(240,180,800,55),"未知のダンジョンへ",32,Color.white,TextAnchor.MiddleCenter);
        Label(new Rect(240,244,800,55),"訪れた部屋と、そこで見つけた通路を地図に記録します。\n赤いネズミの攻撃に注意。HPが0になるとゲームオーバー。",17,Teal,TextAnchor.MiddleCenter);
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
        Label(new Rect(240,605,800,40),"選ぶたびに新しい地図を生成  /  Space / J / パッドX で猫パンチ",15,new Color(.57f,.68f,.73f),TextAnchor.MiddleCenter);
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
    void CancelPunch()
    {
        punch.Reset();swipeTime=0;hitMessageTime=0;
        if(swipe) swipe.enabled=false;
        if(cat) cat.ResetPose();
    }
    void TestPunchFlow()
    {
        BeginGame(0);
        var rat=rats[0].Brain;
        position=Vector3.zero;hero.rotation=Quaternion.identity;rat.Position=Vector2.up;
        Tick(new DungeonInputFrame{Attack=true},0);
        if(!rat.Defeated || defeatedMice!=1 || !swipe.enabled) throw new Exception("Game punch failed");
        Tick(new DungeonInputFrame{Attack=true},0);
        if(defeatedMice!=1) throw new Exception("Duplicate defeat counted");
        foreach(var d in Directions) if(CanTravel(current,d)) { Travel(d);Travel(-d);break; }
        if(rats.Count!=0 || defeatedMice!=1) throw new Exception("Defeated mouse respawned on revisit");
        Restart();if(rats.Count!=1 || defeatedMice!=0 || rats[0].Brain.Defeated) throw new Exception("Restart did not restore mice");
        var alive=rats[0].Brain;position=Vector3.zero;hero.rotation=Quaternion.identity;alive.Position=Vector2.up;
        ShowDifficulty();Tick(new DungeonInputFrame{Attack=true},0);
        if(alive.Defeated || swipe.enabled) throw new Exception("Menu attack allowed");
        BeginGame(0);alive=rats[0].Brain;alive.Position=Vector2.up;position=Vector3.zero;hero.rotation=Quaternion.identity;complete=true;
        Tick(new DungeonInputFrame{Attack=true},0);if(alive.Defeated) throw new Exception("Attack after clear");
        BeginGame(0);alive=rats[0].Brain;alive.Position=Vector2.right;position=Vector3.zero;
        Tick(new DungeonInputFrame{Move=Vector2.right,Attack=true},.01f);
        if(!alive.Defeated) throw new Exception("Moving punch aimed incorrectly");
    }
    void TestRatRooms()
    {
        BeginGame(0);
        if(rats.Count!=1) throw new Exception("Missing start mouse");
        var initial= rats[0].Brain;
        initial.Position=new Vector2(2,2);
        foreach(var d in Directions) if(CanTravel(current,d)) {
            Travel(d);if(rats.Count!=2) throw new Exception("Missing room mice");
            Travel(-d);break;
        }
        if(rats.Count!=1 || rats[0].Brain!=initial || initial.Position!=new Vector2(2,2)) throw new Exception("Mouse position was not preserved");
        ShowDifficulty();Tick(default,.05f);
        if(initial.Position!=new Vector2(2,2)) throw new Exception("Mouse moved in menu");
        BeginGame(1);
        if(rats[0].Brain==initial || roomRats.Count!=1) throw new Exception("New dungeon retained old mice");
        rats[0].Brain.Position=Vector2.zero;Restart();
        if(rats[0].Brain.Position!=new Vector2(1.6f,.35f)) throw new Exception("Mouse restart failed");
        complete=true;var before=rats[0].Brain.Position;Tick(default,.05f);
        if(rats[0].Brain.Position!=before) throw new Exception("Mouse moved after clear");
    }
    void TestHealthFlow()
    {
        BeginGame(0);
        Vector2Int exit=Vector2Int.zero;
        foreach(var d in Directions) if(CanTravel(current,d)) { exit=d;break; }
        Travel(exit);
        var hunter=rats.Find(r=>r.Brain.Aggressive);
        if(hunter==null || !rats.Exists(r=>!r.Brain.Aggressive)) throw new Exception("Missing mixed mouse types");
        position=Vector3.zero;hunter.Brain.Position=new Vector2(0,.7f);
        for(int i=0;i<12;i++) Tick(default,.05f);
        if(health.HP!=4) throw new Exception("Hunter did not damage cat");
        Travel(-exit);if(health.HP!=4) throw new Exception("Room change restored HP");
        ShowDifficulty();int savedHP=health.HP;Tick(default,.05f);
        if(health.HP!=savedHP) throw new Exception("Menu damaged cat");
        BeginGame(0);foreach(var d in Directions) if(CanTravel(current,d)) {exit=d;break;}Travel(exit);hunter=rats.Find(r=>r.Brain.Aggressive);
        position=Vector3.zero;hunter.Brain.Position=new Vector2(0,.7f);
        for(int i=0;i<300 && !health.Dead;i++) Tick(default,.05f);
        if(!health.Dead || complete) throw new Exception("No game over at zero HP");
        var frozen=position;var ratPosition=hunter.Brain.Position;
        Tick(new DungeonInputFrame{Move=Vector2.up,Attack=true},.05f);
        Travel(-exit);
        if(position!=frozen || hunter.Brain.Position!=ratPosition || hunter.Brain.Defeated) throw new Exception("Game continued after death");
        var sameLayout=layout;
        Tick(new DungeonInputFrame{Confirm=true},0);
        if(health.HP!=CatHealth.MaxHP || layout!=sameLayout || current!=Vector2Int.zero) throw new Exception("Game-over retry failed");
        Travel(exit);hunter=rats.Find(r=>r.Brain.Aggressive);position=Vector3.zero;hero.rotation=Quaternion.identity;hunter.Brain.Position=Vector2.up*.7f;
        Tick(new DungeonInputFrame{Attack=true},.05f);
        for(int i=0;i<30;i++) Tick(default,.05f);
        if(!hunter.Brain.Defeated || health.HP!=CatHealth.MaxHP) throw new Exception("Defeated hunter dealt damage");
        // A lethal hit at the exit must lose, not show the clear screen.
        BeginGame(0);current=layout.Goal;BuildRoom();position=Vector3.zero;hunter=rats.Find(r=>r.Brain.Aggressive);hunter.Brain.Position=Vector2.up*.7f;
        for(int i=0;i<4;i++) {health.Tick(2);health.Hurt();}health.Tick(2);
        hunter.Brain.Tick(Vector2.zero,.05f);for(int i=0;i<8;i++) hunter.Brain.Tick(Vector2.zero,.05f);
        Tick(default,.05f);
        if(!health.Dead || complete) throw new Exception("Lethal hit did not take priority over clear");
        Restart();complete=true;int before=health.HP;Tick(default,.05f);
        if(health.HP!=before) throw new Exception("Damage after clear");
    }
    void SelfTest()
    {
        try {
            DungeonTests.Run();
            RatTests.Run();
            HealthTests.Run();
            TestHealthFlow();
            CatPunchTests.Run();
            TestPunchFlow();
            TestRatRooms();
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
            Debug.Log("SELFTEST PASS: hunters, attack windup/dodge/cooldown, HP/invulnerability, death/retry, room HP persistence; cat punch range, facing, cooldown, defeat persistence, keyboard/gamepad attack; mouse sight, escape, wall avoidance, calming, overlap, room persistence, restart, pause; virtual gamepad controls, dead zone, disconnect/reconnect, keyboard mixing, menu repeat, controller game flow; 600 generated layouts; fog of exploration; hidden goal; all difficulties; walls; door crossing; return; goal; restart; menu");
            Application.Quit(0);
        } catch(Exception e) { Debug.LogException(e); Application.Quit(1); }
    }
}
