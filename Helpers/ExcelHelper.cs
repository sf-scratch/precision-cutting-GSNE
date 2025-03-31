using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace 精密切割系统.Utils
{
    /// <summary>
    /// 使用npoi导入导出excleg工具类
    /// see原文链接：https://blog.csdn.net/qq_45864905/article/details/134999602
    /// </summary>
    public class ExcelHelper
    {
        /// <summary>
        /// 写入excel文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName">文件名称（也可以是文件路径）</param>
        /// <param name="dataList">数据列表（数据库查询结果）</param>
        /// <param name="propetryDictinory">实体类属性对应的中文名称（用于在报表上展示）</param>
        /// <param name="version">excel版本（低于2007默认为0，2007及以上为1）</param>
        /// <returns></returns>
        public static bool WriteExcel<T>(string fileName, List<T> dataList,
            Dictionary<string, string> propetryDictinory, int version = 0)
        {
            try
            {
                //【1】.创建工作簿 2007之前用HSSFWorkbook 2007之后用XSSFWorkbook  
                IWorkbook? workBook = null;
                // 根据版本号创建不同版本的excel
                workBook = version == 0 ? new HSSFWorkbook() : new XSSFWorkbook();
                // 每个工作簿有多个sheet，创建一个sheet
                ISheet sheet = workBook.CreateSheet("sheet1");
                //宽度： SetColumnWidth方法里的第二个参数要乘以256，因为这个参数的单位是1 / 256个字符宽度，所以要乘以256才是一整个字符宽度。
                //高度： .Height 属性后面的值的单位是：1 / 20个点，所以要想得到一个点的话，需要乘以20。
                //sheet.DefaultColumnWidth = 5 * 256 * 256;
                //sheet.DefaultRowHeight = 30 * 20;
                
                // 在工作表中创建标题行
                IRow titleRow = sheet.CreateRow(0);
                // 放入属性对应的中文名称
                Type type = typeof(T);
                PropertyInfo[] propertyInfos = type.GetProperties();
                for (int i = 0; i < propetryDictinory.Count; i++)
                {
                    ICell cell = titleRow.CreateCell(i);
                    string value = propetryDictinory[propertyInfos[i].Name];
                    cell.SetCellValue(value);
                }
                // 创建数据行
                for (int i = 0; i < dataList.Count; i++)
                {
                    IRow row = sheet.CreateRow(i + 1);

                    for (int j = 0; j < propetryDictinory.Count; j++)
                    {
                        ICell cell = row.CreateCell(j);
                        if (cell != null)
                        {
                            object? temp = propertyInfos[j].GetValue(dataList[i]);
                            string? data = temp == null ? "" : temp.ToString();
                            cell.SetCellValue(data);
                        }
                    }
                }
                for (int i = 0; i < propetryDictinory.Count; i++)
                {
                    sheet.SetColumnWidth(i, 25 * 256);//
                }
                using (FileStream fs = File.OpenWrite(fileName))
                {
                    workBook.Write(fs);
                    return true;
                }

            }
            catch (Exception e)
            {
                Tools.LogError(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 读取Excel数据
        /// 接收类的属性顺序和excle列的顺序一致
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName">文件名称（也可以是文件路径</param>
        /// <param name="t">excel数据对应的实体类型，使用new的方式传入匿名实例化的对象即可</param>
        /// <param name="version">excel版本（低于2007默认为0，2007及以上为1）</param>
        /// <param name="startRow">数据开始行一般情况下第一行是标题，第二行是数据行startRow=1</param>
        /// <returns></returns>
        public static List<T> ReadExcel<T>(string fileName, T t, int version = 0,int startRow = 1)
        {
            try
            {
                List<T> dataList = new List<T>();
                using (FileStream fs = File.OpenRead(fileName))
                {
                    //【1】.创建工作簿 2007之前用HSSFWorkbook 2007之后用6XSSFWorkbook  
                    IWorkbook? workBook = null;
                    // 根据版本号创建不同版本的excel
                    workBook = version == 0 ? new HSSFWorkbook(fs) : new XSSFWorkbook(fs);
                    // 获取工作表
                    ISheet sheet = workBook.GetSheetAt(0);
                    // 初始化反射
                    Type type = typeof(T);

                    if (type == null)
                    {
                        throw new Exception("数据对应的类不存在！");
                    }

                    // 获取excel中的数据 忽略标题行
                    for (int i = startRow; i <= sheet.LastRowNum; i++)
                    {
                        IRow row = sheet.GetRow(i);
                        object? obj = Activator.CreateInstance(type);
                        PropertyInfo[] propertyInfos = type.GetProperties();
                        for (int j = 0; j < row.LastCellNum; j++)
                        {
                            ICell cell = row.GetCell(j);
                            propertyInfos[j].SetValue(obj, Convert.ChangeType(cell.ToString(), propertyInfos[j].PropertyType));
                        }
                        dataList.Add((T)obj);
                    }
                }
                return dataList;
            }
            catch (Exception e)
            {
                Tools.LogError(e.Message);
                return null;
            }            
        }
    }
}
