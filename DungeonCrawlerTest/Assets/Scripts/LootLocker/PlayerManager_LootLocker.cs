using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
public class PlayerManager_LootLocker : MonoBehaviour
{
    public static PlayerManager_LootLocker Instance { get; private set; }
    private string playerID;

    private void Awake()
    {
        Instance = this;
    }


    void Singleton()
    {
        // Check if an instance already exists and destroy it if it does
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            // If no instance exists, set this as the instance and make it persist across scenes
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(SetupRoutine());
    }

    public void SetPlayerName(string name)
    {
        //string currentUsername = PlayerPrefs.GetString("InputFieldValue");
        LootLockerSDKManager.SetPlayerName(name, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Sucessfully Set Player Name");
                //FindObjectOfType<ScreenLoader>().LoadScene("SampleScene");
            }
            else
            {
                Debug.Log("Failed to change name" + response.errorData);
                //FindObjectOfType<ScreenLoader>().LoadScene("SampleScene");
            }
        });
    }

   

    IEnumerator SetupRoutine()
    {
        yield return LoginRoutine(); //login
        yield return Leaderboard.instance.FetechLeaderboardRoutine(); // get leaderboard
        yield return SetPlayerNameRoutine(); //set name
    }

    IEnumerator LoginRoutine()
    {
        bool done = false;
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success)
            {
                Debug.Log("Player has logged in");
                playerID = response.player_id.ToString();
                done = true;
            }
            else
            {
                Debug.Log("Failed Session");
                done = true;
            }
        });

        yield return new WaitWhile(() => done == false);

    }
    IEnumerator SetPlayerNameRoutine()
    {
        bool done = false;
        string currentUsername = PlayerPrefs.GetString("InputFieldValue");
        LootLockerSDKManager.SetPlayerName(currentUsername, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Sucessfully Set Player Name");

            }
            else
            {
                Debug.Log("Failed to change name" + response.errorData);

            }
        });
        yield return new WaitWhile(() => done == false);
    }

    public string GetPlayerID()
    {
        return playerID;
    }

}
