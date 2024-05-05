using Quantum;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridColorHandler : QuantumCallbacks {

    [SerializeField] private TilemapRenderer tilemapRenderer;
    [SerializeField] private Gradient gradient;

    private MaterialPropertyBlock mpb;

    public override unsafe void OnUpdateView(QuantumGame game) {
        var globals = game.Frames.Predicted.Global;
        float percentage = 1;

        if (globals->PowerPelletTotalDuration != 0) {
            percentage = 1f - (globals->PowerPelletDuration / globals->PowerPelletTotalDuration).AsFloat;
        }

        if (mpb == null) {
            mpb = new();
            tilemapRenderer.GetPropertyBlock(mpb);
        }
        mpb.SetColor("_Color", gradient.Evaluate(percentage));
        tilemapRenderer.SetPropertyBlock(mpb);
    }
}