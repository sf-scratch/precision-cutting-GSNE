using log4net.Repository.Hierarchy;
using NPOI.SS.Formula.Functions;
using OpenCvSharp.XFeatures2D;
using Prism.Common;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Entities;
using 精密切割系统.Helpers;
using 精密切割系统.Model.common;
using 精密切割系统.Model.cut;
using 精密切割系统.Model.sqlite;
using 精密切割系统.View.common;
using 精密切割系统.View.Pages.Auto;
using static NPOI.HSSF.Util.HSSFColor;

namespace 精密切割系统.ViewModel
{
    public class AutoCutConfigViewModel : CustomBindableBase
    {
        public ObservableCollection<string> PrecutProcessNoList { get; set; }

        private SharpenParamsModel _sharpenParams;
        /// <summary>
        /// 磨刀参数
        /// </summary>
        public SharpenParamsModel SharpenParams
        {
            get { return _sharpenParams; }
            set { _sharpenParams = value; RaisePropertyChanged(); }
        }

        private CutParamsModel _cutParams;
        /// <summary>
        /// 切割参数
        /// </summary>
        public CutParamsModel CutParams
        {
            get { return _cutParams; }
            set { _cutParams = value; RaisePropertyChanged(); }
        }

        private ParamsConfigModel paramsConfig;
        public ParamsConfigModel ParamsConfig
        {
            get { return paramsConfig; }
            set { SetProperty(ref paramsConfig, value); }
        }

        private long _selectedConfigId;
        public long SelectedConfigId
        {
            get { return _selectedConfigId; }
            set { _selectedConfigId = value; RaisePropertyChanged(); }
        }

        public AutoCutConfigViewModel()
        {
            PrecutProcessNoList = new ObservableCollection<string>();
        }

        private void InitRightButton()
        {
            RightButtonCollection.Clear();
            RightButtonCollection.Add(ButtonParams.GreenRightButton("保存", "/Assets/icon/tab_1/01/tab_12.png", Save));
            RightButtonCollection.Add(ButtonParams.YelloRightButton("返回", "/Assets/icon/right/back.png", Back));
        }

        private async void Save()
        {
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
            try
            {
                if (SelectedConfigId == -1)
                {
                    await connection.RunInTransactionAsync(tx =>
                    {
                        ParamsConfigEntity paramsConfig = MapperConfig.Mapper.Map<ParamsConfigEntity>(ParamsConfig);
                        paramsConfig.Id = SelectedConfigId;
                        tx.Insert(paramsConfig);
                        paramsConfig.SharpenParamsId = paramsConfig.Id;
                        paramsConfig.CutParamsId = paramsConfig.Id;
                        tx.Update(paramsConfig);
                        SelectedConfigId = paramsConfig.Id;
                        SharpenParams.Id = paramsConfig.Id;
                        CutParams.Id = paramsConfig.Id;
                        tx.Insert(MapperConfig.Mapper.Map<SharpenParamsEntity>(SharpenParams));
                        tx.Insert(MapperConfig.Mapper.Map<CutParamsEntity>(CutParams));
                    });
                }
                else
                {
                    await connection.RunInTransactionAsync(tx =>
                    {
                        tx.Update(MapperConfig.Mapper.Map<ParamsConfigEntity>(ParamsConfig));
                        tx.Update(MapperConfig.Mapper.Map<SharpenParamsEntity>(SharpenParams));
                        tx.Update(MapperConfig.Mapper.Map<CutParamsEntity>(CutParams));
                    });
                }
                await SelectedConfigEntity.SetCurrentSelectedConfigIdAsync(connection, SelectedConfigId);
                MaterialSnackUtils.MaterialSnack("自动切割参数保存成功！", MaterialSnackUtils.SnackType.SUCCESS);
                Back();
            }
            catch (SQLiteException)
            {
                MaterialSnackUtils.MaterialSnack("自动切割参数保存失败！", MaterialSnackUtils.SnackType.WARNING);
            }
        }

        private void Back()
        {
            ContainerLocator.Container.Resolve<IRegionManager>().RequestNavigate(RegionName.MainRegion, nameof(AutoCutSelectConfig));
        }

        private void InitBottomButton()
        {
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            SQLiteAsyncConnection connection = SqlHelper.SQLiteAsync;
            PrecutProcessNoList.Clear();
            PrecutProcessNoList.AddRange((await connection.Table<PreCutModel>().ToListAsync()).Select(p => p.PrecutNo));
            if (navigationContext.Parameters.TryGetValue(nameof(SelectedConfigId), out long configId))
            {
                SelectedConfigId = configId;
                ParamsConfigEntity paramsConfig = await connection.Table<ParamsConfigEntity>().Where(p => p.Id == configId).FirstOrDefaultAsync();
                ParamsConfig = MapperConfig.Mapper.Map<ParamsConfigModel>(paramsConfig);
                SharpenParamsEntity? sharpenParamsEnt = await connection.Table<SharpenParamsEntity>().Where(p => p.Id == paramsConfig.SharpenParamsId).FirstOrDefaultAsync();
                SharpenParams = MapperConfig.Mapper.Map<SharpenParamsModel>(sharpenParamsEnt) ?? new SharpenParamsModel();
                CutParamsEntity? cutParamsEnt = await connection.Table<CutParamsEntity>().Where(p => p.Id == paramsConfig.CutParamsId).FirstOrDefaultAsync();
                CutParams = MapperConfig.Mapper.Map<CutParamsModel>(cutParamsEnt) ?? new CutParamsModel();
            }
            else
            {
                SelectedConfigId = -1;
                ParamsConfig = new ParamsConfigModel();
                SharpenParams = new SharpenParamsModel() { Id = SelectedConfigId };
                CutParams = new CutParamsModel() { Id = SelectedConfigId };
            }
            InitRightButton();
            InitBottomButton();
        }
    }
}
