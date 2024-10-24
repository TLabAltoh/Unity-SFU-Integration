using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TLab.VRClassroom
{
#if UNITY_EDITOR
    public class CodeTester : MonoBehaviour
    {
        [Header("Canvas UI Test")]

        [SerializeField] private GameObject[] m_uiElements;

        [SerializeField] private Canvas m_canvas;

        [SerializeField] private EventSystem m_eventSystem;

        private PointerEventData m_pointerEventData;

        private const float DOUBLE_CLICK_THRESHOLD = 0.3f;

        private GraphicRaycaster m_graphicsRaycaster;

        private List<RaycastResult> m_RaycastResultCache = new List<RaycastResult>();

        [Header("Raycast Test")]

        [SerializeField] private Collider m_collider;

        private IEnumerator PressUIElement(GameObject uiElement, float wait)
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

        public void OnPressed(int id)
        {
            Debug.Log("Ui element pressed !: " + id);
        }

        public void PressUI()
        {
            const float WAIT = 2f;

            if (m_pointerEventData == null)
                m_pointerEventData = new PointerEventData(m_eventSystem);

            StartCoroutine(PressUIElement(m_uiElements[0], WAIT));
            StartCoroutine(PressUIElement(m_uiElements[1], WAIT));
        }

        public void ArrayInstantiateTest()
        {
            var array = new Queue<GameObject>[5];

            for (int i = 0; i < array.Length; i++)
            {
                Debug.Log($"array {i} is null ? :" + (array[i] == null));
            }
        }

        public void MeshColliderClosestPoint()
        {
            var point = m_collider.ClosestPoint(Vector3.zero);

            Debug.Log("Closest Point: " + point);
        }
    }
#endif
}
