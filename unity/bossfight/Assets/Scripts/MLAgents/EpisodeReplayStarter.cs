using UnityEngine;
using System.Collections;

/// <summary>
/// Simple helper script to load and start an episode replay.
/// Attach this to a GameObject with EpisodeReplay component, or assign the EpisodeReplay reference.
/// </summary>
public class EpisodeReplayStarter : MonoBehaviour
{
    [Header("Replay Settings")]
    [Tooltip("The episode file name to load (e.g., 'episode_123.json'). Leave empty to use default.")]
    [SerializeField] private string fileName = "episode_0.json";
    
    [Tooltip("Automatically start replay when scene loads")]
    [SerializeField] private bool autoStart = false;
    
    [Tooltip("Reference to EpisodeReplay component. If null, will try to find it.")]
    [SerializeField] private EpisodeReplay replay;
    
    void Start()
    {
        // Wait one frame to ensure all components are initialized
        StartCoroutine(InitializeReplay());
    }
    
    System.Collections.IEnumerator InitializeReplay()
    {
        // Wait one frame
        yield return null;
        
        if (replay == null)
        {
            replay = GetComponent<EpisodeReplay>();
            if (replay == null)
            {
                replay = FindObjectOfType<EpisodeReplay>();
            }
        }
        
        if (replay == null)
        {
            Debug.LogError("EpisodeReplayStarter: Could not find EpisodeReplay component!");
            yield break;
        }
        
        // Load the episode
        if (!string.IsNullOrEmpty(fileName))
        {
            replay.LoadEpisode(fileName);
        }
        
        // Auto-start if enabled
        if (autoStart)
        {
            // Wait one more frame to ensure episode is loaded
            yield return null;
            replay.StartReplay();
        }
    }
    
    // Public method to start replay from code or UI
    public void StartReplay(string episodeFileName = null)
    {
        if (replay == null)
        {
            Debug.LogError("EpisodeReplayStarter: No EpisodeReplay component found!");
            return;
        }
        
        if (!string.IsNullOrEmpty(episodeFileName))
        {
            fileName = episodeFileName;
        }
        
        if (!string.IsNullOrEmpty(fileName))
        {
            replay.LoadEpisode(fileName);
            replay.StartReplay();
        }
    }
}

