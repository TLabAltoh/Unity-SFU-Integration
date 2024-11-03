using System.IO;
using UnityEditor;

namespace TLab.SFU.Editor
{
    public static class PathUtil
    {
        public static bool SelectPath(ref string currentPath, string panelTitle)
        {
            var initialDir = currentPath != null && FileUtil.FileExists(currentPath) ? currentPath : "Assets";
            var path = EditorUtility.SaveFolderPanel(panelTitle, initialDir, "");
            if (path == "")
                return false;
            var fullPath = Directory.GetCurrentDirectory();
            currentPath = path.Remove(0, fullPath.Length + 1);
            return true;
        }
    }
}
