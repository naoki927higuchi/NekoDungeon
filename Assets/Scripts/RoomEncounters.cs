using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct RatSpawn
{
    public readonly Vector2 Position, Forward;
    public readonly bool Aggressive;
    public RatSpawn(Vector2 position,bool aggressive,Vector2 forward)
    { Position=position;Aggressive=aggressive;Forward=forward; }
    public RatBrain Create() { return new RatBrain(Position,Aggressive){Forward=Forward}; }
}

public static class RoomEncounters
{
    // Count weights: 0, 1, 2, ... mice. Type is independently rolled per mouse.
    static readonly int[][] CountWeights = { new[]{40,40,20}, new[]{25,25,30,20}, new[]{15,15,25,25,20} };
    public static readonly float[] HunterChance = { .20f,.45f,.70f };
    public static readonly int[] MaxCount = { 2,3,4 };
    public static Dictionary<Vector2Int,List<RatSpawn>> Generate(DungeonLayout layout,int difficulty,int seed)
    {
        if(difficulty<0 || difficulty>2) throw new ArgumentOutOfRangeException(nameof(difficulty));
        var random=new System.Random(unchecked(seed ^ 0x514A2D));
        var result=new Dictionary<Vector2Int,List<RatSpawn>>();
        // In addition to a safe start, guarantee an empty room elsewhere in every dungeon.
        int quietRoom=random.Next(1,layout.Rooms.Count);
        for(int index=0;index<layout.Rooms.Count;index++) {
            var spawns=new List<RatSpawn>();result.Add(layout.Rooms[index],spawns);
            if(index==0 || index==quietRoom) continue;
            int roll=random.Next(100),count=0;
            for(;count<CountWeights[difficulty].Length-1;count++) {
                if(roll<CountWeights[difficulty][count]) break;
                roll-=CountWeights[difficulty][count];
            }
            // Shuffled, jittered slots avoid overlap and keep all four entrances clear.
            var slots=new List<Vector2>();
            for(int x=-1;x<=1;x++) for(int y=-1;y<=1;y++) if(x!=0 || y!=0) slots.Add(new Vector2(x*2,y*2));
            for(int i=0;i<count;i++) {
                int slot=random.Next(slots.Count);var position=slots[slot];slots.RemoveAt(slot);
                position+=new Vector2((float)random.NextDouble()*.4f-.2f,(float)random.NextDouble()*.4f-.2f);
                float angle=(float)random.NextDouble()*Mathf.PI*2;
                spawns.Add(new RatSpawn(position,random.NextDouble()<HunterChance[difficulty],new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))));
            }
        }
        return result;
    }
}
