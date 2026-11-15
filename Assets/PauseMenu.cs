using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    public GameObject panel, hud;
    public EarthquakeDirector director;
    public bool IsPaused { get; private set; }

    bool RoundEnded => director && director.CurrentPhase == EarthquakeDirector.Phase.Ended;

    void Start()
    {
        if (panel) panel.SetActive(false);
    }

    void Update()
    {
        if (RoundEnded || OverlayMenus.BlocksPause || !Input.GetKeyDown(KeyCode.Escape)) return;
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (RoundEnded || IsPaused) return;

        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (hud) hud.SetActive(false);
        if (panel) panel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (RoundEnded || !IsPaused) return;

        IsPaused = false;
        if (panel) panel.SetActive(false);
        if (hud) hud.SetActive(true);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void RestartLevel() => GameFlow.Retry();
    public void QuitGame() => GameFlow.Quit();
}
