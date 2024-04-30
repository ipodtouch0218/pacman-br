using Quantum;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridColorHandler : QuantumCallbacks {

    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Gradient gradient;

    public override unsafe void OnUpdateView(QuantumGame game) {
        var globals = game.Frames.Predicted.Global;
        float percentage = 1;

        if (globals->PowerPelletTotalDuration != 0) {
            percentage = 1f - (globals->PowerPelletDuration / globals->PowerPelletTotalDuration).AsFloat;
        }

        tilemap.color = gradient.Evaluate(percentage);
    }
}