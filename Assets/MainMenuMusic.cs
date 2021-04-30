using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuMusic : MonoBehaviour
{
    public string MusicSoundbankName = "Main_Menu";
    // Start is called before the first frame update
    void Start()
    {
        AkSoundEngine.RegisterGameObj(gameObject);
        ActionGameManager.PlayMusic(ActionGameManager.SoundBanksInfo.GetSoundBank(MusicSoundbankName).Id, gameObject);
    }
}
