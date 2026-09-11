using UnityEngine;

/// <summary>
/// Minimal contract shared by all task-category managers in COC II.
/// Allows NetworkCableTaskCategoryController to delegate text/color queries
/// to whichever category is active without type-specific branches.
/// </summary>
public interface ITaskCategory
{
    string GetNextIncompleteTaskText();
    Color  GetDisplayColor(Color fallback);
    void   HideCompletionBanner();
}
