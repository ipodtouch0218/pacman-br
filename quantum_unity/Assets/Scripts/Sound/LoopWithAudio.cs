using System;
using UnityEngine;

[Serializable]
public class LoopingAudioClip {

    public AudioClip Clip;
    public int LoopStartSamples;
    public int LoopEndSamples;

    public int Length => LoopEndSamples - LoopStartSamples;

    public static void Update(AudioSource source, LoopingAudioClip clip) {
        if (source.timeSamples >= clip.LoopEndSamples) {
            source.timeSamples -= clip.Length;
        }
    }
}
