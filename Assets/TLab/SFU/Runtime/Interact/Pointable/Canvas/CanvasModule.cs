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

using System.Collections.Generic;
using System;
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

        [Tooltip("If true, the initial press position will be used as the drag start " +
            "position, rather than the position when drag threshold is exceeded. This is used " +
            "to prevent the pointer position shifting relative to the surface while dragging.")]
        [SerializeField]
        private bool m_useInitialPressPositionForDrag = true;

        private Camera m_pointerEventCamera;
        private static CanvasModule m_instance = null;
        private static CanvasModule instance => m_instance;

        public static void RegisterPointableCanvas(PointableCanvas pointerCanvas) => instance.AddPointerCanvas(pointerCanvas);

        public static void UnregisterPointableCanvas(PointableCanvas pointerCanvas) => instance?.RemovePointerCanvas(pointerCanvas);

        private Dictionary<int, Pointer> _pointerMap = new Dictionary<int, Pointer>();
        private List<RaycastResult> _raycastResultCache = new List<RaycastResult>();
        private List<Pointer> _pointersForDeletion = new List<Pointer>();
        private Dictionary<PointableCanvas, Action<PointerEvent>> _pointerCanvasActionMap = new Dictionary<PointableCanvas, Action<PointerEvent>>();

        private Pointer[] _pointersToProcessScratch = Array.Empty<Pointer>();

        private void AddPointerCanvas(PointableCanvas pointerCanvas)
        {
            Action<PointerEvent> pointerCanvasAction = (args) => HandlePointerEvent(pointerCanvas.canvas, args);
            _pointerCanvasActionMap.Add(pointerCanvas, pointerCanvasAction);
            pointerCanvas.whenPointerEventRaised += pointerCanvasAction;
        }

        private void RemovePointerCanvas(PointableCanvas pointerCanvas)
        {
            Action<PointerEvent> pointerCanvasAction = _pointerCanvasActionMap[pointerCanvas];
            _pointerCanvasActionMap.Remove(pointerCanvas);
            pointerCanvas.whenPointerEventRaised -= pointerCanvasAction;

            List<int> pointerIDs = new List<int>(_pointerMap.Keys);
            foreach (int pointerID in pointerIDs)
            {
                Pointer pointer = _pointerMap[pointerID];
                if (pointer.Canvas != pointerCanvas.canvas)
                {
                    continue;
                }
                ClearPointerSelection(pointer.PointerEventData);
                pointer.MarkForDeletion();
                _pointersForDeletion.Add(pointer);
                _pointerMap.Remove(pointerID);
            }
        }

        private void HandlePointerEvent(Canvas canvas, PointerEvent evt)
        {
            Pointer pointer;

            switch (evt.type)
            {
                case PointerEventType.Hover:
                    pointer = new Pointer(canvas);
                    pointer.PointerEventData = new PointerEventData(eventSystem);
                    pointer.SetPosition(evt.pointer.position);
                    _pointerMap.Add(evt.identifier, pointer);
                    break;
                case PointerEventType.UnHover:
                    pointer = _pointerMap[evt.identifier];
                    _pointerMap.Remove(evt.identifier);
                    pointer.MarkForDeletion();
                    _pointersForDeletion.Add(pointer);
                    break;
                case PointerEventType.Select:
                    pointer = _pointerMap[evt.identifier];
                    pointer.SetPosition(evt.pointer.position);
                    pointer.Press();
                    break;
                case PointerEventType.UnSelect:
                    pointer = _pointerMap[evt.identifier];
                    pointer.SetPosition(evt.pointer.position);
                    pointer.Release();
                    break;
                case PointerEventType.Move:
                    pointer = _pointerMap[evt.identifier];
                    pointer.SetPosition(evt.pointer.position);
                    break;
                case PointerEventType.Cancel:
                    pointer = _pointerMap[evt.identifier];
                    _pointerMap.Remove(evt.identifier);
                    ClearPointerSelection(pointer.PointerEventData);
                    pointer.MarkForDeletion();
                    _pointersForDeletion.Add(pointer);
                    break;
            }
        }

        /// <summary>
        /// Pointer class that is used for state associated with IPointables that are currently
        /// tracked by any PointableCanvases in the scene.
        /// </summary>
        private class Pointer
        {
            public PointerEventData PointerEventData { get; set; }

            public bool MarkedForDeletion { get; private set; }

            private Canvas _canvas;
            public Canvas Canvas => _canvas;

            private Vector3 _position;
            public Vector3 Position => _position;

            private GameObject _hoveredSelectable;
            public GameObject HoveredSelectable => _hoveredSelectable;


            private bool _pressing = false;
            private bool _pressed;
            private bool _released;

            public Pointer(Canvas canvas)
            {
                _canvas = canvas;
                _pressed = _released = false;
            }

            public void Press()
            {
                if (_pressing) return;
                _pressing = true;
                _pressed = true;
            }
            public void Release()
            {
                if (!_pressing) return;
                _pressing = false;
                _released = true;
            }

            public void ReadAndResetPressedReleased(out bool pressed, out bool released)
            {
                pressed = _pressed;
                released = _released;
                _pressed = _released = false;
            }

            public void MarkForDeletion()
            {
                MarkedForDeletion = true;
                Release();
            }

            public void SetPosition(Vector3 position)
            {
                _position = position;
            }

            public void SetHoveredSelectable(GameObject hoveredSelectable)
            {
                _hoveredSelectable = hoveredSelectable;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            m_instance = this;
        }

        protected override void OnDestroy()
        {
            // Must unset m_instance prior to calling the base.OnDestroy, otherwise error is thrown:
            //   Can't add component to object that is being destroyed.
            //   UnityEngine.EventSystems.BaseInputModule:get_input ()
            m_instance = null;
            base.OnDestroy();
        }

        protected bool _started = false;

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                m_pointerEventCamera = gameObject.AddComponent<Camera>();
                m_pointerEventCamera.nearClipPlane = 0.1f;

                // We do not need this camera to be enabled to serve this module's purposes:
                // as a dependency for Canvases and for its WorldToScreenPoint functionality
                m_pointerEventCamera.enabled = false;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                Destroy(m_pointerEventCamera);
                m_pointerEventCamera = null;
            }

            base.OnDisable();
        }

        // Based On FindFirstRaycast
        protected static RaycastResult FindFirstRaycastWithinCanvas(List<RaycastResult> candidates, Canvas canvas)
        {
            GameObject candidateGameObject;
            Canvas candidateCanvas;
            for (var i = 0; i < candidates.Count; ++i)
            {
                candidateGameObject = candidates[i].gameObject;
                if (candidateGameObject == null) continue;

                candidateCanvas = candidateGameObject.GetComponentInParent<Canvas>();
                if (candidateCanvas == null) continue;
                if (candidateCanvas.rootCanvas != canvas) continue;

                return candidates[i];
            }
            return new RaycastResult();
        }

        private void UpdateRaycasts(Pointer pointer, out bool pressed, out bool released)
        {
            PointerEventData pointerEventData = pointer.PointerEventData;
            Vector2 prevPosition = pointerEventData.position;
            pointerEventData.Reset();

            pointer.ReadAndResetPressedReleased(out pressed, out released);

            if (pointer.MarkedForDeletion)
            {
                pointerEventData.pointerCurrentRaycast = new RaycastResult();
                return;
            }

            Canvas canvas = pointer.Canvas;
            canvas.worldCamera = m_pointerEventCamera;

            Vector3 position = Vector3.zero;
            var plane = new Plane(-1f * canvas.transform.forward, canvas.transform.position);
            var ray = new Ray(pointer.Position - canvas.transform.forward, canvas.transform.forward);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                position = ray.GetPoint(enter);
            }

            // We need to position our camera at an offset from the Pointer position or else
            // a graphic raycast may ignore a world canvas that's outside of our regular camera view(s)
            m_pointerEventCamera.transform.position = pointer.Position - canvas.transform.forward;
            m_pointerEventCamera.transform.LookAt(pointer.Position, canvas.transform.up);

            Vector2 pointerPosition = m_pointerEventCamera.WorldToScreenPoint(position);
            pointerEventData.position = pointerPosition;

            // RaycastAll raycasts against with every GraphicRaycaster in the scene,
            // including nested ones like in the case of a dropdown
            eventSystem.RaycastAll(pointerEventData, _raycastResultCache);

            RaycastResult firstResult = FindFirstRaycastWithinCanvas(_raycastResultCache, canvas);
            pointer.PointerEventData.pointerCurrentRaycast = firstResult;

            _raycastResultCache.Clear();

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

        public override void Process()
        {
            ProcessPointers(_pointersForDeletion, true);
            ProcessPointers(_pointerMap.Values, false);
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

            if (pointersToProcessCount > _pointersToProcessScratch.Length)
            {
                _pointersToProcessScratch = new Pointer[pointersToProcessCount];
            }

            pointers.CopyTo(_pointersToProcessScratch, 0);
            if (clearAndReleasePointers)
            {
                pointers.Clear();
            }

            foreach (Pointer pointer in _pointersToProcessScratch)
            {
                ProcessPointer(pointer, clearAndReleasePointers);
            }
        }

        private void ProcessPointer(Pointer pointer, bool forceRelease = false)
        {
            bool pressed = false;
            bool released = false;
            bool wasDragging = pointer.PointerEventData.dragging;

            UpdateRaycasts(pointer, out pressed, out released);

            PointerEventData pointerEventData = pointer.PointerEventData;
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

        private void HandleSelectableHover(Pointer pointer, bool wasDragging)
        {
            bool dragging = pointer.PointerEventData.dragging || wasDragging;

            GameObject currentOverGo = pointer.PointerEventData.pointerCurrentRaycast.gameObject;
            GameObject prevHoveredSelectable = pointer.HoveredSelectable;
            GameObject newHoveredSelectable = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            pointer.SetHoveredSelectable(newHoveredSelectable);

            if (newHoveredSelectable != null && newHoveredSelectable != prevHoveredSelectable)
            {
                WhenSelectableHovered?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, pointer.HoveredSelectable, dragging));
            }
            else if (prevHoveredSelectable != null && newHoveredSelectable == null)
            {
                WhenSelectableUnhovered?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, pointer.HoveredSelectable, dragging));
            }
        }

        private void HandleSelectablePress(Pointer pointer, bool pressed, bool released, bool wasDragging)
        {
            bool dragging = pointer.PointerEventData.dragging || wasDragging;

            if (pressed)
            {
                WhenSelected?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, pointer.HoveredSelectable, dragging));
            }
            else if (released && !pointer.MarkedForDeletion)
            {
                // Unity handles UI selection on release, so we verify the hovered element has been selected
                bool hasSelectedHoveredObject = pointer.HoveredSelectable != null &&
                                                pointer.HoveredSelectable == pointer.PointerEventData.selectedObject;
                GameObject selectedObject = hasSelectedHoveredObject ? pointer.HoveredSelectable : null;
                WhenUnselected?.Invoke(new PointableCanvasEventArgs(pointer.Canvas, selectedObject, dragging));
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
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

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
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);

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
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;
            }
        }

        /// <summary>
        /// Override of PointerInputModule's ProcessDrag to allow using the initial press position for drag begin.
        /// Set m_useInitialPressPositionForDrag to false if you prefer the default behaviour of PointerInputModule.
        /// </summary>
        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            if (!pointerEvent.IsPointerMoving() ||
                pointerEvent.pointerDrag == null)
                return;

            if (!pointerEvent.dragging
                && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position,
                    eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
            {
                if (m_useInitialPressPositionForDrag)
                {
                    pointerEvent.position = pointerEvent.pressPosition;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent,
                    ExecuteEvents.beginDragHandler);
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

                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent,
                    ExecuteEvents.dragHandler);
            }
        }

        private void ClearPointerSelection(PointerEventData pointerEvent)
        {
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent,
                ExecuteEvents.pointerUpHandler);

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
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }
    }
}
