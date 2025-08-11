using UnityEngine;

public class BeatAnimationPlay : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private Animation animation;

    public void OnEnable() {
        BeatManager.OnBeat += OnBeat;
    }

    public void OnDisable() {
        BeatManager.OnBeat -= OnBeat;
    }

    private void OnBeat() {
        animation.Stop();
        animation.Play();
    }
}
