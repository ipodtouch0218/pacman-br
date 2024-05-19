using Quantum;
using UnityEngine;

public class Fruit : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private EntityView entity;
    [SerializeField] private GameObject particles;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] sprites;

    [SerializeField] private AnimationCurve growSizeCurve;

    [SerializeField] private float flashingPeriod = 1.5f;
    [SerializeField] private Vector2 flashingMinMax = new(0.8f, 1);

    //---Private Variables
    private bool active;
    private float timer;

    public void OnValidate() {
        if (!spriteRenderer) {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public void Update() {
        spriteRenderer.color = Color.white * Remap(Mathf.Sin((Time.time * 2 * Mathf.PI) / flashingPeriod), -1, 1, flashingMinMax.x, flashingMinMax.y);

        if (!active) {
            return;
        }

        if ((timer += Time.deltaTime) > growSizeCurve.keys[^1].time) {
            timer = growSizeCurve.keys[^1].time;
            spriteRenderer.gameObject.transform.localScale = Vector3.one;
            active = false;
            return;
        }

        spriteRenderer.gameObject.transform.localScale = Vector3.one * growSizeCurve.Evaluate(timer);
    }

    public void Initialized(QuantumGame game) {
        GameObject newParticles = Instantiate(particles);
        newParticles.transform.position = transform.position;

        Quantum.Fruit fruit = game.Frames.Verified.Get<Quantum.Fruit>(entity.EntityRef);
        spriteRenderer.sprite = sprites[Mathf.Clamp(fruit.Graphic, 0, sprites.Length - 1)];

        active = true;
        spriteRenderer.gameObject.transform.localScale = Vector3.one * growSizeCurve.Evaluate(timer);
    }

    private static float Remap(float value, float oldMin, float oldMax, float newMin, float newMax) {
        float oldRange = oldMax - oldMin;
        float newRange = newMax - newMin;

        float percentage = (value - oldMin) / oldRange;

        return (percentage * newRange) + newMin;
    }
}