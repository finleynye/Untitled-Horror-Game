using UnityEngine;
using TMPro;
using Steamworks;

public class LobbyInfo : MonoBehaviour
{
    [Header("Lobby Info")]
    public CSteamID lobbyID;
    public string lobbyName;

    [Header("UI References")]
    public TMP_Text lobbyNameText;
    public TMP_Text lobbySize;

    [Header("Text Settings")]
    [SerializeField] private int maxLobbyNameLength = 18;

    public void SetLobbyInfo()
    {
        string displayName = string.IsNullOrWhiteSpace(lobbyName) ? "Empty" : lobbyName;

        if (displayName.Length > maxLobbyNameLength)
        {
            displayName = displayName.Substring(0, maxLobbyNameLength) + "...";
        }

        lobbyNameText.text = displayName;

        //stop the lobby name from dropping onto a second line
        lobbyNameText.textWrappingMode = TextWrappingModes.NoWrap;
        lobbyNameText.overflowMode = TextOverflowModes.Ellipsis;

        var currSize = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
        var maxSize = SteamMatchmaking.GetLobbyMemberLimit(lobbyID);

        if (currSize <= 0 || maxSize <= 0)
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
    }
}