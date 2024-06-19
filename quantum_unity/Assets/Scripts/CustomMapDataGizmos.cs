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
        GameObject obj = GameObject.FindGameObjectWithTag("Maze");

        Gizmos.color = Color.yellow;
        for (int i = 0; i < customData.Mazes.Length; i++) {
            if (!obj.transform.GetChild(i).gameObject.activeInHierarchy) {
                continue;
            }
            MapCustomData.MazeData maze = customData.Mazes[i];
            Gizmos.DrawWireCube(
                (maze.Origin + (maze.Size / 2)).XOY.ToUnityVector3() - new Vector3(0.5f, 0, 0.5f),
                maze.Size.XOY.ToUnityVector3());
        }

        foreach (var spawnpoint in GameObject.FindGameObjectsWithTag("Spawnpoint")) {
            float rotation = Mathf.Repeat(spawnpoint.transform.eulerAngles.y, 360);
            int index = rotation switch {
                < 45 => 1,
                < 135 => 2,
                < 225 => 3,
                < 315 => 0,
                _ => 1
            };
            Gizmos.DrawIcon(spawnpoint.transform.position, "spawnpoint" + index, true);
        }

        foreach (var fruit in GameObject.FindGameObjectsWithTag("Fruit Spawnpoint")) {
            Gizmos.DrawIcon(fruit.transform.position, "fruit", true);
        }
    }
}