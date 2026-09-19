using System;
using System.Collections.Generic;
using UnityEngine;

public static class EncounterTests
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Run()
    {
        float lastAverage=-1,lastHunter=-1,lastEmpty=1;
        for(int level=0;level<3;level++) {
            int total=0,hunters=0,empty=0,rooms=0;
            var counts=new HashSet<int>();var mixtures=new HashSet<int>();
            for(int seed=0;seed<200;seed++) {
                var layout=DungeonLayout.Generate(level,seed);
                var plan=RoomEncounters.Generate(layout,level,seed);
                var again=RoomEncounters.Generate(layout,level,seed);
                Check(plan.Count==layout.Rooms.Count && plan[Vector2Int.zero].Count==0,"Unsafe start or missing room");
                int quiet=0;
                foreach(var room in layout.Rooms) {
                    var spawns=plan[room];Check(spawns.Count==again[room].Count,"Unstable count seed");
                    Check(spawns.Count<=RoomEncounters.MaxCount[level],"Spawn limit exceeded");
                    if(room==Vector2Int.zero) continue;
                    rooms++;total+=spawns.Count;counts.Add(spawns.Count);
                    if(spawns.Count==0) {empty++;quiet++;}
                    int types=0;
                    for(int i=0;i<spawns.Count;i++) {
                        var spawn=spawns[i];var same=again[room][i];
                        Check(spawn.Position==same.Position && spawn.Aggressive==same.Aggressive && spawn.Forward==same.Forward,"Unstable spawn seed");
                        Check(Mathf.Abs(spawn.Position.x)<=2.2f && Mathf.Abs(spawn.Position.y)<=2.2f,"Spawn near wall");
                        Check(spawn.Position.magnitude>1.5f,"Spawn blocks center");
                        foreach(var dir in DungeonLayout.Directions) Check(Vector2.Distance(spawn.Position,new Vector2(dir.x,dir.y)*4.15f)>1.8f,"Unsafe entrance spawn");
                        for(int j=0;j<i;j++) Check(Vector2.Distance(spawn.Position,spawns[j].Position)>1.5f,"Overlapping mice");
                        var brain=spawn.Create();brain.Defeat();Check(!spawn.Create().Defeated,"Spawn template mutated by death");
                        if(spawn.Aggressive) {hunters++;types|=2;}else types|=1;
                    }
                    mixtures.Add(types);
                }
                Check(quiet>=1,"No empty room beyond start");
            }
            float average=(float)total/rooms,hunterRate=(float)hunters/total,emptyRate=(float)empty/rooms;
            Check(counts.Count==RoomEncounters.MaxCount[level]+1,"Missing count variation");
            Check(mixtures.Count==4,"Missing empty/timid/hunter/mixed composition");
            Check(Mathf.Abs(hunterRate-RoomEncounters.HunterChance[level])<.04f,"Wrong hunter distribution");
            Check(average>lastAverage && hunterRate>lastHunter && emptyRate<lastEmpty,"Difficulty trends incorrect");
            Debug.Log($"ENCOUNTER TEST level={level}: mean={average:F2}, hunters={hunterRate:P1}, empty={emptyRate:P1}");
            lastAverage=average;lastHunter=hunterRate;lastEmpty=emptyRate;
        }
    }
}
