using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dan.Main;
using TMPro;
public class Leaderboard : MonoBehaviour
{
    public static Leaderboard instance;
    public string publicKey;

    public List<TextMeshProUGUI> leaderboardScores = new List<TextMeshProUGUI>();
    public TextMeshProUGUI myLeaderboardScore;
    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
       /* GetLeaderBoard();
        string PlayerPrefsKey = "InputFieldValue";
        if (PlayerPrefs.HasKey(PlayerPrefsKey))
            StartCoroutine(Enum_UpdateScore());*/
    }

    IEnumerator Enum_UpdateScore()
    {
       
        while (true)
        {
            yield return new WaitForSeconds(8f);
            UpdateLeaderboard();

            if(GameControl.instance.isGameOver = true)
            {
                yield break;
            }
        }
    }
    public void GetLeaderBoard()
    {
        StartCoroutine(Enum_GetLeaderboard());
    }

    IEnumerator Enum_GetLeaderboard()
    {
        yield return null;
        LeaderboardCreator.GetLeaderboard(publicKey, ((msg) =>
        {
            int loopLength = (msg.Length < leaderboardScores.Count) ? msg.Length : leaderboardScores.Count;
            for (int i = 0; i < loopLength; i++)
            {
                leaderboardScores[i].text =msg[i].Rank + ". " +  msg[i].Username + " - " + msg[i].Score.ToString();
                //names[i].text = msg[i].Rank + ". " + msg[i].Username;
                //scores[i].text = msg[i].Score.ToString();
            }
        }));

        yield return new WaitForSeconds(.2f);
        GetPersonalEntry();
    }

    public void SetLeaderboardEntry(string username, int score)
    {
        LeaderboardCreator.UploadNewEntry(publicKey, username, score, ((msg) =>
        {
            //badword ??
            GetLeaderBoard();
        }));
    }

    public void UpdateLeaderboard()
    {
        string currentUsername = PlayerPrefs.GetString("InputFieldValue");
        SetLeaderboardEntry(currentUsername, ScoreManager.instance.GetCurrentScore());
        GetLeaderBoard();
    }


    public void GetPersonalEntry()
    {
        LeaderboardCreator.GetPersonalEntry(publicKey, ((msg) =>
        {
            Debug.Log("My username - " + msg.Username);
            Debug.Log("My Score - " + msg.Score);
            Debug.Log("My Rank - " + msg.Rank);
            myLeaderboardScore.text = msg.Rank + ". " + msg.Username + " - " + msg.Score.ToString();
            //currentPlayerUsername.text = msg.Rank + ". " + msg.Username;
            //currentPlayerScore.text = msg.Score.ToString();
        }));
    }
}
