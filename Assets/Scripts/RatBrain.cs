using UnityEngine;

// Room-local simulation, independent of visuals. Retained when revisiting a room.
public sealed class RatBrain
{
    public const float RoomLimit = 3.8f;
    public Vector2 Position;
    public Vector2 Forward = Vector2.down;
    public bool Fleeing { get; private set; }
    public bool Defeated { get; private set; }
    public float Speed { get; private set; }
    float memory;

    public RatBrain(Vector2 position) { Position = position; }
    public bool Defeat()
    {
        if(Defeated) return false;
        Defeated=true; Fleeing=false; Speed=0; memory=0; return true;
    }
    public bool SeesCat(Vector2 cat)
    {
        var delta=cat-Position;
        if(delta.sqrMagnitude>4.5f*4.5f) return false;
        // Nearby movement is noticed even behind the mouse; farther away requires sight.
        return delta.sqrMagnitude<1.4f*1.4f || Vector2.Dot(Forward,delta.normalized)>=-.5f;
    }
    public void Tick(Vector2 cat,float dt)
    {
        if(dt<=0 || Defeated) return;
        dt=Mathf.Min(dt,.05f);
        if(SeesCat(cat)) memory=1.4f;
        else memory=Mathf.Max(0,memory-dt);
        Fleeing=memory>0; Speed=0;
        if(!Fleeing) return;
        // Evaluate safe escape headings, including tangents along walls. Looking ahead
        // lets the mouse turn before hitting a wall rather than sticking in a corner.
        Vector2 away=Position-cat;
        if(away.sqrMagnitude<.0001f) away=Forward;
        away.Normalize();
        Vector2 best=away; float bestScore=float.NegativeInfinity;
        for(int i=0;i<24;i++) {
            float angle=i*Mathf.PI*2/24;
            var dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            var target=Position+dir*1.1f;
            if(Mathf.Abs(target.x)>RoomLimit || Mathf.Abs(target.y)>RoomLimit) continue;
            float score=(target-cat).magnitude + Vector2.Dot(dir,away)*.45f + Vector2.Dot(dir,Forward)*.22f;
            if(score>bestScore) { bestScore=score; best=dir; }
        }
        var next=Position+best*(3.25f*dt);
        next=new Vector2(Mathf.Clamp(next.x,-RoomLimit,RoomLimit),Mathf.Clamp(next.y,-RoomLimit,RoomLimit));
        var movement=next-Position;
        Speed=movement.magnitude/dt;
        if(movement.sqrMagnitude>.000001f) Forward=movement.normalized;
        Position=next;
    }
}
