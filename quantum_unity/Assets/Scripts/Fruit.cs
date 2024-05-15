using Quantum;
using UnityEngine;

public class Fruit : MonoBehaviour {

    [SerializeField] private EntityView entity;
    [SerializeField] private GameObject particles;

    public void Initialized(QuantumGame game) {
        GameObject newParticles = Instantiate(particles);
        newParticles.transform.position = transform.position;

        Quantum.Fruit fruit = game.Frames.Verified.Get<Quantum.Fruit>(entity.EntityRef);
        Debug.Log($"Graphic: {fruit.Graphic}");
    }
}