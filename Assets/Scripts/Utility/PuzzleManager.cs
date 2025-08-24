using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all puzzles in the game and tracks their completion status.
/// Provides a centralized way to check if all puzzles are solved.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    [Header("Puzzle Tracking")]
    [Tooltip("List of all cracked wall puzzles in the current level")]
    public List<CrackedWall> crackedWalls = new List<CrackedWall>();
    [Tooltip("List of all cracked wall 2 puzzles in the current level")]
    public List<CrackedWall2> crackedWalls2 = new List<CrackedWall2>();
    [Tooltip("List of all cracked wall 3 puzzles in the current level")]
    public List<CrackedWall3> crackedWalls3 = new List<CrackedWall3>();
    [Tooltip("List of all cracked wall 4 puzzles in the current level")]
    public List<CrackedWall4> crackedWalls4 = new List<CrackedWall4>();

    // Event that fires when all puzzles are solved
    public System.Action OnAllPuzzlesSolved;

    private bool allPuzzlesSolved = false;
    private DungeonGenerator dungeonGenerator; // Reference to the dungeon generator
    private bool dungeonInstantiated = false; // Track whether dungeon has been instantiated

    void Start()
    {
        // Find the dungeon generator
        dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        if (dungeonGenerator == null)
        {
            Debug.LogWarning("PuzzleManager: No DungeonGenerator found in scene. Boss room path creation will be disabled.");
        }
        else
        {
            Debug.Log("PuzzleManager: Found DungeonGenerator, boss room path creation enabled.");
        }

        // Find all puzzle objects in the scene after a short delay to ensure dungeon is generated
        StartCoroutine(FindPuzzlesAfterDelay());
    }

    /// <summary>
    /// Coroutine to find puzzles after a delay to ensure dungeon generation is complete
    /// </summary>
    private System.Collections.IEnumerator FindPuzzlesAfterDelay()
    {
        // Wait a short time for dungeon generation to complete
        yield return new WaitForSeconds(0.5f);
        
        FindAllPuzzles();
        
        // Log the initial puzzle count
        int totalPuzzles = GetTotalPuzzleCount();
        Debug.Log($"PuzzleManager: Initial puzzle scan complete. Found {totalPuzzles} total puzzles.");
    }

    /// <summary>
    /// Finds all puzzle objects in the current scene and adds them to the tracking lists
    /// </summary>
    public void FindAllPuzzles()
    {
        // Clear existing lists
        crackedWalls.Clear();
        crackedWalls2.Clear();
        crackedWalls3.Clear();
        crackedWalls4.Clear();

        // Find all cracked wall puzzles
        CrackedWall[] walls = FindObjectsOfType<CrackedWall>();
        crackedWalls.AddRange(walls);

        CrackedWall2[] walls2 = FindObjectsOfType<CrackedWall2>();
        crackedWalls2.AddRange(walls2);

        CrackedWall3[] walls3 = FindObjectsOfType<CrackedWall3>();
        crackedWalls3.AddRange(walls3);

        CrackedWall4[] walls4 = FindObjectsOfType<CrackedWall4>();
        crackedWalls4.AddRange(walls4);

        // Debug.Log($"PuzzleManager: Found {crackedWalls.Count} CrackedWall, {crackedWalls2.Count} CrackedWall2, {crackedWalls3.Count} CrackedWall3, {crackedWalls4.Count} CrackedWall4 puzzles");
        
        // Log details about each puzzle found
        for (int i = 0; i < crackedWalls.Count; i++)
        {
            Debug.Log($"PuzzleManager: CrackedWall {i + 1} at {crackedWalls[i].transform.position}, Solved: {crackedWalls[i].IsPuzzleSolved()}");
        }
        
        for (int i = 0; i < crackedWalls2.Count; i++)
        {
            Debug.Log($"PuzzleManager: CrackedWall2 {i + 1} at {crackedWalls2[i].transform.position}, Solved: {crackedWalls2[i].IsPuzzleSolved()}");
        }
        
        for (int i = 0; i < crackedWalls3.Count; i++)
        {
            Debug.Log($"PuzzleManager: CrackedWall3 {i + 1} at {crackedWalls3[i].transform.position}, Solved: {crackedWalls3[i].IsPuzzleSolved()}");
        }
        
        for (int i = 0; i < crackedWalls4.Count; i++)
        {
            Debug.Log($"PuzzleManager: CrackedWall4 {i + 1} at {crackedWalls4[i].transform.position}, Solved: {crackedWalls4[i].IsPuzzleSolved()}");
        }
    }

    /// <summary>
    /// Checks if all puzzles in the current level are solved
    /// </summary>
    /// <returns>True if all puzzles are solved, false otherwise</returns>
    public bool AreAllPuzzlesSolved()
    {
        if (allPuzzlesSolved)
            return true;

        // Refresh puzzle list if we have no puzzles found (they might be created dynamically)
        if (GetTotalPuzzleCount() == 0)
        {
            FindAllPuzzles();
        }

        int totalPuzzles = GetTotalPuzzleCount();
        int solvedPuzzles = GetSolvedPuzzleCount();
        
        // If no puzzles found, check if dungeon has been instantiated
        if (totalPuzzles == 0)
        {
            if (dungeonInstantiated)
            {
                Debug.Log("PuzzleManager: No puzzles found after instantiation, considering all puzzles solved");
                return true;
            }
            else
            {
                // During generation phase, assume puzzles exist and are unsolved
                return false;
            }
        }

        // Check all cracked wall puzzles
        foreach (CrackedWall wall in crackedWalls)
        {
            if (wall != null && !wall.IsPuzzleSolved())
            {
                return false;
            }
        }

        foreach (CrackedWall2 wall in crackedWalls2)
        {
            if (wall != null && !wall.IsPuzzleSolved())
            {
                return false;
            }
        }

        foreach (CrackedWall3 wall in crackedWalls3)
        {
            if (wall != null && !wall.IsPuzzleSolved())
            {
                return false;
            }
        }

        foreach (CrackedWall4 wall in crackedWalls4)
        {
            if (wall != null && !wall.IsPuzzleSolved())
            {
                return false;
            }
        }

        // If we get here, all puzzles are solved
        if (!allPuzzlesSolved)
        {
            allPuzzlesSolved = true;
            OnAllPuzzlesSolved?.Invoke();
            Debug.Log($"PuzzleManager: All {totalPuzzles} puzzles solved!");
            
            // TODO: Implement boss room path creation when all puzzles are solved
            Debug.Log("PuzzleManager: All puzzles solved! Boss room path creation not yet implemented");
        }

        return true;
    }

    /// <summary>
    /// Gets the total number of puzzles in the current level
    /// </summary>
    /// <returns>Total number of puzzles</returns>
    public int GetTotalPuzzleCount()
    {
        return crackedWalls.Count + crackedWalls2.Count + crackedWalls3.Count + crackedWalls4.Count;
    }

    /// <summary>
    /// Gets the number of solved puzzles in the current level
    /// </summary>
    /// <returns>Number of solved puzzles</returns>
    public int GetSolvedPuzzleCount()
    {
        int solvedCount = 0;

        foreach (CrackedWall wall in crackedWalls)
        {
            if (wall != null && wall.IsPuzzleSolved())
                solvedCount++;
        }

        foreach (CrackedWall2 wall in crackedWalls2)
        {
            if (wall != null && wall.IsPuzzleSolved())
                solvedCount++;
        }

        foreach (CrackedWall3 wall in crackedWalls3)
        {
            if (wall != null && wall.IsPuzzleSolved())
                solvedCount++;
        }

        foreach (CrackedWall4 wall in crackedWalls4)
        {
            if (wall != null && wall.IsPuzzleSolved())
                solvedCount++;
        }

        return solvedCount;
    }

    /// <summary>
    /// Resets the puzzle completion status (useful for level restart)
    /// </summary>
    public void ResetPuzzleStatus()
    {
        allPuzzlesSolved = false;
        Debug.Log("PuzzleManager: Puzzle status reset");
    }

    /// <summary>
    /// Forces a refresh of the puzzle list and resets completion status
    /// </summary>
    public void ForceRefreshPuzzles()
    {
        allPuzzlesSolved = false;
        FindAllPuzzles();
        Debug.Log("PuzzleManager: Forced puzzle refresh completed");
    }

    /// <summary>
    /// Manually triggers the creation of a path to the boss room from the player's current location
    /// This can be called externally if needed
    /// </summary>
    public void TriggerBossRoomPathCreation()
    {
        Debug.Log("PuzzleManager: Boss room path creation not yet implemented");
    }

    /// <summary>
    /// Marks that the dungeon has been instantiated, allowing puzzle checks to work properly
    /// </summary>
    public void SetDungeonInstantiated()
    {
        dungeonInstantiated = true;
        Debug.Log("PuzzleManager: Dungeon instantiated flag set to true");
    }
}
