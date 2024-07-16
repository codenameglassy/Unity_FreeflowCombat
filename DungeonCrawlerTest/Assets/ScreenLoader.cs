using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ScreenLoader : MonoBehaviour
{
    public GameObject loadingBar;

    //Type Text
    public TextMeshProUGUI tmpText;
    public float delay = 0.02f;
    public string textToType;

    private void Start()
    {
        
       
    }
    public void LoadScene(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void LoadLevelAsynchronously(string levelName)
    {
        loadingBar.SetActive(true);
        if (tmpText != null)
        {
            // Clear the text initially
            tmpText.text = "";

            // Start the typewriter effect coroutine
            StartCoroutine(TypeText(textToType));
        }
        Time.timeScale = 1.0f;
        StartCoroutine(Enum_LoadAsynchronously(levelName));
    }

    IEnumerator Enum_LoadAsynchronously(string levelName)
    {

        AsyncOperation operation = SceneManager.LoadSceneAsync(levelName);
        operation.allowSceneActivation = false;
        yield return new WaitForSeconds(4f);
        operation.allowSceneActivation = true;

    }
    public void StartTypewriterEffect(string text)
    {
        textToType = text;
        tmpText.text = "";
        StartCoroutine(TypeText(textToType));
    }

    IEnumerator TypeText(string text)
    {
        foreach (char letter in text.ToCharArray())
        {
            tmpText.text += letter;
            yield return new WaitForSeconds(delay);
        }
    }


}
