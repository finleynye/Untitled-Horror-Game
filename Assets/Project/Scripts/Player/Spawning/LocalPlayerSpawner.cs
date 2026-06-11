using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalPlayerSpawner
{
    private const string PlayerSpawnPointTag = "PlayerSpawnPoint";
    private const float SpawnPointVerticalOffset = 0.25f;

    public static void SpawnAtScenePoint(Transform playerTransform, CharacterController controller, string playerName)
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag(PlayerSpawnPointTag);

        Vector3 spawnPosition;
        if (spawnPoints.Length > 0)
        {
            GameObject selectedSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPosition = selectedSpawnPoint.transform.position + Vector3.up * SpawnPointVerticalOffset;
        }
        else
            spawnPosition = new Vector3(Random.Range(10f, -10f), Random.Range(5f, 1f), Random.Range(10f, -10f));
        
        controller.enabled = false;
        playerTransform.position = spawnPosition;
        controller.enabled = true;
    }
}
