using System;
using System.Collections.Generic;
using UnityEngine;

public class BeatManager : MonoBehaviour {

    //---Static
    public static event Action OnBeat;
    public static bool Beat => Instance != null && Instance.activeBeatCount > 0;
    private static BeatManager Instance;

    //---Serailized Variables
    [SerializeField] private AudioSource music;
    [SerializeField] private float bpm = 136, beatsPerMeasure = 4, offset, beatDuration = (4f / 60f);
    [SerializeField] private List<MeasureData> musicMeasureData;

    //---Private Variables
    private int measureIndex;
    private int previousMeasure = -1;
    private float previousBeat;
    private int activeBeatCount = 0;

    public void OnEnable() {
        Instance = this;
    }

    public void Update() {
        float currentTotalBeats = music.time * (bpm / 60f) + offset;
        int currentMeasure = Mathf.FloorToInt(currentTotalBeats / beatsPerMeasure);
        float currentBeat = currentTotalBeats % beatsPerMeasure;

        MeasureData measure = musicMeasureData[measureIndex];
        if (previousMeasure != currentMeasure) {
            if (measureIndex + 1 < musicMeasureData.Count) {
                if (currentMeasure >= musicMeasureData[measureIndex + 1].Measure) {
                    measure = musicMeasureData[measureIndex + 1];
                    measureIndex += 1;
                }
            }
        }

        if (measure == null) {
            return;
        }

        if (previousMeasure != currentMeasure) {
            previousBeat -= beatsPerMeasure;
        }
        foreach (float beatTrigger in measure.Beats) {
            bool trigger = previousBeat < beatTrigger && currentBeat >= beatTrigger;
            if (trigger) {
                activeBeatCount++;
                OnBeat?.Invoke();
            }

            bool end = previousBeat < (beatTrigger + beatDuration) && currentBeat >= (beatTrigger + beatDuration);
            if (end) {
                activeBeatCount--;
            }
        }

        previousMeasure = currentMeasure;
        previousBeat = currentBeat;
    }

    [Serializable]
    public class MeasureData {
        public int Measure;
        public float[] Beats;
    }
}
