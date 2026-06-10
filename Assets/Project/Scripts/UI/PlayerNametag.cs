using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerNametag : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private CanvasGroup canvasGroup;

    private PlayerController _controller;
    private PlayerMovement _movement;
    private Transform _cam;

    private const float DefaultAlpha = .9f;
    private const float CrouchingAlpha = .6f;

    private void Start()
    {
        _controller = GetComponentInParent<PlayerController>(true);
        _movement = GetComponentInParent<PlayerMovement>(true);

        if (_controller.isOwned)
        {
            gameObject.SetActive(false);
            return;
        }

        if (Camera.main != null)
            _cam = Camera.main.transform;
        nameText.text = _controller.playerName;
        _controller.OnNameChanged += UpdateName;
        
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
        => _controller.OnNameChanged -= UpdateName;

    private void LateUpdate()
    {
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            canvasGroup.alpha = 0;
            return;
        }
        
        if (_cam == null)
        {
            //stupid code 
            //_cam isnt found on start (only sometimes), so need to have 2 checks.
            if (Camera.main != null)
                _cam = Camera.main.transform;
            return;
        }
        
        //billboard effect
        transform.forward = _cam.forward;
        canvasGroup.alpha = _movement.IsCrouching ? CrouchingAlpha : DefaultAlpha;
    }

    private void UpdateName(string newName)
        => nameText.text = newName;
}