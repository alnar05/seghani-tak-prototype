using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject panel;
    public EarthquakeDirector director;

    public bool IsPaused { get; private set; }

    void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Start()
    {
        if (panel) panel.SetActive(false);
    }

    void Update()
    {
        if (director && director.CurrentPhase == EarthquakeDirector.Phase.Ended) return;

        if (OverlayMenus.BlocksPause) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        if (director && director.CurrentPhase == EarthquakeDirector.Phase.Ended) return;
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (panel)
        {
            panel.SetActive(true);
            var first = panel.GetComponentInChildren<UnityEngine.UI.Selectable>();
            if (first) first.Select();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (director && director.CurrentPhase == EarthquakeDirector.Phase.Ended) return;
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (panel) panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameFlow.Retry();
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
