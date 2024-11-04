using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TLab.VRProjct.Test
{
#if UNITY_EDITOR
    [AddComponentMenu("TLab/VRProject/Test Code (VRProject)")]
    public class TestCode : MonoBehaviour
    {
        #region UI_AUTO_PRESS_TEST
        [Header("UI Auto Press Test")]

        [SerializeField] private GameObject[] m_elements;

        [SerializeField] private Canvas m_canvas;

        [SerializeField] private EventSystem m_eventSystem;

        private PointerEventData m_pointerEventData;

        private const float DOUBLE_CLICK_THRESHOLD = 0.3f;

        private GraphicRaycaster m_graphicsRaycaster;

        private List<RaycastResult> m_RaycastResultCache = new List<RaycastResult>();

        private IEnumerator PressUIElementAsync(GameObject uiElement, float wait)
        {
            var pointerEvent = new PointerEventData(m_eventSystem);

            // Press Down

            pointerEvent.eligibleForClick = true;
            pointerEvent.delta = Vector2.zero;
            pointerEvent.dragging = false;
            pointerEvent.useDragThreshold = true;
            pointerEvent.pressPosition = pointerEvent.position;
            pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

            // Search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            var newPressed = ExecuteEvents.ExecuteHierarchy(uiElement, pointerEvent, ExecuteEvents.pointerDownHandler);

            // Didnt find a press handler... search for a click handler
            if (newPressed == null)
                newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(uiElement);

            Debug.Log("Pressed: " + newPressed);

            float time = Time.unscaledTime;

            if (newPressed == pointerEvent.lastPress)
            {
                var diffTime = time - pointerEvent.clickTime;
                if (diffTime < DOUBLE_CLICK_THRESHOLD)
                    pointerEvent.clickCount++;
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.clickTime = time;
            }
            else
            {
                pointerEvent.clickCount = 1;
            }

            pointerEvent.pointerPress = newPressed;
            pointerEvent.rawPointerPress = uiElement;

            pointerEvent.clickTime = time;

            // Save the drag handler as well
            pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(uiElement);

            if (pointerEvent.pointerDrag != null)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

            yield return new WaitForSeconds(wait);

            // Debug.Log("Executing pressup on: " + pointer.pointerPress);
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            // See if we mouse up on the same element that we clicked on ...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(uiElement);

            // PointerClick and Drop events
            if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
            else if (pointerEvent.pointerDrag != null)
                ExecuteEvents.ExecuteHierarchy(uiElement, pointerEvent, ExecuteEvents.dropHandler);

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;

            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            // Redo pointer enter / exit to refresh state so that
            // if we moused over somethign that ignored it before
            // due to having pressed on something else it now gets it.
            if (uiElement != pointerEvent.pointerEnter)
            {
                //HandlePointerExitAndEnter(pointerEvent, null);
                //HandlePointerExitAndEnter(pointerEvent, uiElement);
            }
        }

        public void OnUIElementPressed(int id) => Debug.Log("UI element pressed: " + id);

        public void AutoPressUIElement()
        {
            const float DELAY = 2f;

            if (m_pointerEventData == null)
                m_pointerEventData = new PointerEventData(m_eventSystem);

            StartCoroutine(PressUIElementAsync(m_elements[0], DELAY));
            StartCoroutine(PressUIElementAsync(m_elements[1], DELAY));
        }

        #endregion UI_AUTO_PRESS_TEST

        #region RAYCAST_TEST

        [Header("Raycast Test")]

        [SerializeField] private Collider m_collider;

        public void MeshColliderClosestPoint()
        {
            var point = m_collider.ClosestPoint(Vector3.zero);

            Debug.Log("Closest Point: " + point);
        }

        #endregion RAYCAST_TEST

        #region JSON_UTILITY_TEST
        public class Test
        {
            public Vector2 vector2;
            public Vector3 vector3;
            public Vector4 vector4;
        }

        public void JsonUtilityTest()
        {
            var @object = new Test();
            @object.vector2 = Vector2.one;
            @object.vector3 = Vector3.one;
            @object.vector4 = Vector4.one;

            var json = JsonUtility.ToJson(@object);

            Debug.Log("Json: " + json);

            var @unmarshal = JsonUtility.FromJson<Test>(json);
            Debug.Log(@object.vector2.Equals(@unmarshal.vector2));
            Debug.Log(@object.vector3.Equals(@unmarshal.vector3));
            Debug.Log(@object.vector4.Equals(@unmarshal.vector4));
        }
        #endregion
    }
#endif
}