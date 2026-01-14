using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.database;
using Emgu.CV.Dnn;
using 精密切割系统.Model.sqlite;
using 精密切割系统.Helpers;


//配置数据全局初始化
namespace 精密切割系统.Utils
{
    internal class InitializeData
    {

        //初始化相关内容数据(整个系统参数初始化)
        public static void initSystemData()
        {
            _ = CurrentConfiguration();//当前默认配置
            _ = init30();
            _ = init313();
            _ = init314();
            _ = init315();
            _ = init316();
            _ = init318();
            //_ = init7442();不需要，会重复创建
            _ = init41();
            //_ = init440();            
            _ = init47();
            _ = init51();
            _ = init53();
            _= init532();
            _= init100();
            _ = init64();
            _ = init65();
            _ = init66();
            _ = init44010();
        }

        private static async Task CurrentConfiguration() {
            var list = await SqlHelper.TableAsync<CurrentConfigurationModel>()
                       .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                CurrentConfigurationModel temp = new CurrentConfigurationModel();
                temp.BladeHeightDataId = 1;
                temp.PrecutDataId = "1";
                await SqlHelper.AddAsync(temp);
            }
        }
        //3.0 目录配置
        private static async Task init30()
        {
            //---------------------目录数据-------------------
            var listRoot = await SqlHelper.TableAsync<FileTableModel>()
                    .Where(t => t.Name == "Root")
                    .Where(t => t.Level == 0)
                    .ToListAsync();
            //查询Root节点是否存在
            if (listRoot.Count() == 0)
            {
                FileTableModel model = new FileTableModel();
                model.Id = 1;
                model.Name = "Root";
                model.Level = 0;
                model.ParentId = 0;
                await SqlHelper.AddAsync(model);
            }
            var list01 = await SqlHelper.TableAsync<FileTableItemModel>()
                    .Where(t => t.Id == 1)
                    .ToListAsync();

            if (list01.Count() == 0)
            {
                FileTableItemModel model01 = new FileTableItemModel();
                model01.Id = 1;
                model01.DirectoryId = 1;
                model01.DeviceType = 1;
                model01.DeviceDataNo = "000";
                model01.DeviceDataId = "INCH-SAMPLE";
                String sql = "INSERT INTO file_table_item(id) VALUES(1);";
                await SqlHelper.ExecuteAsync(sql);
                await SqlHelper.UpdateAsync(model01);
                //初始化配置CH数据
                _ = initItemChData(model01);

            }

            var list02 = await SqlHelper.TableAsync<FileTableItemModel>()
                    .Where(t => t.Id == 2)
                    .ToListAsync();
            if (list02.Count() == 0)
            {
                FileTableItemModel model02 = new FileTableItemModel();
                model02.Id = 2;
                model02.DirectoryId = 1;
                model02.DeviceType = 1;
                model02.DeviceDataNo = "111";
                model02.DeviceDataId = "MM-SAMPLE";

                String sql = "INSERT INTO file_table_item(id) VALUES(2);";
                await SqlHelper.ExecuteAsync(sql);
                await SqlHelper.UpdateAsync(model02);
                //初始化配置CH数据
                _ = initItemChData(model02);
            }
        }

        //3.1.3 参数校对置
        private static async Task init313() {
            var list = await SqlHelper.TableAsync<CalibrationParameterModel>()
                        .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new CalibrationParameterModel());
            }
        }


        //3.1.4 方法参数
        private static async Task init314()
        {
            var list = await SqlHelper.TableAsync<FunctionSelectionModel>()
                        .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new FunctionSelectionModel());
            }
        }
        


        //3.1.5 选项设置参数
        private static async Task init315()
        {
            var list = await SqlHelper.TableAsync<OptionSettingModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new OptionSettingModel());
            }
        }

        //3.1.6 程序控制表
        private static async Task init316()
        {
            var list = await SqlHelper.TableAsync<ProcessControlTableModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                for (int i = 0; i < 15; i++) {
                    await SqlHelper.AddAsync(new ProcessControlTableModel());
                }
            }
        }

        //3.1.8 刀痕检查参数
        private static async Task init318()
        {
            var list = await SqlHelper.TableAsync<KerfCheckDataModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new KerfCheckDataModel());
            }
        }

        //4.1 刀片更换记录
        private static async Task init41()
        {
            var list = await SqlHelper.TableAsync<ReplaceBladeModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new ReplaceBladeModel());
            }
        }
        //刀片修正/磨刀（4.4/4.4.0）
        private static async Task init440()
        {
            var list01 = await SqlHelper.TableAsync<BladeSharpenModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list01.Count() == 0)
            {
                BladeSharpenModel model01 = new BladeSharpenModel();
                model01.Id = 1;
                model01.BladeLotID = "0";
                await SqlHelper.AddAsync(model01);
            }
            var list02 = await SqlHelper.TableAsync<BladeSharpenModel>()
                            .Where(t => t.Id == 2).ToListAsync();
            if (list02.Count() == 0)
            {
                BladeSharpenModel model02 = new BladeSharpenModel();
                model02.Id = 2;
                model02.BladeLotID = "1";
                await SqlHelper.AddAsync(model02);
            }
        }

        //预切割参数（5.1）
        private static async Task init51()
        {
            var list01 = await SqlHelper.TableAsync<PreCutModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list01.Count() == 0)
            {
                PreCutModel model01 = new PreCutModel();
                model01.Id = 1;
                model01.PrecutNo = "0";
                model01.PrecutID = "SAMPLE";
                String sql = "INSERT INTO pre_cut(id) VALUES(1);";
                await SqlHelper.ExecuteAsync(sql);
                await SqlHelper.UpdateAsync(model01);
            }
            var list02 = await SqlHelper.TableAsync<PreCutModel>()
                            .Where(t => t.Id == 2).ToListAsync();
            if (list02.Count() == 0)
            {
                PreCutModel model02 = new PreCutModel();
                model02.Id = 2;
                model02.PrecutNo = "1";
                model02.PrecutID = "SAMPLE";
                String sql = "INSERT INTO pre_cut(id) VALUES(2);";
                await SqlHelper.ExecuteAsync(sql);
                await SqlHelper.UpdateAsync(model02);
            }
        }

        //刀片测高参数（4.7）
        private static async Task init47()
        {
            var list = await SqlHelper.TableAsync<BladeHeightModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new BladeHeightModel());
            }
        }
        //功能参数维护（5.3）
        private static async Task init53()
        {
            var list = await SqlHelper.TableAsync<FunctionalParametersModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new FunctionalParametersModel());
            }
        }
        //操作参数维护（5.3.2）
        private static async Task init532()
        {
            var list = await SqlHelper.TableAsync<OperationParametersModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new OperationParametersModel());
            }
        }



        //7.4和7.4.2 用户参数
        private static async Task init7442()
        {
            var list = await SqlHelper.TableAsync<UserDefineDataModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new UserDefineDataModel());
            }
        }

        private static async Task init100()
        {
            var list = await SqlHelper.TableAsync<PositionCompensationModel>()
                            .ToListAsync();
            //数据不存在，则初始化数据
            if (list==null || list.Count() < 5)
            {
                await SqlHelper.DeleteAsync(new PositionCompensationModel());
                PositionCompensationModel modelX = new PositionCompensationModel();
                modelX.AxisType = "X轴";
                PositionCompensationModel modelY = new PositionCompensationModel();
                modelY.AxisType = "Y轴";
                PositionCompensationModel modelZ1 = new PositionCompensationModel();
                modelZ1.AxisType = "Z1轴";
                PositionCompensationModel modelZ2 = new PositionCompensationModel();
                modelZ2.AxisType = "Z2轴";
                PositionCompensationModel modelTheta = new PositionCompensationModel();
                modelTheta.AxisType = "Theta轴";
                await SqlHelper.AddAsync(modelX);
                await SqlHelper.AddAsync(modelY);
                await SqlHelper.AddAsync(modelZ1);
                await SqlHelper.AddAsync(modelZ2);
                await SqlHelper.AddAsync(modelTheta);
            }
        }



        //初始化配置CH数据
        private static async Task initItemChData(FileTableItemModel model)
        {
            FileTableItemChModel ch1 = new FileTableItemChModel();
            ch1.ItemId = model.Id;
            ch1.ChName = GlobalParams.CH1;
            await SqlHelper.AddAsync(ch1);
            FileTableItemChModel ch2 = new FileTableItemChModel();
            ch2.ItemId = model.Id;
            ch2.ChName = GlobalParams.CH2;
            await SqlHelper.AddAsync(ch2);
            if (model.DeviceType == 1)//第一种类型才有4个CH
            {
                FileTableItemChModel ch3 = new FileTableItemChModel();
                ch3.ItemId = model.Id;
                ch3.ChName = GlobalParams.CH3;
                await SqlHelper.AddAsync(ch3);
                FileTableItemChModel ch4 = new FileTableItemChModel();
                ch4.ItemId = model.Id;
                ch4.ChName = GlobalParams.CH4;
                await SqlHelper.AddAsync(ch4);
            }

        }

        //速度设置（6.4）
        private static async Task init64()
        {
            var list = await SqlHelper.TableAsync<SpeedSettingModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new SpeedSettingModel());
            }
        }
        
        //校准参数（6.5）
        private static async Task init65()
        {
            var list = await SqlHelper.TableAsync<PositionAlignmentModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new PositionAlignmentModel());
            }
        }
        //各模式初始位置（6.6）
        private static async Task init66()
        {
            var list = await SqlHelper.TableAsync<InitialPositionModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new InitialPositionModel());
            }
        }

        //4.4.0 磨刀程序-
        private static async Task init44010()
        {
            var list = await SqlHelper.TableAsync<BmSharpenParameterModel>()
                            .Where(t => t.Id == 1).ToListAsync();
            //数据不存在，则初始化数据
            if (list.Count() == 0)
            {
                await SqlHelper.AddAsync(new BmSharpenParameterModel() { BladeLotID = "0" });
            }
        }

    }
}
