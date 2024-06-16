using Quantum;
using System.Collections;
using TMPro;
using UnityEngine;

public class PointUpdater : QuantumCallbacks {

    //---Serialized Variables
    [SerializeField] private TMP_Text text;
    [SerializeField] private GameObject powerMeter;
    [SerializeField] private SlicedFilledImage powerMeterImage;

    //---Private Variables
    private EntityRef entity;

    public void Start() {
        QuantumEvent.Subscribe<EventPacmanScored>(this, OnPacmanScored);
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnPowerPelletEat);
        QuantumEvent.Subscribe<EventPowerPelletEnd>(this, OnPowerPelletEnd);
        QuantumEvent.Subscribe<EventGameEnd>(this, OnGameEnd);
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
    }

    public void Initialize(PacmanAnimator pacman) {
        entity = pacman.entity.EntityRef;
        text.color = pacman.PlayerColor;
        gameObject.SetActive(true);
    }

    public override void OnUpdateView(QuantumGame game) {
        var f = game.Frames.Predicted;
        if (!f.TryGet(entity, out PacmanPlayer pacman) || !pacman.HasPowerPellet) {
            return;
        }

        // TODO: change
        powerMeterImage.fillAmount = pacman.PowerPelletTimer.AsFloat / pacman.PowerPelletFullTimer.AsFloat;
    }

    public void OnPacmanScored(EventPacmanScored e) {
        PacmanPlayer pac = e.Game.Frames.Verified.Get<PacmanPlayer>(entity);
        text.text = Utils.RankingToString(pac.RoundRanking.SharedRanking + 1) + ". " + pac.RoundScore.ToString().PadLeft(6, '0');
    }

    public void OnPowerPelletEat(EventPowerPelletEat e) {
        if (e.Entity != entity) {
            return;
        }

        powerMeter.SetActive(true);
    }

    public void OnPowerPelletEnd(EventPowerPelletEnd e) {
        if (e.Entity != entity) {
            return;
        }

        powerMeter.SetActive(false);
    }

    public void OnGameEnd(EventGameEnd e) {
        powerMeter.SetActive(false);
        StartCoroutine(MoveTowardsPosition(e.Game, 0.75f, 1f));
    }

    public void OnGameStarting(EventGameStarting e) {
        text.text = "1A. 000000";
    }

    private IEnumerator MoveTowardsPosition(QuantumGame game, float travelTime, float delay) {
        yield return new WaitForSeconds(delay);


        RectTransform rt = GetComponent<RectTransform>();
        Vector2 previousPosition = rt.position;
        rt.SetParent(GetComponentInParent<Canvas>().transform, true); // Parent right to canvas
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.position = previousPosition;

        PacmanPlayer player = game.Frames.Verified.Get<PacmanPlayer>(entity);
        Vector2 position = player.RoundRanking.UniqueRanking * rt.sizeDelta.y * Vector2.down;

        Vector2 moveVelocity = position - rt.anchoredPosition;

        while (Vector2.Distance(rt.anchoredPosition, position) > 0.01) {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, position, ref moveVelocity, travelTime);
            yield return null;
        }
    }
}