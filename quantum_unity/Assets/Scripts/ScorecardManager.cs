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
    [SerializeField] private AudioSource sfxSource, musicSource;
    [SerializeField] private AudioClip doneCountingClip, doneSortingClip;
    [SerializeField] private AudioClip[] sortingClips;

    //---Private Variables
    private readonly List<Scorecard> scorecards = new();

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
        while (players.Next(out var entity, out var player)) {
            Scorecard newScorecard = Instantiate(templateScorecard, resultsCanvas.transform);
            newScorecard.Initialize(e.Game.Frames.Verified, entity, player);
            scorecards.Add(newScorecard);
        }
        StartCoroutine(ScorecardSequence());
    }

    private IEnumerator ScorecardSequence() {
        // Wait for initial results screen
        yield return new WaitForSeconds(6);
        resultsCanvas.enabled = true;
        mainCanvas.enabled = false;
        StartCoroutine(fade.FadeToValue(fade.highPriorityImage, 0, 0.5f, 0));
        musicSource.volume = 0;
        musicSource.Play();
        musicSource.time = 6;
        StartCoroutine(FadeVolumeToValue(musicSource, 0.5f, 3f));

        // Start scorecards counting
        yield return new WaitForSeconds(2);
        scorecards.Sort((a, b) => a.Ranking - b.Ranking);
        int scorePerSecond = Mathf.Min(maxScorePerSecond, scorecards.Max(sc => sc.ToAddScore));
        foreach (var scorecard in scorecards) {
            scorecard.StartCounting(scorePerSecond);
        }

        // Wait until all done
        sfxSource.Play();
        while (scorecards.Any(sc => !sc.DoneCounting)) {
            foreach (var scorecard in scorecards) {
                //scorecard.Ranking =
            }
            sfxSource.time %= 0.1f;
            yield return null;
        }

        sfxSource.Stop();
        sfxSource.PlayOneShot(doneCountingClip);

        // Bubble sort!! Fancy
        yield return new WaitForSeconds(2);
        int swaps = 0;
        for (int i = scorecards.Count - 1; i > 0; i--) {
            for (int j = scorecards.Count - 1; j > 0; j--) {
                if (scorecards[j].TotalScore <= scorecards[j - 1].TotalScore) {
                    continue;
                }

                float time = Mathf.Max(0.1f, 1f / ++swaps);

                // Swap
                (scorecards[j], scorecards[j - 1]) = (scorecards[j - 1], scorecards[j]);
                scorecards[j].Ranking = j;
                scorecards[j - 1].Ranking = j - 1;

                scorecards[j].MoveToPosition(time);
                scorecards[j - 1].MoveToPosition(time);

                sfxSource.PlayOneShot(sortingClips[Mathf.Clamp(swaps, 0, sortingClips.Length - 1)]);
                yield return new WaitForSeconds(time);
            }
        }

        if (swaps > 0) {
            sfxSource.PlayOneShot(doneSortingClip);
        }

        yield return new WaitForSeconds(3);

        StartCoroutine(FadeVolumeToValue(musicSource, 0, 2f));
        StartCoroutine(fade.FadeToValue(fade.highPriorityImage, 1, 0.5f, 1));
    }

    private static IEnumerator FadeVolumeToValue(AudioSource source, float target, float time) {
        float start = source.volume;
        float timer = 0;
        while ((timer += Time.deltaTime) < time) {
            source.volume = Mathf.Lerp(start, target, timer / time);
            yield return null;
        }

        source.volume = target;
    }
}