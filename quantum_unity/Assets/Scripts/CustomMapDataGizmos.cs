using Quantum;
using UnityEngine;

public class CustomMapDataGizmos : MonoBehaviour {

    [SerializeField] private MapData data;

    public void OnValidate() {
        if (!data) {
            data = GetComponent<MapData>();
        }
    }

    public void OnDrawGizmos() {
        MapCustomData customData = UnityDB.FindAsset<MapCustomDataAsset>(data.Asset.Settings.UserAsset.Id).Settings;

        Gizmos.color = Color.yellow;
        foreach (MapCustomData.MazeData maze in customData.Mazes) {
            Gizmos.DrawWireCube(
                (maze.Origin + (maze.Size / 2)).XOY.ToUnityVector3() - new Vector3(0.5f, 0, 0.5f),
                maze.Size.XOY.ToUnityVector3());
        }
    }
}