using UnityEngine;
using TMPro;
using Steamworks;

public class LobbyInfo : MonoBehaviour
{
    public CSteamID lobbyID;
    public string lobbyName;
    public TMP_Text lobbyNameText;
    public TMP_Text lobbySize;

    public void SetLobbyInfo()
    {
        lobbyNameText.text = lobbyName == "" ? "Empty" : lobbyName;
        
        var currSize = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
        var maxSize = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);
        
        if (currSize <= 0)
        {
            lobbySize.text = "?/?";
            return;
        }
        
        lobbySize.text = $"{currSize}/{maxSize}";
    }

    public void JoinLobby()
    {
        //clear list immediately so duplicate lobbies dont appear during join process
        LobbyBrowser.Instance.DestroyLobbies();
        SteamLobby.Instance.JoinLobby(lobbyID);
        /*AudioManager.Instance.Play(AudioManager.Instance.buttonClick);*/
    }
}