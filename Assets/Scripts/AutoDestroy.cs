using UnityEngine;

/// <summary>
/// this is the dumbest script in the world
/// </summary>
public class AutoDestroy : MonoBehaviour {

    [SerializeField] private float destroyIn;

    public void Start() {
        Destroy(gameObject, destroyIn);
    }

}