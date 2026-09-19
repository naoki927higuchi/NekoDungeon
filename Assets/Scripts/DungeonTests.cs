using System;
using System.Collections.Generic;
using UnityEngine;

public static class DungeonTests
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run()
    {
        for (int level = 0; level < 3; level++) {
            var signatures = new HashSet<string>();
            for (int seed = 0; seed < 200; seed++) {
                var layout = DungeonLayout.Generate(level, seed);
                var distances = layout.Distances();
                Require(layout.Rooms.Count == DungeonLayout.RoomCounts[level], "Wrong room count");
                Require(distances.Count == layout.Rooms.Count, "Disconnected layout");
                Require(distances[layout.Goal] >= DungeonLayout.MinimumDistances[level], "Goal too near");
                int edges = 0;
                foreach (var room in layout.Rooms) {
                    int degree = 0;
                    foreach (var dir in DungeonLayout.Directions) if (layout.CanTravel(room, dir)) {
                        degree++; edges++;
                        Require(layout.CanTravel(room + dir, -dir), "Missing return path");
                    }
                    Require(degree >= 1 && degree <= 4, "Invalid room degree");
                    Require(!layout.CanTravel(room, new Vector2Int(1, 1)), "Diagonal travel allowed");
                }
                Require(edges == (layout.Rooms.Count - 1) * 2, "Unexpected shortcut in tree");
                var map = new ExplorationMap(); map.Reset();
                Require(map.Visited.Count == 1 && map.Visited.Contains(Vector2Int.zero), "Initial room visibility");
                Require(!map.GoalVisible(layout), "Initial goal leak");
                foreach (var room in layout.Rooms) {
                    int exits = 0;
                    foreach (var dir in map.KnownExits(layout, room)) {
                        exits++; Require(layout.CanTravel(room, dir), "False known passage");
                    }
                    Require(room == Vector2Int.zero || exits == 0, "Unvisited passages leaked");
                }
                // Discover every non-goal room; even a visited neighbor must not expose G.
                foreach (var room in layout.Rooms) if (room != layout.Goal) map.Visit(room);
                Require(!map.GoalVisible(layout), "Adjacent goal leaked");
                foreach (var room in map.Visited) foreach (var dir in DungeonLayout.Directions) {
                    var known = new List<Vector2Int>(map.KnownExits(layout, room));
                    Require(known.Contains(dir) == layout.CanTravel(room, dir), "Known exit missing");
                }
                map.Visit(layout.Goal); Require(map.GoalVisible(layout), "Arrived goal hidden");
                map.Reset(); Require(map.Visited.Count == 1 && !map.GoalVisible(layout), "Reset leaked discovery");
                string signature = string.Join(";", layout.Rooms);
                Require(signature == string.Join(";", DungeonLayout.Generate(level, seed).Rooms), "Seed not reproducible");
                signatures.Add(signature);
            }
            Require(signatures.Count > 190, "Insufficient random variation");
        }
    }
}
