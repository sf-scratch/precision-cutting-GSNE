using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using 精密切割系统.database.db.modle;
using 精密切割系统.Driver;
using 精密切割系统.DTOs;
using 精密切割系统.FrmWindow.common;
using 精密切割系统.Helpers;
using 精密切割系统.HttpClients;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Utils;

namespace 精密切割系统.ViewModel
{
    public class BladeReplacementConfigurationViewModel : INotifyPropertyChanged
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public event PropertyChangedEventHandler? PropertyChanged;
        public RelayCommand AutoRunCommand { get; set; }
        public RelayCommand<string> InitCommand { get; set; }
        // 控制右侧按钮
        private ObservableCollection<RightButtonParams> _rightButtonParams;

        #region 轮毂信息
        private string _lunguId;
        public string LunguId
        {
            get { return _lunguId; }
            set { _lunguId = value; OnPropertyChanged(); }
        }

        private float _abAverageThickness;
        public float ABAverageThickness
        {
            get { return _abAverageThickness; }
            set { _abAverageThickness = value; OnPropertyChanged(); }
        }

        private float _longestBlade;
        public float LongestBlade
        {
            get { return _longestBlade; }
            set { _longestBlade = value; OnPropertyChanged(); }
        }

        private string _bladeType;
        /// <summary>
        /// 刀片类型
        /// </summary>
        public string BladeType
        {
            get { return _bladeType; }
            set { _bladeType = value; OnPropertyChanged(); }
        }

        private string _orderType;
        /// <summary>
        /// 订单类型
        /// </summary>
        public string OrderType
        {
            get { return _orderType; }
            set { _orderType = value; OnPropertyChanged(); }
        }

        private string _bladeEdgeType;
        /// <summary>
        /// 刀刃规格
        /// </summary>
        public string BladeEdgeType
        {
            get { return _bladeEdgeType; }
            set { _bladeEdgeType = value; OnPropertyChanged(); }
        }

        private string _bladeOuterDiameter;
        /// <summary>
        /// 刀片外径
        /// </summary>
        public string BladeOuterDiameter
        {
            get { return _bladeOuterDiameter; }
            set { _bladeOuterDiameter = value; OnPropertyChanged(); }
        }
        #endregion

        private SharpenParamsModel _sharpenParams;
        /// <summary>
        /// 磨刀参数
        /// </summary>
        public SharpenParamsModel SharpenParams
        {
            get { return _sharpenParams; }
            set { _sharpenParams = value; OnPropertyChanged(); }
        }

        private CutParamsModel _cutParams;
        /// <summary>
        /// 切割参数
        /// </summary>
        public CutParamsModel CutParams
        {
            get { return _cutParams; }
            set { _cutParams = value; OnPropertyChanged(); }
        }

        public BladeReplacementConfigurationViewModel()
        {
            LunguId = CameraUtils.GetLunguId();
            _rightButtonParams = RightPageViewModel.RightButtonParams;
            AutoRunCommand = new RelayCommand(AutoRun);
            InitCommand = new RelayCommand<string>(Init);
            InitRightButtonOnlyBack();
        }

        private void InitRightButtonOnlyBack()
        {
            _rightButtonParams.Clear();
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private void InitRightButton()
        {
            _rightButtonParams.Clear();
            _rightButtonParams.Add(RightButtonParams.GreenRightButton("自动执行", "/Assets/icon/right/enter.png", AutoRun));
            _rightButtonParams.Add(RightButtonParams.GreenRightButton("重置磨刀", "/Assets/icon/right/enter.png", SharpenService.Instance.Reset));
            _rightButtonParams.Add(RightButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private async void Init(string lunguId)
        {
            if (!_semaphore.Wait(0)) // 尝试获取锁（0 = 不等待）
            {
                return; // 如果锁已被占用，直接返回
            }
            try
            {
                InitRightButtonOnlyBack();
                //轮毂信息
                LunguSksjDTO? lunguSksj = await HttpUtils.GetLunguSksjAsync(lunguId);
                if (lunguSksj == null)
                {
                    Tools.LogError("轮毂信息获取失败！");
                    MaterialSnackUtils.MaterialSnack("轮毂信息获取失败！", MaterialSnackUtils.SnackType.WARNING);
                    LunguId = string.Empty;
                    return;
                }
                ABAverageThickness = lunguSksj.ABAverageThickness;
                LongestBlade = lunguSksj.LongestBlade;
                BladeType = lunguSksj.BladeType;
                OrderType = lunguSksj.OrderType;
                BladeEdgeType = lunguSksj.BladeEdgeType;
                BladeOuterDiameter = lunguSksj.BladeOuterDiameter;

                //磨刀参数
                int bmSharpParamId = 1;
                List<BmSharpenParameterModel> list = await SqlHelper.TableAsync<BmSharpenParameterModel>()
                                    .Where(t => t.Id == bmSharpParamId).ToListAsync();
                if (list.Count <= 0)
                {
                    Tools.LogError("磨刀参数获取错误！");
                    MaterialSnackUtils.MaterialSnack("磨刀参数获取错误！", MaterialSnackUtils.SnackType.WARNING);
                    return;
                }
                BmSharpenParameterModel sharpenParam = list[0];
                SharpenParams = new SharpenParamsModel
                {
                    RotateSpeed = sharpenParam.RotateSpeed.ToInt(),
                    CutThickness = sharpenParam.CutThickness,
                    CoJiaoHeight = sharpenParam.CoJiaoHeight,
                    CutHeight = AutoCutUtils.GetSharpenDeep(lunguSksj.BladeType),
                    CoOffsetX = sharpenParam.CoOffsetX,
                    CutSize = 0.3f,
                    CutNum = AutoCutUtils.GetNeedSharpenTimes(lunguSksj.LongestBlade / 1000, AutoCutUtils.GetBladeExposedMax(lunguSksj.ABAverageThickness), GlobalParams.SingleBladeWear),
                    CutSpeed1 = 0,
                    CutNum1 = 0,
                    CutSpeed2 = 0,
                    CutNum2 = 0
                };

                //切割参数
                long fileTableId = CurrentUtils.GetCurrentConfiguration().DeviceDataId;
                // 查询配置信息
                var listConf = await SqlHelper.TableAsync<FileTableItemModel>().Where(t => t.Id == fileTableId).ToListAsync();
                if (listConf.Count == 0)
                {
                    MaterialSnackUtils.MaterialSnack("切割参数获取错误！", MaterialSnackUtils.SnackType.WARNING);
                    return;
                }
                FileTableItemModel fileTable = listConf[0];
                // 查询通道信息
                List<FileTableItemChModel> chModels = await SqlHelper.TableAsync<FileTableItemChModel>()
                    .Where(t => t.ItemId == fileTable.Id).ToListAsync();
                if (chModels.Count == 0)
                {
                    MaterialSnackUtils.MaterialSnack("切割通道参数获取错误！", MaterialSnackUtils.SnackType.WARNING);
                    return;
                }
                FileTableItemChModel fileTableCh = chModels[0];
                CutParams = new CutParamsModel
                {
                    CutHeight = AutoCutUtils.GetCuttingZ(lunguSksj.BladeType),
                    TapeThickness = fileTable.TapeThickness,
                    SpindleRev = fileTable.SpindleRev,
                    PrecutProcessNo = fileTable.PrecutProcessNo,
                    MaxCutSpeed = "0",
                    CutNum = fileTableCh.CutLine.ToInt(),
                    WorkThickness = fileTable.WorkThickness,
                    DeviceDataNo = fileTable.DeviceDataNo,
                    OffsetX = fileTableCh.OffsetX.ToInt()
                };
                InitRightButton();
            }
            finally
            {
               _semaphore.Release(); // 释放锁
            }

        }

        private void AutoRun()
        {
            NavigateUtils.NavigateToPage("Pages/Auto/AutoCutRuning", new Tuple<SharpenParamsModel, CutParamsModel>(SharpenParams, CutParams));
        }

        private void Back()
        {
            NavigateUtils.NavigateToPage("MainMenu");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
