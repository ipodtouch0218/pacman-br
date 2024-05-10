using TMPro;
using UnityEngine;

public class PelletPointIndicator : MonoBehaviour {

    [SerializeField] private TextMeshPro text;
    [SerializeField] private AnimationCurve movement;
    [SerializeField] private float lifetime = 2f;

    [SerializeField] private Gradient colorOverLife;

    private float timer;
    private Vector3 origin;
    private Color baseColor;

    public void Initialize(int combo, Color baseColor) {
        text.text = combo.ToString();
        origin = transform.position;
        this.baseColor = baseColor;
        Destroy(gameObject, lifetime);
    }

    public void Update() {
        timer += Time.deltaTime;
        transform.position = origin + Vector3.forward * movement.Evaluate(timer / lifetime);
        text.color = baseColor * colorOverLife.Evaluate(timer / lifetime);
    }
}