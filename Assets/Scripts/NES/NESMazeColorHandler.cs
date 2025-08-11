using Quantum;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NESMazeColorHandler : QuantumSceneViewComponent {

    //---Serialized Variables
    [SerializeField] private Tilemap tilemap;

    [SerializeField] private PacmanAsset pacAsset;
    [SerializeField] private Material sharedMaterial;
    [SerializeField] private int defaultIndex;
    [SerializeField] private int[] powerPelletIndexes;
    [SerializeField] private float powerPelletStartSeconds = 15f;

    [SerializeField] private float beatDuration = 4f / 60f;

    //---Private Variables
    private float beat;

    public void Start() {
        sharedMaterial.SetFloat("_Y", defaultIndex);
        BeatManager.OnBeat += OnBeat;
    }

    public void OnDestroy() {
        sharedMaterial.SetFloat("_Y", defaultIndex);
        BeatManager.OnBeat -= OnBeat;
    }

    public override unsafe void OnUpdateView() {
        Frame f = PredictedFrame;

        float remainingTime = f.Global->PowerPelletRemainingTime.AsFloat;
        if (remainingTime > 0) {
            float percentage = 1f - (remainingTime / pacAsset.PowerPelletTime.AsFloat);
            int index = (int) (percentage * powerPelletIndexes.Length);
            sharedMaterial.SetFloat("_Y", powerPelletIndexes[index]);
        } else {
            sharedMaterial.SetFloat("_Y", defaultIndex);
        }

        if (beat == beatDuration) {
            // Apply replacements
            tilemap.RefreshAllTiles();
        } else if (beat > 0) {
            beat -= Time.deltaTime;
            if (beat <= 0) {
                // Undo replacements
                tilemap.RefreshAllTiles();
                beat = 0;
            }
        }
    }

    private void OnBeat() {
        beat = beatDuration;
    }
}