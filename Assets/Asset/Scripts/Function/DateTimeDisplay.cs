using System;
using TMPro;
using UnityEngine;

public class DateTimeDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private TMP_Text dateText;

    private static readonly TimeZoneInfo PhilippinesTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

    private void Update()
    {
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PhilippinesTimeZone);
        clockText.text = now.ToString("hh:mm tt");
        dateText.text = now.ToString("MM/dd/yyyy");
    }
}
