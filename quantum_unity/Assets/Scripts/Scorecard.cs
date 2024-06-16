using System.Collections;
using System.Linq;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scorecard : MonoBehaviour {

    //---Public
    public bool DoneCounting => toAddScore <= 0;
    public int TotalScore => totalScore;
    public int ToAddScore => toAddScore;
    public int Ranking { get; set; }

    //---Serialized Variables
    [SerializeField] private RectTransform rt;
    [SerializeField] private Image pacmanSprite;
    [SerializeField] private TMP_Text rankingText, totalScoreText, toAddScoreText;
    [SerializeField] private Vector2 origin = new(0, 180);
    [SerializeField] private float height = 110;

    //---Private Variables
    private EntityRef entity;
    private int totalScore;
    private int toAddScore;
    private Vector2 moveVelocity;
    private Coroutine moveCoroutine, rankCorotuine;
    private int finalRanking;

    public void OnValidate() {
        if (!rt) {
            rt = GetComponent<RectTransform>();
        }
    }

    public void Initialize(Frame f, EntityRef entityRef, PacmanPlayer player) {
        entity = entityRef;
        totalScore = player.TotalScore;
        toAddScore = player.RoundScore;
        UpdateText(true);
        gameObject.SetActive(true);
        pacmanSprite.color = Utils.GetPlayerColor(f, entityRef);

        Ranking = player.PreviousRoundRanking.UniqueRanking;
        rankingText.text = Utils.RankingToString(player.PreviousRoundRanking.SharedRanking + 1) + '.';
        finalRanking = player.TotalRanking.SharedRanking;
        MoveToPosition(0);
    }

    public void LateUpdate() {
        if (ScorecardManager.AnyCounting) {
            int rank = ScorecardManager.Scorecards.Count(sc => sc.totalScore > totalScore);
            rankingText.text = Utils.RankingToString(rank + 1) + '.';
        }
    }

    public void StartCounting(int scorePerSecond) {
        StartCoroutine(CountScore(scorePerSecond));
    }

    public void MoveToPosition(float timeToTake) {
        Vector2 position = origin - (Ranking * height * Vector2.up);

        if (timeToTake <= Time.deltaTime) {
            rt.anchoredPosition = position;
        } else {
            if (moveCoroutine != null) {
                StopCoroutine(moveCoroutine);
            }
            moveCoroutine = StartCoroutine(Move(position, timeToTake));
        }
    }

    private IEnumerator Move(Vector2 target, float time) {
        while (Vector2.Distance(rt.anchoredPosition, target) > 0.01f) {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, target, ref moveVelocity, time / 4);
            yield return null;
        }

        rt.anchoredPosition = target;
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
        totalScoreText.text = totalScore.ToString().PadLeft(6, '0');
        toAddScoreText.text = (showZero || toAddScore > 0) ? ('+' + toAddScore.ToString()) : "";
    }
}
