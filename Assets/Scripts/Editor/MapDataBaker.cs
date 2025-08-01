using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using Quantum;

[assembly: QuantumMapBakeAssembly]
public class MazeDataBaker : MapDataBakerCallback {

    public override void OnBeforeBake(QuantumMapData data) {

    }

    public override void OnBake(QuantumMapData data) {
        
        var settings = (PacmanStageMapData) QuantumUnityDB.GetGlobalAssetEditorInstance(data.Asset.UserAsset);

        GameObject parent = GameObject.FindGameObjectWithTag("Maze");
        Tilemap[] mazes = parent.GetComponentsInChildren<Tilemap>(true)
            .Where(tm => tm.transform.parent == parent.transform)
            .ToArray();

        PacmanStageMapData.MazeData[] oldMazes = settings.Mazes;
        settings.Mazes = new PacmanStageMapData.MazeData[mazes.Length];

        for (int i = 0; i < mazes.Length; i++) {
            PacmanStageMapData.MazeData maze;
            if (i < oldMazes.Length) {
                maze = settings.Mazes[i] = oldMazes[i];
            } else {
                maze = settings.Mazes[i] = new();
            }
            Tilemap tilemap = mazes[i];

            tilemap.CompressBounds();
            BoundsInt bounds = tilemap.cellBounds;
            Vector3Int size = bounds.size - new Vector3Int(2, 2, 0);
            Vector3Int origin = bounds.min + new Vector3Int(1, 1, 0);

            maze.Size = ((Vector3) size).ToFPVector3().XY;
            maze.Origin = ((Vector3) origin).ToFPVector3().XY;

            // Collision Data
            int tilesPerScreen = size.x * size.y;
            maze.CollisionData = new bool[tilesPerScreen];
            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    Vector3Int pos = origin + new Vector3Int(x, y, 0);
                    TileBase tile = tilemap.GetTile(pos);

                    maze.CollisionData[x + y * size.x] = !tile;
                }
            }
            Debug.Log($"Baked collision data for maze {i} ({maze.Size.X.AsInt}x{maze.Size.Y.AsInt})");

            // Pellet Data
            Tilemap[] dotMaps = tilemap.GetComponentsInChildren<Tilemap>(true);
            maze.PelletData = new byte[tilesPerScreen * dotMaps.Length];

            for (int dotmapIndex = 1; dotmapIndex < dotMaps.Length; dotmapIndex++) {
                Tilemap dotMap = dotMaps[dotmapIndex];
                for (int x = 0; x < size.x; x++) {
                    for (int y = 0; y < size.y; y++) {
                        Vector3Int pos = origin + new Vector3Int(x, y, 0);
                        TileBase tile = dotMap.GetTile(pos);

                        if (tile) {
                            int index = x + (y * size.x) + ((dotmapIndex - 1) * tilesPerScreen);
                            maze.PelletData[index] = tile.name switch {
                                "SmallPellet" => 1,
                                "PowerPellet" => 2,
                                _ => 0,
                            };
                        }
                    }
                }
            }
            Debug.Log($"-- Baked {dotMaps.Length - 1} dot layouts");

            // Ghost house

            // UNFINISHED: move object in unity to be a child
            maze.GhostHouse = FindChildWithTag(tilemap.transform, "Ghost House", true).position.ToFPVector2();

            // Spawn Data

            // UNFINISHED: move object in unity to be a child
            maze.SpawnPoints = FindChildrenWithTag(tilemap.transform, "Spawnpoint", true)
                .Select(t => new PacmanStageMapData.SpawnPointData() {
                    Position = t.position.ToFPVector2(),
                    Direction = (int)(Mathf.Repeat(t.eulerAngles.y + 135, 360f) / 90),
                })
                .ToArray();

            Debug.Log($"-- Baked {maze.SpawnPoints.Length} Pacman Spawnpoints");

            // Fruit Data

            // UNFINISHED: move object in unity to be a child
            maze.FruitSpawnPoints = FindChildrenWithTag(tilemap.transform, "Fruit Spawnpoint", true)
                .Select(t => t.position.ToFPVector2())
                .ToArray();

            Debug.Log($"-- Baked {maze.FruitSpawnPoints.Length} Fruit Spawnpoints");
        }

        EditorUtility.SetDirty(settings);
    }

    private static IEnumerable<Transform> FindChildrenWithTag(Transform transform, string tag, bool nested = false) {
        if (transform.childCount <= 0) {
            return Array.Empty<Transform>();
        }

        HashSet<Transform> found = new();

        for (int i = 0; i < transform.childCount; i++) {
            Transform child = transform.GetChild(i);
            if (child.CompareTag(tag)) {
                found.Add(child);
            }

            if (nested) {
                found.UnionWith(FindChildrenWithTag(child, tag, true));
            }
        }

        return found;
    }

    private static Transform FindChildWithTag(Transform transform, string tag, bool nested = false) {

        for (int i = 0; i < transform.childCount; i++) {

            Transform child = transform.GetChild(i);
            if (child.CompareTag(tag)) {
                return child;
            }

            if (nested) {
                Transform foundFromNested = FindChildWithTag(child, tag, true);
                if (foundFromNested) {
                    return foundFromNested;
                }
            }
        }

        return null;
    }
}