using Quantum;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridColorHandler : QuantumCallbacks {

    [SerializeField] private TilemapRenderer tilemapRenderer;
    [SerializeField] private Gradient gradient;

    [SerializeField] private float gradientStartSeconds = 5f;
    [SerializeField] private float maxWaveIntensity = 0.07f;

    private MaterialPropertyBlock mpb;

    private void Start() {
        mpb = new();
        tilemapRenderer.GetPropertyBlock(mpb);
    }

    public override unsafe void OnUpdateView(QuantumGame game) {
        var globals = game.Frames.Predicted.Global;

        float remainingTime = globals->PowerPelletRemainingTime.AsFloat;
        float percentage;
        if (remainingTime > 0) {
            if (remainingTime < gradientStartSeconds) {
                percentage = 1f - (remainingTime / gradientStartSeconds);
            } else {
                percentage = 0;
            }
        } else {
            percentage = 1;
        }

        mpb.SetColor("_Color", gradient.Evaluate(percentage));
        mpb.SetFloat("_WaveIntensity", (1f - percentage) * maxWaveIntensity);
        tilemapRenderer.SetPropertyBlock(mpb);
    }
}
