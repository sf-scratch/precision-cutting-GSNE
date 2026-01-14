using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.database.db.modle;
using 精密切割系统.Entities;
using 精密切割系统.Model.sqlite;
using 精密切割系统.View.Pages.F4_BladeMaintenance;
using 精密切割系统.ViewModel;

namespace 精密切割系统.Helpers
{
    public static class SqlHelper
    {
        private static string connstr = Environment.CurrentDirectory + "\\qg_data.db"; //没有数据库会创建数据库
        public static SQLiteConnection db;
        public static SQLiteAsyncConnection dbAsync;
        public const long DefaultId = 1;

        static SqlHelper()
        {
            db = new SQLiteConnection(connstr, false);
            dbAsync = new SQLiteAsyncConnection(connstr, false);

            //创建数据库，新的数据库需要在这儿先创建
            db.CreateTable<TestModel>();
            try
            {
                db.CreateTable<CurrentConfigurationModel>();//默认配置集合
                db.CreateTable<FileTableModel>();//目录文件夹
                db.CreateTable<FileTableItemModel>();//配置文件
                db.CreateTable<FileTableItemChModel>();//目录库
                db.CreateTable<ReplaceBladeModel>();//刀片更换记录(4.1)
                db.CreateTable<BladeHeightModel>();// 刀片测高参数（4.7）
                db.CreateTable<PreCutModel>();// 预切割参数（5.1）
                db.CreateTable<FunctionalParametersModel>();// 功能参数维护（5.3）
                db.CreateTable<OperationParametersModel>();// 操作参数维护（5.3.2）
                db.CreateTable<CalibrationParameterModel>();//校准参数(3.1.3)
                db.CreateTable<OptionSettingModel>();//选项参数设置(3.1.5)
                db.CreateTable<FunctionSelectionModel>();//方法参数(3.1.4)
                db.CreateTable<ProcessControlTableModel>();//程序控制表(3.1.6)
                db.CreateTable<KerfCheckDataModel>();//刀痕检查参数(3.1.8)
                db.CreateTable<UserDefineDataModel>();//用户参数(7.4和7.4.2)
                db.CreateTable<ElectricalDischargeTruingModel>();//电火花修刀(8.1)
                db.CreateTable<PositionCompensationModel>();//轴运动位置补偿(10.0)
                db.CreateTable<SpeedSettingModel>();//速度设置(6.4)
                db.CreateTable<PositionAlignmentModel>();//校准参数(6.5)
                db.CreateTable<InitialPositionModel>();//各模式初始位置 (6.6)

                db.CreateTable<BladeSharpenModel>();//刀片修正/磨刀（4.4/4.4.0） 未用
                db.CreateTable<BmSharpenParameterModel>();//刀片修正/磨刀 (4.4.0) 新
                db.CreateTable<AutoAlignPositionParamsModel>(); // 自动切割校准
                db.CreateTable<AutoAlignPositionModel>(); // 自动切割校准
                db.CreateTable<BunkeringRecordModel>(); // 加油记录
                db.CreateTable<RunLogsModel>(); // 运行日志
                db.CreateTable<FlangeTrimmingModel>(); // 法兰修整
                db.CreateTable<ThetaCenterAlignModel>(); // theta轴中心位置校正
                db.CreateTable<SharpenParamsEntity>();
                db.CreateTable<CutParamsEntity>();
                db.CreateTable<SelectedConfigEntity>();
                db.CreateTable<KnifeWearEntity>();
                db.CreateTable<ParamsConfigEntity>();
                db.CreateTable<BMParameterMaintenanceEntity>();
                db.CreateTable<BaselineCalibrationEntity>();
                db.CreateTable<BaselineCalibrationEntity>();
                db.CreateTable<AutomaticCompensationCutHeightEntity>();
                db.CreateTable<ScratchInspectionParametersEntity>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("数据库错误：" + ex.Message);
            }
        }

        public static SQLiteConnection getSQLiteConnection()
        {
            return db;
        }

        public static SQLiteAsyncConnection SQLiteAsync
        {
            get
            {
                return dbAsync;
            }
        }

        public static int Add(object model)
        {
            return db.Insert(model);
        }

        public static async Task<int> AddAsync(object model)
        {
            return await dbAsync.InsertAsync(model);
        }

        public static int Update(object model)
        {
            return db.Update(model);
        }

        public static async Task<int> UpdateAsync(object model)
        {
            return await dbAsync.UpdateAsync(model);
        }

        public static int Delete(object model)
        {
            return db.Delete(model);
        }

        public static async Task<int> DeleteAsync(object model)
        {
            return await dbAsync.DeleteAsync(model);
        }

        public static List<T> Query<T>(string sql) where T : new()
        {
            return db.Query<T>(sql);
        }

        public static async Task<List<T>> QueryAsync<T>(string sql) where T : new()
        {
            return await dbAsync.QueryAsync<T>(sql);
        }

        public static int Execute(string sql)
        {
            return db.Execute(sql);
        }

        public static async Task<int> ExecuteAsync(string sql)
        {
            return await dbAsync.ExecuteAsync(sql);
        }

        public static TableQuery<T> Table<T>() where T : new()
        {
            return db.Table<T>();
        }

        public static AsyncTableQuery<T> TableAsync<T>() where T : new()
        {
            return dbAsync.Table<T>();
        }

        /// <summary>
        /// 通用实体获取方法
        /// </summary>
        public static async Task<TEntity> GetOrCreateEntityAsync<TEntity>(Func<TEntity> createDefault) where TEntity : class, IEntityWithId, new()
        {
            long defaultId = DefaultId;
            var list = await TableAsync<TEntity>().Where(t => t.Id == defaultId).ToListAsync();
            if (list.Count == 0)
            {
                TEntity entity = createDefault.Invoke();
                entity.Id = defaultId;
                await AddAsync(entity);
                return entity;
            }
            return list.First();
        }

        /// <summary>
        /// 通用实体获取方法
        /// </summary>
        public static async Task<TEntity?> GetEntityAsync<TEntity>() where TEntity : class, IEntityWithId, new()
        {
            long defaultId = DefaultId;
            var list = await TableAsync<TEntity>().Where(t => t.Id == defaultId).ToListAsync();
            if (list.Count == 0)
            {
                return null;
            }
            return list.First();
        }
    }
}