using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Text Parameters")]
    public TextMeshProUGUI TimerText;
    public float timerCount = 0.0f;

    void Start()
    {
        StartCoroutine(UpdateTimer());
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    IEnumerator UpdateTimer()
    {
        while (true)
        {
            timerCount += 0.01f;
            TimerText.text = timerCount.ToString("F2") + " s";
            yield return new WaitForSeconds(0.01f);
        }
    }
    
    public void ResetTimer()
    {
        timerCount = 0.0f;
        TimerText.text = timerCount+" s";
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
