using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Interact.Editor
{
    [CustomEditor(typeof(PopupController))]
    public class PopupControllerEditor : UnityEditor.Editor
    {
        private void OverwritePointablePopup(int index, ref PopupController popupController)
        {
            const string NAME = "[PopupController] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = popupController.pointerPairs[index].target;

            var pointableOutline = target.GetComponent<PointableOutline>();
            var pointablePopup = target.GetComponent<PointablePopup>();
            if (pointablePopup == null || pointablePopup == pointableOutline)
                pointablePopup = target.AddComponent<PointablePopup>();

            if (pointablePopup != null)
            {
                pointablePopup.outlineMat = pointableOutline.outlineMat;
                pointablePopup.popupController = popupController;
                pointablePopup.index = index;
                pointablePopup.enableCollision = false;
                DestroyImmediate(pointableOutline);
                EditorUtility.SetDirty(pointablePopup);
                Debug.Log(NAME + "Update to PointablePopup " + index.ToString());
            }

            Debug.Log(NAME + BAR);
        }

        private void RevertPointableOutline(int index, ref PopupController popupController)
        {
            const string NAME = "[PopupController] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = popupController.pointerPairs[index].target;

            var pointableOutline = target.GetComponent<PointableOutline>();
            var pointablePopup = target.GetComponent<PointablePopup>();

            if (pointableOutline == null || pointableOutline == pointablePopup)
                pointableOutline = target.AddComponent<PointableOutline>();

            if (pointablePopup != null)
            {
                Debug.Log(NAME + "Revert to OutlinePointable " + index.ToString());
                pointableOutline.outlineMat = pointablePopup.outlineMat;
                DestroyImmediate(pointablePopup);
            }

            if (pointableOutline != null)
            {
                pointableOutline.enableCollision = false;
                EditorUtility.SetDirty(pointableOutline);
            }

            Debug.Log(NAME + BAR);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var popupController = target as PopupController;

            if (GUILayout.Button("Overwrite to PointablePopup"))
            {
                for (int index = 0; index < popupController.pointerPairs.Length; index++)
                    OverwritePointablePopup(index, ref popupController);
                EditorUtility.SetDirty(popupController);
            }

            if (GUILayout.Button("Revert to PointableOutline"))
            {
                for (int index = 0; index < popupController.pointerPairs.Length; index++)
                    RevertPointableOutline(index, ref popupController);
                EditorUtility.SetDirty(popupController);
            }
        }
    }
}