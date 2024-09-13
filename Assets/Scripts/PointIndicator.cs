using TMPro;
using UnityEngine;

public class PointIndicator : MonoBehaviour {

    [SerializeField] private TMP_Text text;
    [SerializeField] private float destroyAfter = 2, spawnOffset = 2.25f, downwardsScreenHeight = 0.7f;

    public void Initialize(int points) {
        text.text = points.ToString();
        Destroy(gameObject, destroyAfter);

        if (Camera.main.WorldToViewportPoint(transform.position).y > downwardsScreenHeight) {
            text.transform.localPosition = -1 * spawnOffset * Vector2.up;
        } else {
            text.transform.localPosition = spawnOffset * Vector2.up;
        }
    }
}