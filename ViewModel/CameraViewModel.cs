using SciCamera.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using 精密切割系统.Driver;

namespace 精密切割系统.ViewModel
{
    public class CameraViewModel : INotifyPropertyChanged
    {
        public CameraViewModel()
        {
            ConnectCamera();
        }

        private WriteableBitmap bs = new WriteableBitmap(100, 100, 96, 96, PixelFormats.Gray8, null);
        public WriteableBitmap ImageSource
        {
            get => bs;
            set
            {
                bs = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        SciCam.SCI_DEVICE_INFO_LIST m_stDevList = new SciCam.SCI_DEVICE_INFO_LIST();
        SciCam m_currentDev = new SciCam();

        static WriteableBitmap bitmap;

        public static bool m_bDeviceReady = false;         //是否存在相机
        public static bool m_bDeviceOpened = false;        //相机是否打开
        public static bool m_bStartGrabbing = false;       //开始采集状态

        static Thread m_hGrabThread = null;            //取流线程句柄
        static bool m_bThreadState = false;			//线程状态  

        private readonly object bitmapLock = new object();

        public void ConnectCamera()
        {
            Discovery_Camera();
            OpenDevice();
            //Thread.Sleep(1000);
            //StartGrabbing();
        }

        private void Discovery_Camera()
        {
            System.GC.Collect();
            uint nReVal = SciCam.DiscoveryDevices(ref m_stDevList, (uint)(SciCam.SciCamTLType.SciCam_TLType_Gige | SciCam.SciCamTLType.SciCam_TLType_Usb3));
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                MessageBox.Show("搜索相机失败");
                return;
            }
            if (m_stDevList.count == 0)
            {
                MessageBox.Show("搜索相机成功，但是相机数为 0");
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
                    Dictionary<int, string> Camera_List = new Dictionary<int, string>();
                    string itemName = string.Format("[{0}] GigE: {1}({2})----[{3}]", i, devModelName, devSerialNumber, devIP);
                    Camera_List.Add(i, itemName);
                }
            }
        }

        private void OpenDevice()
        {

            uint nReVal = m_currentDev.CreateDevice(ref m_stDevList.pDevInfo[0]);
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {

                return;
            }

            nReVal = m_currentDev.OpenDevice();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {

                return;
            }
            else
            {
                // MessageBox.Show("打开成功！");
            }

        }
        public void StartGrabbing()
        {
            m_bThreadState = true;
            m_hGrabThread = new Thread(GrabThreadProcess);
            m_hGrabThread.Start();

            uint nReVal = m_currentDev.StartGrabbing();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                m_bThreadState = false;
                return;
            }

            m_bStartGrabbing = true;
        }

        public void StopGrabbing()
        {
            m_bThreadState = false;
            m_hGrabThread.Join();

            uint nReVal = m_currentDev.StopGrabbing();
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                //errorMessage = "Stop grabbing failed";
                return;
            }

            m_bStartGrabbing = false;
        }


        private void GrabThreadProcess()
        {
            uint nReVal = SciCam.SCI_CAMERA_OK;
            IntPtr payload = IntPtr.Zero;

            while (m_bThreadState)
            {
                nReVal = m_currentDev.Grab(ref payload);
                /*if (nReVal == SciCam.SCI_CAMERA_OK)
                {
                    // 使用Dispatcher将更新操作封送到UI线程  
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DisplayImage(payload);
                    });
                }*/

                nReVal = m_currentDev.FreePayload(payload);
            }
        }
        private string i4tos(uint ip)
        {
            IPAddress iPAddress = new IPAddress(ip);
            return iPAddress.ToString();
        }
        public void DisplayImage(IntPtr payload)
        {
            /*WriteableBitmap localBitmap = null;
            int result = GetConvertedInfo(payload, out localBitmap);
            if (result == 0 && localBitmap != null)
            {
                lock (bitmapLock)
                {
                    // 更新bitmap变量，确保线程安全  
                    bitmap = localBitmap;
                }
                // 使用Dispatcher将UI更新操作封送到UI线程
                if (cameraImage != null) // 确保Image_Control已初始化  
                {
                    cameraImage.Source = bitmap;
                }
            }
            else
            {
                // Handle error  
            }*/
        }
        private int GetConvertedInfo(IntPtr payload, out WriteableBitmap bitmap)
        {
            bitmap = null;
            if (payload == IntPtr.Zero)
            {
                return -1;
            }

            SciCam.SCI_CAM_PAYLOAD_ATTRIBUTE payloadAttribute = new SciCam.SCI_CAM_PAYLOAD_ATTRIBUTE();
            uint nReVal = SciCam.PayloadGetAttribute(payload, ref payloadAttribute);
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                return -1;
            }

            bool imgIsComplete = payloadAttribute.isComplete;
            SciCam.SciCamPayloadMode payloadMode = payloadAttribute.payloadMode;
            SciCam.SciCamPixelType imgPixelType = payloadAttribute.imgAttr.pixelType;
            ulong imgWidth = payloadAttribute.imgAttr.width;
            ulong imgHeight = payloadAttribute.imgAttr.height;
            ulong framID = payloadAttribute.frameID;

            if (!imgIsComplete || payloadMode != SciCam.SciCamPayloadMode.SciCam_PayloadMode_2D)
            {
                return -1;
            }

            IntPtr imgData = IntPtr.Zero;
            nReVal = SciCam.PayloadGetImage(payload, ref imgData);
            if (nReVal != SciCam.SCI_CAMERA_OK)
            {
                return -1;
            }

            long destImgSize = 0;

            if (imgPixelType == SciCam.SciCamPixelType.Mono1p ||
                imgPixelType == SciCam.SciCamPixelType.Mono2p ||
                imgPixelType == SciCam.SciCamPixelType.Mono4p ||
                imgPixelType == SciCam.SciCamPixelType.Mono8s ||
                imgPixelType == SciCam.SciCamPixelType.Mono8 ||
                imgPixelType == SciCam.SciCamPixelType.Mono10 ||
                imgPixelType == SciCam.SciCamPixelType.Mono10p ||
                imgPixelType == SciCam.SciCamPixelType.Mono12 ||
                imgPixelType == SciCam.SciCamPixelType.Mono12p ||
                imgPixelType == SciCam.SciCamPixelType.Mono14 ||
                imgPixelType == SciCam.SciCamPixelType.Mono16 ||
                imgPixelType == SciCam.SciCamPixelType.Mono10Packed ||
                imgPixelType == SciCam.SciCamPixelType.Mono12Packed ||
                imgPixelType == SciCam.SciCamPixelType.Mono14p)
            {
                nReVal = SciCam.PayloadConvertImage(ref payloadAttribute.imgAttr, imgData, SciCam.SciCamPixelType.Mono8, IntPtr.Zero, ref destImgSize, true);
                if (nReVal == SciCam.SCI_CAMERA_OK)
                {
                    IntPtr destImg = Marshal.AllocHGlobal((int)destImgSize);
                    try
                    {
                        nReVal = SciCam.PayloadConvertImage(ref payloadAttribute.imgAttr, imgData, SciCam.SciCamPixelType.Mono8, destImg, ref destImgSize, true);
                        if (nReVal == SciCam.SCI_CAMERA_OK)
                        {
                            byte[] bBitmap = new byte[destImgSize];
                            Marshal.Copy(destImg, bBitmap, 0, (int)destImgSize);

                            int stride = (int)imgWidth; // Assuming 1 byte per pixel  
                            //bitmap = new WriteableBitmap((int)imgWidth, (int)imgHeight, 96, 96, PixelFormats.Gray8, null);
                            //bitmap.WritePixels(new Int32Rect(0, 0, (int)imgWidth, (int)imgHeight), bBitmap, stride, 0);
                            //bs.WritePixels(new Int32Rect(0, 0, (int)imgWidth, (int)imgHeight), bBitmap, stride, 0);

                            // 第一种：每次重复赋值触发PropertyChanged
                            /*WriteableBitmap TmpImage = new WriteableBitmap((int)imgWidth, (int)imgHeight, 96, 96, PixelFormats.Gray8, null);
                            TmpImage.WritePixels(new Int32Rect(0, 0, (int)imgWidth, (int)imgHeight), bBitmap, stride, 0);
                            ImageSource = TmpImage;*/

                            // 第二种：每次主动触发PropertyChanged
                            ImageSource.WritePixels(new Int32Rect(0, 0, (int)imgWidth, (int)imgHeight), bBitmap, stride, 0);
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageSource)));
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
            return 0;
        }
    }
}
