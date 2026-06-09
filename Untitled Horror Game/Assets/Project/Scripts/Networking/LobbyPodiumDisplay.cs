using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LobbyPodiumDisplay : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject playerVisualPrefab;

    [Header("Line Formation")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private float spacing = 1.5f;
    [SerializeField] private bool centreLine = true;

    private readonly List<GameObject> spawnedVisuals = new();

    private static UHG_NetworkManager Manager => NetworkManager.singleton as UHG_NetworkManager;

    public void RefreshVisuals()
    {
        //clear old visuals before rebuilding the line
        ClearVisuals();

        if (Manager == null) return;
        if (Manager.Players == null) return;
        if (playerVisualPrefab == null) return;
        if (startPoint == null) return;

        int playerCount = Manager.Players.Count;

        //spawn one visual character for each connected player
        for (int i = 0; i < playerCount; i++)
        {
            Vector3 spawnPosition = GetSpawnPosition(i, playerCount);

            GameObject newVisual = Instantiate(playerVisualPrefab, spawnPosition, startPoint.rotation, transform);

            spawnedVisuals.Add(newVisual);
        }
    }

    private Vector3 GetSpawnPosition(int index, int playerCount)
    {
        Vector3 position = startPoint.position;

        //moves each player along the start points local right direction
        float offset = index * spacing;

        //keeps the full line centred around the start point
        if (centreLine)
        {
            float totalWidth = (playerCount - 1) * spacing;
            offset -= totalWidth * 0.5f;
        }

        position += startPoint.right * offset;

        return position;
    }

    private void ClearVisuals()
    {
        //destroy any existing lobby visuals before respawning them
        for (int i = 0; i < spawnedVisuals.Count; i++)
        {
            if (spawnedVisuals[i] != null)
                Destroy(spawnedVisuals[i]);
        }

        spawnedVisuals.Clear();
    }
}