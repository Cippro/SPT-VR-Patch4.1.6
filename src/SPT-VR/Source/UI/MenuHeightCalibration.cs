using UnityEngine;
using Valve.VR;

namespace TarkovVR.Source.UI
{
    internal sealed class MenuHeightCalibration : MonoBehaviour
    {
        private Camera menuCamera;
        private Transform trackingOrigin;
        private Transform menuCanvas;
        private SteamVR_TrackedObject trackedHead;
        private float deadline;
        private bool pending;
        private int trackedFrames;
        private bool trackingWarningLogged;

        internal void Request(Camera camera, Transform origin, Transform canvas)
        {
            menuCamera = camera;
            trackingOrigin = origin;
            menuCanvas = canvas;
            trackedHead = camera.GetComponent<SteamVR_TrackedObject>();
            deadline = Time.unscaledTime + 5f;
            pending = true;
            trackedFrames = 0;
            trackingWarningLogged = false;
        }

        private void LateUpdate()
        {
            if (!pending) return;
            if (VRGlobals.inGame || !menuCamera || !trackingOrigin || !menuCanvas)
            {
                pending = false;
                return;
            }
            if (!trackedHead || !trackedHead.isValid || !menuCamera.stereoEnabled)
            {
                if (Time.unscaledTime > deadline && !trackingWarningLogged)
                {
                    trackingWarningLogged = true;
                    Plugin.MyLog.LogWarning("[MenuVR] Waiting for a valid stereo headset pose before calibrating menu height.");
                }
                return;
            }
            if (++trackedFrames < 2) return;

            // Preserve the original canvas placement and its ~3 cm offset below the eyes.
            // A standing tracking origin reports headset height above the floor, not zero.
            Vector3 leftEye = menuCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse.GetColumn(3);
            Vector3 rightEye = menuCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse.GetColumn(3);
            float eyeHeight = (leftEye.y + rightEye.y) * 0.5f;
            float correction = menuCanvas.position.y + 0.0333f - eyeHeight;
            trackingOrigin.position += Vector3.up * correction;
            if (VRGlobals.camRoot) VRGlobals.camRoot.transform.position = trackingOrigin.position;
            pending = false;
            Plugin.MyLog.LogInfo($"[MenuVR] Headset height calibrated: eye {eyeHeight:F3}, menu {menuCanvas.position.y:F3}, correction {correction:F3} m.");
        }
    }
}
