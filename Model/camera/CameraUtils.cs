using CSharp_OPTControllerAPI;
using SciCamera.Net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using GdiPlus = System.Drawing.Imaging;
using Emgu.CV;
using System.Windows.Threading;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using Emgu.CV.Reg;
using System.Diagnostics;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Utils;

namespace 精密切割系统.Driver
{
    internal class CameraUtils
    {
        static SciCam.SCI_DEVICE_INFO_LIST m_stDevList = new SciCam.SCI_DEVICE_INFO_LIST();    //设备列表
        public static SciCam m_currentDev = new SciCam();     //当前设备
        public static List<SciCam> sciCams = new List<SciCam>(); // 所有相机设备
        static bool m_bDeviceReady = false;         //是否存在相机
        public static bool m_bDeviceOpened = false;        //相机是否打开
        static bool m_bStartGrabbing = false;       //开始采集状态
        static System.Windows.Controls.Image cameraPictureBox; // 相机显示的图片对象
        public static string errorMessage = ""; // 异常信息

        static Thread m_hGrabThread = null;            //取流线程句柄
        static bool m_bThreadState = false;            //线程状态
        static string lightIp = "192.168.10.150"; // 光源IP
        public static bool l_lightConnectStatus = false; // 光源连接状态
        public static string l_errorMessage = ""; // 光源异常信息
        static LightControllerAPI LightController = new LightControllerAPI();
        public static Mat? curMat;
        public static BitmapSource? imageUI;
        private static readonly object bitmapLock = new object();
        static WriteableBitmap bitmap;
        // 当前相机索引 0 高倍 1 低倍
        public static int currentCameraIndex = 0;

        enum ImageSaveType
        {
            Type_NONE = 0,
            Type_BMP,
            Type_JPG,
            Type_TIFF,
            Type_PNG,
        };
        static ImageSaveType m_imageSaveType = ImageSaveType.Type_PNG;          //保存图像类型
        static string imageSavePath; // 保存的路径，包含文件名
        static bool matImgFlag = false;
        static Mutex mutex = new Mutex();


        public static void connectDevice()
        {
            // 查找相机
            DiscoveryDevices();
            // 判断相机是否打开 
            if (!m_bDeviceOpened)
            {
                // 打开相机
                OpenDevice();
            }
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
            /*if (!GlobalParams.onlineFlag)
            {
                return;
            }*/
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

        /// <summary>
        /// 切换相机
        /// </summary>
        public static void ChangeCamera()
        {
            int tempIndex = currentCameraIndex == 0 ? 1 : 0;
            m_currentDev = sciCams[tempIndex];
            currentCameraIndex = tempIndex;
        }

        private static void OpenDevice()
        {
            if (m_stDevList.count == 0)
            {
                errorMessage = "Please select a device first!";
                return;
            }
            for (int i = 0; i < m_stDevList.pDevInfo.Length; i++)
            {
                SciCam.SCI_DEVICE_INFO item = m_stDevList.pDevInfo[i];
                SciCam.SCI_DEVICE_GIGE_INFO gigeDevInfo = (SciCam.SCI_DEVICE_GIGE_INFO)SciCam.ByteToStruct(item.info.gigeInfo, typeof(SciCam.SCI_DEVICE_GIGE_INFO));
                string devIP = i4tos(gigeDevInfo.ip);
                if (devIP.StartsWith("192.168.10"))
                {
                    Debug.WriteLine(devIP);
                    SciCam tempSciCam = new SciCam();
                    uint nReVal = tempSciCam.CreateDevice(ref item);
                    if (nReVal != SciCam.SCI_CAMERA_OK)
                    {
                        errorMessage = "Create device failed";
                        return;
                    }

                    nReVal = tempSciCam.OpenDevice();
                    if (nReVal != SciCam.SCI_CAMERA_OK)
                    {
                        errorMessage = "Open device failed";
                        return;
                    }

                    nReVal = tempSciCam.SetEnumValueByString("TriggerMode", "Off");
                    if (nReVal != SciCam.SCI_CAMERA_OK)
                    {
                        errorMessage = "Set trigger mode off failed";
                    }
                    nReVal = tempSciCam.SetEnumValueByString("PixelFormat", "RGB8");
                    sciCams.Add(tempSciCam);
                }
            }
            // 当前设备默认是第一个
            m_currentDev = sciCams[currentCameraIndex];
            m_bDeviceOpened = true;
        }

        private static void SetDeviceParams()
        {

        }



        private static void OpenDevice1()
        {
            if (m_stDevList.count == 0)
            {
                errorMessage = "Please select a device first!";
                return;
            }
            // 过滤出ip为169.254.119.111的设备
            SciCam.SCI_DEVICE_INFO device = m_stDevList.pDevInfo.FirstOrDefault(dev =>
            {
                SciCam.SCI_DEVICE_GIGE_INFO gigeDevInfo = (SciCam.SCI_DEVICE_GIGE_INFO)SciCam.ByteToStruct(dev.info.gigeInfo, typeof(SciCam.SCI_DEVICE_GIGE_INFO));
                string devIP = i4tos(gigeDevInfo.ip);
                return devIP == "192.168.10.180";
            });
            uint nReVal =m_currentDev.CreateDevice(ref device);
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Create device failed";
                return;
            }

            nReVal = m_currentDev.OpenDevice();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Open device failed";
                return;
            }

            nReVal = m_currentDev.SetEnumValueByString("TriggerMode", "Off");
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Set trigger mode off failed";
            }
            nReVal = m_currentDev.SetEnumValueByString("PixelFormat", "RGB8");

            m_bDeviceOpened = true;
        }
        private static void GetPixelFormat()
        {

            SciCam.SCI_NODE_VAL_ENUM eNodeVal = new SciCam.SCI_NODE_VAL_ENUM();
            uint nReVal = m_currentDev.GetEnumValue("PixelFormat", ref eNodeVal);
            if (nReVal == SciCam.SCI_CAMERA_OK)
            {
                int currentIndex = 0;
                for (int i = 0; i < eNodeVal.itemCount; i++)
                {
                    string itemStr = eNodeVal.items[i].desc;
                    if (eNodeVal.nVal == eNodeVal.items[i].val)
                    {
                        currentIndex = i;
                    }
                }
            }
        }
        private static void DiscoveryDevices()
        {
            GC.Collect();
            uint nReVal = 0;
            try
            {
                nReVal = SciCam.DiscoveryDevices(ref m_stDevList, (uint)(SciCam.SciCamTLType.SciCam_TLType_Gige | SciCam.SciCamTLType.SciCam_TLType_Usb3));
            }
            catch (DllNotFoundException ex)
            {
                errorMessage = "DLL not found: " + ex.Message;
                return;
            }
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Discovery devices failed!";
                m_bDeviceReady = false;
                return;
            }
            if (m_stDevList.count == 0)
            {
                errorMessage = "Discovery devices Success, but found 0 device.";
                m_bDeviceReady = false;
                return;
            }

            for (int i = 0; i < m_stDevList.count; i++)
            {
                SciCam.SCI_DEVICE_INFO device = m_stDevList.pDevInfo[i];
                SciCam.SciCamTLType devTlType = device.tlType;

                if (devTlType == SciCam.SciCamTLType.SciCam_TLType_Gige)
                {
                    SciCam.SCI_DEVICE_GIGE_INFO gigeDevInfo = (SciCam.SCI_DEVICE_GIGE_INFO)SciCam.ByteToStruct(device.info.gigeInfo, typeof(SciCam.SCI_DEVICE_GIGE_INFO));
                    string devModelName = gigeDevInfo.modelName;
                    string devSerialNumber = gigeDevInfo.serialNumber;
                    string devIP = i4tos(gigeDevInfo.ip);

                    string itemName = string.Format("[{0}] GigE: {1}({2})----[{3}]", i, devModelName, devSerialNumber, devIP);
                }
                else if (devTlType == SciCam.SciCamTLType.SciCam_TLType_Usb3)
                {
                    SciCam.SCI_DEVICE_USB3_INFO usb3Info = (SciCam.SCI_DEVICE_USB3_INFO)SciCam.ByteToStruct(device.info.usb3Info, typeof(SciCam.SCI_DEVICE_USB3_INFO));
                    string devModelName = usb3Info.modelName;
                    string devSerialNumber = usb3Info.serialNumber;

                    string itemName = string.Format("[{0}] U3V: {1}({2})", i, devModelName, devSerialNumber);
                }
            }

        }
        public static void CloseDevice()
        {
            if (!m_bDeviceOpened)
            {
                return;
            }
            if (m_bStartGrabbing)
            {
                StopGrabbing();
            }

            uint nReVal = m_currentDev.CloseDevice();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Close device failed";
                return;
            }

            nReVal = m_currentDev.DeleteDevice();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Delete device failed";

            }

            m_bDeviceOpened = false;
            m_bStartGrabbing = false;
        }


        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

       
        private static void StopGrabbing()
        {
            m_bThreadState = false;
            m_hGrabThread.Join();

            uint nReVal = m_currentDev.StopGrabbing();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Stop grabbing failed";
                return;
            }

            m_bStartGrabbing = false;
        }
        private static string i4tos(uint ip)
        {
            IPAddress iPAddress = new IPAddress(ip);
            return iPAddress.ToString();
        }

        private static void RefreshTriggerModeStatus()
        {
            SciCam.SCI_NODE_VAL_ENUM eNodeVal = new SciCam.SCI_NODE_VAL_ENUM();
            uint nReVal = m_currentDev.GetEnumValue("TriggerMode", ref eNodeVal);
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Get TriggerMode failed";
            }

            string triggerMode = "Off";
            for (int i = 0; i < eNodeVal.itemCount; i++)
            {
                if (eNodeVal.nVal == eNodeVal.items[i].val)
                {
                    triggerMode = eNodeVal.items[i].desc;
                    break;
                }
            }
            if (triggerMode == "Off")
            {
                /*radioButton_continusMode.Checked = true;
                radioButton_triggerMode.Checked = false;
                checkBox_softwareTrigger.Enabled = false;*/
            }
            else
            {
                /*radioButton_continusMode.Checked = false;
                radioButton_triggerMode.Checked = true;
                checkBox_softwareTrigger.Enabled = true;*/
            }
        }

        private static uint SetPixelFormat()
        {
            uint nReVal = SciCam.SCI_CAMERA_OK;
            int nIndex = 0;
            if (nIndex != -1)
            {
                // string str = comboBox_pixelFormat.SelectedItem.ToString();
                string str = "";
                nReVal = m_currentDev.SetEnumValueByString("PixelFormat", str);
            }

            return nReVal;
        }

        private static uint GetExposureTime()
        {
            string[] nodeName = new string[]
            {
                "ExposureTime",
                "ExposureTimeAbs",
                "ExposureTimeRaw"
            };

            uint nReVal = SciCam.SCI_CAMERA_OK;
            for (int i = 0; i < nodeName.Count(); i++)
            {
                SciCam.SCI_NODE_VAL_INT iNodeVal = new SciCam.SCI_NODE_VAL_INT();
                nReVal = m_currentDev.GetIntValue(nodeName[i], ref iNodeVal);
                if (nReVal != SciCam.SCI_CAMERA_OK)
                {
                    SciCam.SCI_NODE_VAL_FLOAT fNodeVal = new SciCam.SCI_NODE_VAL_FLOAT();
                    nReVal = m_currentDev.GetFloatValue(nodeName[i], ref fNodeVal);
                    if (nReVal == SciCam.SCI_CAMERA_OK)
                    {
                        // textBox_exposure.Text = fNodeVal.dVal.ToString();
                        break;
                    }
                }
                else
                {
                    // textBox_exposure.Text = iNodeVal.nVal.ToString();
                    break;
                }
            }

            return nReVal;
        }

        private static uint SetExposureTime()
        {
            string[] nodeName = new string[]
            {
                "ExposureTime",
                "ExposureTimeAbs",
                "ExposureTimeRaw"
            };

            uint nReVal = SciCam.SCI_CAMERA_OK;

            int iExposure = 0;
            // bool success = int.TryParse(textBox_exposure.Text, out iExposure);
            bool success = int.TryParse("", out iExposure);
            if (success)
            {
                for (int i = 0; i < nodeName.Count(); i++)
                {
                    nReVal = m_currentDev.SetIntValue(nodeName[i], iExposure);
                    if (nReVal != SciCam.SCI_CAMERA_OK)
                    {
                        double dExposure = 0;
                        // success = double.TryParse(textBox_exposure.Text, out dExposure);
                        success = double.TryParse("", out dExposure);
                        if (success)
                        {
                            nReVal = m_currentDev.SetFloatValue(nodeName[i], dExposure);
                            if (nReVal == SciCam.SCI_CAMERA_OK)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return nReVal;
        }

        private static uint GetGain()
        {
            string[] nodeName = new string[]
            {
                "Gain",
                "GainRaw"
            };

            uint nReVal = SciCam.SCI_CAMERA_OK;
            for (int i = 0; i < nodeName.Count(); i++)
            {
                SciCam.SCI_NODE_VAL_INT iNodeVal = new SciCam.SCI_NODE_VAL_INT();
                nReVal = m_currentDev.GetIntValue(nodeName[i], ref iNodeVal);
                if (nReVal != SciCam.SCI_CAMERA_OK)
                {
                    SciCam.SCI_NODE_VAL_FLOAT fNodeVal = new SciCam.SCI_NODE_VAL_FLOAT();
                    nReVal = m_currentDev.GetFloatValue(nodeName[i], ref fNodeVal);
                    if (nReVal == SciCam.SCI_CAMERA_OK)
                    {
                        // textBox_gain.Text = fNodeVal.dVal.ToString();
                        break;
                    }
                }
                else
                {
                    // textBox_gain.Text = iNodeVal.nVal.ToString();
                    break;
                }
            }

            return nReVal;
        }

        private static uint SetGain()
        {
            string[] nodeName = new string[]
            {
                "Gain",
                "GainRaw"
            };

            uint nReVal = SciCam.SCI_CAMERA_OK;

            int iGain = 0;
            // bool success = int.TryParse(textBox_gain.Text, out iGain);
            bool success = int.TryParse("", out iGain);
            if (success)
            {
                for (int i = 0; i < nodeName.Count(); i++)
                {
                    nReVal = m_currentDev.SetIntValue(nodeName[i], iGain);
                    if (nReVal != SciCam.SCI_CAMERA_OK)
                    {
                        double dGain = 0;
                        // success = double.TryParse(textBox_gain.Text, out dGain);
                        success = double.TryParse("", out dGain);
                        if (success)
                        {
                            nReVal = m_currentDev.SetFloatValue(nodeName[i], dGain);
                            if (nReVal == SciCam.SCI_CAMERA_OK)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return nReVal;
        }

        private static uint GetFrameRate()
        {
            string[] nodeName = new string[]
            {
                "ResultingFrameRate",
                "ResultingFrameRateAbs",
                "AcquisitionActualFrameRate"
            };

            uint nReVal = SciCam.SCI_CAMERA_OK;

            for (int i = 0; i < nodeName.Count(); i++)
            {
                SciCam.SCI_NODE_VAL_FLOAT fNodeVal = new SciCam.SCI_NODE_VAL_FLOAT();
                nReVal = m_currentDev.GetFloatValue(nodeName[i], ref fNodeVal);
                if (nReVal == SciCam.SCI_CAMERA_OK)
                {
                    // textBox_frameRate.Text = fNodeVal.dVal.ToString();
                    break;
                }
            }

            return nReVal;
        }

        private uint SetFrameRate()
        {
            string[] nodeName = new string[]
            {
                "ResultingFrameRate",
                "ResultingFrameRateAbs",
                "AcquisitionActualFrameRate"
            };

            uint nReVal = SciCam.SCI_CAMERA_OK;
            //nReVal = SciCam_SetBoolValue(m_currentDevHandle, "AcquisitionFrameRateEnable", true);
            nReVal = m_currentDev.SetBoolValue("AcquisitionFrameRateEnable", true);
            if (nReVal == SciCam.SCI_CAMERA_OK)
            {
                for (int i = 0; i < nodeName.Count(); i++)
                {
                    double dFrameRate = 0;
                    // bool success = double.TryParse(textBox_frameRate.Text, out dFrameRate);
                    bool success = double.TryParse("", out dFrameRate);
                    if (success)
                    {
                        nReVal = m_currentDev.SetFloatValue(nodeName[i], dFrameRate);
                        if (nReVal == SciCam.SCI_CAMERA_OK)
                        {
                            break;
                        }
                    }

                }
            }


            return nReVal;
        }

        private static void GetParameter()
        {
            uint nReVal = GetExposureTime();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Get ExposureTime failed";
            }

            nReVal = GetGain();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Get Gain failed";
            }

            nReVal = GetFrameRate();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "FrameRate failed";
            }
        }

        private void SetParameter()
        {
            uint nReVal = SetExposureTime();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Set ExposureTime failed";
            }

            nReVal = SetGain();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                errorMessage = "Set Gain failed";
            }

        }
    }
}
