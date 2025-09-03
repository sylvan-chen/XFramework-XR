#if XRIT_INTEGRATION || STEAM_VR_SDK || OCCULUS_XR_PLUGIN
#define USING_XR_SDK
#endif

using UnityEngine;
#if USING_XR_SDK
using UnityEngine.XR;
#endif

namespace XGame.Modules.SpatialMagnifier
{
    [ExecuteAlways]
    public class SpatialMagnifierController : MonoBehaviour
    {
        [Header("目标3D点")]
        public Transform targetWorldPoint;

        [Header("放大镜材质")]
        public Material magnifierMaterial;

        private Camera xrCamera;
#if USING_XR_SDK
        private bool isDoubleEye = false;
#endif

        void Start()
        {
            if (xrCamera == null)
            {
                xrCamera = Camera.main;
            }

#if USING_XR_SDK
        switch (XRSettings.stereoRenderingMode)
        {
            case XRSettings.StereoRenderingMode.SinglePassInstanced:
                Debug.Log("XR Camera is in SinglePassInstanced mode.");
                isDoubleEye = true;
                break;
            case XRSettings.StereoRenderingMode.SinglePassMultiview:
                Debug.Log("XR Camera is in SinglePassMultiview mode.");
                isDoubleEye = true;
                break;
            case XRSettings.StereoRenderingMode.MultiPass:
                Debug.Log("XR Camera is in MultiPass mode.");
                isDoubleEye = true;
                break;
            default:
                Debug.Log("XR Camera is in Unknown mode.");
                FallbackStereoCheck();
                break;
        }
#endif
        }

#if USING_XR_SDK
        private void FallbackStereoCheck()
        {
            var leftView = xrCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
            var rightView = xrCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

            if (leftView == rightView)
            {
                Debug.Log("Fallback check: XR Camera is NOT stereo.");
                isDoubleEye = false;
            }
            else
            {
                Debug.Log("Fallback check: XR Camera is stereo.");
                isDoubleEye = true;
            }
        }
#endif

        void Update()
        {
            if (targetWorldPoint == null || xrCamera == null || magnifierMaterial == null)
                return;

            Vector3 leftViewport, rightViewport;

#if USING_XR_SDK
        if (isDoubleEye)
        {
            leftViewport = xrCamera.WorldToViewportPoint(targetWorldPoint.position, Camera.MonoOrStereoscopicEye.Left);
            rightViewport = xrCamera.WorldToViewportPoint(targetWorldPoint.position, Camera.MonoOrStereoscopicEye.Right);
        }
        else
        {
            leftViewport = xrCamera.WorldToViewportPoint(targetWorldPoint.position);
            rightViewport = leftViewport;
        }
#else
            leftViewport = xrCamera.WorldToViewportPoint(targetWorldPoint.position);
            rightViewport = leftViewport;
#endif

            if (leftViewport.z > 0 && rightViewport.z > 0)
            {
                magnifierMaterial.SetVector("_CenterL", new Vector4(leftViewport.x, leftViewport.y, 0, 0));
                magnifierMaterial.SetVector("_CenterR", new Vector4(rightViewport.x, rightViewport.y, 0, 0));
            }
        }

        public void EnableMagnifierOpaqueTexture()
        {
            MagnifierOpaqueTexture.IsEnabled = true;
            if (magnifierMaterial != null) magnifierMaterial.SetFloat("_EnableMagnifierOpaqueTexture", 1f);
        }

        public void DisableMagnifierOpaqueTexture()
        {
            MagnifierOpaqueTexture.IsEnabled = false;
            if (magnifierMaterial != null) magnifierMaterial.SetFloat("_EnableMagnifierOpaqueTexture", 0f);
        }
    }
}