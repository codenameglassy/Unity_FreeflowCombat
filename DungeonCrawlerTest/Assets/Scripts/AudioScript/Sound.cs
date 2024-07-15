using UnityEngine.Audio;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume;

    [Range(.1f, 3f)]
    public float pitch;

    public bool isLoop;
    public enum SoundTag
    {
        sfx, music
    }
    public SoundTag tag;

    public bool CompareTag(SoundTag t)
    {
        return t == tag;
    }

    public void mute(bool toggle)
    {
        if (source)
            source.mute = toggle;
    }


    [HideInInspector]public AudioSource source;
}
