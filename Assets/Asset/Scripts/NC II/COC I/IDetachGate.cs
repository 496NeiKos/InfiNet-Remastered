/// <summary>
/// Optional gate checked by CableBehavior.CanDetach() before allowing hold-to-detach.
/// Wire a MonoBehaviour implementing this into CableBehavior's detachGate inspector field.
/// </summary>
public interface IDetachGate
{
    bool CanDetach();
    string BlockedReason { get; }
}
