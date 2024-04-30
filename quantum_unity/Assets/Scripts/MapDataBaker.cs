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

        Tilemap tilemap = Object.FindObjectOfType<Tilemap>();

        if (!tilemap) {
            return;
        }

        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        int tilesPerScreen = bounds.size.x * bounds.size.y;
        dataAsset.Settings.MapOrigin = new FPVector2(bounds.position.x, bounds.position.y);
        dataAsset.Settings.MapSize = new FPVector2(bounds.size.x, bounds.size.y);
        dataAsset.Settings.CollisionData = new bool[tilesPerScreen];
        dataAsset.Settings.PelletData = new byte[tilesPerScreen * (bounds.size.z - 1)];

        Vector3 ghostHouse = GameObject.FindGameObjectWithTag("Ghost House").transform.position;
        dataAsset.Settings.GhostHouse = new FPVector2(ghostHouse.x.ToFP(), ghostHouse.z.ToFP());

        for (int z = 0; z < bounds.size.z; z++) {
            for (int x = 0; x < bounds.size.x; x++) {
                for (int y = 0; y < bounds.size.y; y++) {
                    Vector3Int pos = bounds.position + new Vector3Int(x, y, z);
                    TileBase tile = tilemap.GetTile(pos);

                    if (z == 0) {
                        // Collision layer
                        dataAsset.Settings.CollisionData[x + y * bounds.size.x] = !tile;
                    } else {
                        // Pellet layer
                        if (tile) {
                            dataAsset.Settings.PelletData[x + (y * bounds.size.x) + ((z - 1) * tilesPerScreen)] = tile.name switch {
                                "SmallPellet" => 1,
                                "PowerPellet" => 2,
                                _ => 0,
                            };
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(dataAsset);
#endif
    }
}