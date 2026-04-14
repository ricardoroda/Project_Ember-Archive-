using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectEmber.Loading
{
    public static class SceneLoader
    {
        /// <summary>
        /// Load a scene asynchronously while showing the LoadingScreen.
        /// If manualActivation is true the loader will wait for user input before activating the scene.
        /// onReadyToActivate is called when loading reaches 90% and is ready to be activated.
        /// </summary>
        public static void Load(string sceneName, bool manualActivation = false, Action onReadyToActivate = null)
        {
            if(SceneManager.GetActiveScene().buildIndex != 0)
            {
                GameObject.FindWithTag("Player").GetComponent<PlayerStats>().SavePLayerDataToSaveSO();
            }

            LoadingBehaviour.Instance.StartCoroutine(LoadCoroutine(sceneName, manualActivation, onReadyToActivate));
        }

        /// <summary>
        /// Load a scene by build index and show the LoadingScreen.
        /// </summary>
        public static void Load(int buildIndex, bool manualActivation = false, Action onReadyToActivate = null)
        {
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                GameObject.FindWithTag("Player").GetComponent<PlayerStats>().SavePLayerDataToSaveSO();
            }

            LoadingBehaviour.Instance.StartCoroutine(LoadByIndexCoroutine(buildIndex, manualActivation, onReadyToActivate));
        }

        static IEnumerator LoadCoroutine(string sceneName, bool manualActivation, Action onReadyToActivate)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            // Let LoadingScreen decide about manual activation if a LoadingScreen exists (must be created in the Editor)
            var ls = LoadingScreen.Instance;
            if (ls != null)
                ls.ShowForAsyncOperation(op, "Loading " + sceneName, manualActivation, onReadyToActivate);
            while (!op.isDone)
                yield return null;
        }

        static IEnumerator LoadByIndexCoroutine(int buildIndex, bool manualActivation, Action onReadyToActivate)
        {
            var op = SceneManager.LoadSceneAsync(buildIndex);
            var ls2 = LoadingScreen.Instance;
            if (ls2 != null)
                ls2.ShowForAsyncOperation(op, "Loading...", manualActivation, onReadyToActivate);
            while (!op.isDone)
                yield return null;
        }
    }

    // Helper MonoBehaviour used to run coroutines from static API
    class LoadingBehaviour : MonoBehaviour
    {
        static LoadingBehaviour _instance;
        public static LoadingBehaviour Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("_LoadingBehaviour");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<LoadingBehaviour>();
                }
                return _instance;
            }
        }
    }
}
