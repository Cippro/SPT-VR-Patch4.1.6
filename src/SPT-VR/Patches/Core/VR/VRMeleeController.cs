using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EFT.Player;
using EFT.Ballistics;
using TarkovVR.ModSupport;
using TarkovVR.Source.Controls;
using Valve.VR;
using TarkovVR.Source.Settings;
using UnityEngine;

namespace TarkovVR.Patches.Core.VR
{
    [HarmonyPatch]
    internal class VRMeleeController
    {
        private const float KNIFE_SWING_THRESHOLD = 4.0f;
        private const float KNIFE_SWING_RESET = 1.0f;
        // Set true to log every surface hit we broadcast (pair with MeleeHitApply.debug on the observer
        // to see send vs. apply in the BepInEx log).
        public static bool debugMeleeSend = false;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EFT.Player), "LateUpdate")]
        private static void ManageKnifeCollider(EFT.Player __instance)
        {
            if (__instance == null || !__instance.IsYourPlayer)
                return;
            BaseKnifeController knifeController = __instance.HandsController as BaseKnifeController;
            if (knifeController?._knifeCollider == null)
                return;
            Vector3 controllerVelocity = VRSettings.GetLeftHandedMode() ? ControllerVelocity.GetSteamVRVelocity(SteamVR_Input_Sources.LeftHand) : ControllerVelocity.GetSteamVRVelocity(SteamVR_Input_Sources.RightHand);
            Vector3 worldVelocity = VRGlobals.vrOffsetter.transform.TransformDirection(controllerVelocity);
            float speed = worldVelocity.magnitude;
            // On start of swing
            if (speed > KNIFE_SWING_THRESHOLD && !knifeController._knifeCollider.enabled)
            {
                knifeController.LastKickType = EKickType.Slash;
                knifeController._knifeCollider.MaxDistance = knifeController.Knife.Template.PrimaryDistance;
                knifeController._knifeCollider.enabled = true;
                knifeController._knifeCollider.OnHit -= knifeController.ProcessOverlap;
                knifeController._knifeCollider.OnHit += knifeController.ProcessOverlap;
                knifeController._knifeCollider.OnFire();
                float staminaCost = (knifeController.LastKickType == EKickType.Slash) ? knifeController.Knife.Template.PrimaryConsumption : knifeController.Knife.Template.SecondaryConsumption;
                knifeController._player.Physical.ConsumeAsMelee(staminaCost);
                knifeController._firearmsAnimator?.SetAnimationSpeed(50f);
                knifeController._firearmsAnimator?.SetFire(true);
                knifeController.PreviousColliderPosition = knifeController._knifeCollider.transform.position;
            }
            else if (knifeController._knifeCollider.enabled && speed > KNIFE_SWING_RESET)
            {
                knifeController._knifeCollider.ManualUpdate();
                Vector3 position = knifeController._knifeCollider.transform.position;
                Vector3 normalized = (position - knifeController.PreviousColliderPosition).normalized;
                knifeController.GlobalSwingDirection = Vector3.Lerp(knifeController.GlobalSwingDirection, normalized, 0.9f);
                knifeController.PreviousColliderPosition = position;
                knifeController._firearmsAnimator?.SetFire(false);
            }
            else if (speed < KNIFE_SWING_RESET && knifeController._knifeCollider.enabled)
            {
                knifeController._knifeCollider.enabled = false;
                knifeController._knifeCollider.OnFireEnd();
            }
        }
    }
}
