using UnityEngine;
using Quantum;
using Photon.Deterministic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnPointBaker : MapDataBakerCallback {

    public override void OnBeforeBake(MapData data) {

    }

    public override void OnBake(MapData data) {
        var dataAsset = UnityDB.FindAsset<MapCustomDataAsset>(data.Asset.Settings.UserAsset.Id);

        BakeSpawnpoints(dataAsset);
        BakePillSpawnpoints(dataAsset);

#if UNITY_EDITOR
        EditorUtility.SetDirty(dataAsset);
#endif
    }

    private void BakeSpawnpoints(MapCustomDataAsset dataAsset) {
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("Spawnpoint");
        if (spawns == null || spawns.Length <= 0) {
            return;
        }

        dataAsset.Settings.SpawnPoints = new MapCustomData.SpawnPointData[spawns.Length];

        for (int i = 0; i < spawns.Length; i++) {
            dataAsset.Settings.SpawnPoints[i].Position = spawns[i].transform.position.ToFPVector2();

            float angle = Mathf.Repeat(spawns[i].transform.eulerAngles.y + 135, 360f);
            int direction = (int) (angle / 90);
            dataAsset.Settings.SpawnPoints[i].Direction = direction;
        }
    }
    private void BakePillSpawnpoints(MapCustomDataAsset dataAsset) {
        GameObject[] spawns = GameObject.FindGameObjectsWithTag("Pill Spawnpoint");
        if (spawns == null || spawns.Length <= 0) {
            return;
        }

        dataAsset.Settings.PillSpawnPoints = new FPVector2[spawns.Length];

        for (int i = 0; i < spawns.Length; i++) {
            dataAsset.Settings.PillSpawnPoints[i] = spawns[i].transform.position.ToFPVector2();
        }
    }
}