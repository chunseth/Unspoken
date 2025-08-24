using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages and tracks the interaction status of all hint components in the game.
/// Provides centralized access to hint interaction data and can be used for
/// save/load systems, achievement tracking, or game progression logic.
/// </summary>
public class HintManager : MonoBehaviour
{
    [Header("Hint Tracking")]
    [Tooltip("List of all Puzzle1Hint components being tracked")]
    public List<Puzzle1Hint> trackedPuzzle1Hints = new List<Puzzle1Hint>();
    [Tooltip("List of all Puzzle2Hint components being tracked")]
    public List<Puzzle2Hint> trackedPuzzle2Hints = new List<Puzzle2Hint>();
    
    [Header("Debug Information")]
    [Tooltip("Shows debug information in the inspector")]
    public bool showDebugInfo = true;
    
    [Header("Auto Discovery")]
    [Tooltip("Automatically find all hint components in the scene on start")]
    public bool autoDiscoverHints = true;
    
    [Header("UI Display")]
    [Tooltip("Hint 1 mini image component on the canvas")]
    public UnityEngine.UI.Image hint1MiniImage;
    [Tooltip("Hint 2 mini image component on the canvas")]
    public UnityEngine.UI.Image hint2MiniImage;
    [Tooltip("Hint 3 mini image component on the canvas")]
    public UnityEngine.UI.Image hint3MiniImage;
    [Tooltip("Hint 4 mini image component on the canvas")]
    public UnityEngine.UI.Image hint4MiniImage;
    
    // Singleton pattern for easy access
    public static HintManager Instance { get; private set; }
    
    // Events for other systems to listen to
    public System.Action<Puzzle1Hint> OnPuzzle1HintTriggered;
    public System.Action<Puzzle1Hint> OnPuzzle1HintClosed;
    public System.Action<Puzzle2Hint> OnPuzzle2HintTriggered;
    public System.Action<Puzzle2Hint> OnPuzzle2HintClosed;
    
    // UI references
    private Canvas targetCanvas;
    private Dictionary<string, UnityEngine.UI.Image> hintMiniImages = new Dictionary<string, UnityEngine.UI.Image>();
    
    private void Awake()
    {
        // Singleton pattern setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to scene load events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Called when a new scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"HintManager: Scene loaded - {scene.name}");
        
        // Clear tracked hints for new scene
        trackedPuzzle1Hints.Clear();
        trackedPuzzle2Hints.Clear();
        
        // Immediately try to reset hint mini images
        ForceResetHintMiniImagesByName();
        
        // Use a coroutine to setup UI references after a short delay
        StartCoroutine(SetupUIAfterSceneLoad());
        
        // Also schedule another reset after a longer delay to ensure UI is ready
        StartCoroutine(DelayedResetHintMiniImages());
    }
    
    /// <summary>
    /// Coroutine to setup UI references after scene load with delay
    /// </summary>
    private System.Collections.IEnumerator SetupUIAfterSceneLoad()
    {
        // Wait a short time for UI components to be created
        yield return new WaitForSeconds(0.1f);
        
        // Try to setup UI references multiple times if needed
        int maxRetries = 5;
        int currentRetry = 0;
        
        while (currentRetry < maxRetries)
        {
            // Re-setup UI references for the new scene
            SetupUIReferences();
            SetupHintMiniImages();
            
            // Check if we found any hint mini images
            if (hintMiniImages.Count > 0)
            {
                Debug.Log($"HintManager: Successfully found {hintMiniImages.Count} hint mini images on attempt {currentRetry + 1}");
                break;
            }
            
            currentRetry++;
            if (currentRetry < maxRetries)
            {
                Debug.Log($"HintManager: No hint mini images found, retrying in 0.1s (attempt {currentRetry + 1}/{maxRetries})");
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Reset hint mini images to transparent state
        MakeAllMiniImagesTransparent();
        
        // Reset all hint trigger states for fresh start
        ResetAllHintStates();
        
        // Re-discover hints in the new scene
        if (autoDiscoverHints)
        {
            DiscoverAllHints();
        }
        
        Debug.Log("HintManager: UI setup complete after scene load");
    }
    
    /// <summary>
    /// Coroutine to reset hint mini images after a longer delay
    /// </summary>
    private System.Collections.IEnumerator DelayedResetHintMiniImages()
    {
        // Wait longer for UI to be fully ready
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("HintManager: Performing delayed reset of hint mini images");
        ForceResetHintMiniImagesByName();
        
        // Wait a bit more and try again
        yield return new WaitForSeconds(0.5f);
        Debug.Log("HintManager: Performing final reset of hint mini images");
        ForceResetHintMiniImagesByName();
    }
    
    void Start()
    {
        // Set up UI references
        SetupUIReferences();
        
        if (autoDiscoverHints)
        {
            DiscoverAllHints();
        }
        
        // Set up hint mini images dictionary
        SetupHintMiniImages();
        
        // Make all mini images transparent initially
        MakeAllMiniImagesTransparent();
        
        // Reset all hint trigger states for fresh start
        ResetAllHintStates();
        
        // Subscribe to hint events
        SubscribeToHintEvents();
    }
    
    /// <summary>
    /// Automatically finds all Puzzle1Hint and Puzzle2Hint components in the scene
    /// </summary>
    public void DiscoverAllHints()
    {
        // Discover Puzzle1Hints
        Puzzle1Hint[] puzzle1HintsInScene = FindObjectsOfType<Puzzle1Hint>();
        
        foreach (Puzzle1Hint hint in puzzle1HintsInScene)
        {
            if (!trackedPuzzle1Hints.Contains(hint))
            {
                trackedPuzzle1Hints.Add(hint);
                Debug.Log($"HintManager: Discovered Puzzle1Hint '{hint.name}'");
            }
        }
        
        // Discover Puzzle2Hints
        Puzzle2Hint[] puzzle2HintsInScene = FindObjectsOfType<Puzzle2Hint>();
        
        foreach (Puzzle2Hint hint in puzzle2HintsInScene)
        {
            if (!trackedPuzzle2Hints.Contains(hint))
            {
                trackedPuzzle2Hints.Add(hint);
                Debug.Log($"HintManager: Discovered Puzzle2Hint '{hint.name}'");
            }
        }
        
        Debug.Log($"HintManager: Total hints discovered: {trackedPuzzle1Hints.Count} Puzzle1Hints, {trackedPuzzle2Hints.Count} Puzzle2Hints");
    }
    
    /// <summary>
    /// Set up UI references and find canvas if not assigned
    /// </summary>
    private void SetupUIReferences()
    {
        // Always find and assign GameCanvas
        GameObject gameCanvasObj = GameObject.Find("GameCanvas");
        if (gameCanvasObj != null)
        {
            targetCanvas = gameCanvasObj.GetComponent<Canvas>();
            if (targetCanvas != null)
            {
                Debug.Log("HintManager: Found and assigned GameCanvas automatically.");
            }
            else
            {
                Debug.LogError("HintManager: GameCanvas found but doesn't have Canvas component!");
            }
        }
        else
        {
            Debug.LogError("HintManager: GameCanvas not found in scene! Please ensure there is a Canvas named 'GameCanvas' in the scene.");
        }
        
        // Try to find mini image components if not assigned
        if (hint1MiniImage == null)
        {
            hint1MiniImage = FindHintMiniImage("hint1");
            if (hint1MiniImage != null)
            {
                Debug.Log("HintManager: Found Hint1MiniImage automatically");
            }
            else
            {
                Debug.LogWarning("HintManager: Hint1MiniImage is not assigned and could not be found! Please assign the hint1 mini image in the inspector.");
            }
        }
        
        if (hint2MiniImage == null)
        {
            hint2MiniImage = FindHintMiniImage("hint2");
            if (hint2MiniImage != null)
            {
                Debug.Log("HintManager: Found Hint2MiniImage automatically");
            }
            else
            {
                Debug.LogWarning("HintManager: Hint2MiniImage is not assigned and could not be found! Please assign the hint2 mini image in the inspector.");
            }
        }
        
        if (hint3MiniImage == null)
        {
            hint3MiniImage = FindHintMiniImage("hint3");
            if (hint3MiniImage != null)
            {
                Debug.Log("HintManager: Found Hint3MiniImage automatically");
            }
            else
            {
                Debug.LogWarning("HintManager: Hint3MiniImage is not assigned and could not be found! Please assign the hint3 mini image in the inspector.");
            }
        }
        
        if (hint4MiniImage == null)
        {
            hint4MiniImage = FindHintMiniImage("hint4");
            if (hint4MiniImage != null)
            {
                Debug.Log("HintManager: Found Hint4MiniImage automatically");
            }
            else
            {
                Debug.LogWarning("HintManager: Hint4MiniImage is not assigned and could not be found! Please assign the hint4 mini image in the inspector.");
            }
        }
    }
    
    /// <summary>
    /// Helper method to find hint mini images by various naming patterns
    /// </summary>
    /// <param name="hintKey">The hint key (hint1, hint2, etc.)</param>
    /// <returns>The found Image component or null</returns>
    private UnityEngine.UI.Image FindHintMiniImage(string hintKey)
    {
        // Try different naming patterns
        string[] possibleNames = {
            $"{hintKey}MiniImage",
            $"Hint{hintKey.Substring(4)}MiniImage", // hint1 -> Hint1MiniImage
            $"{hintKey}Image",
            $"Hint{hintKey.Substring(4)}Image", // hint1 -> Hint1Image
            $"{hintKey}",
            $"Hint{hintKey.Substring(4)}" // hint1 -> Hint1
        };
        
        foreach (string name in possibleNames)
        {
            UnityEngine.UI.Image foundImage = GameObject.Find(name)?.GetComponent<UnityEngine.UI.Image>();
            if (foundImage != null)
            {
                Debug.Log($"HintManager: Found {hintKey} mini image with name '{name}'");
                return foundImage;
            }
        }
        
        // If not found by name, try to find by tag
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("HintMiniImage");
        foreach (GameObject obj in taggedObjects)
        {
            if (obj.name.ToLower().Contains(hintKey.ToLower()))
            {
                UnityEngine.UI.Image foundImage = obj.GetComponent<UnityEngine.UI.Image>();
                if (foundImage != null)
                {
                    Debug.Log($"HintManager: Found {hintKey} mini image by tag with name '{obj.name}'");
                    return foundImage;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Set up the hint mini images dictionary
    /// </summary>
    private void SetupHintMiniImages()
    {
        hintMiniImages.Clear();
        
        // Add hint mini images to dictionary if they exist
        if (hint1MiniImage != null)
        {
            hintMiniImages["hint1"] = hint1MiniImage;
            Debug.Log("HintManager: Added hint1MiniImage to tracking");
        }
        
        if (hint2MiniImage != null)
        {
            hintMiniImages["hint2"] = hint2MiniImage;
            Debug.Log("HintManager: Added hint2MiniImage to tracking");
        }
        
        if (hint3MiniImage != null)
        {
            hintMiniImages["hint3"] = hint3MiniImage;
            Debug.Log("HintManager: Added hint3MiniImage to tracking");
        }
        
        if (hint4MiniImage != null)
        {
            hintMiniImages["hint4"] = hint4MiniImage;
            Debug.Log("HintManager: Added hint4MiniImage to tracking");
        }
        
        Debug.Log($"HintManager: Set up {hintMiniImages.Count} hint mini images");
    }
    
    /// <summary>
    /// Make all mini images transparent initially
    /// </summary>
    private void MakeAllMiniImagesTransparent()
    {
        foreach (var kvp in hintMiniImages)
        {
            if (kvp.Value != null)
            {
                Color transparentColor = kvp.Value.color;
                transparentColor.a = 0f; // Make completely transparent
                kvp.Value.color = transparentColor;
                Debug.Log($"HintManager: Made {kvp.Key} mini image transparent");
            }
        }
    }
    
    /// <summary>
    /// Public method to force reset all hint mini images to transparent
    /// </summary>
    public void ForceResetMiniImages()
    {
        // First try the normal approach
        MakeAllMiniImagesTransparent();
        
        // Then try to find and reset hint mini images directly by name
        ForceResetHintMiniImagesByName();
        
        Debug.Log("HintManager: Force reset all mini images to transparent");
    }
    
    /// <summary>
    /// Force reset hint mini images by directly finding them by name
    /// </summary>
    private void ForceResetHintMiniImagesByName()
    {
        string[] hintNames = { "hint1", "hint2", "hint3", "hint4" };
        
        foreach (string hintName in hintNames)
        {
            // Try multiple naming patterns
            string[] possibleNames = {
                $"{hintName}MiniImage",
                $"Hint{hintName.Substring(4)}MiniImage", // hint1 -> Hint1MiniImage
                $"{hintName}Image",
                $"Hint{hintName.Substring(4)}Image", // hint1 -> Hint1Image
                $"{hintName}",
                $"Hint{hintName.Substring(4)}" // hint1 -> Hint1
            };
            
            foreach (string name in possibleNames)
            {
                GameObject foundObject = GameObject.Find(name);
                if (foundObject != null)
                {
                    UnityEngine.UI.Image foundImage = foundObject.GetComponent<UnityEngine.UI.Image>();
                    if (foundImage != null)
                    {
                        // Force the image to be transparent
                        Color transparentColor = foundImage.color;
                        transparentColor.a = 0f;
                        foundImage.color = transparentColor;
                        Debug.Log($"HintManager: Force reset {name} to transparent");
                        break; // Found this hint, move to next
                    }
                }
            }
        }
        
        // Also search for any UI Images that might be hint images
        ForceResetAllPotentialHintImages();
    }
    
    /// <summary>
    /// Force reset all UI Images that might be hint images
    /// </summary>
    private void ForceResetAllPotentialHintImages()
    {
        UnityEngine.UI.Image[] allImages = FindObjectsOfType<UnityEngine.UI.Image>();
        
        foreach (UnityEngine.UI.Image image in allImages)
        {
            string name = image.name.ToLower();
            if (name.Contains("hint") && (name.Contains("mini") || name.Contains("image")))
            {
                // Force this image to be transparent
                Color transparentColor = image.color;
                transparentColor.a = 0f;
                image.color = transparentColor;
                Debug.Log($"HintManager: Force reset potential hint image '{image.name}' to transparent");
            }
        }
    }
    
    /// <summary>
    /// Manually add a Puzzle1Hint to the tracking system
    /// </summary>
    /// <param name="hint">The Puzzle1Hint component to track</param>
    public void AddPuzzle1Hint(Puzzle1Hint hint)
    {
        if (hint != null && !trackedPuzzle1Hints.Contains(hint))
        {
            trackedPuzzle1Hints.Add(hint);
            Debug.Log($"HintManager: Added Puzzle1Hint '{hint.name}' to tracking");
        }
    }
    
    /// <summary>
    /// Manually add a Puzzle2Hint to the tracking system
    /// </summary>
    /// <param name="hint">The Puzzle2Hint component to track</param>
    public void AddPuzzle2Hint(Puzzle2Hint hint)
    {
        if (hint != null && !trackedPuzzle2Hints.Contains(hint))
        {
            trackedPuzzle2Hints.Add(hint);
            Debug.Log($"HintManager: Added Puzzle2Hint '{hint.name}' to tracking");
        }
    }
    
    /// <summary>
    /// Remove a Puzzle1Hint from the tracking system
    /// </summary>
    /// <param name="hint">The Puzzle1Hint component to stop tracking</param>
    public void RemovePuzzle1Hint(Puzzle1Hint hint)
    {
        if (trackedPuzzle1Hints.Remove(hint))
        {
            Debug.Log($"HintManager: Removed Puzzle1Hint '{hint.name}' from tracking");
        }
    }
    
    /// <summary>
    /// Remove a Puzzle2Hint from the tracking system
    /// </summary>
    /// <param name="hint">The Puzzle2Hint component to stop tracking</param>
    public void RemovePuzzle2Hint(Puzzle2Hint hint)
    {
        if (trackedPuzzle2Hints.Remove(hint))
        {
            Debug.Log($"HintManager: Removed Puzzle2Hint '{hint.name}' from tracking");
        }
    }
    
    /// <summary>
    /// Check if a specific hint has been triggered by name
    /// </summary>
    /// <param name="hintName">The name of the hint GameObject</param>
    /// <returns>True if the hint has been triggered, false otherwise</returns>
    public bool HasHintBeenTriggered(string hintName)
    {
        // Check Puzzle1Hints first
        Puzzle1Hint puzzle1Hint = GetPuzzle1HintByName(hintName);
        if (puzzle1Hint != null)
            return puzzle1Hint.hasBeenTriggered;
        
        // Check Puzzle2Hints
        Puzzle2Hint puzzle2Hint = GetPuzzle2HintByName(hintName);
        if (puzzle2Hint != null)
            return puzzle2Hint.hasBeenTriggered;
        
        return false;
    }
    
    /// <summary>
    /// Check if a specific Puzzle1Hint has been triggered
    /// </summary>
    /// <param name="hint">The Puzzle1Hint component to check</param>
    /// <returns>True if the hint has been triggered, false otherwise</returns>
    public bool HasPuzzle1HintBeenTriggered(Puzzle1Hint hint)
    {
        return hint != null && hint.hasBeenTriggered;
    }
    
    /// <summary>
    /// Check if a specific Puzzle2Hint has been triggered
    /// </summary>
    /// <param name="hint">The Puzzle2Hint component to check</param>
    /// <returns>True if the hint has been triggered, false otherwise</returns>
    public bool HasPuzzle2HintBeenTriggered(Puzzle2Hint hint)
    {
        return hint != null && hint.hasBeenTriggered;
    }
    
    /// <summary>
    /// Get the total number of hints that have been triggered
    /// </summary>
    /// <returns>Number of triggered hints</returns>
    public int GetTriggeredHintCount()
    {
        int puzzle1Triggered = trackedPuzzle1Hints.Count(hint => hint.hasBeenTriggered);
        int puzzle2Triggered = trackedPuzzle2Hints.Count(hint => hint.hasBeenTriggered);
        return puzzle1Triggered + puzzle2Triggered;
    }
    
    /// <summary>
    /// Get the total number of hints in the tracking system
    /// </summary>
    /// <returns>Total number of tracked hints</returns>
    public int GetTotalHintCount()
    {
        return trackedPuzzle1Hints.Count + trackedPuzzle2Hints.Count;
    }
    
    /// <summary>
    /// Get the percentage of hints that have been triggered
    /// </summary>
    /// <returns>Percentage as a float between 0 and 1</returns>
    public float GetHintCompletionPercentage()
    {
        int totalHints = GetTotalHintCount();
        if (totalHints == 0) return 0f;
        return (float)GetTriggeredHintCount() / totalHints;
    }
    
    /// <summary>
    /// Get a Puzzle1Hint by its GameObject name
    /// </summary>
    /// <param name="hintName">The name of the hint GameObject</param>
    /// <returns>The Puzzle1Hint component or null if not found</returns>
    public Puzzle1Hint GetPuzzle1HintByName(string hintName)
    {
        return trackedPuzzle1Hints.FirstOrDefault(hint => hint.name == hintName);
    }
    
    /// <summary>
    /// Get a Puzzle2Hint by its GameObject name
    /// </summary>
    /// <param name="hintName">The name of the hint GameObject</param>
    /// <returns>The Puzzle2Hint component or null if not found</returns>
    public Puzzle2Hint GetPuzzle2HintByName(string hintName)
    {
        return trackedPuzzle2Hints.FirstOrDefault(hint => hint.name == hintName);
    }
    
    /// <summary>
    /// Get all Puzzle1Hints that have been triggered
    /// </summary>
    /// <returns>List of triggered Puzzle1Hints</returns>
    public List<Puzzle1Hint> GetTriggeredPuzzle1Hints()
    {
        return trackedPuzzle1Hints.Where(hint => hint.hasBeenTriggered).ToList();
    }
    
    /// <summary>
    /// Get all Puzzle2Hints that have been triggered
    /// </summary>
    /// <returns>List of triggered Puzzle2Hints</returns>
    public List<Puzzle2Hint> GetTriggeredPuzzle2Hints()
    {
        return trackedPuzzle2Hints.Where(hint => hint.hasBeenTriggered).ToList();
    }
    
    /// <summary>
    /// Get all Puzzle1Hints that have not been triggered yet
    /// </summary>
    /// <returns>List of untriggered Puzzle1Hints</returns>
    public List<Puzzle1Hint> GetUntriggeredPuzzle1Hints()
    {
        return trackedPuzzle1Hints.Where(hint => !hint.hasBeenTriggered).ToList();
    }
    
    /// <summary>
    /// Get all Puzzle2Hints that have not been triggered yet
    /// </summary>
    /// <returns>List of untriggered Puzzle2Hints</returns>
    public List<Puzzle2Hint> GetUntriggeredPuzzle2Hints()
    {
        return trackedPuzzle2Hints.Where(hint => !hint.hasBeenTriggered).ToList();
    }
    
    /// <summary>
    /// Reset all hint trigger states (useful for testing or new game)
    /// </summary>
    public void ResetAllHintStates()
    {
        foreach (Puzzle1Hint hint in trackedPuzzle1Hints)
        {
            if (hint != null)
            {
                hint.hasBeenTriggered = false;
            }
        }
        
        foreach (Puzzle2Hint hint in trackedPuzzle2Hints)
        {
            if (hint != null)
            {
                hint.hasBeenTriggered = false;
            }
        }
        Debug.Log("HintManager: Reset all hint trigger states");
    }
    
    /// <summary>
    /// Complete reset of HintManager state for fresh game start
    /// </summary>
    public void CompleteReset()
    {
        Debug.Log("HintManager: Starting complete reset...");
        
        // Clear all tracked hints
        trackedPuzzle1Hints.Clear();
        trackedPuzzle2Hints.Clear();
        
        // Clear hint mini images dictionary
        hintMiniImages.Clear();
        
        // Reset UI references
        hint1MiniImage = null;
        hint2MiniImage = null;
        hint3MiniImage = null;
        hint4MiniImage = null;
        
        // Re-setup everything
        SetupUIReferences();
        SetupHintMiniImages();
        MakeAllMiniImagesTransparent();
        
        Debug.Log("HintManager: Complete reset finished");
    }
    
    /// <summary>
    /// Emergency reset method that forces all hint mini images to be transparent
    /// </summary>
    public void EmergencyResetHintMiniImages()
    {
        Debug.Log("HintManager: Emergency reset of hint mini images");
        
        // Force reset by name immediately
        ForceResetHintMiniImagesByName();
        
        // Also try to reset through the dictionary
        MakeAllMiniImagesTransparent();
        
        // Schedule additional resets
        StartCoroutine(DelayedResetHintMiniImages());
    }
    
    /// <summary>
    /// Get hint interaction data for save/load systems
    /// </summary>
    /// <returns>Dictionary of hint names and their trigger states</returns>
    public Dictionary<string, bool> GetHintInteractionData()
    {
        Dictionary<string, bool> hintData = new Dictionary<string, bool>();
        
        foreach (Puzzle1Hint hint in trackedPuzzle1Hints)
        {
            if (hint != null)
            {
                hintData[hint.name] = hint.hasBeenTriggered;
            }
        }
        
        foreach (Puzzle2Hint hint in trackedPuzzle2Hints)
        {
            if (hint != null)
            {
                hintData[hint.name] = hint.hasBeenTriggered;
            }
        }
        
        return hintData;
    }
    
    /// <summary>
    /// Load hint interaction data from save/load systems
    /// </summary>
    /// <param name="hintData">Dictionary of hint names and their trigger states</param>
    public void LoadHintInteractionData(Dictionary<string, bool> hintData)
    {
        foreach (var kvp in hintData)
        {
            // Try to find in Puzzle1Hints first
            Puzzle1Hint puzzle1Hint = GetPuzzle1HintByName(kvp.Key);
            if (puzzle1Hint != null)
            {
                puzzle1Hint.hasBeenTriggered = kvp.Value;
                continue;
            }
            
            // Try to find in Puzzle2Hints
            Puzzle2Hint puzzle2Hint = GetPuzzle2HintByName(kvp.Key);
            if (puzzle2Hint != null)
            {
                puzzle2Hint.hasBeenTriggered = kvp.Value;
            }
        }
        Debug.Log($"HintManager: Loaded interaction data for {hintData.Count} hints");
    }
    
    /// <summary>
    /// Subscribe to hint events for real-time tracking
    /// </summary>
    private void SubscribeToHintEvents()
    {
        // This would require modifying Puzzle1Hint to include events
        // For now, we'll use polling in Update() or provide manual methods
    }
    
    /// <summary>
    /// Manually notify the manager that a Puzzle1Hint was triggered
    /// </summary>
    /// <param name="hint">The Puzzle1Hint that was triggered</param>
    public void NotifyPuzzle1HintTriggered(Puzzle1Hint hint)
    {
        if (hint != null)
        {
            OnPuzzle1HintTriggered?.Invoke(hint);
            Debug.Log($"HintManager: Puzzle1Hint '{hint.name}' was triggered");
            
            // Refresh UI references in case they were recreated
            RefreshUIReferences();
            
            // Show the appropriate mini image for this hint
            ShowHintMiniImage(hint);
        }
    }
    
    /// <summary>
    /// Manually notify the manager that a Puzzle2Hint was triggered
    /// </summary>
    /// <param name="hint">The Puzzle2Hint that was triggered</param>
    public void NotifyPuzzle2HintTriggered(Puzzle2Hint hint)
    {
        if (hint != null)
        {
            OnPuzzle2HintTriggered?.Invoke(hint);
            Debug.Log($"HintManager: Puzzle2Hint '{hint.name}' was triggered");
            
            // Refresh UI references in case they were recreated
            RefreshUIReferences();
            
            // Show the appropriate mini image for this hint
            ShowHintMiniImage(hint);
        }
    }
    
    /// <summary>
    /// Manually notify the manager that a Puzzle1Hint was closed
    /// </summary>
    /// <param name="hint">The Puzzle1Hint that was closed</param>
    public void NotifyPuzzle1HintClosed(Puzzle1Hint hint)
    {
        if (hint != null)
        {
            OnPuzzle1HintClosed?.Invoke(hint);
            Debug.Log($"HintManager: Puzzle1Hint '{hint.name}' was closed");
        }
    }
    
    /// <summary>
    /// Manually notify the manager that a Puzzle2Hint was closed
    /// </summary>
    /// <param name="hint">The Puzzle2Hint that was closed</param>
    public void NotifyPuzzle2HintClosed(Puzzle2Hint hint)
    {
        if (hint != null)
        {
            OnPuzzle2HintTriggered?.Invoke(hint);
            Debug.Log($"HintManager: Puzzle2Hint '{hint.name}' was closed");
        }
    }
    
    /// <summary>
    /// Show the appropriate mini image for the triggered hint
    /// </summary>
    /// <param name="hint">The Puzzle1Hint that was triggered</param>
    private void ShowHintMiniImage(Puzzle1Hint hint)
    {
        if (hint.hintSprite == null)
        {
            Debug.LogWarning($"HintManager: Puzzle1Hint '{hint.name}' has no sprite assigned!");
            return;
        }
        
        // Determine which hint this is based on the hint name
        string hintKey = GetHintKeyFromName(hint.name);
        
        // Try to show using the dictionary first
        if (hintMiniImages.ContainsKey(hintKey))
        {
            UnityEngine.UI.Image miniImage = hintMiniImages[hintKey];
            if (miniImage != null)
            {
                // Assign the hint sprite to the mini image
                miniImage.sprite = hint.hintSprite;
                
                // Make the image visible by setting alpha to 1
                Color visibleColor = miniImage.color;
                visibleColor.a = 1f;
                miniImage.color = visibleColor;
                
                Debug.Log($"HintManager: Showed {hintKey} mini image with sprite from Puzzle1Hint '{hint.name}' (via dictionary)");
                return;
            }
        }
        
        // Fallback: try to find the mini image directly by name
        ShowHintMiniImageByName(hintKey, hint.hintSprite, hint.name);
    }
    
    /// <summary>
    /// Show the appropriate mini image for the triggered hint
    /// </summary>
    /// <param name="hint">The Puzzle2Hint that was triggered</param>
    private void ShowHintMiniImage(Puzzle2Hint hint)
    {
        if (hint.hintSprite == null)
        {
            Debug.LogWarning($"HintManager: Puzzle2Hint '{hint.name}' has no sprite assigned!");
            return;
        }
        
        // Determine which hint this is based on the hint name
        string hintKey = GetHintKeyFromName(hint.name);
        
        // Try to show using the dictionary first
        if (hintMiniImages.ContainsKey(hintKey))
        {
            UnityEngine.UI.Image miniImage = hintMiniImages[hintKey];
            if (miniImage != null)
            {
                // Assign the hint sprite to the mini image
                miniImage.sprite = hint.hintSprite;
                
                // Make the image visible by setting alpha to 1
                Color visibleColor = miniImage.color;
                visibleColor.a = 1f;
                miniImage.color = visibleColor;
                
                Debug.Log($"HintManager: Showed {hintKey} mini image with sprite from Puzzle2Hint '{hint.name}' (via dictionary)");
                return;
            }
        }
        
        // Fallback: try to find the mini image directly by name
        ShowHintMiniImageByName(hintKey, hint.hintSprite, hint.name);
    }
    
    /// <summary>
    /// Get the hint key from the hint name
    /// </summary>
    /// <param name="hintName">The name of the hint GameObject</param>
    /// <returns>The hint key (hint1, hint2, etc.)</returns>
    private string GetHintKeyFromName(string hintName)
    {
        // Convert hint name to lowercase for easier matching
        string lowerName = hintName.ToLower();
        
        // Check for common hint naming patterns
        if (lowerName.Contains("hint1") || lowerName.Contains("puzzle1"))
            return "hint1";
        else if (lowerName.Contains("hint2") || lowerName.Contains("puzzle2"))
            return "hint2";
        else if (lowerName.Contains("hint3") || lowerName.Contains("puzzle3"))
            return "hint3";
        else if (lowerName.Contains("hint4") || lowerName.Contains("puzzle4"))
            return "hint4";
        
        // Default fallback
        return "hint1";
    }
    
    /// <summary>
    /// Refresh UI references in case they were recreated
    /// </summary>
    private void RefreshUIReferences()
    {
        // Re-setup UI references
        SetupUIReferences();
        SetupHintMiniImages();
        Debug.Log("HintManager: Refreshed UI references");
    }
    
    /// <summary>
    /// Show hint mini image by finding it directly by name
    /// </summary>
    /// <param name="hintKey">The hint key (hint1, hint2, etc.)</param>
    /// <param name="sprite">The sprite to assign</param>
    /// <param name="hintName">The name of the hint for logging</param>
    private void ShowHintMiniImageByName(string hintKey, Sprite sprite, string hintName)
    {
        // Try multiple naming patterns
        string[] possibleNames = {
            $"{hintKey}MiniImage",
            $"Hint{hintKey.Substring(4)}MiniImage", // hint1 -> Hint1MiniImage
            $"{hintKey}Image",
            $"Hint{hintKey.Substring(4)}Image", // hint1 -> Hint1Image
            $"{hintKey}",
            $"Hint{hintKey.Substring(4)}" // hint1 -> Hint1
        };
        
        foreach (string name in possibleNames)
        {
            GameObject foundObject = GameObject.Find(name);
            if (foundObject != null)
            {
                UnityEngine.UI.Image foundImage = foundObject.GetComponent<UnityEngine.UI.Image>();
                if (foundImage != null)
                {
                    // Assign the sprite and make visible
                    foundImage.sprite = sprite;
                    Color visibleColor = foundImage.color;
                    visibleColor.a = 1f;
                    foundImage.color = visibleColor;
                    
                    Debug.Log($"HintManager: Showed {hintKey} mini image '{name}' with sprite from '{hintName}' (via direct search)");
                    return;
                }
            }
        }
        
        Debug.LogWarning($"HintManager: Could not find mini image for {hintKey} from hint '{hintName}'");
    }
    
    /// <summary>
    /// Hide all mini images (make them transparent)
    /// </summary>
    public void HideAllMiniImages()
    {
        MakeAllMiniImagesTransparent();
        Debug.Log("HintManager: Hid all mini images");
    }
    
    /// <summary>
    /// Get the number of mini images currently visible
    /// </summary>
    /// <returns>Number of visible mini images</returns>
    public int GetVisibleMiniImageCount()
    {
        int visibleCount = 0;
        foreach (var kvp in hintMiniImages)
        {
            if (kvp.Value != null && kvp.Value.color.a > 0f)
            {
                visibleCount++;
            }
        }
        return visibleCount;
    }
    
    /// <summary>
    /// Destroy a specific hint mini image
    /// </summary>
    /// <param name="hintKey">The hint key (hint1, hint2, etc.)</param>
    public void DestroyHintMiniImage(string hintKey)
    {
        if (hintMiniImages.ContainsKey(hintKey))
        {
            UnityEngine.UI.Image miniImage = hintMiniImages[hintKey];
            if (miniImage != null)
            {
                // Destroy the GameObject that contains the Image component
                Destroy(miniImage.gameObject);
                Debug.Log($"HintManager: Destroyed {hintKey} mini image");
            }
            else
            {
                Debug.LogWarning($"HintManager: {hintKey} mini image is null");
            }
        }
        else
        {
            Debug.LogWarning($"HintManager: No mini image found for key '{hintKey}'");
        }
    }
    
    /// <summary>
    /// Check if all hints have been triggered
    /// </summary>
    /// <returns>True if all hints have been triggered, false otherwise</returns>
    public bool AreAllHintsTriggered()
    {
        int totalHints = GetTotalHintCount();
        if (totalHints == 0) return false;
        
        int puzzle1Triggered = trackedPuzzle1Hints.Count(hint => hint.hasBeenTriggered);
        int puzzle2Triggered = trackedPuzzle2Hints.Count(hint => hint.hasBeenTriggered);
        
        return (puzzle1Triggered + puzzle2Triggered) == totalHints;
    }
    
    /// <summary>
    /// Get a list of all hint names for debugging or UI purposes
    /// </summary>
    /// <returns>List of all hint names</returns>
    public List<string> GetHintNames()
    {
        List<string> allNames = new List<string>();
        allNames.AddRange(trackedPuzzle1Hints.Select(hint => hint.name));
        allNames.AddRange(trackedPuzzle2Hints.Select(hint => hint.name));
        return allNames;
    }
    
    /// <summary>
    /// Get a list of triggered hint names
    /// </summary>
    /// <returns>List of triggered hint names</returns>
    public List<string> GetTriggeredHintNames()
    {
        List<string> triggeredNames = new List<string>();
        triggeredNames.AddRange(trackedPuzzle1Hints.Where(hint => hint.hasBeenTriggered).Select(hint => hint.name));
        triggeredNames.AddRange(trackedPuzzle2Hints.Where(hint => hint.hasBeenTriggered).Select(hint => hint.name));
        return triggeredNames;
    }
    
    /// <summary>
    /// Get a list of untriggered hint names
    /// </summary>
    /// <returns>List of untriggered hint names</returns>
    public List<string> GetUntriggeredHintNames()
    {
        List<string> untriggeredNames = new List<string>();
        untriggeredNames.AddRange(trackedPuzzle1Hints.Where(hint => !hint.hasBeenTriggered).Select(hint => hint.name));
        untriggeredNames.AddRange(trackedPuzzle2Hints.Where(hint => !hint.hasBeenTriggered).Select(hint => hint.name));
        return untriggeredNames;
    }
    
    // Debug method to print current hint status
    [ContextMenu("Print Hint Status")]
    public void PrintHintStatus()
    {
        Debug.Log("=== HintManager Status ===");
        Debug.Log($"Total Hints: {GetTotalHintCount()}");
        Debug.Log($"Triggered Hints: {GetTriggeredHintCount()}");
        Debug.Log($"Completion: {GetHintCompletionPercentage():P1}");
        Debug.Log($"Puzzle1Hints: {trackedPuzzle1Hints.Count}");
        Debug.Log($"Puzzle2Hints: {trackedPuzzle2Hints.Count}");
        Debug.Log($"Hint1 Mini Image: {(hint1MiniImage != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Hint2 Mini Image: {(hint2MiniImage != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Hint3 Mini Image: {(hint3MiniImage != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Hint4 Mini Image: {(hint4MiniImage != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Target Canvas: {(targetCanvas != null ? "✓ Assigned" : "✗ Missing")}");
        Debug.Log($"Visible Mini Images: {GetVisibleMiniImageCount()}");
        Debug.Log($"Hint Mini Images Dictionary Count: {hintMiniImages.Count}");
        
        Debug.Log("--- Hint Mini Images Dictionary ---");
        foreach (var kvp in hintMiniImages)
        {
            if (kvp.Value != null)
            {
                Debug.Log($"  {kvp.Key}: {(kvp.Value.color.a > 0f ? "Visible" : "Transparent")} - Alpha: {kvp.Value.color.a:F2}");
            }
            else
            {
                Debug.Log($"  {kvp.Key}: NULL");
            }
        }
        
        Debug.Log("--- Puzzle1Hints ---");
        foreach (Puzzle1Hint hint in trackedPuzzle1Hints)
        {
            if (hint != null)
            {
                Debug.Log($"  {hint.name}: {(hint.hasBeenTriggered ? "✓ Triggered" : "○ Not Triggered")} - Sprite: {(hint.hintSprite != null ? "✓" : "✗")}");
            }
        }
        
        Debug.Log("--- Puzzle2Hints ---");
        foreach (Puzzle2Hint hint in trackedPuzzle2Hints)
        {
            if (hint != null)
            {
                Debug.Log($"  {hint.name}: {(hint.hasBeenTriggered ? "✓ Triggered" : "○ Not Triggered")} - Sprite: {(hint.hintSprite != null ? "✓" : "✗")}");
            }
        }
        Debug.Log("=========================");
    }
    
    // Debug method to search for hint mini images in scene
    [ContextMenu("Search for Hint Mini Images")]
    public void SearchForHintMiniImages()
    {
        Debug.Log("=== Searching for Hint Mini Images ===");
        
        // Search for all UI Images in the scene
        UnityEngine.UI.Image[] allImages = FindObjectsOfType<UnityEngine.UI.Image>();
        Debug.Log($"Found {allImages.Length} total UI Images in scene");
        
        foreach (UnityEngine.UI.Image image in allImages)
        {
            string name = image.name.ToLower();
            if (name.Contains("hint") || name.Contains("mini"))
            {
                Debug.Log($"Potential hint mini image: '{image.name}' (Active: {image.gameObject.activeInHierarchy}, Alpha: {image.color.a:F2})");
            }
        }
        
        // Search for objects with specific tags
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("HintMiniImage");
        Debug.Log($"Found {taggedObjects.Length} objects with 'HintMiniImage' tag");
        
        foreach (GameObject obj in taggedObjects)
        {
            Debug.Log($"Tagged object: '{obj.name}' (Active: {obj.activeInHierarchy})");
        }
        
        Debug.Log("=====================================");
    }
    
    // Debug method to force reset all hint images
    [ContextMenu("Force Reset All Hint Images")]
    public void DebugForceResetAllHintImages()
    {
        Debug.Log("=== Force Resetting All Hint Images ===");
        ForceResetHintMiniImagesByName();
        Debug.Log("=== Force Reset Complete ===");
    }
    
    // Debug method to test showing hint images
    [ContextMenu("Test Show Hint Images")]
    public void DebugTestShowHintImages()
    {
        Debug.Log("=== Testing Show Hint Images ===");
        
        // Create a test sprite (you can assign a real sprite in the inspector)
        Sprite testSprite = null;
        
        // Try to show hint1
        ShowHintMiniImageByName("hint1", testSprite, "TestHint1");
        
        // Try to show hint2
        ShowHintMiniImageByName("hint2", testSprite, "TestHint2");
        
        // Try to show hint3
        ShowHintMiniImageByName("hint3", testSprite, "TestHint3");
        
        // Try to show hint4
        ShowHintMiniImageByName("hint4", testSprite, "TestHint4");
        
        Debug.Log("=== Test Show Complete ===");
    }
    
    void OnDestroy()
    {
        // Clean up singleton reference
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Unsubscribe from scene load events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}


