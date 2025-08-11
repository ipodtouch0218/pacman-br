using System.Collections;
using UnityEngine;

public class BeatSpriteSwap : MonoBehaviour {

    //---Serailized Variables
    [SerializeField] private SpriteRenderer sRenderer;
    [SerializeField] private Sprite before, after;
    [SerializeField] private float beatLength = 0.1f;

    //---Private Variables
    private Coroutine changeSpriteCoroutine;

    public void OnEnable() {
        BeatManager.OnBeat += OnBeat;
    }

    public void OnDisable() {
        BeatManager.OnBeat -= OnBeat;
    }

    public void OnBeat() {
        if (changeSpriteCoroutine != null) {
            StopCoroutine(changeSpriteCoroutine);
            changeSpriteCoroutine = null;
        }

        sRenderer.sprite = after;
        changeSpriteCoroutine = StartCoroutine(ChangeSpriteAfterPause(before, beatLength));
    }

    private IEnumerator ChangeSpriteAfterPause(Sprite sprite, float time) {
        yield return new WaitForSeconds(time);
        sRenderer.sprite = sprite;
        changeSpriteCoroutine = null;
    }
}