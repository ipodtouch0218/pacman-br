using UnityEngine;
using UnityEngine.Tilemaps;

public class BeatTile : Tile {

    public Sprite beatSprite;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {
        base.GetTileData(position, tilemap, ref tileData);
        tileData.sprite = BeatManager.Beat ? beatSprite : sprite;
    }
}