using UnityEngine;
using UnityEditor;
using TLab.NetworkedVR.Interact;

namespace TLab.VRClassroom.Editor
{
    [CustomEditor(typeof(PopupController))]
    public class PopupControllerEditor : UnityEditor.Editor
    {
        private void OverwritePointablePopup(int index, ref PopupController popupController)
        {
            const string NAME = "[popup_controller] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = popupController.pointerPairs[index].target;

            var pointableOutline = target.GetComponent<PointableOutline>();
            var pointablePopup = target.GetComponent<PointablePopup>();
            if (pointablePopup == null || pointablePopup == pointableOutline)
            {
                pointablePopup = target.AddComponent<PointablePopup>();
            }

            if (pointablePopup != null)
            {
                pointablePopup.outlineMat = pointableOutline.outlineMat;
                pointablePopup.popupController = popupController;
                pointablePopup.index = index;
                pointablePopup.enableCollision = false;

                DestroyImmediate(pointableOutline);

                EditorUtility.SetDirty(pointablePopup);

                Debug.Log(NAME + "update to pointablePopup " + index.ToString());
            }

            Debug.Log(NAME + BAR);
        }

        private void RevertPointableOutline(int index, ref PopupController popupController)
        {
            const string NAME = "[popup_controller] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = popupController.pointerPairs[index].target;

            var pointableOutline = target.GetComponent<PointableOutline>();
            var pointablePopup = target.GetComponent<PointablePopup>();

            if (pointableOutline == null || pointableOutline == pointablePopup)
            {
                pointableOutline = target.AddComponent<PointableOutline>();
            }

            if (pointablePopup != null)
            {
                Debug.Log(NAME + "revert to outline pointable " + index.ToString());

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

            PopupController popupController = target as PopupController;

            if (GUILayout.Button("Overwrite to PointablePopup"))
            {
                for (int index = 0; index < popupController.pointerPairs.Length; index++)
                {
                    OverwritePointablePopup(index, ref popupController);
                }

                EditorUtility.SetDirty(popupController);
            }

            if (GUILayout.Button("Revert to PointableOutline"))
            {
                for (int index = 0; index < popupController.pointerPairs.Length; index++)
                {
                    RevertPointableOutline(index, ref popupController);
                }

                EditorUtility.SetDirty(popupController);
            }
        }
    }
}