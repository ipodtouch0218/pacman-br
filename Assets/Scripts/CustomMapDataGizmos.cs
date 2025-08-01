using Quantum;
using UnityEngine;

public class CustomMapDataGizmos : MonoBehaviour {

    [SerializeField] private QuantumMapData data;

    public void OnValidate() {
        this.SetIfNull(ref data);
    }

    public void OnDrawGizmos() {
        PacmanStageMapData customData = QuantumUnityDB.GetGlobalAssetEditorInstance<PacmanStageMapData>(data.Asset.UserAsset);
        GameObject obj = GameObject.FindGameObjectWithTag("Maze");

        Gizmos.color = Color.yellow;
        for (int i = 0; i < customData.Mazes.Length; i++) {
            Transform mazeGameObject = obj.transform.GetChild(i);
            if (!mazeGameObject.gameObject.activeInHierarchy) {
                continue;
            }
            PacmanStageMapData.MazeData maze = customData.Mazes[i];
            Gizmos.DrawWireCube(
                (maze.Origin + (maze.Size / 2)).ToUnityVector3() + mazeGameObject.position,
                maze.Size.ToUnityVector3());
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