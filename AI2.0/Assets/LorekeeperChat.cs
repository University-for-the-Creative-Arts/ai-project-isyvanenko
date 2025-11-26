using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;

public class LorekeeperChat : MonoBehaviour
{
    //UI REFERENCES
    [Header("UI References")]
    public TextMeshProUGUI chatLogText;
    public TMP_InputField playerInputField;
    public Button sendButton;
    public TextMeshProUGUI timerText; // For Inference Time logging
    public OllamaClient ollamaClient;


    private const string LOREKEEPER_SYSTEM_PROMPT = 
        "You are RuPaul, you are a famous TV host and a drag queen. Your responses must be brief, glamorous, and relate to gay culture. Do not break character. Keep your answers under 3 sentences.";
    
    private StringBuilder conversationHistory = new StringBuilder();

    void Start()
    {
        // Initial setup and listener assignment
        sendButton.onClick.AddListener(OnSendMessage);
        
        // Initial greeting
        conversationHistory.Append("--- CONVERSATION START ---\n");
        conversationHistory.Append("RuPaul: Welcome, Queen. Are u ready for the big stage?\n");
        chatLogText.text = conversationHistory.ToString();

        if (ollamaClient == null)
        {
             Debug.LogError("OllamaClient reference is missing! Check console.");
             sendButton.interactable = false;
             return;
        }
    }

    public void OnSendMessage()
    {
        string playerMessage = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(playerMessage)) return;

        //Log Player Message to History
        conversationHistory.Append($"YOU: {playerMessage}\n");
        chatLogText.text = conversationHistory.ToString();
        playerInputField.text = ""; 

        sendButton.interactable = false; // Disable button while waiting for AI
        timerText.text = "RuPaul is thinking";

        //Start Local AI Dialogue Generation
        StartCoroutine(
            ollamaClient.GeneratePrompt(
                LOREKEEPER_SYSTEM_PROMPT, 
                playerMessage, 
                OnDialogueGenerated
            )
        );
    }

    //Callback when the Ollama response is received
    private void OnDialogueGenerated(string responseText, float inferenceTime, int tokenCount)
    {
        sendButton.interactable = true;
        
      
        if (responseText.StartsWith("ERROR:"))
        {
            conversationHistory.Append($"ELARA: [Connection Error: Cannot speak now]\n");
            timerText.text = "ERROR: See Console.";
        }
        else
        {
            //Log AI Dialogue
            conversationHistory.Append($"ELARA: {responseText}\n");
            
            
            string log = $"[INFERENCE LOG | Model: {OllamaClient.MODEL_NAME} | Time: {inferenceTime:F2}s | Tokens: {tokenCount}]";
            timerText.text = log;
            Debug.Log(log);
        }

        
        chatLogText.text = conversationHistory.ToString();
    }
}