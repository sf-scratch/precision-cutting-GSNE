using CSharp_OPTControllerAPI;
using Emgu.CV;
using Emgu.CV.Reg;
using NPOI.SS.Formula.Functions;
using SciCamera.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using 精密切割系统.Model.camera;
using 精密切割系统.Utils;
using static OpenCvSharp.ML.DTrees;
using static SciCamera.Net.SciCam;

using GdiPlus = System.Drawing.Imaging;

namespace 精密切割系统.Driver
{
    internal class CameraUtils
    {
        public static event Action<ImageData>? PayloadReceived;

        private static SCI_DEVICE_INFO_LIST _stDevList = new SCI_DEVICE_INFO_LIST();    //设备列表
        private static List<SciCam> _sciCams = new List<SciCam>(); // 所有相机设备
        private static fnOnPayloadDelegate ImageCallback;		// 静态回调变量
        public static string errorMessage = ""; // 异常信息

        private static SciCam _currentDev = new SciCam();     //当前设备

        public static SciCam CurrentDev
        { get { return _currentDev; } }

        private static bool _bDeviceOpened = false;        //相机是否打开

        public static bool BDeviceOpened
        { get { return _bDeviceOpened; } }

        private static string lightIp = "192.168.10.150"; // 光源IP
        public static bool l_lightConnectStatus = false; // 光源连接状态
        public static string l_errorMessage = ""; // 光源异常信息
        private static LightControllerAPI LightController = new LightControllerAPI();

        static CameraUtils()
        {
            ImageCallback = new fnOnPayloadDelegate(OnPayloadReceived);
        }

        private static void OnPayloadReceived(nint payload, nint tag)
        {
            if (TryGetConvertedInfo(payload, out ImageData bitmap))
            {
                PayloadReceived?.Invoke(bitmap);
            }
        }

        private static bool TryGetConvertedInfo(IntPtr payload, out ImageData bitmapResult)
        {
            bitmapResult = null;
            if (payload == IntPtr.Zero)
            {
                return false;
            }

            SCI_CAM_PAYLOAD_ATTRIBUTE payloadAttribute = new SCI_CAM_PAYLOAD_ATTRIBUTE();
            uint nReVal = PayloadGetAttribute(payload, ref payloadAttribute);
            if (nReVal != SCI_CAMERA_OK)
            {
                return false;
            }

            bool imgIsComplete = payloadAttribute.isComplete;
            SciCamPayloadMode payloadMode = payloadAttribute.payloadMode;
            SciCamPixelType imgPixelType = payloadAttribute.imgAttr.pixelType;
            ulong imgWidth = payloadAttribute.imgAttr.width;
            ulong imgHeight = payloadAttribute.imgAttr.height;
            ulong framID = payloadAttribute.frameID;

            if (!imgIsComplete || payloadMode != SciCamPayloadMode.SciCam_PayloadMode_2D)
            {
                return false;
            }

            IntPtr imgData = IntPtr.Zero;
            nReVal = PayloadGetImage(payload, ref imgData);
            if (nReVal != SCI_CAMERA_OK)
            {
                return false;
            }

            long destImgSize = 0;

            if (imgPixelType == SciCamPixelType.Mono1p ||
                imgPixelType == SciCamPixelType.Mono2p ||
                imgPixelType == SciCamPixelType.Mono4p ||
                imgPixelType == SciCamPixelType.Mono8s ||
                imgPixelType == SciCamPixelType.Mono8 ||
                imgPixelType == SciCamPixelType.Mono10 ||
                imgPixelType == SciCamPixelType.Mono10p ||
                imgPixelType == SciCamPixelType.Mono12 ||
                imgPixelType == SciCamPixelType.Mono12p ||
                imgPixelType == SciCamPixelType.Mono14 ||
                imgPixelType == SciCamPixelType.Mono16 ||
                imgPixelType == SciCamPixelType.Mono10Packed ||
                imgPixelType == SciCamPixelType.Mono12Packed ||
                imgPixelType == SciCamPixelType.Mono14p)
            {
                nReVal = PayloadConvertImage(ref payloadAttribute.imgAttr, imgData, SciCamPixelType.Mono8, IntPtr.Zero, ref destImgSize, true);
                if (nReVal == SCI_CAMERA_OK)
                {
                    IntPtr destImg = Marshal.AllocHGlobal((int)destImgSize);
                    try
                    {
                        nReVal = PayloadConvertImage(ref payloadAttribute.imgAttr, imgData, SciCamPixelType.Mono8, destImg, ref destImgSize, true);
                        if (nReVal == SCI_CAMERA_OK)
                        {
                            byte[] bBitmap = new byte[destImgSize];
                            Marshal.Copy(destImg, bBitmap, 0, (int)destImgSize);
                            bitmapResult = new ImageData(bBitmap, (int)imgWidth, (int)imgHeight);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(destImg);
                    }
                }
            }
            else
            {
                nReVal = PayloadConvertImage(ref payloadAttribute.imgAttr, imgData, SciCamPixelType.RGB8, IntPtr.Zero, ref destImgSize, true);
                if (nReVal == SCI_CAMERA_OK)
                {
                    IntPtr destImg = Marshal.AllocHGlobal((int)destImgSize);
                    try
                    {
                        nReVal = PayloadConvertImage(ref payloadAttribute.imgAttr, imgData, SciCamPixelType.RGB8, destImg, ref destImgSize, true);
                        if (nReVal == SCI_CAMERA_OK)
                        {
                            byte[] bBitmap = new byte[destImgSize];
                            Marshal.Copy(destImg, bBitmap, 0, (int)destImgSize);
                            bitmapResult = new ImageData(bBitmap, (int)imgWidth, (int)imgHeight);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(destImg);
                    }
                }
            }
            return false;
        }

        public static void ConnectDevice()
        {
            // 查找相机
            DiscoveryDevices();
            // 判断相机是否打开
            if (!_bDeviceOpened)
            {
                // 打开相机
                OpenDevice();
            }
            SetCameraDeviceWaferParams();
        }

        /// <summary>
        /// 连接光源控制器
        /// </summary>
        public static void ConnectLight()
        {
            long lRet = LightController.CreateEthernetConnectionByIP(lightIp);
            if (0 != lRet)
            {
                // "Failed to create Ethernet connection by IP"
                l_errorMessage = "Failed to create Ethernet connection by IP";
                l_lightConnectStatus = false;
                return;
            }
            else
            {
                l_lightConnectStatus = true;
                // textBox_Status.Text = "Succeed";
            }
        }

        /// <summary>
        /// 断开光源控制器连接
        /// </summary>
        public static void DisconnectLight()
        {
            long lRet = LightController.DestroyEthernetConnect();
            if (0 != lRet)
            {
                // textBox_Status.Text = "Failed to disconnect Ethernet connection by IP";
                return;
            }
        }

        /// <summary>
        /// 设置光源亮度
        /// </summary>
        /// <param name="intensity">光源亮度 1-255</param>
        public static void SetLightIntensity(int intensity, int channel)
        {
            if (LightController.SetIntensity(channel, intensity) == 0)
            {
                l_errorMessage = "Set intensity successfully";
            }
            else
            {
                l_errorMessage = "Failed to set intensity";
            }
            Tools.LogError(l_errorMessage);
        }

        /// <summary>
        /// 关闭光源
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static bool TurnOffChannel(int channel)
        {
            if (LightController.TurnOffChannel(channel) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 打开光源
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static bool TurnOnChannel(int channel)
        {
            if (LightController.TurnOnChannel(channel) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void OpenDevice()
        {
            if (_stDevList.count == 0)
            {
                Tools.LogError("Please select a device first!");
                return;
            }
            for (int i = 0; i < _stDevList.pDevInfo.Length; i++)
            {
                SCI_DEVICE_INFO item = _stDevList.pDevInfo[i];
                SCI_DEVICE_GIGE_INFO gigeDevInfo = (SCI_DEVICE_GIGE_INFO)ByteToStruct(item.info.gigeInfo, typeof(SCI_DEVICE_GIGE_INFO));
                string devIP = new IPAddress(gigeDevInfo.ip).ToString();
                if (devIP.StartsWith("192.168.10"))
                {
                    SciCam tempSciCam = new SciCam();
                    uint nReVal = tempSciCam.CreateDevice(ref item);
                    if (nReVal != SCI_CAMERA_OK)
                    {
                        Tools.LogError("Create device failed");
                        return;
                    }

                    nReVal = tempSciCam.OpenDevice();
                    if (nReVal != SCI_CAMERA_OK)
                    {
                        Tools.LogError("Open device failed");
                        return;
                    }

                    nReVal = tempSciCam.SetEnumValueByString("TriggerMode", "Off");
                    if (nReVal != SCI_CAMERA_OK)
                    {
                        Tools.LogError("Set trigger mode off failed");
                    }
                    nReVal = tempSciCam.SetEnumValueByString("PixelFormat", "RGB8");
                    _sciCams.Add(tempSciCam);
                }
            }
            // 当前设备默认是第一个
            _currentDev = _sciCams[0];
            // 注册回调采集
            uint result = _currentDev.RegisterPayloadCallBack(ImageCallback, IntPtr.Zero, true);
            _bDeviceOpened = true;
        }

        public static void SetCameraExposureTime(double exposureTime)
        {
            _currentDev.SetEnumValueByStringEx(SciCam.SciCamDeviceXmlType.SciCam_DeviceXml_Camera, "ExposureAuto", "Off");
            _currentDev.SetFloatValueEx(SciCam.SciCamDeviceXmlType.SciCam_DeviceXml_Camera, "ExposureTime", exposureTime);
        }

        public static double GetCameraExposureTime()
        {
            SciCam.SCI_NODE_VAL_FLOAT pVal = new SciCam.SCI_NODE_VAL_FLOAT();
            _currentDev.GetFloatValueEx(SciCam.SciCamDeviceXmlType.SciCam_DeviceXml_Camera, "ExposureTime", ref pVal);
            return pVal.dVal;
        }

        public static void SetCameraExposureAutoContinus()
        {
            _currentDev.SetEnumValueByStringEx(SciCam.SciCamDeviceXmlType.SciCam_DeviceXml_Camera, "ExposureAuto", "Continuous");
        }

        public static void SetCameraDeviceWaferParams()
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "Assets\\config\\OPT-CC1-M050-GG3-14(D24B110358).camcfg");
            _currentDev.FeatureLoad(configPath);
        }

        public static void SetCameraDeviceSharpenParams()
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "Assets\\config\\OPT-CC1-M050-GG3-14(D24B110358)Sharpen.camcfg");
            _currentDev.FeatureLoad(configPath);
        }

        public static void SetCameraDeviceVCaoParams()
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "Assets\\config\\OPT-CC1-M050-GG3-14(D24B110358)VCao.camcfg");
            _currentDev.FeatureLoad(configPath);
        }

        public static void SetCameraDeviceAutoParams()
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "Assets\\config\\OPT-CC1-M050-GG3-14(D24B110358)Auto.camcfg");
            _currentDev.FeatureLoad(configPath);
        }

        private static void DiscoveryDevices()
        {
            uint nReVal = 0;
            try
            {
                nReVal = SciCam.DiscoveryDevices(ref _stDevList, (uint)(SciCamTLType.SciCam_TLType_Gige | SciCamTLType.SciCam_TLType_Usb3));
            }
            catch (DllNotFoundException ex)
            {
                Tools.LogError("DLL not found: " + ex.Message);
                return;
            }
            if (nReVal != SCI_CAMERA_OK)
            {
                Tools.LogError("Discovery devices failed!");
                return;
            }
            if (_stDevList.count == 0)
            {
                Tools.LogError("Discovery devices Success, but found 0 device.");
                return;
            }
        }

        public static void CloseDevice()
        {
            if (!_bDeviceOpened)
            {
                return;
            }
            {
                StopGrabbing();
            }

            uint nReVal = _currentDev.CloseDevice();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Close device failed";
                return;
            }

            nReVal = _currentDev.DeleteDevice();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Delete device failed";
            }

            _bDeviceOpened = false;
        }

        public static void StartGrabbing()
        {
            uint nReVal = _currentDev.StartGrabbing();
            if (nReVal != SCI_CAMERA_OK)
            {
                Tools.LogError("Start grabbing failed");
                return;
            }
        }

        public static void StopGrabbing()
        {
            uint nReVal = _currentDev.StopGrabbing();
            if (nReVal != SCI_CAMERA_OK)
            {
                Tools.LogError("Stop grabbing failed");
                return;
            }
        }
    }
}