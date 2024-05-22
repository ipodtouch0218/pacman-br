using Quantum;
using System.Collections;
using TMPro;
using UnityEngine;

public class PointUpdater : QuantumCallbacks {

    [SerializeField] public EntityView entity;
    [SerializeField] private TMP_Text text;

    [SerializeField] private GameObject powerMeter;
    [SerializeField] private SlicedFilledImage powerMeterImage;

    public void Start() {
        QuantumEvent.Subscribe<EventPacmanScored>(this, OnPacmanScored);
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnPowerPelletEat);
        QuantumEvent.Subscribe<EventPowerPelletEnd>(this, OnPowerPelletEnd);
        QuantumEvent.Subscribe<EventGameEnd>(this, OnGameEnd);
    }

    public void Initialize(PacmanAnimator pacman) {
        entity = pacman.entity;
        text.color = pacman.PlayerColor;
        gameObject.SetActive(true);
    }

    public override void OnUpdateView(QuantumGame game) {
        var f = game.Frames.Predicted;
        if (!f.TryGet(entity.EntityRef, out PacmanPlayer pacman) || !pacman.HasPowerPellet) {
            return;
        }

        // TODO: change
        powerMeterImage.fillAmount = pacman.PowerPelletTimer.AsFloat / pacman.PowerPelletFullTimer.AsFloat;
    }

    public void OnPacmanScored(EventPacmanScored e) {
        PacmanPlayer pac = e.Game.Frames.Verified.Get<PacmanPlayer>(entity.EntityRef);
        text.text = RankingToString(pac.Ranking + 1) + ". " + pac.Score.ToString().PadLeft(6, '0');
    }

    private static string RankingToString(int ranking) {

        ranking = Mathf.Abs(ranking);

        int lastNumber = ranking % 10;
        char character;

        // what.
        if (ranking < 10 || ranking >= 20) {
            character = lastNumber switch {
                1 => 'A',
                2 => 'B',
                3 => 'C',
                _ => 'D',
            };
        } else {
            character = 'D';
        }

        return ranking.ToString() + character;
    }

    public void OnPowerPelletEat(EventPowerPelletEat e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        powerMeter.SetActive(true);
    }

    public void OnPowerPelletEnd(EventPowerPelletEnd e) {
        if (e.Entity != entity.EntityRef) {
            return;
        }

        powerMeter.SetActive(false);
    }

    public void OnGameEnd(EventGameEnd e) {
        powerMeter.SetActive(false);
        StartCoroutine(MoveTowardsPosition(e.Game, 0.75f, 1f));
    }

    private IEnumerator MoveTowardsPosition(QuantumGame game, float travelTime, float delay) {
        yield return new WaitForSeconds(delay);


        RectTransform rt = GetComponent<RectTransform>();
        Vector2 previousPosition = rt.position;
        rt.SetParent(GetComponentInParent<Canvas>().transform, true); // Parent right to canvas
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.position = previousPosition;

        PacmanPlayer player = game.Frames.Verified.Get<PacmanPlayer>(entity.EntityRef);
        Vector2 position = player.UniqueRanking * rt.sizeDelta.y * Vector2.down;

        Vector2 moveVelocity = position - rt.anchoredPosition;

        while (Vector2.Distance(rt.anchoredPosition, position) > 0.01) {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, position, ref moveVelocity, travelTime);
            yield return null;
        }
    }
}