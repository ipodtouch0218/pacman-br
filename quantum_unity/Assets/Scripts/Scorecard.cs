using System.Collections;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scorecard : MonoBehaviour {

    //---Public
    public bool DoneCounting => toAddScore <= 0;
    public int ToAddScore => toAddScore;

    //---Serialized Variables
    [SerializeField] private Image pacmanSprite;
    [SerializeField] private TMP_Text rankingText, totalScoreText, toAddScoreText;

    //---Private Variables
    private int totalScore;
    private int toAddScore;

    public void Initialize(PacmanPlayer player) {
        totalScore = player.TotalScore;
        toAddScore = player.Score;
        UpdateText(true);
        gameObject.SetActive(true);
    }

    public void StartCounting(int scorePerSecond) {
        StartCoroutine(CountScore(scorePerSecond));
    }

    private IEnumerator CountScore(int scorePerSecond) {
        do {
            int change = Mathf.Min(Mathf.RoundToInt(scorePerSecond * Time.deltaTime), toAddScore);
            totalScore += change;
            toAddScore -= change;

            UpdateText(false);
            yield return null;
        } while (toAddScore > 0);
    }

    private void UpdateText(bool showZero) {
        totalScoreText.text = totalScore.ToString();
        toAddScoreText.text = (showZero || toAddScore > 0) ? ('+' + toAddScore.ToString()) : "";
    }
}
