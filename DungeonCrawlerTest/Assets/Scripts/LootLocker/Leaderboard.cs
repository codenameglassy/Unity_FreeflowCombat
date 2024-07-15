using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using TMPro;

public class Leaderboard : MonoBehaviour
{
    public static Leaderboard instance;

    string leaderboardID = "23459";

    public TextMeshProUGUI playerNamesTxt;
    public TextMeshProUGUI playerScoresTxt;
    public TextMeshProUGUI personalNameAndScoreTxt;

    private void Awake()
    {
        instance = this;
    }

    public void SubmitScore(int scoreToUpload)
    {
        StartCoroutine(SubmitScoreRoutine(scoreToUpload));
    }

    IEnumerator SubmitScoreRoutine(int scoreToUpload)
    {
        bool done = false;
        string playerID = PlayerManager_LootLocker.Instance.GetPlayerID();

        LootLockerSDKManager.SubmitScore(playerID, scoreToUpload, leaderboardID, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Sucessfully uploaded score");
                done = true;
            }
            else
            {
                Debug.Log("Failed" + response.errorData);
                done = true;
            }
        });

        yield return new WaitWhile(() => done == false);
    }

    public IEnumerator FetechLeaderboardRoutine()
    {
        bool done = false;
        LootLockerSDKManager.GetScoreList(leaderboardID, 8, 0, (response) =>
        {
            if (response.success)
            {
                Debug.Log("Sucessfully feteched leaderboard");

                string tempPlayerNames = "Name\n <br>";
                string tempPlayerScores = "Scores\n <br>";

                LootLockerLeaderboardMember[] member = response.items;

                for (int i = 0; i < member.Length; i++)
                {
                    tempPlayerNames += member[i].rank + ". ";
                    if(member[i].player.name != "")
                    {
                        tempPlayerNames += member[i].player.name;
                    }
                    else
                    {
                        tempPlayerNames += member[i].player.id;
                    }
                    tempPlayerScores += member[i].score + "\n";
                    tempPlayerNames += "\n";
                }
                done = true;
                playerNamesTxt.text = tempPlayerNames;
                playerScoresTxt.text = tempPlayerScores;
            }
            else
            {
                Debug.Log("Failed fetching leaderboard" + response.errorData);
                done = true;
            }
        });
        yield return new WaitWhile(() => done == false);
    }

    public void FetechPersonalScore()
    {
        StartCoroutine(FetechPersonalScoreRoutine());
    }

    public IEnumerator FetechPersonalScoreRoutine()
    {
        bool done = false;
        LootLockerSDKManager.GetMemberRank(leaderboardID, PlayerManager_LootLocker.Instance.GetPlayerID(), (response) =>
        {
            if (response.success)
            {
                Debug.Log("Player's score: " + response.score);
                Debug.Log("Player's rank: " + response.rank);
                personalNameAndScoreTxt.text = "Your rank: " + response.rank + ". " + response.player.name + " - " + response.score;
                done = true;
            }
            else
            {
                Debug.Log("Failed to get player's score");
                done = true;
            }
        });
        yield return new WaitWhile(() => done == false);
    }
}
