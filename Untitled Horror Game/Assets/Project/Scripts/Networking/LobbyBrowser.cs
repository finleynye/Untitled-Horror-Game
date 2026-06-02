using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class LobbyBrowser : MonoBehaviour
{
    public static LobbyBrowser Instance;

    public GameObject lobbyMenu;
    public GameObject lobbyInfoPref;
    public GameObject lobbyListContent;
    public GameObject mainMenu;

    public List<GameObject> lobbyList;

    private void Awake()
    {
        Instance = this;
        Cursor.visible = true;
    }

    public void DisplayLobbies(List<CSteamID> lobbyIDs, LobbyDataUpdate_t result)
    {
        for (var i = 0; i < lobbyIDs.Count; i++)
        {
            if (lobbyIDs[i].m_SteamID == result.m_ulSteamIDLobby)
            {
                var lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDs[i].m_SteamID, "name");
                if (string.IsNullOrEmpty(lobbyName)) continue;
                
                var createdLobby = Instantiate(lobbyInfoPref, lobbyListContent.transform);
                createdLobby.GetComponent<LobbyInfo>().lobbyID = (CSteamID)lobbyIDs[i].m_SteamID;
                createdLobby.GetComponent<LobbyInfo>().lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)lobbyIDs[i].m_SteamID, "name");
                createdLobby.GetComponent<LobbyInfo>().SetLobbyInfo();

                if (!createdLobby.activeSelf)
                {
                    Destroy(createdLobby);
                    continue;
                }
                
                createdLobby.transform.localScale = Vector3.one;
                lobbyList.Add(createdLobby);
            }
        }
    }
    
    public void Refresh()
    {
        DestroyLobbies();
        SteamLobby.Instance.GetLobbyList();
        /*AudioManager.Instance.Play(AudioManager.Instance.buttonClick);*/
    }
    
    public void DestroyLobbies()
    {
        foreach (var lobby in lobbyList)
            Destroy(lobby);
        
        lobbyList.Clear();
    }

    public void GetLobbies()
    {
        DestroyLobbies();
        
        mainMenu.SetActive(false);
        lobbyMenu.SetActive(true);
        
        SteamLobby.Instance.GetLobbyList();
        /*DiscordManager.Instance.Presence.SetPresence("Browsing lobbies");
        AudioManager.Instance.Play(AudioManager.Instance.buttonClick);*/
    }

    public void BackToMainMenu()
    {
        DestroyLobbies();
        
        mainMenu.SetActive(true);
        lobbyMenu.SetActive(false);
        
        /*DiscordManager.Instance.Presence.SetPresence("In the menus");
        AudioManager.Instance.Play(AudioManager.Instance.buttonClick);*/
    }
}
