using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Deterministic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapDataBaker : MapDataBakerCallback {

    public override void OnBeforeBake(MapData data) {

    }

    public override void OnBake(MapData data) {
        var dataAsset = UnityDB.FindAsset<MapCustomDataAsset>(data.Asset.Settings.UserAsset.Id);

        Tilemap tilemap = GameObject.FindGameObjectWithTag("Maze").GetComponent<Tilemap>();

        if (!tilemap) {
            return;
        }

        // Collision Data
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        bounds.size -= new Vector3Int(2, 2, 0);
        bounds.position += new Vector3Int(1, 1, 0);
        int tilesPerScreen = bounds.size.x * bounds.size.y;
        dataAsset.Settings.MapOrigin = new FPVector2(bounds.position.x, bounds.position.y);
        dataAsset.Settings.MapSize = new FPVector2(bounds.size.x, bounds.size.y);
        dataAsset.Settings.CollisionData = new bool[tilesPerScreen];

        for (int x = 0; x < bounds.size.x; x++) {
            for (int y = 0; y < bounds.size.y; y++) {
                Vector3Int pos = bounds.position + new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(pos);

                dataAsset.Settings.CollisionData[x + y * bounds.size.x] = !tile;
            }
        }
        Debug.Log($"Baked collision data ({bounds.size.x}x{bounds.size.y})");

        // Pellet data
        Tilemap[] dotMaps = tilemap.GetComponentsInChildren<Tilemap>(true);
        dataAsset.Settings.PelletData = new byte[tilesPerScreen * dotMaps.Length];

        for (int i = 1; i < dotMaps.Length; i++) {
            Tilemap dotMap = dotMaps[i];
            for (int x = 0; x < bounds.size.x; x++) {
                for (int y = 0; y < bounds.size.y; y++) {
                    Vector3Int pos = bounds.position + new Vector3Int(x, y, 0);
                    TileBase tile = dotMap.GetTile(pos);

                    if (tile) {
                        int index = x + (y * bounds.size.x) + ((i - 1) * tilesPerScreen);
                        dataAsset.Settings.PelletData[index] = tile.name switch {
                            "SmallPellet" => 1,
                            "PowerPellet" => 2,
                            _ => 0,
                        };
                    }
                }
            }
        }
        Debug.Log($"Baked {dotMaps.Length - 1} dot layouts");

        // Ghost house
        Vector3 ghostHouse = GameObject.FindGameObjectWithTag("Ghost House").transform.position;
        dataAsset.Settings.GhostHouse = new FPVector2(ghostHouse.x.ToFP(), ghostHouse.z.ToFP());

#if UNITY_EDITOR
        EditorUtility.SetDirty(dataAsset);
#endif
    }
}