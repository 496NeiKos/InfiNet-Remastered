/*
 * ================================================================
 *  UNITY SETUP GUIDE — WindowsInstallationManager
 * ================================================================
 *  COMPONENT PLACEMENT
 *    Add this script to the FourthPhase GameObject.
 *
 *  HIERARCHY EXPECTED
 *
 *    FourthPhase  (this script here)
 *      ├─ Title
 *      ├─ Status
 *      └─ StatusDisplay
 *           ├─ Copying Windows files          → stepLabels[0]
 *           ├─ Getting files ready for installation → stepLabels[1]
 *           ├─ Installing features            → stepLabels[2]
 *           ├─ Installing updates             → stepLabels[3]
 *           └─ Finishing up                  → stepLabels[4]
 *
 *  VISUAL STATES PER STEP LABEL
 *    • Not yet started  — black, alpha 0.80  (dimmed)
 *    • Currently active — black, alpha 1.00  (full, percentage counting up)
 *    • Completed        — black, alpha 1.00  (full, locked at 100%)
 *
 *  HOW IT WORKS
 *    Call StartInstallation() (from WindowsSetupNavigator) every time
 *    FourthPhase becomes active. It runs through each step in order,
 *    counting the percentage from 0 → 100 over stepDurations[i] seconds.
 *    When all five steps finish, OnInstallationComplete() fires and
 *    notifies the navigator.
 *
 *  INSPECTOR ASSIGNMENTS
 *    stepLabels[0] → StatusDisplay > Copying Windows files          (TextMeshProUGUI)
 *    stepLabels[1] → StatusDisplay > Getting files ready for installation
 *    stepLabels[2] → StatusDisplay > Installing features
 *    stepLabels[3] → StatusDisplay > Installing updates
 *    stepLabels[4] → StatusDisplay > Finishing up
 *
 *    stepDurations  — time in seconds each step takes (default 5/3/4/4/3)
 *    navigator      → WindowsSetupPanel > WindowsSetupNavigator component
 * ================================================================
 */

using System.Collections;
using TMPro;
using UnityEngine;

public class WindowsInstallationManager : MonoBehaviour
{
    [Header("Step Labels  (StatusDisplay children, in order)")]
    [SerializeField] private TextMeshProUGUI[] stepLabels;

    [Header("Duration of each step in seconds")]
    [SerializeField] private float[] stepDurations = { 5f, 3f, 4f, 4f, 3f };

    [Header("Navigation")]
    [SerializeField] private WindowsSetupNavigator navigator;

    // ----------------------------------------------------------------
    //  Visual constants
    // ----------------------------------------------------------------

    private static readonly Color ActiveColor  = new Color(0f, 0f, 0f, 1.00f); // black, full opacity
    private static readonly Color PendingColor = new Color(0f, 0f, 0f, 0.80f); // black, 80% opacity

    // ----------------------------------------------------------------
    //  Runtime state
    // ----------------------------------------------------------------

    private string[] originalLabels;   // text captured from each TMP on first run
    private Coroutine installCoroutine;

    // ----------------------------------------------------------------
    //  Public entry point — called by WindowsSetupNavigator
    // ----------------------------------------------------------------

    public void StartInstallation()
    {
        if (installCoroutine != null)
            StopCoroutine(installCoroutine);

        CacheOriginalLabels();
        installCoroutine = StartCoroutine(RunInstallation());
    }

    // ----------------------------------------------------------------
    //  Label caching — read original text from each TMP once
    // ----------------------------------------------------------------

    private void CacheOriginalLabels()
    {
        if (originalLabels != null && originalLabels.Length == stepLabels.Length) return;

        originalLabels = new string[stepLabels.Length];
        for (int i = 0; i < stepLabels.Length; i++)
        {
            if (stepLabels[i] != null)
                originalLabels[i] = stepLabels[i].text;
        }
    }

    // ----------------------------------------------------------------
    //  Main installation coroutine
    // ----------------------------------------------------------------

    private IEnumerator RunInstallation()
    {
        // Set all steps to pending state at the start
        for (int i = 0; i < stepLabels.Length; i++)
            SetStepState(i, StepState.Pending, 0);

        for (int step = 0; step < stepLabels.Length; step++)
        {
            // Activate current step
            SetStepState(step, StepState.Active, 0);

            float duration = (stepDurations != null && step < stepDurations.Length)
                ? Mathf.Max(0.1f, stepDurations[step])
                : 3f;

            float elapsed = 0f;
            int   lastPct = -1;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                int pct = Mathf.Clamp(Mathf.FloorToInt((elapsed / duration) * 100f), 0, 99);

                if (pct != lastPct)
                {
                    lastPct = pct;
                    SetStepState(step, StepState.Active, pct);
                }

                yield return null;
            }

            // Lock step at 100% and mark complete
            SetStepState(step, StepState.Done, 100);
        }

        installCoroutine = null;
        OnInstallationComplete();
    }

    // ----------------------------------------------------------------
    //  Step state application
    // ----------------------------------------------------------------

    private enum StepState { Pending, Active, Done }

    private void SetStepState(int index, StepState state, int pct)
    {
        if (index < 0 || index >= stepLabels.Length || stepLabels[index] == null) return;

        TextMeshProUGUI label = stepLabels[index];
        string          baseText = (originalLabels != null && index < originalLabels.Length)
            ? originalLabels[index]
            : label.text;

        switch (state)
        {
            case StepState.Pending:
                label.color = PendingColor;
                label.text  = baseText;
                break;

            case StepState.Active:
                label.color = ActiveColor;
                label.text  = $"{baseText} ({pct}%)";
                break;

            case StepState.Done:
                label.color = ActiveColor;
                label.text  = $"{baseText} (100%)";
                break;
        }
    }

    // ----------------------------------------------------------------
    //  Completion
    // ----------------------------------------------------------------

    private void OnInstallationComplete()
    {
        Debug.Log("[WindowsInstallationManager] Installation complete.");
        navigator?.OnInstallationComplete();
    }
}
