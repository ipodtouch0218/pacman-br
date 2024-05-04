using TMPro;
using UnityEngine;

public class PelletPointIndicator : MonoBehaviour {

    [SerializeField] private TextMeshPro text;
    [SerializeField] private AnimationCurve movement;
    [SerializeField] private float lifetime = 2f;

    private float timer;
    private Vector3 origin;

    public void Initialize(int combo) {
        text.text = combo.ToString();
        origin = transform.position;
        Destroy(gameObject, lifetime);
    }

    public void Update() {
        timer += Time.deltaTime;
        transform.position = origin + Vector3.forward * movement.Evaluate(timer / lifetime);
    }
}