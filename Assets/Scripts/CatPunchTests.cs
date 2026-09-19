using System;
using UnityEngine;

public static class CatPunchTests
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Run()
    {
        RatBrain front=new RatBrain(Vector2.up), behind=new RatBrain(Vector2.down);
        RatBrain far=new RatBrain(Vector2.up*2), side=new RatBrain(Vector2.right);
        var punch=new CatPunch();
        Check(punch.Strike(Vector2.zero,Vector2.up,new[]{front,behind,far,side})==1,"Punch range/cone failed");
        Check(front.Defeated && !behind.Defeated && !far.Defeated && !side.Defeated,"Wrong mouse defeated");
        var before=front.Position;front.Tick(Vector2.zero,.05f);
        Check(front.Position==before && !front.Fleeing && front.Speed==0,"Defeated mouse moved");
        Check(!front.Defeat(),"Defeat counted twice");
        Check(punch.Strike(Vector2.zero,Vector2.down,new[]{behind})==-1,"Cooldown ignored");
        punch.Tick(CatPunch.Cooldown+.01f);
        Check(punch.Strike(Vector2.zero,Vector2.down,new[]{behind})==1,"Second punch failed");
        punch.Reset(); Check(punch.Strike(Vector2.zero,Vector2.up,new[]{front,behind})==0,"Dead mice hit again");
        punch.Reset();var overlapping=new RatBrain(Vector2.zero);
        Check(punch.Strike(Vector2.zero,Vector2.up,new[]{overlapping})==1,"Overlapping mouse missed");
        Check(CatPunch.CanHit(Vector2.zero,Vector2.right,Vector2.right*1.29f),"Facing right failed");
        Check(!CatPunch.CanHit(Vector2.zero,Vector2.right,Vector2.left),"Rear hit allowed");
    }
}
