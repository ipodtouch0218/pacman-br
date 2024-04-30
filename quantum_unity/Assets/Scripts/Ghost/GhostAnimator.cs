using Quantum;
using UnityEngine;

public class GhostAnimator : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private EntityView entity;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite scaredSprite, eatenSprite;

    //---Private Variables
    private Sprite originalSprite;

    public void OnValidate() {
        if (!entity) {
            entity = GetComponentInChildren<EntityView>();
        }
        if (!spriteRenderer) {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public void Start() {
        originalSprite = spriteRenderer.sprite;
        QuantumEvent.Subscribe<EventGhostStateChanged>(this, OnGhostStateChanged);
    }

    public void OnGhostStateChanged(EventGhostStateChanged e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        spriteRenderer.sprite = e.State switch {
            GhostState.Chase => originalSprite,
            GhostState.Scared => scaredSprite,
            GhostState.Eaten => eatenSprite,
            _ => originalSprite
        };
    }
}