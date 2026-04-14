using UnityEngine;
using UnityEngine.UI;


[DefaultExecutionOrder(-100)] // make sure this runs before any UI initialization
public class UIButtonSfxBinder : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BindButtonSfx()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("UIButtonSfxBinder: AudioManager.Instance not found. Button SFX will not play.");
            return;
        }
        var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var button in buttons)
        {

            button.onClick.RemoveListener(PlayButtonSfx);
            button.onClick.AddListener(PlayButtonSfx);
        }
    }

    private static void PlayButtonSfx()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonSfx();
        }
    }
}