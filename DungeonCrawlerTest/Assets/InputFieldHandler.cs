using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputFieldHandler : MonoBehaviour
{
    public TMP_InputField inputField; // Reference to the TextMeshPro input field
    private string inputValue; // Variable to store the input value

    private const string PlayerPrefsKey = "InputFieldValue"; // Key for PlayerPrefs
    public Button playBtn;
    // Start is called before the first frame update
    void Start()
    {
        // Optionally, you can initialize inputValue with the current input field text
        inputValue = inputField.text;

        // Load the saved value from PlayerPrefs (if exists) and set it to the input field
        LoadInputValue();

        // Add listener to handle the input value change
        inputField.onValueChanged.AddListener(OnInputValueChanged);
    }

    private void FixedUpdate()
    {
        if (inputField.text.Length <= 0)
        {
            playBtn.interactable = false;
        }
        else if (inputField.text.Length > 0)
        {
            playBtn.interactable = true;
        }
    }
    // Method to handle the input value change
    void OnInputValueChanged(string newValue)
    {
        inputValue = newValue;
        Debug.Log("Input value changed to: " + inputValue);
    }

    // Method to retrieve the stored input value
    public string GetInputValue()
    {
        return inputValue;
    }

    // Method to save the input value to PlayerPrefs
    public void SaveInputValue()
    {
        PlayerPrefs.SetString(PlayerPrefsKey, inputValue);
        PlayerPrefs.Save();
        Debug.Log("Input value saved: " + inputValue);
        FindObjectOfType<ScreenLoader>().LoadScene("SampleScene");
        //ScoreManager.instance.SumbitScore();
    }

    // Method to load the input value from PlayerPrefs
    public void LoadInputValue()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            inputValue = PlayerPrefs.GetString(PlayerPrefsKey);
            inputField.text = inputValue;
            Debug.Log("Input value loaded: " + inputValue);
        }
    }
}
