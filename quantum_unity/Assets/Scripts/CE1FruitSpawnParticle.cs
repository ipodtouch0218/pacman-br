using Quantum;
using UnityEngine;

[ExecuteAlways]
public class CE1FruitSpawnParticle : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private AnimationCurve sizeCurve, rotationCurve;

    [InspectorButton("PlayEffect")]
    [SerializeField] private bool startAnimation;

    //---Private Variables
    private bool active;
    private float timer;

    public void OnValidate() {
        if (!particles) {
            particles = GetComponent<ParticleSystem>();
        }
    }

    public void Start() {
        PlayEffect();
    }

    public void Update() {
        if (!particles || !active) {
            return;
        }

        timer += Time.deltaTime;
        float percentage = timer / particles.main.duration;

        if (percentage > 1) {
            active = false;
            timer = 0;
            return;
        }

        var shape = particles.shape;
        shape.rotation = Vector3.forward * rotationCurve.Evaluate(percentage);
        shape.radius = sizeCurve.Evaluate(percentage);
    }

    public void PlayEffect() {
        active = true;
        timer = 0;
        particles.Play();
    }
}