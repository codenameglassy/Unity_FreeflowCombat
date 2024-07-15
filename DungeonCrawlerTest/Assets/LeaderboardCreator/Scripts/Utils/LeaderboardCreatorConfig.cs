using UnityEngine;

namespace Dan
{
    public enum AuthSaveMode
    {
        PlayerPrefs,
        PersistentDataPath,
        Unhandled
    }
    
    public class LeaderboardCreatorConfig : ScriptableObject
    {
        public AuthSaveMode authSaveMode = AuthSaveMode.PlayerPrefs;
        public string fileName = "leaderboard-creator-guid.txt";
        public bool isUpdateLogsEnabled = true;
        
        public TextAsset leaderboardsFile;
#if UNITY_EDITOR
        public TextAsset editorOnlyLeaderboardsFile;
#endif
    }
}