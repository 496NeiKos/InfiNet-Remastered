using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent singleton that drives CableBehavior drag even when the cable
/// component gets disabled mid-drag by MotherboardPhaseManager or phase switching.
/// Created automatically on first access.
/// </summary>
public class CableDragManager : MonoBehaviour
{
    private static CableDragManager _instance;

    public static CableDragManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CableDragManager");
                _instance = go.AddComponent<CableDragManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private readonly List<CableBehavior> _active = new List<CableBehavior>();

    public bool HasActiveDrag => _active.Count > 0;

    public void Register(CableBehavior cable)
    {
        if (!_active.Contains(cable)) _active.Add(cable);
    }

    public void Unregister(CableBehavior cable)
    {
        _active.Remove(cable);
    }

    private void Update()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i] == null) { _active.RemoveAt(i); continue; }
            _active[i].DragUpdate();
        }
    }
}
