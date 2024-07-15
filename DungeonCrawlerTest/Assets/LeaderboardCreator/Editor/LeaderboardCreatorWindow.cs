using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dan;
using UnityEditor;
using UnityEngine;

namespace LeaderboardCreatorEditor
{
    public class LeaderboardCreatorWindow : EditorWindow
    {
        [System.Serializable]
        private class SavedLeaderboard
        {
            public string name, publicKey, secretKey;
            
            public SavedLeaderboard(string name, string publicKey, string secretKey)
            {
                this.name = name;
                this.publicKey = publicKey;
                this.secretKey = secretKey;
            }
        }
        
        [System.Serializable]
        private struct SavedLeaderboardList
        {
            public List<SavedLeaderboard> leaderboards;
        }

        private const string ITCH_PAGE_URL = "https://danqzq.itch.io/leaderboard-creator";
        private const string AUTHOR_URL = "https://www.danqzq.games";
        private const string VERSION = "2.8";

        private static bool _isAddLeaderboardMenuOpen;
        private static string _name, _publicKey, _secretKey;
        private static Vector2 _scrollPos;

        private static SavedLeaderboardList _savedLeaderboardList;

        private static GUIStyle _titleStyle;
        
        private static int _menuOpened;
        
        private static LeaderboardCreatorConfig Config => Resources.Load<LeaderboardCreatorConfig>("LeaderboardCreatorConfig");

        [MenuItem("Leaderboard Creator/My Leaderboards")]
        private static void ShowWindow()
        {
            var window = GetWindow<LeaderboardCreatorWindow>();
            window.minSize = new Vector2(400, 475);
            window.titleContent = new GUIContent("Leaderboard Creator");
            window.Show();

            CheckVersion();
        }

        private static void CheckVersion()
        {
            var request = UnityEngine.Networking.UnityWebRequest.Get("https://lcv2-server.danqzq.games/version");
            var operation = request.SendWebRequest();
            Log("Checking for updates...");
            operation.completed += _ =>
            {
                if (request.responseCode != 200) return;
                var response = request.downloadHandler.text;
                if (response == VERSION)
                {
                    Log("<color=green><b>Leaderboard Creator is up to date!</b></color>");
                    return;
                }
                
                Log("<color=red><b>There is a new version of Leaderboard Creator available!</b></color>");
                
                var dialog = EditorUtility.DisplayDialog("Leaderboard Creator", 
                    "There is a new version of Leaderboard Creator available. Download it now?", "Yes", "No");
                if (!dialog) return;
                
                Application.OpenURL(ITCH_PAGE_URL);
            };
        }

        private void OnBecameVisible()
        {
            _titleStyle = new GUIStyle
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState {textColor = Color.white}
            };
            _savedLeaderboardList = GetSavedLeaderboardList();
        }

        private static SavedLeaderboardList GetSavedLeaderboardList()
        {
            var path = AssetDatabase.GetAssetPath(Config.editorOnlyLeaderboardsFile);
            var file = new System.IO.StreamReader(path);
            var json = file.ReadToEnd();
            file.Close();

            if (string.IsNullOrEmpty(json))
            {
                SaveLeaderboardList();
                return new SavedLeaderboardList {leaderboards = new List<SavedLeaderboard>()};
            }
            
            var savedLeaderboardList = JsonUtility.FromJson<SavedLeaderboardList>(json);
            return savedLeaderboardList;
        }

        private static void SaveLeaderboardList()
        {
            _savedLeaderboardList.leaderboards ??= new List<SavedLeaderboard>();
            
            var path = AssetDatabase.GetAssetPath(Config.editorOnlyLeaderboardsFile);
            var json = JsonUtility.ToJson(_savedLeaderboardList);
            
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }

        private void OnGUI()
        {
            _menuOpened = GUILayout.Toolbar(_menuOpened, new[] {"My Leaderboards", "Settings"});

            switch (_menuOpened)
            {
                case 0:
                    OnMyLeaderboardsGUI();
                    break;
                case 1:
                    OnSettingsGUI();
                    break;
            }
            
            DrawSeparator();
            
            if (GUILayout.Button("<color=#2a9df4>Made by @danqzq</color>",
                    new GUIStyle{alignment = TextAnchor.LowerRight, richText = true}))
                Application.OpenURL(AUTHOR_URL);
            
            GUILayout.Label($"<color=white>v{VERSION}</color>", new GUIStyle{alignment = TextAnchor.LowerRight});
        }

        private void OnMyLeaderboardsGUI()
        {
            DisplayLeaderboardsMenu();

            if (!_isAddLeaderboardMenuOpen && GUILayout.Button("Enter New Leaderboard"))
                _isAddLeaderboardMenuOpen = true;
            
            if (_isAddLeaderboardMenuOpen) DisplayEnterNewLeaderboardMenu();

            if (GUILayout.Button("Save to C# Script")) 
                SaveLeaderboardsToScript();

            if (GUILayout.Button("Manage Leaderboards"))
                Application.OpenURL(ITCH_PAGE_URL);
        }

        private void OnSettingsGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Settings", _titleStyle);
            GUILayout.Space(10);
            
            var oldAuthSaveMode = Config.authSaveMode;
            Config.authSaveMode = (AuthSaveMode) EditorGUILayout.EnumPopup("Authorization Save Mode", Config.authSaveMode);
            if (oldAuthSaveMode != Config.authSaveMode)
                EditorUtility.SetDirty(Config);
            
            if (Config.authSaveMode == AuthSaveMode.PersistentDataPath)
            {
                var oldFileName = Config.fileName;
                Config.fileName = EditorGUILayout.TextField("File Name", Config.fileName);
                if (Config.fileName.Contains("/")) 
                    Config.fileName = Config.fileName.Replace("/", "");
                if (oldFileName != Config.fileName)
                    EditorUtility.SetDirty(Config);
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                    EditorGUILayout.HelpBox("Saving to persistent data path may not work on WebGL builds.", MessageType.Warning);
            }
            
            GUILayout.Space(20);
            
            var oldIsUpdateLogsEnabled = Config.isUpdateLogsEnabled;
            Config.isUpdateLogsEnabled = GUILayout.Toggle(Config.isUpdateLogsEnabled, "Enable Update Logs");
            if (oldIsUpdateLogsEnabled != Config.isUpdateLogsEnabled)
                EditorUtility.SetDirty(Config);
        }

        private static void DrawSeparator()
        {
            GUILayout.Space(10);
            var rect = EditorGUILayout.BeginHorizontal();
            Handles.color = Color.gray;
            Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private static void DisplayLeaderboardsMenu()
        {
            if (_savedLeaderboardList.leaderboards.Count == 0)
            {
                GUILayout.Label("You don't have any saved leaderboards.");
                return;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200));
            for (var i = 0; i < _savedLeaderboardList.leaderboards.Count; i++)
            {
                GUILayout.Space(10);
                GUILayout.Label("Leaderboard #" + (i + 1), EditorStyles.boldLabel);
                
                var savedLeaderboard = _savedLeaderboardList.leaderboards[i];
                savedLeaderboard.name = EditorGUILayout.TextField("Name", savedLeaderboard.name);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy Public Key"))
                    EditorGUIUtility.systemCopyBuffer = savedLeaderboard.publicKey;
                if (GUILayout.Button("Copy Secret Key"))
                    EditorGUIUtility.systemCopyBuffer = savedLeaderboard.secretKey;
                GUILayout.EndHorizontal();

                if (!GUILayout.Button("Forget Leaderboard"))
                    continue;

                _savedLeaderboardList.leaderboards.Remove(savedLeaderboard);
                SaveLeaderboardList();
                break;
            }

            GUILayout.EndScrollView();
        }

        private static void DisplayEnterNewLeaderboardMenu()
        {
            DrawSeparator();
            GUILayout.Label("Enter New Leaderboard", _titleStyle);

            _name      = EditorGUILayout.TextField("Name", _name);
            _publicKey = EditorGUILayout.TextField("Public Key", _publicKey);
            _secretKey = EditorGUILayout.TextField("Secret Key", _secretKey);

            if (GUILayout.Button("Add Leaderboard"))
                EnterNewLeaderboard();
            
            if (GUILayout.Button("Cancel"))
                _isAddLeaderboardMenuOpen = false;
                
            DrawSeparator();
        }

        private static void EnterNewLeaderboard()
        {
            if (string.IsNullOrEmpty(_publicKey) || string.IsNullOrEmpty(_secretKey))
            {
                EditorUtility.DisplayDialog("Leaderboard Creator Error", "Please fill all the fields.", "OK");
                return;
            }
            
            if (!ValidateLeaderboardName(_name)) return;

            _savedLeaderboardList = GetSavedLeaderboardList();
            if (_savedLeaderboardList.leaderboards.Exists(l => l.name == _name))
            {
                EditorUtility.DisplayDialog("Leaderboard Creator Error", "You already have a leaderboard with that name.", "OK");
                return;
            }
                
            _savedLeaderboardList.leaderboards.Add(new SavedLeaderboard(_name, _publicKey, _secretKey));
            SaveLeaderboardList();
                
            _name = _publicKey = _secretKey = "";
        }

        private static bool ValidateLeaderboardName(string leaderboardName)
        {
            if (string.IsNullOrEmpty(leaderboardName))
            {
                EditorUtility.DisplayDialog("Leaderboard Creator Error", "Please enter a name.", "OK");
                return false;
            }
            
            if (!Regex.IsMatch(leaderboardName, @"^[a-zA-Z0-9_]+$"))
            {
                EditorUtility.DisplayDialog("Leaderboard Creator Error", "The name can only contain alphabetical letters, numbers and underscores.", "OK");
                return false;
            }

            if (!Regex.IsMatch(leaderboardName, @"^[0-9]"))
                return true;
            
            EditorUtility.DisplayDialog("Leaderboard Creator Error", "The name cannot start with a number.", "OK");
            return false;
        }

        private static void SaveLeaderboardsToScript()
        {
            if (_savedLeaderboardList.leaderboards.Any(savedLeaderboard => !ValidateLeaderboardName(savedLeaderboard.name)))
                return;
            
            SaveLeaderboardList();

            var path = AssetDatabase.GetAssetPath(Config.leaderboardsFile);
            var file = new System.IO.StreamWriter(path);

            file.WriteLine("namespace Dan.Main");
            file.WriteLine("{");
            file.WriteLine("    public static class Leaderboards");
            file.WriteLine("    {");

            foreach (var savedLeaderboard in _savedLeaderboardList.leaderboards)
            {
                file.WriteLine($"        public static LeaderboardReference {savedLeaderboard.name} = " +
                               $"new LeaderboardReference(\"{savedLeaderboard.publicKey}\");");
            }
                
            file.WriteLine("    }");
            file.WriteLine("}");
            file.Close();
            AssetDatabase.Refresh();
        }
        
        private static void Log(string message)
        {
            if (!Config.isUpdateLogsEnabled) return;
            Debug.Log($"[Leaderboard Creator] {message}");
        }
    }
}