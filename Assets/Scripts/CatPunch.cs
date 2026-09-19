using System.Collections.Generic;
using UnityEngine;

public sealed class CatPunch
{
    public const float Range=1.3f, Cooldown=.38f;
    float remaining;
    public void Tick(float dt) { remaining=Mathf.Max(0,remaining-Mathf.Max(0,dt)); }
    public void Reset() { remaining=0; }
    public static bool CanHit(Vector2 origin,Vector2 forward,Vector2 target)
    {
        var offset=target-origin;
        if(offset.sqrMagnitude>Range*Range) return false;
        return offset.sqrMagnitude<.0001f || Vector2.Dot(forward.normalized,offset.normalized)>=.5f;
    }
    // -1 means cooling down; zero is a legitimate missed swing.
    public int Strike(Vector2 origin,Vector2 forward,IEnumerable<RatBrain> targets)
    {
        if(remaining>0) return -1;
        remaining=Cooldown; int hits=0;
        foreach(var rat in targets) if(CanHit(origin,forward,rat.Position) && rat.Defeat()) hits++;
        return hits;
    }
}
