using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrapKind { Spikes, Saw, SpikeBlock }
public readonly struct TrapSpawn
{
    public readonly TrapKind Kind;
    public readonly Vector2 Center, Axis;
    public readonly float Phase;
    public TrapSpawn(TrapKind kind,Vector2 center,Vector2 axis,float phase)
    {Kind=kind;Center=center;Axis=axis;Phase=phase;}
}
public sealed class TrapState
{
    public readonly TrapSpawn Spawn;
    public float Time { get; private set; }
    public float Cycle => Mathf.Repeat(Time+Spawn.Phase,4.5f);
    public bool Warning => Spawn.Kind==TrapKind.Spikes && Cycle>=2 && Cycle<2.7f;
    public bool Active => Spawn.Kind!=TrapKind.Spikes || Cycle>=2.7f && Cycle<3.8f;
    public Vector2 Position => Spawn.Center+(Spawn.Kind==TrapKind.Saw?Spawn.Axis*(Mathf.Sin((Time+Spawn.Phase)*1.6f)*.65f):Vector2.zero);
    public TrapState(TrapSpawn spawn) {Spawn=spawn;}
    public void Tick(float dt) {Time+=Mathf.Max(0,dt);}
    public bool Hits(Vector2 cat)
    {
        if(!Active) return false;
        var d=cat-Position;
        if(Spawn.Kind==TrapKind.Saw) return d.sqrMagnitude<=.85f*.85f;
        float half=Spawn.Kind==TrapKind.Spikes?.84f:.77f;
        return Mathf.Abs(d.x)<=half && Mathf.Abs(d.y)<=half;
    }
}
public static class RoomTraps
{
    public static Dictionary<Vector2Int,List<TrapSpawn>> Generate(DungeonLayout layout,Dictionary<Vector2Int,List<RatSpawn>> mice,int level,int seed)
    {
        var random=new System.Random(unchecked(seed^0x64A37F));
        var result=new Dictionary<Vector2Int,List<TrapSpawn>>();
        int quiet=random.Next(1,layout.Rooms.Count);
        var chance=new[]{.45,.65,.80};
        for(int index=0;index<layout.Rooms.Count;index++) {
            var room=layout.Rooms[index];var traps=new List<TrapSpawn>();result[room]=traps;
            if(index==0 || index==quiet || random.NextDouble()>chance[level]) continue;
            var slots=new List<Vector2>{new Vector2(-2.65f,-2.65f),new Vector2(-2.65f,2.65f),new Vector2(2.65f,-2.65f),new Vector2(2.65f,2.65f)};
            int desired=1+random.Next(level+1);
            while(slots.Count>0 && traps.Count<desired) {
                int pick=random.Next(slots.Count);var center=slots[pick];slots.RemoveAt(pick);
                var kind=(TrapKind)random.Next(3);var axis=random.Next(2)==0?Vector2.right:Vector2.up;
                bool clear=true;
                foreach(var mouse in mice[room]) {
                    var delta=mouse.Position-center;
                    if(kind==TrapKind.Saw) delta-=axis*Mathf.Clamp(Vector2.Dot(delta,axis),-.65f,.65f);
                    if(delta.magnitude<1.3f) {clear=false;break;}
                }
                if(clear) traps.Add(new TrapSpawn(kind,center,axis,(float)random.NextDouble()*1.7f));
            }
        }
        return result;
    }
}
