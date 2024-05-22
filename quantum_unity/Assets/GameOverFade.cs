using Quantum;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameOverFade : MonoBehaviour {

    [SerializeField] private Image image;

    public void Start() {
        QuantumEvent.Subscribe<EventGameEnd>(this, OnGameEnd);
        Color color = image.color;
        color.a = 0;
        image.color = color;
    }

    public void OnGameEnd(EventGameEnd e) {
        StartCoroutine(FadeOut(3f, 2f));
    }

    private IEnumerator FadeOut(float time, float delay) {
        yield return new WaitForSeconds(delay);

        Color color = image.color;
        float timer = 0;

        while ((timer += Time.deltaTime) < time) {
            color.a = (timer / time);
            image.color = color;
            yield return null;
        }
    }
}