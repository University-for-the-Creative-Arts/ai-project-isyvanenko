using UnityEngine;
using UnityEngine.Networking;
using System.Collections; // Required for Coroutines
using System.Text;
using System; // Required for Action/Callback

public class OllamaClient : MonoBehaviour
{
    // The local address for the Ollama generate endpoint
    private const string OllamaURL = "http://localhost:11434/api/generate";
    public const string MODEL_NAME = "mistral"; // <--- CHANGE THIS if you used a different model (e.g., "phi-3")

    // Coroutine to handle the asynchronous API call
    public IEnumerator GeneratePrompt(string systemPrompt, string userPrompt, Action<string, float, int> callback)
    {
        // 1. Construct the JSON payload for the Ollama API
        // We set 'stream' to false to get the complete response in one go.
        // We include a 'system' prompt to instruct the AI on its role.
        string jsonPayload = $"{{\"model\": \"{MODEL_NAME}\", \"prompt\": \"{userPrompt}\", \"system\": \"{systemPrompt}\", \"stream\": false}}";
        
        using (UnityWebRequest www = new UnityWebRequest(OllamaURL, "POST"))
        {
            // Set up the raw data body and headers
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            float startTime = Time.time;
            yield return www.SendWebRequest(); // Send the request and PAUSE until response is back

            // 2. Log and Handle Response
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Ollama Error: {www.error}");
                callback?.Invoke($"ERROR: Ollama failed to respond ({www.error})", 0f, 0);
            }
            else
            {
                float inferenceTime = Time.time - startTime;
                string responseText = www.downloadHandler.text;
                
                // --- Simple JSON Parsing (We must parse the Ollama response) ---
                // The JSON response contains the generated text inside the "response" key.
                
                // Find the response field
                int responseKeyIndex = responseText.IndexOf("\"response\":\"");
                if (responseKeyIndex != -1)
                {
                    int startIndex = responseKeyIndex + 12; // Start after "response":"
                    int endIndex = responseText.IndexOf("\"", startIndex); // Find the end quote
                    
                    if (endIndex != -1)
                    {
                        string generatedText = responseText.Substring(startIndex, endIndex - startIndex);
                        
                        // Clean up model-specific remnants (e.g., newlines/extra characters)
                        string cleanedPrompt = generatedText.Trim().Replace("\\n", " ").Replace("\"", "");
                        
                        // 3. Extracting Telemetry Data
                        // The prompt_eval_count is the number of tokens/words processed in the prompt.
                        // We use the simplified playerPrompt length as tokens are complex to count accurately here.
                        int tokenCount = userPrompt.Length; 

                        // 4. Return the result via the callback
                        callback?.Invoke(cleanedPrompt, inferenceTime, tokenCount);
                        yield break;
                    }
                }
                
                // If parsing fails:
                callback?.Invoke("ERROR: Failed to parse Ollama response.", 0f, 0);
            }
        }
    }
}