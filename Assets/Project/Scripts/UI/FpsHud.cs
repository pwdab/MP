using UnityEngine;

namespace MP.UI
{
    /*
        화면 오른쪽 위에 현재 FPS를 표시하는 디버그 HUD.
        재생 시 자동으로 생성되며 씬 전환에도 유지된다. 별도로 씬에 붙여도 중복 생성되지 않는다.
    */
    public sealed class FpsHud : MonoBehaviour
    {
        [Tooltip("FPS 표시값의 스무딩 계수입니다. 작을수록 부드럽고 느리게, 1에 가까울수록 즉각적으로 반응합니다.")]
        [SerializeField, Range(0.01f, 1f)] private float smoothingFactor = 0.1f;

        private static FpsHud instance;
        private float smoothedDeltaTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            new GameObject(nameof(FpsHud)).AddComponent<FpsHud>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            smoothedDeltaTime = Time.unscaledDeltaTime;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            smoothedDeltaTime += (Time.unscaledDeltaTime - smoothedDeltaTime) * smoothingFactor;
        }

        private void OnGUI()
        {
            float fps = smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;

            const float width = 96f;
            const float height = 28f;
            const float margin = 16f;
            var area = new Rect(Screen.width - width - margin, margin, width, height);

            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label($"FPS: {fps:0}");
            GUILayout.EndArea();
        }
    }
}
