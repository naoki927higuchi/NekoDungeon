using System;
using UnityEngine;

public static class RatTests
{
    static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
    public static void Run()
    {
        var rat=new RatBrain(Vector2.zero);
        rat.Tick(new Vector2(0,-7),.05f);
        Check(!rat.Fleeing && rat.Position==Vector2.zero,"Distant cat triggered escape");
        rat.Tick(new Vector2(0,3),.05f);
        Check(!rat.Fleeing,"Cat behind mouse was visible at distance");
        var cat=new Vector2(0,-3);
        rat.Tick(cat,.05f);
        Check(rat.Fleeing && Vector2.Distance(rat.Position,cat)>3,"Mouse did not flee from visible cat");
        for(int i=0;i<40;i++) rat.Tick(new Vector2(0,-20),.05f);
        Check(!rat.Fleeing && rat.Speed==0,"Mouse never calmed down");
        rat=new RatBrain(Vector2.zero);rat.Tick(Vector2.up*.5f,.05f);
        Check(rat.Fleeing,"Mouse failed to notice nearby cat behind it");
        rat=new RatBrain(Vector2.zero);rat.Tick(Vector2.zero,.05f);
        Check(rat.Fleeing && rat.Position.sqrMagnitude>0,"Overlapping cat produced stuck mouse");
        foreach(var corner in new[]{new Vector2(3.8f,3.8f),new Vector2(-3.8f,3.8f),new Vector2(3.8f,-3.8f),new Vector2(-3.8f,-3.8f)}) {
            rat=new RatBrain(corner);var initial=rat.Position;
            for(int i=0;i<500;i++) {
                // Simulated pursuit keeps pressure on the mouse through every turn.
                cat=rat.Position-rat.Forward*.65f;
                var before=rat.Position;rat.Tick(cat,.05f);
                Check(Mathf.Abs(rat.Position.x)<=RatBrain.RoomLimit && Mathf.Abs(rat.Position.y)<=RatBrain.RoomLimit,"Mouse crossed wall");
                Check((rat.Position-before).magnitude<=3.25f*.05f+.0001f,"Mouse teleported");
                Check(!float.IsNaN(rat.Position.x),"Invalid escape direction");
            }
            Check(Vector2.Distance(initial,rat.Position)>.3f,"Mouse stuck in corner");
        }
        var frozen=rat.Position;rat.Tick(Vector2.zero,0);
        Check(rat.Position==frozen,"Paused mouse moved");
    }
}
