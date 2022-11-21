using UnityEngine;
using UnityEngine.UI;

public class WanderSwitchButton : MonoBehaviour
{
    private static bool switchButton; // bool that specifies if we are using switch points or not

    public static bool SwitchButton => switchButton; // public bool for other classes to know if we're using switch points

    [SerializeField] private Text buttonText; // Text that shows if we're using switch points or not

    /// <summary>
    /// We initialize the bool and the text
    /// </summary>
    private void Start()
    {
        switchButton = true;
        UpdateButtonText();
    }

    /// <summary>
    /// Method that will be accessed by the button to change from using switch points to not using them
    /// </summary>
    public void ActivateSwitchPoints()
    {
        switchButton = !switchButton;
        UpdateButtonText();
    }

    /// <summary>
    /// Method that updates the text
    /// </summary>
    private void UpdateButtonText()
    {
        if (switchButton)
            buttonText.text = "Switch Points: ON";
        else
            buttonText.text = "Switch Points: OFF";
    }
}
