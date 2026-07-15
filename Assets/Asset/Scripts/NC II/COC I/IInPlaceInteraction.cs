// Implemented by any monitor-style object that opens a full-screen Canvas
// in place (no reparenting). GameManager uses this to open and close the panel
// and to handle the Escape key generically regardless of which monitor type is active.
public interface IInPlaceInteraction
{
    void ShowDetail();
    void HideDetail();
}
