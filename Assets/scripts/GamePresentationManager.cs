using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

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
    public BattleManager battleManager;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip bgmClip;
    public AudioClip defeatClip;
    public AudioClip victoryClip;
    public AudioClip gameClearClip;
    public AudioClip buttonClickClip;
    public AudioClip cardDrawClip;
    public AudioClip cardClickClip;

    [Header("Card Draw SFX")]
    [Range(0f, 1f)] public float cardDrawVolume = 0.45f;
    public float cardDrawMaxDuration = 1.5f;

    private Coroutine cardDrawRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSfxSource();
        PlayBgm();
    }

    void Start()
    {
        if (startPanel != null)
            ShowStartScreen();

        RegisterButtonSounds();
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

        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();

        if (battleManager != null)
            battleManager.ContinueAfterBattleVictory();
        else if (mapManager != null)
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
        PlayButtonClick();
    }

    public void PlayButtonClick()
    {
        PlaySfx(buttonClickClip);
    }

    public void PlayCardDraw()
    {
        if (sfxSource == null || cardDrawClip == null) return;

        if (cardDrawRoutine != null)
            StopCoroutine(cardDrawRoutine);

        cardDrawRoutine = StartCoroutine(PlayCardDrawRoutine());
    }

    IEnumerator PlayCardDrawRoutine()
    {
        sfxSource.Stop();
        sfxSource.clip = cardDrawClip;
        sfxSource.volume = cardDrawVolume;
        sfxSource.loop = false;
        sfxSource.Play();

        yield return new WaitForSeconds(cardDrawMaxDuration);

        if (sfxSource.clip == cardDrawClip)
            sfxSource.Stop();

        sfxSource.clip = null;
        sfxSource.volume = 1f;
        cardDrawRoutine = null;
    }

    public void PlayCardClick()
    {
        PlaySfx(cardClickClip);
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

    void EnsureSfxSource()
    {
        if (sfxSource != null) return;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    public void RegisterButtonSounds()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            button.onClick.RemoveListener(PlayButtonClick);
            button.onClick.AddListener(PlayButtonClick);
        }
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
