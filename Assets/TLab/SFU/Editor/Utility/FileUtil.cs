using System.IO;

namespace TLab.SFU.Editor
{
    public static class FileUtil
    {
        public static bool FileExists(string assetPath)
        {
            if (assetPath.Length < "Assets".Length - 1)
                return false;

            return File.Exists(assetPath);
        }
    }
}
