using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

public sealed class AvatarManager : SingletonInstance<AvatarManager>
{
    [DllImport("__Internal")]
    private static extern void SendStringToPage(string message);

    [DllImport("__Internal")]
    private static extern void StartWebGLMicrophone();

    [DllImport("__Internal")]
    private static extern void StopWebGLMicrophone();

    [SerializeField]
    private AudioSource avatarAudioSource;

    [SerializeField]
    private UnityEvent onInitializationCompletedEvent;

    private void Start()
    {
        if (avatarAudioSource == null)
            throw new ArgumentNullException(nameof(avatarAudioSource), "Cannot be null.");
    }

    public void OnInitializationCompleted()
    {
        onInitializationCompletedEvent?.Invoke();
        Debug.Log($"~Recieved Initialization event in Unity from the web.");
    }

    public void StartSpeaking()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
StartWebGLMicrophone();
#else
        Debug.LogError($"~Speech recognition is not supported inside the Unity editor.");
#endif
    }

    public void StopSpeaking()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
StopWebGLMicrophone();
#else
        Debug.LogError($"~Speech recognition is not supported inside the Unity editor.");
#endif
    }

    public void StopSpeaking(string speech)
    {
#if !UNITY_EDITOR && UNITY_WEBGL
StopWebGLMicrophone();
SendUserSpeechToLLM(speech);
#else
        Debug.LogError($"~Speech recognition is not supported inside the Unity editor.");
#endif
    }

    public void OnSpeechRecognizedEvent(string speechResults)
    {
        Debug.Log($"~Recieved recognized speech text: {speechResults}.");
    }

    public void OnLLMResponse(string response)
         => SendStringToPage(response);

    public void OnSpeechGeneratedEvent(string speechData)
    {
        if (string.IsNullOrWhiteSpace(speechData)) return;

        Debug.Log($"~Recieved speech data from outside of unity: {speechData}");

        try
        {
            string[] parts = speechData.Split('|');

            if (parts.Length < 2) return;

            int sampleRate = int.Parse(parts[0]);
            string[] sampleStrings = parts[1].Split(",");

            float[] audioData = new float[sampleStrings.Length];

            for (int i = 0; i < sampleStrings.Length; i++)
            {
                string token = sampleStrings[i].Trim();

                if(!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                {
                    audioData[i] = 0f;
                    continue;
                }

                audioData[i] = result;
            }

            AudioClip speechAudioClip = AudioClip.Create("AvatarVoice", audioData.Length, 1, sampleRate, false);
            speechAudioClip.SetData(audioData, 0);

            avatarAudioSource.clip = speechAudioClip;
            avatarAudioSource.Play();
        }
        catch (Exception exception)
        {
            Debug.LogError($"Generated speech playback failed with exception: {exception.Message}");
        }
    }

    public void SendUserSpeechToLLM(string message)
        => SendStringToPage(message);
}
