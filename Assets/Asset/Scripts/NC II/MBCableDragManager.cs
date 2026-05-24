using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent manager that drives MBCable drag even when the cable component
/// gets disabled by MotherboardPhaseManager during phase switching.
/// Created automatically by MBCable.Start() � one instance per session.
/// </summary>
public class MBCableDragManager : MonoBehaviour
{
    private List<MBCable> _activeCables = new List<MBCable>();

    public bool HasActiveDrag => _activeCables.Count > 0;

    public void Register(MBCable cable)
    {
        if (!_activeCables.Contains(cable))
            _activeCables.Add(cable);
    }

    public void Unregister(MBCable cable)
    {
        _activeCables.Remove(cable);
    }

    private void Update()
    {
        for (int i = _activeCables.Count - 1; i >= 0; i--)
        {
            if (_activeCables[i] == null) { _activeCables.RemoveAt(i); continue; }
            _activeCables[i].DragUpdate();
        }
    }
}