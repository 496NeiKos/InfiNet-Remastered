using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
public class LessonOverviewManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject overviewPanel;       // Drag your OverviewPanel here
    public Image overviewImage;            // Drag the Image child here
    public TMP_Text overviewTitle;         // Drag the Title TMP_Text here
    public TMP_Text overviewDescription;   // Drag the Description TMP_Text here

    [Header("Lesson Data")]
    public Sprite lesson1Sprite;
    public Sprite lesson2Sprite;
    public Sprite lesson3Sprite;

    private string targetSceneName;

    private IEnumerator WaitAndLoadScene(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    // Generic method to show overview
    public void ShowOverview(Sprite image, string title, string description, string sceneName)
    {
        overviewPanel.SetActive(true);

        if (overviewImage != null) overviewImage.sprite = image;
        if (overviewTitle != null) overviewTitle.text = title;
        if (overviewDescription != null) overviewDescription.text = description;

        targetSceneName = sceneName;
    }

    // Wrapper methods for each lesson button
    public void ShowLesson1()
    {
        ShowOverview(lesson1Sprite,
            "Hardware",
            "This module focuses on hands-on tasks such as assembling and disassembling computer components. It helps students understand the proper handling, identification, and installation of hardware parts in a safe virtual environment.\r\n",
            "COC I"); // replace with your actual scene name
    }

    public void ShowLesson2()
    {
        ShowOverview(lesson2Sprite,
            "Software",
            "This module covers operating system installation and software setup, including the use of tools like Rufus for bootable media creation. It allows students to practice installing OS, configuring drivers, and performing basic system setup.\r\n",
            "Software"); // replace with your actual scene name
    }

    public void ShowLesson3()
    {
        ShowOverview(lesson3Sprite,
            "Networking",
            "This module introduces basic networking concepts and tasks such as cable management and patch panel configuration. It enables students to simulate network setup and troubleshooting in a structured and practical way",
            "Networking"); // replace with your actual scene name
    }

    // Called by Return button
    public void CloseOverview()
    {
        overviewPanel.SetActive(false);
    }

    // Called by Proceed button
    public void ProceedToLesson()
    {
        AudioClip sfx = SoundManager.instance.confirmSFX;
        SoundManager.instance.PlaySFX(sfx);

        if (!string.IsNullOrEmpty(targetSceneName))
            StartCoroutine(WaitAndLoadScene(targetSceneName, sfx.length));
    }
}
