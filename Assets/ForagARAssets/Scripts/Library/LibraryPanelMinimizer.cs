using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForagAR.Library
{
    /// <summary>
    /// Slides the bottom panel down until only its handle strip is left on screen, so the AR view
    /// with the placed mushrooms fills the screen. The arrow button on the handle toggles it back.
    /// The panel keeps its size while minimized, so the library and information section stay as
    /// they were and come back unchanged.
    /// </summary>
    public class LibraryPanelMinimizer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The panel to slide. Defaults to this object.")]
        RectTransform m_Panel;

        [SerializeField]
        [Tooltip("Button on the panel's handle that minimizes and expands it.")]
        Button m_ToggleButton;

        [SerializeField]
        [Tooltip("Label of the toggle button, swapped between the two arrows.")]
        TMP_Text m_ToggleLabel;

        [SerializeField]
        [Tooltip("How much of the top of the panel stays on screen while minimized, in canvas units.")]
        float m_MinimizedHeight = 96f;

        [SerializeField]
        [Tooltip("Seconds the slide takes.")]
        float m_Duration = 0.25f;

        [SerializeField]
        string m_MinimizeArrow = "▼";

        [SerializeField]
        string m_ExpandArrow = "▲";

        // 0 while expanded, 1 while minimized, in between while sliding.
        float m_Progress;

        /// <summary>
        /// Whether the panel is minimized or sliding down to it.
        /// </summary>
        public bool isMinimized { get; private set; }

        void Awake()
        {
            if (m_Panel == null)
                m_Panel = transform as RectTransform;
        }

        void OnEnable()
        {
            if (m_ToggleButton != null)
                m_ToggleButton.onClick.AddListener(Toggle);
        }

        void OnDisable()
        {
            if (m_ToggleButton != null)
                m_ToggleButton.onClick.RemoveListener(Toggle);
        }

        void Start()
        {
            UpdateLabel();
            ApplyPosition();
        }

        void Update()
        {
            var target = isMinimized ? 1f : 0f;
            var sliding = m_Progress != target;
            if (sliding)
            {
                m_Progress = m_Duration > 0f
                    ? Mathf.MoveTowards(m_Progress, target, Time.unscaledDeltaTime / m_Duration)
                    : target;
            }

            // Also runs while resting minimized, so the handle stays put if the screen rotates.
            if (sliding || isMinimized)
                ApplyPosition();
        }

        public void Toggle()
        {
            SetMinimized(!isMinimized);
        }

        public void SetMinimized(bool minimized)
        {
            isMinimized = minimized;
            UpdateLabel();
        }

        void ApplyPosition()
        {
            if (m_Panel == null)
                return;

            var hiddenHeight = Mathf.Max(0f, m_Panel.rect.height - m_MinimizedHeight);
            var eased = Mathf.SmoothStep(0f, 1f, m_Progress);
            m_Panel.anchoredPosition = new Vector2(m_Panel.anchoredPosition.x, -hiddenHeight * eased);
        }

        void UpdateLabel()
        {
            if (m_ToggleLabel != null)
                m_ToggleLabel.text = isMinimized ? m_ExpandArrow : m_MinimizeArrow;
        }
    }
}
