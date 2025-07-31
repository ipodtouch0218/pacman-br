using Quantum;
using UnityEngine;

public unsafe class FruitAnimator : QuantumEntityViewComponent {

    //---Serialized Variables
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
        this.SetIfNull(ref spriteRenderer, Utils.GetComponentType.Children);
    }

    public override void OnActivate(Frame f) {
        GameObject newParticles = Instantiate(particles);
        newParticles.transform.position = transform.position;

        var fruit = f.Unsafe.GetPointer<Quantum.Fruit>(EntityRef);
        spriteRenderer.sprite = sprites[Mathf.Clamp(fruit->Graphic, 0, sprites.Length - 1)];

        active = true;
        spriteRenderer.gameObject.transform.localScale = Vector3.one * growSizeCurve.Evaluate(timer);
    }

    public override void OnUpdateView() {
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


    private static float Remap(float value, float oldMin, float oldMax, float newMin, float newMax) {
        float oldRange = oldMax - oldMin;
        float newRange = newMax - newMin;

        float percentage = (value - oldMin) / oldRange;

        return (percentage * newRange) + newMin;
    }
}