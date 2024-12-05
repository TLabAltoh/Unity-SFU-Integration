/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TLab.SFU.Interact;

namespace TLab.SFU
{
    public class PointableCanvasEventArgs
    {
        public readonly Canvas Canvas;
        public readonly GameObject Hovered;
        public readonly bool Dragging;

        public PointableCanvasEventArgs(Canvas canvas, GameObject hovered, bool dragging)
        {
            Canvas = canvas;
            Hovered = hovered;
            Dragging = dragging;
        }
    }

    [AddComponentMenu("TLab/SFU/Canvas Module (TLab)")]
    public class CanvasModule : PointerInputModule
    {
        public static event Action<PointableCanvasEventArgs> WhenSelected;

        public static event Action<PointableCanvasEventArgs> WhenUnselected;

        public static event Action<PointableCanvasEventArgs> WhenSelectableHovered;

        public static event Action<PointableCanvasEventArgs> WhenSelectableUnhovered;

        [SerializeField]
        private bool m_useInitialPressPositionForDrag = true;

        protected bool m_started = false;

        private Camera m_pointerEventCamera;

        private Dictionary<int, Pointer> m_pointerMap = new Dictionary<int, Pointer>();
        private List<RaycastResult> m_raycastResultCache = new List<RaycastResult>();
        private List<Pointer> m_pointersForDeletion = new List<Pointer>();
        private Dictionary<PointableCanvas, Action<PointerEvent>> m_pointerCanvasActionMap = new Dictionary<PointableCanvas, Action<PointerEvent>>();

        private Pointer[] m_pointersToProcessScratch = Array.Empty<Pointer>();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        private static CanvasModule m_instance = null;

        private static CanvasModule Instance => m_instance;

        private class Pointer
        {
            public PointerEventData pointerEventData { get; set; }

            public bool markedForDeletion { get; private set; }

            private Canvas m_canvas;
            private GameObject m_hoveredSelectable;
            private Vector3 m_position;

            private bool m_pressing = false;
            private bool m_pressed;
            private bool m_released;

            public Canvas canvas => m_canvas;
            public Vector3 position => m_position;
            public GameObject hoveredSelectable => m_hoveredSelectable;

            public Pointer(Canvas canvas)
            {
                m_canvas = canvas;
                m_pressed = m_released = false;
            }

            public void Press()
            {
                if (m_pressing)
                {
                    return;
                }

                m_pressing = true;
                m_pressed = true;
            }
            public void Release()
            {
                if (!m_pressing)
                {
                    return;
                }

                m_pressing = false;
                m_released = true;
            }

            public void ReadAndResetPressedReleased(out bool pressed, out bool released)
            {
                pressed = m_pressed;
                released = m_released;
                m_pressed = m_released = false;
            }

            public void MarkForDeletion()
            {
                markedForDeletion = true;
                Release();
            }

            public void SetPosition(Vector3 position)
            {
                m_position = position;
            }

            public void SetHoveredSelectable(GameObject hoveredSelectable)
            {
                m_hoveredSelectable = hoveredSelectable;
            }
        }

        private void HandlePointerEvent(Canvas canvas, PointerEvent evt)
        {
            Pointer pointer;

            switch (evt.type)
            {
                case PointerEventType.Hover:
                    pointer = new Pointer(canvas);
                    pointer.pointerEventData = new PointerEventData(eventSystem);
                    pointer.SetPosition(evt.pointer.position);
                    m_pointerMap.Add(evt.identifier, pointer);
                    break;
                case PointerEventType.UnHover:
                    pointer = m_pointerMap[evt.identifier];
                    m_pointerMap.Remove(evt.identifier);
                    pointer.MarkForDeletion();
                    m_pointersForDeletion.Add(pointer);
                    break;
                case PointerEventType.Select:
                    pointer = m_pointerMap[evt.identifier];
                    pointer.SetPosition(evt.pointer.position);
                    pointer.Press();
                    break;
                case PointerEventType.UnSelect:
                    pointer = m_pointerMap[evt.identifier];
                    pointer.SetPosition(evt.pointer.position);
                    pointer.Release();
                    break;
                case PointerEventType.Move:
                    pointer = m_pointerMap[evt.identifier];
                    pointer.SetPosition(evt.pointer.position);
                    break;
                case PointerEventType.Cancel:
                    pointer = m_pointerMap[evt.identifier];
                    m_pointerMap.Remove(evt.identifier);
                    ClearPointerSelection(pointer.pointerEventData);
                    pointer.MarkForDeletion();
                    m_pointersForDeletion.Add(pointer);
                    break;
            }
        }

        private void AddPointerCanvas(PointableCanvas pointerCanvas)
        {
            Action<PointerEvent> pointerCanvasAction = (args) => HandlePointerEvent(pointerCanvas.canvas, args);
            m_pointerCanvasActionMap.Add(pointerCanvas, pointerCanvasAction);
            pointerCanvas.whenPointerEventRaised += pointerCanvasAction;
        }

        private void RemovePointerCanvas(PointableCanvas pointerCanvas)
        {
            Action<PointerEvent> pointerCanvasAction = m_pointerCanvasActionMap[pointerCanvas];
            m_pointerCanvasActionMap.Remove(pointerCanvas);
            pointerCanvas.whenPointerEventRaised -= pointerCanvasAction;

            List<int> pointerIDs = new List<int>(m_pointerMap.Keys);
            foreach (int pointerID in pointerIDs)
            {
                Pointer pointer = m_pointerMap[pointerID];
                if (pointer.canvas != pointerCanvas.canvas)
                {
                    continue;
                }
                ClearPointerSelection(pointer.pointerEventData);
                pointer.MarkForDeletion();
                m_pointersForDeletion.Add(pointer);
                m_pointerMap.Remove(pointerID);
            }
        }

        public static void RegisterPointableCanvas(PointableCanvas pointerCanvas)
        {
            Instance.AddPointerCanvas(pointerCanvas);
        }

        public static void UnregisterPointableCanvas(PointableCanvas pointerCanvas)
        {
            Instance?.RemovePointerCanvas(pointerCanvas);
        }

        private void HandleSelectableHover(Pointer pointer, bool wasDragging)
        {
            bool dragging = pointer.pointerEventData.dragging || wasDragging;

            GameObject currentOverGo = pointer.pointerEventData.pointerCurrentRaycast.gameObject;
            GameObject prevHoveredSelectable = pointer.hoveredSelectable;
            GameObject newHoveredSelectable = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            pointer.SetHoveredSelectable(newHoveredSelectable);

            if (newHoveredSelectable != null && newHoveredSelectable != prevHoveredSelectable)
            {
                WhenSelectableHovered?.Invoke(new PointableCanvasEventArgs(pointer.canvas, pointer.hoveredSelectable, dragging));
            }
            else if (prevHoveredSelectable != null && newHoveredSelectable == null)
            {
                WhenSelectableUnhovered?.Invoke(new PointableCanvasEventArgs(pointer.canvas, pointer.hoveredSelectable, dragging));
            }
        }

        private void HandleSelectablePress(Pointer pointer, bool pressed, bool released, bool wasDragging)
        {
            bool dragging = pointer.pointerEventData.dragging || wasDragging;

            if (pressed)
            {
                WhenSelected?.Invoke(new PointableCanvasEventArgs(pointer.canvas, pointer.hoveredSelectable, dragging));
            }
            else if (released && !pointer.markedForDeletion)
            {
                // Unity handles UI selection on release, so we verify the hovered element has been selected
                bool hasSelectedHoveredObject = pointer.hoveredSelectable != null &&
                                                pointer.hoveredSelectable == pointer.pointerEventData.selectedObject;
                GameObject selectedObject = hasSelectedHoveredObject ? pointer.hoveredSelectable : null;
                WhenUnselected?.Invoke(new PointableCanvasEventArgs(pointer.canvas, selectedObject, dragging));
            }
        }

        /// <summary>
        /// This method is based on ProcessTouchPoint in StandaloneInputModule,
        /// but is instead used for Pointer events
        /// </summary>
        protected void UpdatePointerEventData(PointerEventData pointerEvent, bool pressed, bool released)
        {
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (pressed)
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                {
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
                }

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                    {
                        ++pointerEvent.clickCount;
                    }
                    else
                    {
                        pointerEvent.clickCount = 1;
                    }

                    pointerEvent.clickTime = time;
                }
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
                }

            }

            // PointerUp notification
            if (released)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                // see if we mouse up on the same element that we clicked on...
                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                }

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);
                }

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;
            }
        }

        private void ClearPointerSelection(PointerEventData pointerEvent)
        {
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;
        }

        /// <summary>
        /// Used in PointerInputModule's ProcessDrag implementation. Brought into this subclass with a protected
        /// signature (as opposed to the parent's private signature) to be used in this subclass's overridden ProcessDrag.
        /// </summary>
        protected static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
            {
                return true;
            }

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }

        /// <summary>
        /// Override of PointerInputModule's ProcessDrag to allow using the initial press position for drag begin.
        /// Set _useInitialPressPositionForDrag to false if you prefer the default behaviour of PointerInputModule.
        /// </summary>
        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            if (!pointerEvent.IsPointerMoving() || pointerEvent.pointerDrag == null)
            {
                return;
            }

            if (!pointerEvent.dragging &&
                ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
            {
                if (m_useInitialPressPositionForDrag)
                {
                    pointerEvent.position = pointerEvent.pressPosition;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            if (pointerEvent.dragging)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
                {
                    ClearPointerSelection(pointerEvent);
                }

                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }
        }

        protected static RaycastResult FindFirstRaycastWithinCanvas(List<RaycastResult> candidates, Canvas canvas)
        {
            GameObject candidateGameObject;
            Canvas candidateCanvas;
            for (var i = 0; i < candidates.Count; ++i)
            {
                candidateGameObject = candidates[i].gameObject;
                if (candidateGameObject == null)
                {
                    continue;
                }

                candidateCanvas = candidateGameObject.GetComponentInParent<Canvas>();
                if (candidateCanvas == null)
                {
                    continue;
                }
                if (candidateCanvas.rootCanvas != canvas)
                {
                    continue;
                }

                return candidates[i];
            }
            return new RaycastResult();
        }

        private void UpdateRaycasts(Pointer pointer, out bool pressed, out bool released)
        {
            PointerEventData pointerEventData = pointer.pointerEventData;
            Vector2 prevPosition = pointerEventData.position;
            pointerEventData.Reset();

            pointer.ReadAndResetPressedReleased(out pressed, out released);

            if (pointer.markedForDeletion)
            {
                pointerEventData.pointerCurrentRaycast = new RaycastResult();
                return;
            }

            Canvas canvas = pointer.canvas;
            canvas.worldCamera = m_pointerEventCamera;

            Vector3 position = Vector3.zero;
            var plane = new Plane(-1f * canvas.transform.forward, canvas.transform.position);
            var ray = new Ray(pointer.position - canvas.transform.forward, canvas.transform.forward);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                position = ray.GetPoint(enter);
            }

            // We need to position our camera at an offset from the Pointer position or else
            // a graphic raycast may ignore a world canvas that's outside of our regular camera view(s)
            m_pointerEventCamera.transform.position = pointer.position - canvas.transform.forward;
            m_pointerEventCamera.transform.LookAt(pointer.position, canvas.transform.up);

            Vector2 pointerPosition = m_pointerEventCamera.WorldToScreenPoint(position);
            pointerEventData.position = pointerPosition;

            // RaycastAll raycasts against with every GraphicRaycaster in the scene,
            // including nested ones like in the case of a dropdown
            eventSystem.RaycastAll(pointerEventData, m_raycastResultCache);

            RaycastResult firstResult = FindFirstRaycastWithinCanvas(m_raycastResultCache, canvas);
            pointer.pointerEventData.pointerCurrentRaycast = firstResult;

            m_raycastResultCache.Clear();

            // We use a static translation offset from the canvas for 2D position delta tracking
            m_pointerEventCamera.transform.position = canvas.transform.position - canvas.transform.forward;
            m_pointerEventCamera.transform.LookAt(canvas.transform.position, canvas.transform.up);

            pointerPosition = m_pointerEventCamera.WorldToScreenPoint(position);
            pointerEventData.position = pointerPosition;

            if (pressed)
            {
                pointerEventData.delta = Vector2.zero;
            }
            else
            {
                pointerEventData.delta = pointerEventData.position - prevPosition;
            }

            pointerEventData.button = PointerEventData.InputButton.Left;
        }

        private void ProcessPointer(Pointer pointer, bool forceRelease = false)
        {
            bool pressed = false;
            bool released = false;
            bool wasDragging = pointer.pointerEventData.dragging;

            UpdateRaycasts(pointer, out pressed, out released);

            PointerEventData pointerEventData = pointer.pointerEventData;
            UpdatePointerEventData(pointerEventData, pressed, released);

            released |= forceRelease;

            if (!released)
            {
                ProcessMove(pointerEventData);
                ProcessDrag(pointerEventData);
            }
            else
            {
                HandlePointerExitAndEnter(pointerEventData, null);
                RemovePointerData(pointerEventData);
            }

            HandleSelectableHover(pointer, wasDragging);
            HandleSelectablePress(pointer, pressed, released, wasDragging);
        }

        private void ProcessPointers(ICollection<Pointer> pointers, bool clearAndReleasePointers)
        {
            // Before processing pointers, take a copy of the array since _pointersForDeletion or
            // _pointerMap may be modified if a pointer event handler adds or removes a
            // PointableCanvas.

            int pointersToProcessCount = pointers.Count;
            if (pointersToProcessCount == 0)
            {
                return;
            }

            if (pointersToProcessCount > m_pointersToProcessScratch.Length)
            {
                m_pointersToProcessScratch = new Pointer[pointersToProcessCount];
            }

            pointers.CopyTo(m_pointersToProcessScratch, 0);
            if (clearAndReleasePointers)
            {
                pointers.Clear();
            }

            foreach (Pointer pointer in m_pointersToProcessScratch)
            {
                ProcessPointer(pointer, clearAndReleasePointers);
            }
        }

        public override void Process()
        {
            ProcessPointers(m_pointersForDeletion, true);
            ProcessPointers(m_pointerMap.Values, false);
        }

        protected override void Start()
        {
            this.BeginStart(ref m_started, () => base.Start());
            this.EndStart(ref m_started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_started)
            {
                m_pointerEventCamera = gameObject.RequireComponent<Camera>();
                m_pointerEventCamera.nearClipPlane = 0.1f;

                // We do not need this camera to be enabled to serve this module's purposes:
                // as a dependency for Canvases and for its WorldToScreenPoint functionality
                m_pointerEventCamera.enabled = false;
            }
        }

        protected override void OnDisable()
        {
            if (m_started)
            {
                Destroy(m_pointerEventCamera);
                m_pointerEventCamera = null;
            }

            base.OnDisable();
        }

        protected override void Awake()
        {
            base.Awake();

            m_instance = this;
        }

        protected override void OnDestroy()
        {
            // Must unset _instance prior to calling the base.OnDestroy, otherwise error is thrown:
            //   Can't add component to object that is being destroyed.
            //   UnityEngine.EventSystems.BaseInputModule:get_input ()
            m_instance = null;
            base.OnDestroy();
        }
    }
}
