using UnityEngine;
using Valve.VR;
using TarkovVR.Patches.Misc;
using TarkovVR.Source.Misc;
using TarkovVR.Source.UI;
using System.Reflection;
using TarkovVR.Source.Settings;

namespace TarkovVR.Source.Player.VRManager
{
    internal class MenuVRManager : MonoBehaviour
    {
        public Vector3 initPos;
        // VR Origin and body stuff
        public GameObject LeftHand = null;
        public GameObject RightHand = null;
        public LaserPointer pointer = null;
        private float x = 42;
        private float y = 355;
        private float z = 0;
        private Vector3 pos;
        public SteamVR_Action_Vibration hapticAction;
        private float timeHeld = 0;
        private bool _listenersAdded = false;
        public void Awake()
        {
            SpawnHands();
            if (RightHand)
            {
                RightHand.transform.parent = VRGlobals.vrOffsetter.transform;
            }

            if (LeftHand)
                LeftHand.transform.parent = VRGlobals.vrOffsetter.transform;
            if (VRKeyboardController.Instance == null)
            {
                GameObject kbHandler = new GameObject("VRKeyboardManager");
                kbHandler.AddComponent<VRKeyboardController>();
                DontDestroyOnLoad(kbHandler);
            }

            AddPoseListeners();
        }

        /*
        public void OnDisable()
        {
            
            if (pointer && pointer.holder)
            {
                pointer.enabled = false;
                pointer.holder.active = false;
                MenuPatches.vrUiInteracter.enabled = false;
                SteamVR_Actions._default.RightHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateRightHand);
                SteamVR_Actions._default.LeftHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateLeftHand);
                SteamVR_Actions._default.RightHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateLeftHand);
                SteamVR_Actions._default.LeftHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateRightHand);
            }
            
        }
        
        public void OnEnable()
        {
            if (pointer && pointer.holder)
            {
                pointer.enabled = true;
                pointer.holder.active = true;
                MenuPatches.vrUiInteracter.enabled = true;
                if (VRSettings.GetLeftHandedMode())
                {
                    SteamVR_Actions._default.LeftHandPose.AddOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateRightHand);
                    SteamVR_Actions._default.RightHandPose.AddOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateLeftHand);
                }
                else
                {
                    SteamVR_Actions._default.RightHandPose.AddOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateRightHand);
                    SteamVR_Actions._default.LeftHandPose.AddOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateLeftHand);
                }
            }
        }
        */
        public void OnDisable()
        {
            RemovePoseListeners();
            if (pointer)
            {
                pointer.enabled = false;
                if (pointer.holder) pointer.holder.SetActive(false);
            }
            if (MenuPatches.vrUiInteracter)
                MenuPatches.vrUiInteracter.enabled = false;
        }

        public void OnDestroy()
        {
            RemovePoseListeners();
        }

        public void OnEnable()
        {
            SpawnHands();
            if (pointer)
            {
                pointer.enabled = true;
                if (pointer.holder) pointer.holder.SetActive(true);
            }
            if (MenuPatches.vrUiInteracter)
                MenuPatches.vrUiInteracter.enabled = true;
            AddPoseListeners();
        }

        private void AddPoseListeners()
        {
            RemovePoseListeners();
            if (VRSettings.GetLeftHandedMode())
            {
                SteamVR_Actions._default.LeftHandPose.AddOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateRightHand);
                SteamVR_Actions._default.RightHandPose.AddOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateLeftHand);
            }
            else
            {
                SteamVR_Actions._default.RightHandPose.AddOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateRightHand);
                SteamVR_Actions._default.LeftHandPose.AddOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateLeftHand);
            }
        }

        private void RemovePoseListeners()
        {
            SteamVR_Actions._default.RightHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateRightHand);
            SteamVR_Actions._default.LeftHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateLeftHand);
            SteamVR_Actions._default.RightHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateLeftHand);
            SteamVR_Actions._default.LeftHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateRightHand);
        }

        public void LeftHandedMode()
        {
            SpawnHands();
        }

        public void RightHandedMode()
        {
            SpawnHands();
        }

        //private void Update()
        //{
        //    if (Camera.main == null)
        //        return;
        //    if (initPos.y == 0)
        //        initPos = Camera.main.transform.localPosition;
        //    VRGlobals.vrOffsetter.transform.localPosition = initPos * -1;
        //    if (SteamVR_Actions._default.ClickRightJoystick.GetState(SteamVR_Input_Sources.Any))
        //    {
        //        timeHeld += Time.deltaTime;
        //        if (Camera.main != null && timeHeld > 0.75f)
        //        {
        //            initPos = Camera.main.transform.localPosition;
        //        }
        //    }
        //    else if (timeHeld != 0)
        //    {
        //        timeHeld = 0;
        //    }
        //    //SteamVR_Actions._default.Haptic.Execute(0, 0.1f, 1, 1, SteamVR_Input_Sources.RightHand);
        //}
        private void UpdateRightHand(SteamVR_Action_Pose fromAction, SteamVR_Input_Sources fromSource)
        {
            if (RightHand)
            {
                RightHand.transform.localPosition = fromAction.localPosition + new Vector3(0, -0.03f, 0);
                //RightHand.transform.localPosition = fromAction.localPosition;
                //RightHand.transform.localEulerAngles = fromAction.localRotation.eulerAngles + new Vector3(x,y,z);
                RightHand.transform.localRotation = fromAction.localRotation;
                RightHand.transform.Rotate(45, 10, 0);
            }
        }

        private void UpdateLeftHand(SteamVR_Action_Pose fromAction, SteamVR_Input_Sources fromSource)
        {
            if (LeftHand)
            {
                LeftHand.transform.localPosition = fromAction.localPosition;
                LeftHand.transform.localRotation = fromAction.localRotation;
            }
        }

        public void SpawnHands()
        {
            if (!VRGlobals.vrOffsetter)
                return;
            if (!RightHand)
            {
                RightHand = CreateMenuHand("RightHand", true);
            }
            if (!LeftHand)
            {
                LeftHand = CreateMenuHand("LeftHand", false);
            }
            RightHand.transform.SetParent(VRGlobals.vrOffsetter.transform, false);
            LeftHand.transform.SetParent(VRGlobals.vrOffsetter.transform, false);
            ConfigureMenuPose(RightHand, true);
            ConfigureMenuPose(LeftHand, false);
            MenuPatches.vrUiInteracter = RightHand.GetComponent<VRUIInteracter>() ?? RightHand.AddComponent<VRUIInteracter>();
            pointer = RightHand.GetComponent<LaserPointer>() ?? RightHand.AddComponent<LaserPointer>();
            pointer.pose = RightHand.GetComponent<SteamVR_Behaviour_Pose>();
            pointer.interactWithUI = VRSettings.GetLeftHandedMode() ? SteamVR_Actions._default.ButtonX : SteamVR_Actions._default.ButtonA;
            pointer.color = Color.cyan;
            pointer.enabled = isActiveAndEnabled;
            MenuPatches.vrUiInteracter.enabled = isActiveAndEnabled;
            if (isActiveAndEnabled) AddPoseListeners();
        }

        private static GameObject CreateMenuHand(string name, bool primary)
        {
            var hand = new GameObject(name);
            hand.SetActive(false);
            hand.transform.SetParent(VRGlobals.vrOffsetter.transform, false);
            hand.AddComponent<SteamVR_Behaviour_Pose>();
            ConfigureMenuPose(hand, primary);
            hand.SetActive(true);
            return hand;
        }

        private static void ConfigureMenuPose(GameObject hand, bool primary)
        {
            var pose = hand.GetComponent<SteamVR_Behaviour_Pose>() ?? hand.AddComponent<SteamVR_Behaviour_Pose>();
            bool useLeftPose = primary == VRSettings.GetLeftHandedMode();
            pose.poseAction = useLeftPose ? SteamVR_Actions._default.LeftHandPose : SteamVR_Actions._default.RightHandPose;
            pose.inputSource = useLeftPose ? SteamVR_Input_Sources.LeftHand : SteamVR_Input_Sources.RightHand;
            // This manager applies the same poses with VR hand offsets in its listeners.
            // Keep the behaviour only as the pointer's action/source descriptor.
            pose.enabled = false;
        }
    }
}
