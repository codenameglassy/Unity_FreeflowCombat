using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class audiosettings : MonoBehaviour
{
    public Button musicbtn;
    public Button sfxbtn;

    public Sprite musicoff;
    public Sprite musicon;

    public Sprite sfxoff;
    public Sprite sfxon;




    public void musicMute()
    {
        FindObjectOfType<AudioManagerCS>().Play("button");
        FindObjectOfType<AudioManagerCS>().MuteMusic();
    }

    public void sfxMute()
    {
        FindObjectOfType<AudioManagerCS>().Play("button");
        FindObjectOfType<AudioManagerCS>().MuteSfx();
    }

    public void PlayAudioClick()
    {
        FindObjectOfType<AudioManagerCS>().Play("button");
    }

    private void Update()
    {
        if (PlayerPrefs.GetInt("musicVolume") == 0)
        {
            musicbtn.image.sprite = musicoff;

        }
        else if (PlayerPrefs.GetInt("musicVolume") == 1)
        {
            musicbtn.image.sprite = musicon;
        }



        if (PlayerPrefs.GetInt("sfxVolume") == 0)
        {
            sfxbtn.image.sprite = sfxoff;
        }
        else if (PlayerPrefs.GetInt("sfxVolume") == 1)
        {
            sfxbtn.image.sprite = sfxon;
        }



      
    }


    public void Reset_()
    {
        sfxbtn.image.sprite = sfxon;
        musicbtn.image.sprite = musicon;

        PlayerPrefs.SetInt("sfxVolume", 1);
        PlayerPrefs.SetInt("musicVolume", 1);
    }
}
