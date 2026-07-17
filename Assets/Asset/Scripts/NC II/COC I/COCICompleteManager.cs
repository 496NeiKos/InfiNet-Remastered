using UnityEngine;
using UnityEngine.SceneManagement;

public class COCICompleteManager : MonoBehaviour
{
    [SerializeField] private GameObject cocICompletePanel;

    private void OnEnable()
    {
        TopicManager.OnAllTopicsComplete += ShowPanel;
    }

    private void OnDisable()
    {
        TopicManager.OnAllTopicsComplete -= ShowPanel;
    }

    private void ShowPanel()
    {
        cocICompletePanel.SetActive(true);
    }

    public void ClosePanel()
    {
        cocICompletePanel.SetActive(false);
    }

    public void ExitToLessonSelection()
    {
        SceneManager.LoadScene("LessonSelection");
    }

    public void LoadNextCOC()
    {
        SceneManager.LoadScene("COC II");
    }
}
