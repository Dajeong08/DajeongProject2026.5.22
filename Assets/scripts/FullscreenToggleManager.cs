using UnityEngine;
using UnityEngine.InputSystem;

public class FullscreenToggleManager : MonoBehaviour
{
    public Key toggleKey = Key.F11;
    public FullScreenMode fullscreenMode = FullScreenMode.FullScreenWindow;
    public int defaultWindowedWidth = 1280;
    public int defaultWindowedHeight = 720;

    private int lastWindowedWidth;
    private int lastWindowedHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateInstance()
    {
        if (FindObjectOfType<FullscreenToggleManager>() != null) return;

        GameObject go = new GameObject("FullscreenToggleManager");
        DontDestroyOnLoad(go);
        go.AddComponent<FullscreenToggleManager>();
    }

    void Awake()
    {
        lastWindowedWidth = Mathf.Max(defaultWindowedWidth, Screen.width);
        lastWindowedHeight = Mathf.Max(defaultWindowedHeight, Screen.height);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard[toggleKey].wasPressedThisFrame) return;

        ToggleFullscreen();
    }

    void ToggleFullscreen()
    {
        if (Screen.fullScreen)
        {
            Screen.SetResolution(lastWindowedWidth, lastWindowedHeight, FullScreenMode.Windowed);
            return;
        }

        lastWindowedWidth = Mathf.Max(defaultWindowedWidth, Screen.width);
        lastWindowedHeight = Mathf.Max(defaultWindowedHeight, Screen.height);
        Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, fullscreenMode);
    }
}
