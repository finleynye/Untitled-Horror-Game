using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalPlayerSpawner
{
    private const string PlayerSpawnPointTag = "PlayerSpawnPoint";
    private const float SpawnPointVerticalOffset = 0.25f;

    public static void SpawnAtScenePoint(Transform playerTransform, CharacterController controller, string playerName)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("Could not move local player because the player transform was missing.");
            return;
        }

        GameObject[] spawnPoints = null;
        try
        {
            spawnPoints = GameObject.FindGameObjectsWithTag(PlayerSpawnPointTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"Tag '{PlayerSpawnPointTag}' does not exist. Add it in Unity's Tag Manager.");
        }

        Vector3 spawnPosition;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            GameObject selectedSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPosition = selectedSpawnPoint.transform.position + Vector3.up * SpawnPointVerticalOffset;
        }
        else
        {
            Debug.LogWarning($"No objects tagged '{PlayerSpawnPointTag}' were found in {SceneManager.GetActiveScene().name}. Using a random fallback position for {playerName}.");
            spawnPosition = new Vector3(Random.Range(-10f, 10f), Random.Range(1f, 5f), Random.Range(-10f, 10f));
        }
        

        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
            controller.enabled = false;

        playerTransform.position = spawnPosition;

        if (controller != null)
            controller.enabled = controllerWasEnabled;
    }
}
