using UnityEngine;
using System;
using System.Collections.Generic;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.UI;
using UnityEngine.UI;

namespace LoreLegacyMonsters
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] Canvas rootCanvas;
        bool _loadingOverlayEnsured;
        readonly Dictionary<UiModal, bool> modalStates = new Dictionary<UiModal, bool>();
        string currentToast;
        string loadingMessage = "Loading...";
        float toastUntil;
        RectTransform loadingRoot;
        Text loadingText;

        /// <summary>Ensures overlay canvas exists (lazy init if something accesses UI before <see cref="Awake"/> finishes).</summary>
        public Canvas Root
        {
            get
            {
                if (rootCanvas == null)
                    rootCanvas = RuntimeUiFactory.EnsureCanvas(ref rootCanvas);
                if (!_loadingOverlayEnsured && rootCanvas != null)
                {
                    EnsureLoadingOverlay();
                    _loadingOverlayEnsured = true;
                }

                return rootCanvas;
            }
        }
        public string CurrentToast => Time.unscaledTime <= toastUntil ? currentToast : string.Empty;
        public string LoadingMessage => loadingMessage;
        public bool IsBlockingWorldInput => HasOpenBlockingModal();
        public event Action ModalStateChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            rootCanvas = RuntimeUiFactory.EnsureCanvas(ref rootCanvas);
            EnsureLoadingOverlay();
            _loadingOverlayEnsured = true;
            if (GetComponent<TooltipOverlay>() == null)
                gameObject.AddComponent<TooltipOverlay>();
            SeedModalStateDefaults();
        }

        void OnEnable()
        {
            GameEvents.ToastRequested += OnToastRequested;
        }

        void OnDisable()
        {
            GameEvents.ToastRequested -= OnToastRequested;
        }

        void OnToastRequested(string message) => ShowToast(message);

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (!string.IsNullOrEmpty(currentToast) && Time.unscaledTime > toastUntil)
                currentToast = string.Empty;
        }

        public bool IsModalOpen(UiModal modal) =>
            modalStates.TryGetValue(modal, out var isOpen) && isOpen;

        public bool IsModalBlocking(UiModal modal) => UiModalRegistry.Get(modal).BlocksWorldInput;

        public void SetModalOpen(UiModal modal, bool isOpen)
        {
            if (IsModalOpen(modal) == isOpen) return;
            modalStates[modal] = isOpen;
            ModalStateChanged?.Invoke();
        }

        public void ToggleModal(UiModal modal) => SetModalOpen(modal, !IsModalOpen(modal));

        bool HasOpenBlockingModal()
        {
            foreach (var pair in modalStates)
            {
                if (!pair.Value || !UiModalRegistry.Get(pair.Key).BlocksWorldInput)
                    continue;
                return true;
            }

            return false;
        }

        void SeedModalStateDefaults()
        {
            var all = (UiModal[])Enum.GetValues(typeof(UiModal));
            for (var i = 0; i < all.Length; i++)
                if (!modalStates.ContainsKey(all[i]))
                    modalStates[all[i]] = false;
        }

        public void ShowToast(string message, float duration = 3.5f)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            currentToast = message.Trim();
            toastUntil = Time.unscaledTime + Mathf.Max(0.5f, duration);
        }

        public void BeginLoading(string message = null)
        {
            loadingMessage = string.IsNullOrWhiteSpace(message) ? "Loading..." : message.Trim();
            if (loadingText != null) loadingText.text = loadingMessage;
            if (loadingRoot != null) loadingRoot.gameObject.SetActive(true);
            SetModalOpen(UiModal.Loading, true);
        }

        public void EndLoading()
        {
            SetModalOpen(UiModal.Loading, false);
            loadingMessage = "Loading...";
            if (loadingText != null) loadingText.text = loadingMessage;
            if (loadingRoot != null) loadingRoot.gameObject.SetActive(false);
        }

        void EnsureLoadingOverlay()
        {
            if (loadingRoot != null || rootCanvas == null) return;
            loadingRoot = RuntimeUiFactory.CreatePanel(rootCanvas.transform, "LoadingOverlay",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.72f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            loadingText = RuntimeUiFactory.CreateText(loadingRoot, "LoadingText", loadingMessage, 28,
                TextAnchor.MiddleCenter, GameVisualTheme.Accent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            loadingRoot.gameObject.SetActive(false);
        }
    }
}
