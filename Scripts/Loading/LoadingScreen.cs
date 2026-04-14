using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectEmber.Loading
{
    [DefaultExecutionOrder(-1000)]
    public class LoadingScreen : MonoBehaviour
    {
        static LoadingScreen _instance;
        public static LoadingScreen Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find an existing LoadingScreen in the scene (including inactive) to avoid creating duplicates
                    try
                    {
                        // Use the newer API where available for faster lookup
                        LoadingScreen existing = null;
                        try
                        {
                            // Prefer FindAnyObjectByType if available
                            var method = typeof(UnityEngine.Object).GetMethod("FindAnyObjectByType");
                            if (method != null)
                            {
                                existing = (LoadingScreen)method.Invoke(null, new object[] { typeof(LoadingScreen) });
                            }
                        }
                        catch { }

                        if (existing == null)
                        {
                            try
                            {
                                var method2 = typeof(UnityEngine.Object).GetMethod("FindFirstObjectByType");
                                if (method2 != null)
                                {
                                    existing = (LoadingScreen)method2.Invoke(null, new object[] { typeof(LoadingScreen) });
                                }
                            }
                            catch { }
                        }

                        if (existing == null)
                        {
                            try
                            {
                                // Try to locate any instance using the older API via reflection to avoid direct obsolete call
                                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", new System.Type[] { typeof(System.Type) });
                                if (findMethod != null)
                                {
                                    var obj = findMethod.Invoke(null, new object[] { typeof(LoadingScreen) });
                                    existing = obj as LoadingScreen;
                                }
                            }
                            catch { }
                        }

                        if (existing != null)
                        {
                            _instance = existing;
                            return _instance;
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    // If no existing LoadingScreen found, only create one at runtime if a LoadingConfig with a prefab exists
                    try
                    {
                        var cfg = Resources.Load<LoadingConfig>("LoadingConfig");
                        if (cfg != null && cfg.loadingPrefab != null)
                        {
                            var go = new GameObject("LoadingScreen");
                            DontDestroyOnLoad(go);
                            _instance = go.AddComponent<LoadingScreen>();
                            return _instance;
                        }
                    }
                    catch { }

                    // No config/prefab available — do not create automatically
                    return null;
                }
                return _instance;
            }
        }

        Canvas _canvas;
        CanvasGroup _canvasGroup;
        Image _background;
        Text _label;
        Text _tip;
        Slider _progressBar;
    GameObject _prefabInstance;


            Font GetSafeBuiltinFont()
            {
                Font f = null;
                try
                {
                    f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                catch { }
                if (f == null)
                {
                    try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                }
                if (f == null)
                {
                    try
                    {
                
                        f = Font.CreateDynamicFontFromOSFont("Arial", 16);
                    }
                    catch { }
                }
                return f;
            }

        Coroutine _current;
        LoadingConfig _config;
    bool _continueRequested = false;

#if ENABLE_INPUT_SYSTEM
    InputSystemActions _generatedInputActions;
#endif

        // Smooth progress display
        float _displayProgress = 0f;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _config = Resources.Load<LoadingConfig>("LoadingConfig");
            CreateUI();
            HideImmediate(true);
            TryBindInputSystem();
        }

        void OnDestroy()
        {
            CleanupBindings();
        }

        void CleanupBindings()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
#if ENABLE_INPUT_SYSTEM
            if (_generatedInputActions != null && _config != null && _config.useNewInputSystem)
            {
                try
                {
                    _generatedInputActions.Player.Interact.performed -= OnGeneratedContinue;
                    _generatedInputActions.Player.Disable();
                    _generatedInputActions.Dispose();
                }
                catch { }
                _generatedInputActions = null;
            }
#endif
        }

        void TryBindInputSystem()
        {
#if ENABLE_INPUT_SYSTEM
            if (_config != null && _config.useNewInputSystem)
            {
                try
                {
                    if (_generatedInputActions == null)
                        _generatedInputActions = new InputSystemActions();

          
                    _generatedInputActions.Player.Enable();

             
                    _generatedInputActions.Player.Interact.performed += OnGeneratedContinue;
                }
                catch (Exception)
                {
           
                }
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        void OnGeneratedContinue(InputAction.CallbackContext ctx)
        {
            _continueRequested = true;
        }
#endif

        void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        void CreateUI()
        {
            if (_config != null && _config.preferPrefab && _config.loadingPrefab != null)
            {
                try
                {
                    _prefabInstance = Instantiate(_config.loadingPrefab, transform);
                    _prefabInstance.name = "LoadingPrefabInstance";
                    _canvasGroup = _prefabInstance.GetComponent<CanvasGroup>() ?? _prefabInstance.AddComponent<CanvasGroup>();
          
                    _progressBar = _prefabInstance.GetComponentInChildren<Slider>(true);
                    var texts = _prefabInstance.GetComponentsInChildren<Text>(true);
                    if (texts != null && texts.Length > 0)
                    {
                        _label = Array.Find(texts, t => t.name.IndexOf("label", StringComparison.OrdinalIgnoreCase) >= 0) ?? texts[0];
                        _tip = Array.Find(texts, t => t.name.IndexOf("tip", StringComparison.OrdinalIgnoreCase) >= 0) ?? (texts.Length > 1 ? texts[1] : texts[0]);
                        if (_config != null && _config.font != null)
                        {
                            foreach (var t in texts)
                            {
                                try { t.font = _config.font; } catch { }
                            }
                        }
                    }
                    _background = _prefabInstance.GetComponentInChildren<Image>(true);
                   
                    if (_canvasGroup != null && _progressBar != null && _label != null)
                    {
                        return;
                    }
                    else
                    {
                        DestroyImmediate(_prefabInstance);
                        _prefabInstance = null;
                    }
                }
                catch
                {
                    if (_prefabInstance != null) DestroyImmediate(_prefabInstance);
                    _prefabInstance = null;
                }
            }
            // Canvas
            var canvasGO = new GameObject("LoadingCanvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Ensure the loading canvas always renders on top of everything else
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 10000;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();

            // Background panel
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            _background = bgGO.AddComponent<Image>();
            _background.color = _config != null ? _config.backgroundColor : new Color(0f, 0f, 0f, 1f);
            var bgRect = _background.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Progress container
            var containerGO = new GameObject("ProgressContainer");
            containerGO.transform.SetParent(canvasGO.transform, false);
            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.12f, 0.12f);
            containerRect.anchorMax = new Vector2(0.88f, 0.28f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Progress background
            var pbBgGO = new GameObject("ProgressBG");
            pbBgGO.transform.SetParent(containerGO.transform, false);
            var pbBg = pbBgGO.AddComponent<Image>();
            pbBg.color = new Color(1f, 1f, 1f, 0.06f);
            var pbBgRect = pbBg.rectTransform;
            pbBgRect.anchorMin = new Vector2(0f, 0.15f);
            pbBgRect.anchorMax = new Vector2(1f, 0.85f);
            pbBgRect.offsetMin = Vector2.zero;
            pbBgRect.offsetMax = Vector2.zero;

            // Progress bar
            var pbGO = new GameObject("ProgressBar");
            pbGO.transform.SetParent(pbBgGO.transform, false);
            _progressBar = pbGO.AddComponent<Slider>();
            _progressBar.interactable = false;
            var pbRect = _progressBar.GetComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.02f, 0.1f);
            pbRect.anchorMax = new Vector2(0.98f, 0.9f);
            pbRect.offsetMin = Vector2.zero;
            pbRect.offsetMax = Vector2.zero;

            // Fill area
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(pbGO.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = _config != null ? _config.progressColor : new Color(0.2f, 0.6f, 1f, 1f);
            var fillRect = fillImg.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            _progressBar.fillRect = fillRect;

            // Label
            var labelGO = new GameObject("LoadingLabel");
            labelGO.transform.SetParent(canvasGO.transform, false);
            _label = labelGO.AddComponent<Text>();
            _label.alignment = TextAnchor.MiddleCenter;
            if (_config != null && _config.font != null)
                _label.font = _config.font;
            else
                _label.font = GetSafeBuiltinFont();
            _label.fontSize = 26;
            _label.color = Color.white;
            var labelRect = _label.rectTransform;
            labelRect.anchorMin = new Vector2(0.12f, 0.3f);
            labelRect.anchorMax = new Vector2(0.88f, 0.38f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            // Tip text
            var tipGO = new GameObject("LoadingTip");
            tipGO.transform.SetParent(canvasGO.transform, false);
            _tip = tipGO.AddComponent<Text>();
            _tip.alignment = TextAnchor.MiddleCenter;
            if (_config != null && _config.font != null)
                _tip.font = _config.font;
            else
                _tip.font = GetSafeBuiltinFont();
            _tip.fontSize = 16;
            _tip.color = new Color(1f, 1f, 1f, 0.85f);
            var tipRect = _tip.rectTransform;
            tipRect.anchorMin = new Vector2(0.12f, 0.2f);
            tipRect.anchorMax = new Vector2(0.88f, 0.28f);
            tipRect.offsetMin = Vector2.zero;
            tipRect.offsetMax = Vector2.zero;
        }

        void OnActiveSceneChanged(Scene prev, Scene next)
        {
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(ShowQuick());
        }

        public bool IsShowing => _canvasGroup != null && _canvasGroup.alpha > 0.001f;

        public event Action OnShown;
        public event Action OnHidden;

        IEnumerator ShowQuick()
        {
            yield return ShowFadeIn("Loading...");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return ShowFadeOut(0.25f);
            _current = null;
        }

        IEnumerator ShowFadeIn(string message)
        {
            _label.text = message;
            _progressBar.value = 0f;
            _displayProgress = 0f;
            _tip.text = GetRandomTip();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            float dur = _config != null ? _config.fadeDuration : 0.25f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / dur);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
            OnShown?.Invoke();
        }

        IEnumerator ShowFadeOut(float waitBefore)
        {
            yield return new WaitForSecondsRealtime(waitBefore);
            float dur = _config != null ? _config.fadeDuration : 0.25f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / dur);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            OnHidden?.Invoke();
        }

        void HideImmediate(bool force = false)
        {
            if (_background == null) return;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        string GetRandomTip()
        {
            if (_config == null || _config.tips == null || _config.tips.Length == 0) return string.Empty;
            int idx = UnityEngine.Random.Range(0, _config.tips.Length);
            return _config.tips[idx];
        }

        public void ShowForAsyncOperation(AsyncOperation op, string message = "Loading...", bool manualActivation = false, Action onReadyToActivate = null)
        {
            if (op == null) return;
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(TrackAsync(op, message, manualActivation, onReadyToActivate));
        }

        public void Continue()
        {
            _continueRequested = true;
        }

        IEnumerator TrackAsync(AsyncOperation op, string message, bool manualActivation, Action onReadyToActivate)
        {
            if (_label == null || _tip == null || _progressBar == null || _canvasGroup == null)
            {
                try
                {
                    CreateUI();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("LoadingScreen UI creation failed: " + ex.Message);
                }
            }

            if (_label == null || _tip == null || _progressBar == null || _canvasGroup == null)
            {
                Debug.LogWarning("LoadingScreen: UI unavailable, proceeding without overlay.");
                while (!op.isDone) yield return null;
                yield break;
            }

            _label.text = message;
            _tip.text = GetRandomTip();
            yield return ShowFadeIn(message);

            if (manualActivation)
                op.allowSceneActivation = false;

            _displayProgress = 0f;
            float smoothing = _config != null ? _config.progressSmoothing : 8f;
            float minShow = _config != null ? _config.minDisplayTime : 5f;
            float startTime = Time.realtimeSinceStartup;

            while (!op.isDone)
            {
                float target = Mathf.Clamp01(op.progress / 0.9f);
                if (op.progress >= 0.9f) target = 1f;

                _displayProgress = Mathf.Lerp(_displayProgress, target, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
                _progressBar.value = _displayProgress;
                _label.text = message + " " + Mathf.RoundToInt(_displayProgress * 100f) + "%";

                if (manualActivation && op.progress >= 0.9f)
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    if (elapsed < minShow)
                    {
                        yield return null;
                        continue;
                    }

                    onReadyToActivate?.Invoke();


                    _label.text = "Press any key to continue";
                    bool continued = false;
                    while (!continued)
                    {
#if ENABLE_INPUT_SYSTEM
                        if (_config != null && _config.useNewInputSystem && _generatedInputActions != null)
                        {
                        }
#endif
                        if (Input.anyKeyDown || _continueRequested)
                        {
                            continued = true;
                        }
                        yield return null;
                    }
                    _continueRequested = false;
                    op.allowSceneActivation = true;
                }

                yield return null;
            }


            {
                float elapsedSinceStart = Time.realtimeSinceStartup - startTime;
                if (elapsedSinceStart < minShow)
                {
                    yield return new WaitForSecondsRealtime(minShow - elapsedSinceStart);
                }
            }

            _progressBar.value = 1f;
            _label.text = "Finalizing...";
            yield return new WaitForSecondsRealtime(0.15f);
            yield return ShowFadeOut(0.05f);
            _current = null;
        }
    }
}
