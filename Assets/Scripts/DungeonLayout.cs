using System;
using System.Collections.Generic;
using UnityEngine;

// A connected cardinal tree: every door has a return door, with no isolated rooms.
public sealed class DungeonLayout
{
    public static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    public static readonly int[] RoomCounts = { 12, 24, 42 };
    public static readonly int[] MinimumDistances = { 5, 8, 12 };
    public readonly List<Vector2Int> Rooms = new List<Vector2Int>();
    readonly HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
    public Vector2Int Goal { get; private set; }
    public bool CanTravel(Vector2Int cell, Vector2Int dir)
    { return Array.IndexOf(Directions, dir) >= 0 && cells.Contains(cell) && cells.Contains(cell + dir); }

    public static DungeonLayout Generate(int difficulty, int seed)
    {
        if (difficulty < 0 || difficulty >= RoomCounts.Length) throw new ArgumentOutOfRangeException(nameof(difficulty));
        var random = new System.Random(seed);
        for (int attempt = 0; ; attempt++) {
            // A straight initial route on the last attempt guarantees bounded generation.
            var candidate = GenerateCandidate(difficulty, random, attempt == 63);
            if (candidate.Distances()[candidate.Goal] >= MinimumDistances[difficulty]) return candidate;
        }
    }
    static DungeonLayout GenerateCandidate(int difficulty, System.Random random, bool straightRoute)
    {
        var layout = new DungeonLayout();
        layout.Add(Vector2Int.zero);
        // Reserve a sufficiently long route before growing branches. One-neighbor additions
        // preserve that route's graph distance even after more rooms are attached.
        var tip = Vector2Int.zero;
        var straightDirection = Directions[random.Next(Directions.Length)];
        for (int i = 0; i < MinimumDistances[difficulty]; i++) {
            var candidates = layout.Candidates(new[] { tip });
            if (candidates.Count == 0) break;
            tip = straightRoute ? tip + straightDirection : candidates[random.Next(candidates.Count)]; layout.Add(tip);
        }
        while (layout.Rooms.Count < RoomCounts[difficulty]) {
            var candidates = layout.Candidates(layout.Rooms);
            layout.Add(candidates[random.Next(candidates.Count)]);
        }
        var distances = layout.Distances(); int farthest = -1;
        foreach (var room in layout.Rooms) if (distances[room] > farthest) {
            farthest = distances[room]; layout.Goal = room;
        }
        return layout;
    }
    void Add(Vector2Int room) { cells.Add(room); Rooms.Add(room); }
    List<Vector2Int> Candidates(IEnumerable<Vector2Int> origins)
    {
        var result = new List<Vector2Int>();
        foreach (var r in origins) foreach (var d in Directions) {
            var next = r + d; if (cells.Contains(next)) continue;
            int neighbors = 0; foreach (var side in Directions) if (cells.Contains(next + side)) neighbors++;
            if (neighbors == 1) result.Add(next);
        }
        return result;
    }
    public Dictionary<Vector2Int, int> Distances()
    {
        var result = new Dictionary<Vector2Int, int> { [Vector2Int.zero] = 0 };
        var queue = new Queue<Vector2Int>(); queue.Enqueue(Vector2Int.zero);
        while (queue.Count > 0) {
            var r = queue.Dequeue();
            foreach (var d in Directions) if (CanTravel(r, d) && !result.ContainsKey(r + d)) {
                result[r + d] = result[r] + 1; queue.Enqueue(r + d);
            }
        }
        return result;
    }
}

// This is the only source of room/goal visibility used by the map renderer.
public sealed class ExplorationMap
{
    public readonly HashSet<Vector2Int> Visited = new HashSet<Vector2Int>();
    public void Reset() { Visited.Clear(); Visit(Vector2Int.zero); }
    public void Visit(Vector2Int cell) { Visited.Add(cell); }
    public bool GoalVisible(DungeonLayout layout) { return Visited.Contains(layout.Goal); }
    public IEnumerable<Vector2Int> KnownExits(DungeonLayout layout, Vector2Int room)
    {
        if (!Visited.Contains(room)) yield break;
        foreach (var d in DungeonLayout.Directions) if (layout.CanTravel(room, d)) yield return d;
    }
}
