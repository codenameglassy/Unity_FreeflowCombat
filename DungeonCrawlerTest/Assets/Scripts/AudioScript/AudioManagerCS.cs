using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManagerCS : MonoBehaviour
{
    public static AudioManagerCS instance;
    public Sound[] sounds;

    // Start is called before the first frame update

    //toogle Mute or Unmute
    private bool musicMutetoogle;
    private bool sfxMutetoggle;

    private void Awake()
    {
        if (instance)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this);
        }


        foreach (Sound s in sounds)
        {
            s.source =  gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;

            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.isLoop;
            
           
        }


    }

  

    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
       

        if (s == null)
            return;

        
        


       
        s.source.Play();
    }

    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);


        if (s == null)
            return;






        s.source.Stop();
       
    }

    public void Pause(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
            return;
        s.source.Pause();
    }

    public void unPause(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
            return;
        s.source.UnPause();
    }

    public void MuteSfx()
    {
        sfxMutetoggle = !sfxMutetoggle;
        if (sfxMutetoggle == true)
        {

            toggleTag(Sound.SoundTag.sfx, true);
            PlayerPrefs.SetInt("sfxVolume", 0);
           
          
            
        }
        else if (sfxMutetoggle == false)
        {


            toggleTag(Sound.SoundTag.sfx, false);
            PlayerPrefs.SetInt("sfxVolume", 1);


        }
    }

    public void MuteMusic()
    {
        musicMutetoogle = !musicMutetoogle;
        if (musicMutetoogle == true)
        {
            toggleTag(Sound.SoundTag.music, true);
            PlayerPrefs.SetInt("musicVolume", 0);


        }
        else if (musicMutetoogle == false)
        {

            toggleTag(Sound.SoundTag.music, false);
            PlayerPrefs.SetInt("musicVolume", 1);



        }
    }

   
    public List<Sound> GetSounds0fTag(Sound.SoundTag tag)
    {
        var returnlist = new List<Sound>();

        foreach (Sound s in sounds)
        {
            if (s.CompareTag(tag))
                returnlist.Add(s);
        }
        return returnlist;
    }

    public void toggleTag(Sound.SoundTag tag, bool mute)
    {
        foreach (var item in GetSounds0fTag(tag))
        {
            item.mute(mute);
        }
    }

    public void PlayOneShot(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);


        if (s == null)
            return;

        AudioClip clip = s.clip;




        s.source.PlayOneShot(clip);
    }

}
