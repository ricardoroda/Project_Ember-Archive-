using UnityEngine;

namespace ProjectEmber.Loading
{
    [CreateAssetMenu(fileName = "LoadingConfig", menuName = "ProjectEmber/LoadingConfig", order = 10)]
    public class LoadingConfig : ScriptableObject
    {
        [Header("Visual")]
        public Color backgroundColor = new Color(0f, 0f, 0f, 1f);
        public Color progressColor = new Color(0.2f, 0.6f, 1f, 1f);

        [Header("Timing")]
        public float fadeDuration = 0.3f;
    public float minDisplayTime = 5f;
        public float progressSmoothing = 8f;

        [Header("Tips")]
        [TextArea]
        public string[] tips = new string[] { "Tip: Press 'Space' to dodge", "Tip: Another cool tip", "Tip: Idk man gl" };

        [Header("Input")]
        public bool useNewInputSystem = true;
        public string continueActionMap = "Player";
        public string continueActionName = "Interact";
        
    [Header("Prefab")]
    [Tooltip("Optional prefab to use for the loading UI")]
    public GameObject loadingPrefab;
    [Tooltip("If true and a loadingPrefab is set, the system will try to use it")]
    public bool preferPrefab = false;
        
        [Header("Font")]
        [Tooltip("Optional font to use for the loading screen UI")]
        public Font font;
    }
}
