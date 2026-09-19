using System;
using System.Collections.Generic;
using UnityEngine;

public static class TrapTests
{
    static void Check(bool ok,string message) {if(!ok)throw new Exception(message);}
    public static void Run()
    {
        var spike=new TrapState(new TrapSpawn(TrapKind.Spikes,Vector2.zero,Vector2.right,0));
        Check(!spike.Hits(Vector2.zero),"Retracted spikes hurt");
        spike.Tick(2.2f);Check(spike.Warning && !spike.Active,"Missing spike warning");
        spike.Tick(.6f);Check(spike.Active && spike.Hits(Vector2.zero),"Extended spikes harmless");
        spike.Tick(1.1f);Check(!spike.Active,"Spikes never retract");
        var kinds=new HashSet<TrapKind>();int previousTotal=0;
        for(int level=0;level<3;level++) {
            int total=0;
            for(int seed=0;seed<100;seed++) {
                var layout=DungeonLayout.Generate(level,seed);var mice=RoomEncounters.Generate(layout,level,seed);
                var plan=RoomTraps.Generate(layout,mice,level,seed);var again=RoomTraps.Generate(layout,mice,level,seed);
                Check(plan[Vector2Int.zero].Count==0,"Unsafe starting room");
                int empty=0;
                foreach(var room in layout.Rooms) {
                    if(room!=Vector2Int.zero && plan[room].Count==0)empty++;
                    Check(plan[room].Count<=level+1 && plan[room].Count==again[room].Count,"Trap count or seed mismatch");
                    for(int i=0;i<plan[room].Count;i++) {
                        var spawn=plan[room][i];var copy=again[room][i];kinds.Add(spawn.Kind);total++;
                        Check(spawn.Kind==copy.Kind && spawn.Center==copy.Center && spawn.Phase==copy.Phase && spawn.Axis==copy.Axis,"Trap seed not reproducible");
                        var state=new TrapState(spawn);
                        for(int frame=0;frame<100;frame++) {
                            state.Tick(.05f);
                            Check(Mathf.Abs(state.Position.x)<=3.31f && Mathf.Abs(state.Position.y)<=3.31f,"Trap crosses wall");
                            // The cross-shaped route between all doors and goal stays clear.
                            for(int step=-10;step<=10;step++) {
                                Check(!state.Hits(new Vector2(step*.5f,0)) && !state.Hits(new Vector2(0,step*.5f)),"Trap blocks safe route");
                            }
                        }
                        foreach(var mouse in mice[room]) {
                            var d=mouse.Position-spawn.Center;
                            if(spawn.Kind==TrapKind.Saw)d-=spawn.Axis*Mathf.Clamp(Vector2.Dot(d,spawn.Axis),-.65f,.65f);
                            Check(d.magnitude>=1.3f,"Trap on mouse spawn");
                        }
                    }
                }
                Check(empty>0,"No trap-free room");
            }
            Check(total>previousTotal,"Difficulty did not increase traps");previousTotal=total;
        }
        Check(kinds.Count==3,"Missing trap type");
        var saw=new TrapState(new TrapSpawn(TrapKind.Saw,Vector2.zero,Vector2.right,0));saw.Tick(.5f);
        Check(saw.Position.x>.1f && saw.Hits(saw.Position) && !saw.Hits(Vector2.one*3),"Moving saw collision incorrect");
        var block=new TrapState(new TrapSpawn(TrapKind.SpikeBlock,Vector2.zero,Vector2.up,0));
        Check(block.Hits(Vector2.zero) && !block.Hits(Vector2.one*2),"Spike block collision incorrect");
    }
}
