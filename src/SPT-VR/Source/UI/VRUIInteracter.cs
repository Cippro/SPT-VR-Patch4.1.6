using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.Health;
using EFT.UI.Ragfair;
using EFT.UI.Utilities.LightScroller;
using System.Collections.Generic;
using TarkovVR.Source.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;

namespace TarkovVR.Source.UI
{
    internal class VRUIInteracter : MonoBehaviour
    {
        private const float RayDistance = 100f;
        private const float DragTriggerThreshold = 0.5f;
        private PointerEventData eventData;
        private EventSystem pointerSystem;
        private Camera interactionCamera;
        private readonly List<RaycastResult> raycasts = new List<RaycastResult>();
        private readonly UiConfirmState confirm = new UiConfirmState();
        public GameObject lastHighlightedObject;
        private GameObject dragObject;
        public GameObject hitObject;
        public Vector3 uiPointerPos;
        public float timeHeld;
        public Vector3 pressPosition;
        private GameObject pressedObject;
        private GameObject pressedClickObject;
        private GameObject lastClickObject;
        private bool skipLeftClick;
        private float lastClickTime;
        private int clickCount;
        private float lastHitDistance = 1f;
        private bool loggedMissingTarget;
        private bool loggedValidClick;
        private bool dragCanceledUntilRelease;
        private int interactionRevision;
        public bool rotated;
        public float rightJoyTimeHeld;
        public bool IsDragging => dragObject;

        public static bool IsDragPressed()
        {
            return (VRSettings.GetLeftHandedMode() ? SteamVR_Actions._default.LeftTrigger.axis : SteamVR_Actions._default.RightTrigger.axis) > DragTriggerThreshold;
        }

        private void OnEnable()
        {
            loggedMissingTarget = false;
            loggedValidClick = false;
            ResetInteraction();
        }

        private void OnDisable()
        {
            ResetInteraction();
        }

        private void ResetInteraction()
        {
            interactionRevision++;
            var data = eventData;
            var releaseTarget = pressedObject;
            var canceledDrag = data != null && data.dragging ? dragObject : null;
            var hoverTarget = lastHighlightedObject;
            // Clear ownership before native callbacks, which can disable or reopen the UI.
            pressedObject = null;
            pressedClickObject = null;
            lastHighlightedObject = null;
            hitObject = null;
            dragObject = null;
            interactionCamera = null;
            lastClickObject = null;
            clickCount = 0;
            rightJoyTimeHeld = 0f;
            rotated = false;
            confirm.BlockUntilRelease();
            dragCanceledUntilRelease = true;
            if (data != null)
            {
                data.button = PointerEventData.InputButton.Left;
                data.pointerPress = null;
                data.rawPointerPress = null;
                data.pointerEnter = null;
                data.eligibleForClick = false;
                data.dragging = false;
                data.pointerDrag = canceledDrag;
                if (releaseTarget) ExecuteEvents.Execute(releaseTarget, data, ExecuteEvents.pointerUpHandler);
                if (canceledDrag) ExecuteEvents.Execute(canceledDrag, data, ExecuteEvents.endDragHandler);
                data.pointerDrag = null;
                if (hoverTarget) ExecuteEvents.Execute(hoverTarget, data, ExecuteEvents.pointerExitHandler);
            }
        }

        private bool IsCurrentInteraction(int revision)
        {
            return isActiveAndEnabled && revision == interactionRevision && pointerSystem && pointerSystem == EventSystem.current;
        }

        public void Update()
        {
            if (!EventSystem.current)
            {
                if (eventData != null) ResetInteraction();
                eventData = null;
                pointerSystem = null;
                return;
            }
            if (eventData == null || pointerSystem != EventSystem.current)
            {
                ResetInteraction();
                if (!isActiveAndEnabled || !EventSystem.current) return;
                pointerSystem = EventSystem.current;
                eventData = new PointerEventData(pointerSystem);
            }
            int revision = interactionRevision;
            float joystick = VRSettings.GetLeftHandedMode() ? SteamVR_Actions._default.LeftJoystick.axis.x : SteamVR_Actions._default.RightJoystick.axis.x;
            if (dragObject && Mathf.Abs(joystick) > 0.7f) rightJoyTimeHeld += Time.unscaledDeltaTime;
            else if (Mathf.Abs(joystick) < 0.7f) { rightJoyTimeHeld = 0f; rotated = false; }

            bool physicsHit = Physics.Raycast(transform.position, transform.forward, out var hit, RayDistance, LayerMask.GetMask("UI"));
            if (physicsHit)
            {
                lastHitDistance = hit.distance;
                uiPointerPos = hit.point;
                hitObject = NormalizeTarget(RaycastFindHit(hit));
            }
            else
            {
                hitObject = null;
                uiPointerPos = transform.position + transform.forward * lastHitDistance;
                eventData.pointerCurrentRaycast = default;
                if (interactionCamera) SetPointerPosition(interactionCamera.WorldToScreenPoint(uiPointerPos));
            }
            eventData.worldPosition = uiPointerPos;
            eventData.pointerEnter = hitObject;
            HandleHover();
            if (!IsCurrentInteraction(revision)) return;
            HandleConfirm(); // Release must be delivered even when the ray leaves the UI.
            if (!IsCurrentInteraction(revision)) return;
            if (hitObject) HandleScroll();
            if (!IsCurrentInteraction(revision)) return;
            HandleDragging(); // Observe trigger release even while pointing away from UI.
        }

        public void EndDrop()
        {
            dragObject = null;
            if (eventData != null) { eventData.pointerDrag = null; eventData.dragging = false; }
        }

        private void HandleHover()
        {
            if (lastHighlightedObject == hitObject) return;
            int revision = interactionRevision;
            var previous = lastHighlightedObject;
            lastHighlightedObject = null;
            if (previous) ExecuteEvents.Execute(previous, eventData, ExecuteEvents.pointerExitHandler);
            if (!IsCurrentInteraction(revision)) return;
            if (hitObject && (!dragObject || hitObject.GetComponent<HealthBarButton>()))
            {
                lastHighlightedObject = hitObject;
                ExecuteEvents.Execute(hitObject, eventData, ExecuteEvents.pointerEnterHandler);
            }
        }

        private static GameObject NormalizeTarget(GameObject target)
        {
            if (!target || !target.transform.parent) return target;
            var parent = target.transform.parent;
            if (target.name == "Toggle" && parent.parent && parent.parent.TryGetComponent(out CategoryView category)) return category._toggle.gameObject;
            if (parent.TryGetComponent(out SubcategoryView _)) return parent.gameObject;
            if (target.name == "Main" && parent.TryGetComponent(out CategoryView _)) return parent.gameObject;
            return target;
        }

        private void HandleConfirm()
        {
            int revision = interactionRevision;
            var action = VRSettings.GetLeftHandedMode() ? SteamVR_Actions._default.ButtonX : SteamVR_Actions._default.ButtonA;
            var clickTarget = hitObject ? (ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject) ?? hitObject) : null;
            var events = confirm.Step(action.stateDown, action.state, action.stateUp, hitObject,
                pressedClickObject && clickTarget == pressedClickObject, dragObject || IsDragPressed(), Time.unscaledDeltaTime);
            if (action.stateDown && !hitObject && !loggedMissingTarget)
            {
                loggedMissingTarget = true;
                Plugin.MyLog.LogInfo($"[VR UI] Confirm pressed without a UI target (camera '{interactionCamera?.name}').");
            }
            if ((events & UiConfirmEvent.Down) != 0)
            {
                eventData.button = PointerEventData.InputButton.Left;
                eventData.eligibleForClick = true;
                eventData.delta = Vector2.zero;
                eventData.pressPosition = eventData.position;
                eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
                eventData.rawPointerPress = hitObject;
                pressedClickObject = clickTarget;
                skipLeftClick = hitObject.GetComponent<EmptyItemView>();
                pressedObject = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hitObject) ?? clickTarget;
                eventData.pointerPress = pressedObject;
                ExecuteEvents.Execute(pressedObject, eventData, ExecuteEvents.pointerDownHandler);
                if (!IsCurrentInteraction(revision)) return;
            }
            if ((events & UiConfirmEvent.ContextClick) != 0 && pressedClickObject)
            {
                eventData.button = PointerEventData.InputButton.Right;
                eventData.clickCount = 1;
                ExecuteEvents.Execute(pressedClickObject, eventData, ExecuteEvents.pointerClickHandler);
                if (!IsCurrentInteraction(revision)) return;
            }
            if ((events & UiConfirmEvent.Up) != 0)
            {
                eventData.button = PointerEventData.InputButton.Left;
                var releaseTarget = pressedObject;
                pressedObject = null;
                if (releaseTarget) ExecuteEvents.Execute(releaseTarget, eventData, ExecuteEvents.pointerUpHandler);
                if (!IsCurrentInteraction(revision)) return;
                if ((events & UiConfirmEvent.Click) != 0 && !skipLeftClick && clickTarget)
                {
                    clickCount = clickTarget == lastClickObject && Time.unscaledTime - lastClickTime <= 0.25f ? clickCount + 1 : 1;
                    lastClickTime = Time.unscaledTime;
                    lastClickObject = clickTarget;
                    eventData.clickCount = clickCount;
                    eventData.clickTime = lastClickTime;
                    string targetName = clickTarget.name;
                    string cameraName = interactionCamera ? interactionCamera.name : "none";
                    bool delivered = ExecuteEvents.Execute(clickTarget, eventData, ExecuteEvents.pointerClickHandler);
                    if (!IsCurrentInteraction(revision)) return;
                    if (delivered && !loggedValidClick)
                    {
                        loggedValidClick = true;
                        Plugin.MyLog.LogInfo($"[VR UI] Confirm delivered to '{targetName}' using '{cameraName}'.");
                    }
                }
                pressedObject = null;
                pressedClickObject = null;
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;
                eventData.eligibleForClick = false;
            }
        }

        private void HandleScroll()
        {
            int revision = interactionRevision;
            float axis = VRSettings.GetLeftHandedMode() ? SteamVR_Actions._default.LeftJoystick.axis.y : SteamVR_Actions._default.RightJoystick.axis.y;
            if (Mathf.Abs(axis) <= 0.4f) return;
            eventData.scrollDelta = new Vector2(0, axis / 1.5f);
            var noDrag = hitObject.GetComponentInParent<ScrollRectNoDrag>();
            if (noDrag) ExecuteEvents.Execute(noDrag.gameObject, eventData, ExecuteEvents.scrollHandler);
            if (!IsCurrentInteraction(revision)) return;
            var scroll = hitObject.GetComponentInParent<ScrollRect>();
            if (scroll) ExecuteEvents.Execute(scroll.gameObject, eventData, ExecuteEvents.scrollHandler);
            if (!IsCurrentInteraction(revision)) return;
            var light = hitObject.GetComponentInParent<LightScroller>();
            if (light) ExecuteEvents.Execute(light.gameObject, eventData, ExecuteEvents.scrollHandler);
        }

        private void HandleDragging()
        {
            int revision = interactionRevision;
            eventData.button = PointerEventData.InputButton.Left;
            if (dragCanceledUntilRelease)
            {
                if (!IsDragPressed()) dragCanceledUntilRelease = false;
                return;
            }
            if (IsDragPressed())
            {
                if (!dragObject)
                {
                    if (!hitObject) return;
                    dragObject = ExecuteEvents.GetEventHandler<IDragHandler>(hitObject);
                    if (!dragObject) return;
                    pressPosition = uiPointerPos;
                    eventData.pressPosition = eventData.position;
                    eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
                    eventData.pointerDrag = dragObject;
                    ExecuteEvents.Execute(dragObject, eventData, ExecuteEvents.initializePotentialDrag);
                    if (!IsCurrentInteraction(revision) || !dragObject) return;
                    eventData.dragging = true;
                    ExecuteEvents.Execute(dragObject, eventData, ExecuteEvents.beginDragHandler);
                }
                else
                {
                    eventData.pointerDrag = dragObject;
                    eventData.dragging = true;
                    ExecuteEvents.Execute(dragObject, eventData, ExecuteEvents.dragHandler);
                }
                if (!IsCurrentInteraction(revision)) return;
                var cancel = VRSettings.GetLeftHandedMode() ? SteamVR_Actions._default.ButtonY : SteamVR_Actions._default.ButtonB;
                if (dragObject && cancel.stateUp)
                {
                    CancelDrag();
                    dragCanceledUntilRelease = true;
                }
            }
            else if (dragObject && hitObject) DropItem();
            else if (dragObject) CancelDrag();
        }

        private void DropItem()
        {
            var source = dragObject;
            dragObject = null;
            eventData.dragging = false;
            eventData.pointerDrag = source;
            ExecuteEvents.ExecuteHierarchy(hitObject, eventData, ExecuteEvents.dropHandler);
            if (source) ExecuteEvents.Execute(source, eventData, ExecuteEvents.endDragHandler);
            eventData.pointerDrag = null;
        }

        private void CancelDrag()
        {
            var source = dragObject;
            dragObject = null;
            eventData.pointerEnter = null;
            eventData.pointerDrag = source;
            eventData.dragging = false;
            if (source) ExecuteEvents.Execute(source, eventData, ExecuteEvents.endDragHandler);
            eventData.pointerDrag = null;
        }

        private void SetPointerPosition(Vector2 screenPosition)
        {
            eventData.delta = screenPosition - eventData.position;
            eventData.position = screenPosition;
        }

        private GameObject RaycastFindHit(RaycastHit hit)
        {
            var canvas = hit.collider.GetComponentInParent<Canvas>();
            interactionCamera = canvas && canvas.rootCanvas.worldCamera ? canvas.rootCanvas.worldCamera : Camera.main;
            if (!interactionCamera || !pointerSystem) return null;
            SetPointerPosition(interactionCamera.WorldToScreenPoint(hit.point));
            raycasts.Clear();
            pointerSystem.RaycastAll(eventData, raycasts);
            eventData.pointerCurrentRaycast = raycasts.Count > 0 ? raycasts[0] : default;
            if (hit.collider.gameObject.name == "Cube") return hit.collider.gameObject;
            if (raycasts.Count == 0) return null;
            GameObject target = raycasts[0].gameObject;
            while (target)
            {
                if (target.GetComponent<IPointerEnterHandler>() != null || target.GetComponent<IPointerClickHandler>() != null || target.GetComponent<IPointerDownHandler>() != null || target.GetComponent<IBeginDragHandler>() != null || target.GetComponent<IDragHandler>() != null || target.GetComponent<IContainerView>() != null)
                {
                    var viewporter = target.GetComponentInParent<CameraViewporter>();
                    if (viewporter && viewporter.TargetCamera) SetPointerPosition(viewporter.TargetCamera.WorldToScreenPoint(hit.point));
                    return target;
                }
                if ((target.name == "Background Tile" || target.name == "HighlightPanel") && target.transform.parent) return target.transform.parent.gameObject;
                target = target.transform.parent ? target.transform.parent.gameObject : null;
            }
            return null;
        }
    }
}
