using Quantum;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PointUpdater : QuantumCallbacks {

    private static int StillCounting;
    private static float LastPlayedBombSound;

    //---Serialized Variables
    [SerializeField] private TMP_Text text, bombs, addScore;
    [SerializeField] private GameObject powerMeter;
    [SerializeField] private SlicedFilledImage powerMeterImage;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bombAddClip;
    [SerializeField] private Image background;
    [SerializeField] private Animation animation;
    [SerializeField] private Animator scoreAddAnimator;

    //---Private Variables
    private Transform originalTextParent;
    private Vector2 originalTextPosition;
    private EntityRef entity;
    private PlayerRef player;
    private int currentScore;

    public void Start() {
        QuantumEvent.Subscribe<EventGameStarting>(this, OnGameStarting);
        QuantumEvent.Subscribe<EventGameEnd>(this, OnGameEnd);
        QuantumEvent.Subscribe<EventPacmanScored>(this, OnPacmanScored);
        QuantumEvent.Subscribe<EventPowerPelletEat>(this, OnPowerPelletEat);
        QuantumEvent.Subscribe<EventPowerPelletEnd>(this, OnPowerPelletEnd);
        QuantumEvent.Subscribe<EventPacmanCollectBomb>(this, OnCollectBomb);
        QuantumEvent.Subscribe<EventPacmanUseBomb>(this, OnUseBomb);

        originalTextParent = text.transform.parent;
        originalTextPosition = text.transform.position;
    }

    public void Initialize(Frame f, PacmanAnimator pacman) {
        entity = pacman.EntityRef;
        text.color = pacman.PlayerColor;
        bombs.color = pacman.PlayerColor;
        gameObject.SetActive(true);
        if (f.TryGet(entity, out PlayerLink pl)) {
            player = pl.Player;
        }
    }

    public override void OnUpdateView(QuantumGame game) {
        var f = game.Frames.Predicted;
        if (!f.TryGet(entity, out PacmanPlayer pacman)) {
            return;
        }

        // TODO: change
        if (pacman.HasPowerPellet) {
            powerMeterImage.fillAmount = pacman.PowerPelletTimer.AsFloat / pacman.PowerPelletFullTimer.AsFloat;
        }
    }

    private IEnumerator CountBombs(Frame f, float delay) {
        int bombCount = f.Get<PacmanPlayer>(entity).Bombs;

        yield return new WaitForSeconds(delay);
        int count = 1;
        while (bombCount > 0) {
            bombCount--;
            SetScore(currentScore + (750 * count));
            SetBombs(bombCount);
            addScore.text = '+' + (750 * count).ToString();
            scoreAddAnimator.Play("CE1ScoreAdd", -1, 0f);

            if (LastPlayedBombSound != Time.time) {
                LastPlayedBombSound = Time.time;
                audioSource.PlayOneShot(bombAddClip);
            }

            animation.Play();
            if (bombCount != 0) {
                yield return new WaitForSeconds(1f / ++count);
            }
        }

        StillCounting--;
        while (StillCounting > 0) {
            yield return null;
        }

        yield return MoveTowardsPosition(f, 0.75f, 0.5f);
    }

    public void OnPacmanScored(EventPacmanScored e) {
        if (e.Entity != entity) {
            return;
        }

        SetScore(e.TotalPoints);
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
        Frame f = e.Game.Frames.Verified;
        if (StillCounting == 0) {
            StillCounting = f.PlayerCount;
        }

        powerMeter.SetActive(false);
        StartCoroutine(CountBombs(f, 0.5f));
    }

    public void OnGameStarting(EventGameStarting e) {

        RectTransform rt = text.GetComponent<RectTransform>();
        rt.SetParent(originalTextParent, true); // Parent right to canvas
        rt.position = originalTextPosition;

        SetScore(0);

        int bombCount = e.Game.Frames.Predicted.Get<PacmanPlayer>(entity).Bombs;
        SetBombs(bombCount);
    }

    public void OnUseBomb(EventPacmanUseBomb e) {
        int bombCount = e.Game.Frames.Predicted.Get<PacmanPlayer>(entity).Bombs;
        SetBombs(bombCount);
    }

    public void OnCollectBomb(EventPacmanCollectBomb e) {
        int bombCount = e.Game.Frames.Predicted.Get<PacmanPlayer>(entity).Bombs;
        SetBombs(bombCount);
    }

    private void SetScore(int score) {
        text.text = (player.IsValid ? "P" + (player + 1) + " " : "") + score.ToString().PadLeft(6, '0');
        currentScore = score;
    }

    private void SetBombs(int bombCount) {
        bombs.text = $"<sprite=0>X{bombCount}";
    }

    private IEnumerator MoveTowardsPosition(Frame f, float travelTime, float delay) {
        yield return new WaitForSeconds(delay);

        RectTransform rt = text.GetComponent<RectTransform>();
        Vector2 previousPosition = rt.position;
        rt.SetParent(GetComponentInParent<Canvas>().transform, true); // Parent right to canvas
        rt.position = previousPosition;

        PacmanPlayer player = f.Get<PacmanPlayer>(entity);
        Vector2 position = player.RoundRanking.UniqueRanking * rt.sizeDelta.y * Vector2.down;

        Vector2 moveVelocity = position - rt.anchoredPosition;

        while (Vector2.Distance(rt.anchoredPosition, position) > 0.01) {
            rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, position, ref moveVelocity, travelTime);
            yield return null;
        }
    }
}