using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Steamworks;

public class UserInfo : MonoBehaviour
{
    [Header("Steam Info")]
    public string userName;
    public int connectionID;
    public ulong steamID;
    private bool _iconReceived;

    [Header("User Name Display Info")]
    public TMP_Text userNameText;
    public TMP_Text role;
    public PlayerRole playerRole;
    public RawImage userIcon;
    public bool isReady;

    [SerializeField] private Image readyBtnImage;
    public Button readyBtn;

    protected Callback<AvatarImageLoaded_t> IconLoaded;

    [Header("Ready Status")]
    public TMP_Text readyStatusText;

    [Header("Ready Status Colours")]
    [SerializeField] private Color readyButtonColour = new Color32(23, 36, 28, 255);
    [SerializeField] private Color notReadyButtonColour = new Color32(36, 26, 26, 255);

    [SerializeField] private Color readyTextColour = new Color32(168, 216, 160, 255);
    [SerializeField] private Color notReadyTextColour = new Color32(224, 106, 95, 255);

    private void Start()
    {
        IconLoaded = Callback<AvatarImageLoaded_t>.Create(OnIconLoaded);
        readyBtn.onClick.AddListener(() => LobbyController.Instance.ReadyPlayer());
    }

    public void SetUserValues()
    {
        userNameText.text = userName;
        UpdateReadyState();
        
        readyBtn.gameObject.SetActive(steamID == SteamUser.GetSteamID().m_SteamID);
        role.gameObject.SetActive(steamID == SteamUser.GetSteamID().m_SteamID);
        role.text = playerRole == PlayerRole.Unassigned ? "Awaiting Role" : playerRole.ToString();
        
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
                
                var circleImage = new byte[image.Length];
                var centreX = width / 2f;
                var centreY = height / 2f;
                var radius = Mathf.Min(width, height) / 2f;

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var pixelIndex = (y * (int)width + x) * 4;
                        var dx = x - centreX;
                        var dy = y - centreY;

                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            circleImage[pixelIndex] = flippedImage[pixelIndex]; //red
                            circleImage[pixelIndex + 1] = flippedImage[pixelIndex + 1]; //green
                            circleImage[pixelIndex + 2] = flippedImage[pixelIndex + 2]; //blue
                            circleImage[pixelIndex + 3] = flippedImage[pixelIndex + 3]; //alpha
                        }
                    }
                }
                
                texture.LoadRawTextureData(circleImage); //W code
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
    {
        if (readyStatusText == null)
            return;

        if (readyBtnImage == null)
            return;

        if (isReady)
        {
            readyStatusText.text = "READY";

            readyBtnImage.color = readyButtonColour;
            readyStatusText.color = readyTextColour;
        }
        else
        {
            readyStatusText.text = "WAITING";

            readyBtnImage.color = notReadyButtonColour;
            readyStatusText.color = notReadyTextColour;
        }
    }
}