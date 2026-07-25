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
            "Certificate of Competency I",
            "Objective goal: Develop the learner's ability to safely assemble, disassemble, install, configure, and test computer systems. It focuses on identifying hardware components, selecting and using the proper tools and PPE, installing operating systems and drivers, configuring system settings, and verifying that the computer functions according to manufacturer specifications and workplace standards.\r\n",
            "COC I"); // replace with your actual scene name
    }

    public void ShowLesson2()
    {
        ShowOverview(lesson2Sprite,
            "Certificate of Competency II",
            "Objective goal: Equip learners with the skills to install, configure, and test basic computer networks. It emphasizes network planning, cable termination, device installation, IP addressing, network connectivity, and troubleshooting to ensure reliable communication between computers while following industry standards and occupational safety procedures.\r\n",
            "COC II"); // replace with your actual scene name
    }

    public void ShowLesson3()
    {
        ShowOverview(lesson3Sprite,
            "Certificate of Competency III",
            "Objective Goal: To prepare learners to install, configure, secure, and manage computer servers that provide network services. It includes creating user accounts, assigning access permissions, configuring server roles and services, implementing security measures, testing server functionality, and ensuring that the server operates efficiently based on organizational requirements.",
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
