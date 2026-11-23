using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CSSModuleManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown moduleDropdown;
    public Image contentImage;
    public Button prevButton;
    public Button nextButton;
    public TMP_Text pageCounterText;

    [Header("Module Content")]
    public ModuleData[] modules;

    private int currentPage;

    [Serializable]
    public class ModuleData
    {
        public string moduleName;
        public Sprite[] pages;
    }

    void Start()
    {
        moduleDropdown.onValueChanged.AddListener(OnModuleSelected);
        prevButton.onClick.AddListener(OnPrev);
        nextButton.onClick.AddListener(OnNext);

        int savedIndex = Mathf.Clamp(ModuleSessionData.SelectedModuleIndex, 0, modules.Length - 1);
        moduleDropdown.SetValueWithoutNotify(savedIndex);
        LoadModule(savedIndex);
    }

    void OnModuleSelected(int index)
    {
        LoadModule(index);
    }

    void LoadModule(int index)
    {
        currentPage = 0;
        RefreshPage();
    }

    void OnPrev()
    {
        int total = modules[moduleDropdown.value].pages.Length;
        if (total == 0) return;
        currentPage = Mathf.Max(0, currentPage - 1);
        RefreshPage();
    }

    void OnNext()
    {
        int total = modules[moduleDropdown.value].pages.Length;
        if (total == 0) return;
        currentPage = Mathf.Min(total - 1, currentPage + 1);
        RefreshPage();
    }

    void RefreshPage()
    {
        ModuleData mod = modules[moduleDropdown.value];
        int total = mod.pages.Length;

        if (total == 0)
        {
            contentImage.gameObject.SetActive(false);
            pageCounterText.text = "0 / 0";
            prevButton.interactable = false;
            nextButton.interactable = false;
            return;
        }

        contentImage.gameObject.SetActive(true);
        contentImage.sprite = mod.pages[currentPage];
        pageCounterText.text = $"{currentPage + 1} / {total}";
        prevButton.interactable = currentPage > 0;
        nextButton.interactable = currentPage < total - 1;
    }

    void OnDestroy()
    {
        moduleDropdown.onValueChanged.RemoveListener(OnModuleSelected);
        prevButton.onClick.RemoveListener(OnPrev);
        nextButton.onClick.RemoveListener(OnNext);
    }
}
