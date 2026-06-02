using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Steamworks;

public class UserInfo : MonoBehaviour
{
    public string userName;
    public int connectionID;
    public ulong steamID;
    private bool _iconReceived;
    
    public TMP_Text userNameText;
    public RawImage userIcon;
    public Image readyIcon;
    public bool isReady;

    public Sprite greenTick;
    public Sprite redCross;
    
    protected Callback<AvatarImageLoaded_t> IconLoaded;

    private void Start()
        => IconLoaded = Callback<AvatarImageLoaded_t>.Create(OnIconLoaded);

    public void SetUserValues()
    {
        userNameText.text = userName;
        UpdateReadyState();
        
        if(!_iconReceived) 
            GetUserIcon();
    }
    
    private void OnIconLoaded(AvatarImageLoaded_t callback)
    {
        if (callback.m_steamID.m_SteamID == steamID)
            userIcon.texture = GetSteamProfileIcon(callback.m_iImage);
    }

    private Texture2D GetSteamProfileIcon(int iImage)
    {
        Texture2D texture = null;
        
        var isValid = SteamUtils.GetImageSize(iImage, out var width, out var height);
        if (isValid)
        {
            var image = new byte[width * height * 4];
            
            isValid = SteamUtils.GetImageRGBA(iImage, image, (int)(width * height * 4));
            if (isValid)
            {
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, false);

                var flippedImage = new byte[image.Length];
                var rowSize = (int)width * 4;
                for (var y = 0; y < height; y++)
                    System.Array.Copy(image, y * rowSize, flippedImage, (height - 1 - y) * rowSize, rowSize);
                
                texture.LoadRawTextureData(flippedImage);
                texture.Apply();
            }
        }

        _iconReceived = true;
        return texture;
    }

    private void GetUserIcon()
    {
        var imageID = SteamFriends.GetLargeFriendAvatar((CSteamID)steamID);
        if (imageID == -1) 
            return;
        
        userIcon.texture = GetSteamProfileIcon(imageID);
    }

    private void UpdateReadyState()
        => readyIcon.sprite = isReady ? greenTick : redCross;
}