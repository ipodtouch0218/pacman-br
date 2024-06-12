using Quantum;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour {

    //---Public
    public Image lowPriorityImage, highPriorityImage;

    public void Start() {
        QuantumEvent.Subscribe<EventGameEnd>(this, OnGameEnd);
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
    }

    public void OnGameStarting(EventGameStarting e) {
        StartCoroutine(FadeToValue(lowPriorityImage, 0, 2, 0.5f));
    }

    public void OnGameEnd(EventGameEnd e) {
        StartCoroutine(FadeToValue(lowPriorityImage, 1, 3, 2));
        StartCoroutine(FadeToValue(highPriorityImage, 1, 3, 7));
    }

    public IEnumerator FadeToValue(Image image, float target, float time, float delay) {
        yield return new WaitForSeconds(delay);
        Color color = image.color;
        float start = color.a;
        float timer = 0;

        while ((timer += Time.deltaTime) < time) {
            color.a = Mathf.Lerp(start, target, timer / time);
            image.color = color;
            yield return null;
        }

        color.a = target;
        image.color = color;
    }
}