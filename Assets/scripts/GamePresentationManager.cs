using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePresentationManager : MonoBehaviour
{
    public static GamePresentationManager Instance { get; private set; }

    [Header("Screens")]
    public GameObject startPanel;
    public GameObject defeatPanel;
    public GameObject victoryPanel;
    public GameObject gameClearPanel;
    public GameObject[] gameplayPanels;

    [Header("Managers")]
    public MapManager mapManager;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip bgmClip;
    public AudioClip defeatClip;
    public AudioClip victoryClip;
    public AudioClip gameClearClip;
    public AudioClip healButtonClip;
    public AudioClip cardDrawClip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        PlayBgm();
    }

    void Start()
    {
        if (startPanel != null)
            ShowStartScreen();
    }

    public void ShowStartScreen()
    {
        HideAllResultScreens();
        SetGameplayPanels(false);
        SetPanel(startPanel, true);
    }

    public void StartGame()
    {
        HideAllResultScreens();
        SetPanel(startPanel, false);
        SetGameplayPanels(true);
    }

    public void ShowDefeat()
    {
        PlaySfx(defeatClip);
        HideAllResultScreens();
        SetGameplayPanels(false);
        SetPanel(defeatPanel, true);
    }

    public void ShowVictory()
    {
        PlaySfx(victoryClip);
        HideAllResultScreens();

        if (victoryPanel == null)
        {
            ContinueAfterVictory();
            return;
        }

        SetGameplayPanels(false);
        SetPanel(victoryPanel, true);
    }

    public void ContinueAfterVictory()
    {
        HideAllResultScreens();
        SetGameplayPanels(true);

        if (mapManager == null)
            mapManager = FindObjectOfType<MapManager>();

        if (mapManager != null)
            mapManager.FinishRound();
    }

    public void ShowGameClear()
    {
        PlaySfx(gameClearClip);
        HideAllResultScreens();
        SetGameplayPanels(false);
        SetPanel(gameClearPanel, true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void PlayHealButton()
    {
        PlaySfx(healButtonClip);
    }

    public void PlayCardDraw()
    {
        PlaySfx(cardDrawClip);
    }

    void PlayBgm()
    {
        if (bgmSource == null || bgmClip == null) return;

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    void HideAllResultScreens()
    {
        SetPanel(defeatPanel, false);
        SetPanel(victoryPanel, false);
        SetPanel(gameClearPanel, false);
    }

    void SetGameplayPanels(bool isActive)
    {
        if (gameplayPanels == null) return;

        foreach (GameObject panel in gameplayPanels)
        {
            SetPanel(panel, isActive);
        }
    }

    void SetPanel(GameObject panel, bool isActive)
    {
        if (panel != null)
            panel.SetActive(isActive);
    }
}
