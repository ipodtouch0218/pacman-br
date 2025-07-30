using Quantum;
using UnityEngine;
using UnityEngine.UI;

public class CE1Countdown : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private Animation animation;
    [SerializeField] private Image image;

    [SerializeField] private Sprite[] sprites;

    [SerializeField] private Vector2 finishAnchors;
    [SerializeField] private Sprite finishSprite;


    public void OnValidate() {
        if (!image) {
            image = GetComponent<Image>();
        }
        if (!animation) {
            animation = GetComponent<Animation>();
        }
    }

    public void Start() {
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
        QuantumEvent.Subscribe<EventTimerSecondPassed>(this, OnTimerSecondPassed);
        QuantumEvent.Subscribe<EventGameEnd>(this, OnGameEnd);

        image.enabled = false;
    }

    public void OnGameStarting(EventGameStarting e) {
        image.enabled = false;
    }

    public void OnGameEnd(EventGameEnd e) {
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = new(rt.anchorMin.x, finishAnchors.x);
        rt.anchorMax = new(rt.anchorMax.x, finishAnchors.y);
        rt.anchoredPosition = Vector2.zero;

        image.sprite = finishSprite;
        image.enabled = true;
        animation.Stop();
        animation.Play();
    }

    public void OnTimerSecondPassed(EventTimerSecondPassed e) {
        if (e.SecondsRemaining > 10) {
            return;
        }

        image.sprite = sprites[Mathf.Clamp(e.SecondsRemaining - 1, 0, sprites.Length - 1)];
        image.enabled = true;
        animation.Stop();
        animation.Play();
    }
}