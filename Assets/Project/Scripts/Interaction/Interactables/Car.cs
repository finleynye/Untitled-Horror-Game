using TMPro;
using UnityEngine;

public class Car : MonoBehaviour
{
    [Header("Message")]
    [SerializeField] private string locationText = "The Car";
    [SerializeField] private string objectiveText = "Car's busted, better head to that creepy camp...";

    public void ShowBustedCarMessage()
    {
        if (LocalPopupUI.Instance == null)
        {
            Debug.LogWarning("LocalPopupUI was not found in the scene.");
            return;
        }

        LocalPopupUI.Instance.ShowPopup(locationText, objectiveText);
    }
}
