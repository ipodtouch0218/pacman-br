using UnityEngine;

public class PointIndicator : MonoBehaviour {

    [SerializeField] private Vector3 velocity;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float destroyAfter = 2;

    public void Initialize(int index) {
        spriteRenderer.sprite = sprites[Mathf.Clamp(index, 0, sprites.Length)];
        Destroy(gameObject, destroyAfter);
    }

    public void Update() {
        transform.position += velocity * Time.deltaTime;
    }
}