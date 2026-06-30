using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public class DumpHierarchy : MonoBehaviour
{
    [MenuItem("Tools/Dump Scene Hierarchy to File")]
    public static void Dump()
    {
        StringBuilder sb = new StringBuilder();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Add a header
        sb.AppendLine($"================================================");
        sb.AppendLine($"SCENE HIERARCHY DUMP: {sceneName}");
        sb.AppendLine($"Generated on: {System.DateTime.Now}");
        sb.AppendLine($"================================================");
        sb.AppendLine();

        // Traverse all root objects
        foreach (GameObject obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            DumpGameObject(obj, sb, 0);
        }

        // Define file path (saves directly into your main Project folder)
        string fileName = $"{sceneName}_Hierarchy_Dump.txt";
        string filePath = Path.Combine(Application.dataPath, "..", fileName);

        // Write the file
        File.WriteAllText(filePath, sb.ToString());

        // Alert the user and highlight the file
        Debug.Log($"成功! Entire hierarchy saved to: {filePath}");
        EditorUtility.RevealInFinder(filePath);
    }

    private static void DumpGameObject(GameObject obj, StringBuilder sb, int indent)
    {
        // Add indentation for hierarchy structure
        sb.Append(new string(' ', indent * 4));

        // Indicate if the object is active or inactive in the scene
        string activeStatus = obj.activeSelf ? "" : " (Inactive)";
        sb.Append($"- {obj.name}{activeStatus}");

        // Grab components
        var components = obj.GetComponents<Component>();
        if (components.Length > 1)
        {
            sb.Append(" [Components: ");
            for (int i = 1; i < components.Length; i++)
            {
                if (components[i] == null) continue;
                sb.Append(components[i].GetType().Name);
                if (i < components.Length - 1) sb.Append(", ");
            }
            sb.Append("]");
        }

        sb.AppendLine();

        // Recurse into children
        foreach (Transform child in obj.transform)
        {
            DumpGameObject(child.gameObject, sb, indent + 1);
        }
    }
}