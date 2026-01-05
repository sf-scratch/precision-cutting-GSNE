using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Entities
{
    /// <summary>
    /// 定义具有Id属性的实体接口
    /// </summary>
    public interface IEntityWithId
    {
        long Id { get; set; }
    }
}