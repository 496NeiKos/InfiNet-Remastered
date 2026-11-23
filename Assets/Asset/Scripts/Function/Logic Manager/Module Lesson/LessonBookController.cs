using UnityEngine;

public class LessonBookController : MonoBehaviour
{
    public void OpenModule(int moduleIndex)
    {
        ModuleSessionData.SelectedModuleIndex = moduleIndex;
        SceneController.Instance.LoadScene("HandTools");
    }
}
