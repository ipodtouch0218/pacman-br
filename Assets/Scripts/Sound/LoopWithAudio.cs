using System;
using UnityEngine;

[Serializable]
public class LoopingAudioClip {

    public AudioClip Clip;
    public int LoopStartSamples;
    public int LoopEndSamples;

    public int Length => LoopEndSamples - LoopStartSamples;

    public static void Update(AudioSource source, LoopingAudioClip clip) {
        if (clip == null) {
            return;
        }

        if (source.timeSamples >= clip.LoopEndSamples) {
            source.timeSamples -= clip.Length;
        }
    }
}
