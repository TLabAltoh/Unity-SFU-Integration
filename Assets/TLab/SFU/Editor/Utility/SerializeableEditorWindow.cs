using System.IO;
using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Editor
{
    public abstract class SerializeableEditorWindow : EditorWindow
    {
        private string savePath => $"Assets/SerializeData/EditorWindow/{GetType()}.json";

        protected virtual void OnEnable()
        {
            if (File.Exists(savePath))
            {
                using (var sr = new StreamReader(savePath))
                    JsonUtility.FromJsonOverwrite(sr.ReadToEnd(), this);
            }
        }

        protected virtual void OnDisable()
        {
            var dir = Path.GetDirectoryName(savePath);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var sw = new StreamWriter(savePath, false))
            {
                var json = JsonUtility.ToJson(this, false);
                sw.Write(json);
                sw.Flush();
            }
        }
    }
}