using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Quantum;
using UnityEngine;

public class ScorecardManager : MonoBehaviour {

    //---Serialized Variables
    [SerializeField] private Canvas mainCanvas, resultsCanvas;
    [SerializeField] private ScreenFade fade;
    [SerializeField] private Scorecard templateScorecard;
    [SerializeField] private int maxScorePerSecond = 25_000;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wa, ka, doneCountingClip;

    //---Private Variables
    private readonly HashSet<Scorecard> scorecards = new();
    private bool countingKa;
    private float timeOfLastSound;

    public void OnValidate() {
        if (!resultsCanvas) {
            resultsCanvas = GetComponent<Canvas>();
        }
    }

    public void Start() {
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
        QuantumEvent.Subscribe<EventGameEnd>(this, OnGameEnd);
        resultsCanvas.enabled = false;
    }

    public void OnGameStarting(EventGameStarting e) {
        foreach (var scorecard in scorecards) {
            Destroy(scorecard.gameObject);
        }
        scorecards.Clear();
        resultsCanvas.enabled = false;
    }

    public void OnGameEnd(EventGameEnd e) {
        var players = e.Game.Frames.Predicted.Filter<PacmanPlayer>();
        while (players.Next(out _, out var player)) {
            Scorecard newScorecard = Instantiate(templateScorecard, resultsCanvas.transform);
            newScorecard.Initialize(player);
            scorecards.Add(newScorecard);
        }
        StartCoroutine(ScorecardSequence());
    }

    private IEnumerator ScorecardSequence() {
        // Wait for initial results screen
        yield return new WaitForSeconds(10);
        resultsCanvas.enabled = true;
        mainCanvas.enabled = false;
        StartCoroutine(fade.FadeToValue(fade.highPriorityImage, 0, 0, 0));

        // Start scorecards counting
        yield return new WaitForSeconds(2);
        int scorePerSecond = Mathf.Min(maxScorePerSecond, scorecards.Max(sc => sc.ToAddScore));
        foreach (var scorecard in scorecards) {
            scorecard.StartCounting(scorePerSecond);
        }

        // Wait until all done
        while (scorecards.Any(sc => !sc.DoneCounting)) {
            if (Time.time - timeOfLastSound > 0.05f) {
                timeOfLastSound = Time.time;
                audioSource.clip = countingKa ? ka : wa;
                countingKa = !countingKa;
                audioSource.Play();
            }
            yield return null;
        }

        audioSource.Stop();
        audioSource.PlayOneShot(doneCountingClip);

        // Sort
        yield return new WaitForSeconds(2);
        foreach (var scorecard in scorecards) {
            // scorecard.ChangeRanking
        }

    }
}