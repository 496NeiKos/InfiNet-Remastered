using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TaskListManager : MonoBehaviour
{
    [System.Serializable]
    public class TaskItem
    {
        public string description;
        public Text uiText;
        public bool isCompleted;
    }

    public List<TaskItem> tasks;

    private Color notDoneColor = Color.white;
    private Color doneColor = Color.green; // grayish

    public static TaskListManager Instance;

    [Header("Completion UI")]
    public GameObject completionPanel; // assign in Inspector

    void Awake()
    {
        Instance = this;
        ResetTasks();
        if (completionPanel != null)
            completionPanel.SetActive(false);
    }

    public void ResetTasks()
    {
        foreach (var task in tasks)
        {
            task.isCompleted = false;
            task.uiText.color = notDoneColor;
        }

        if (completionPanel != null)
            completionPanel.SetActive(false);

        Time.timeScale = 1f; // resume gameplay when resetting
    }

    public void SetTaskCompleted(int index, bool completed)
    {
        if (index < 0 || index >= tasks.Count) return;

        // Special case: first task = motherboard installation
        if (index == 0)
        {
            Motherboard mb = FindObjectOfType<Motherboard>();
            if (mb != null && mb.AreAllSlotsCorrect())
            {
                tasks[index].isCompleted = true;
                tasks[index].uiText.color = doneColor; // ✅ green when correct
            }
            else
            {
                tasks[index].isCompleted = false;
                tasks[index].uiText.color = notDoneColor; // stays white if wrong
            }
        }
        else
        {
            // Normal tasks behave as before
            tasks[index].isCompleted = completed;
            tasks[index].uiText.color = completed ? doneColor : notDoneColor;
        }

        CheckStageCompletion();
    }


    public bool IsTaskCompleted(int index)
    {
        if (index < 0 || index >= tasks.Count) return false;
        return tasks[index].isCompleted;
    }

    public void CheckStageCompletion()
    {
        bool allComplete = true;

        foreach (TaskItem task in tasks)
        {
            if (!task.isCompleted)
            {
                allComplete = false;
                break;
            }
        }

        if (allComplete && completionPanel != null)
        {
            completionPanel.SetActive(true); // show popup
            Time.timeScale = 0f;             // freeze gameplay
            LockStage(); // ✅ disable stage scripts
        }
    }

    public void ExitToLessonSelection()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("LessonSelection");
    }

    public void ReturnToStage()
    {
        completionPanel.SetActive(false);
        Time.timeScale = 1f; // resume gameplay
        UnlockStage(); // ✅ re-enable stage scripts
    }

    public void RestartStage()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void LockStage()
    {
        foreach (var avr in FindObjectsOfType<AVR>(true))
            avr.enabled = false;

        foreach (var button in FindObjectsOfType<Button>(true))
        {
            if (button.gameObject.name == "AVR") // or tag it "AVR"
                button.interactable = false;
        }

        foreach (var cable in FindObjectsOfType<VGACable>(true))
            cable.enabled = false;

        foreach (var descslot in FindObjectsOfType<DescSlot>(true))
            descslot.enabled = false;

        foreach (var componentitem in FindObjectsOfType<ComponentItem>(true))
            componentitem.enabled = false;

        foreach (var dragitem in FindObjectsOfType<DragItem>(true))
            dragitem.enabled = false;

        foreach (var systemUnit in FindObjectsOfType<SystemUnit>(true))
            systemUnit.enabled = false;

        foreach (var motherboard in FindObjectsOfType<Motherboard>(true))
            motherboard.enabled = false;
    }

    public void UnlockStage()
    {
        foreach (var avr in FindObjectsOfType<AVR>(true))
            avr.enabled = true;

        foreach (var button in FindObjectsOfType<Button>(true))
        {
            if (button.gameObject.name == "AVR") // or tag it "AVR"
                button.interactable = true;
        }

        foreach (var cable in FindObjectsOfType<VGACable>(true))
            cable.enabled = true;

        foreach (var descslot in FindObjectsOfType<DescSlot>(true))
            descslot.enabled = true;

        foreach (var componentitem in FindObjectsOfType<ComponentItem>(true))
            componentitem.enabled = true;

        foreach (var dragitem in FindObjectsOfType<DragItem>(true))
            dragitem.enabled = true;

        foreach (var systemUnit in FindObjectsOfType<SystemUnit>(true))
            systemUnit.enabled = true;

        foreach (var motherboard in FindObjectsOfType<Motherboard>(true))
            motherboard.enabled = true;
    }



}
