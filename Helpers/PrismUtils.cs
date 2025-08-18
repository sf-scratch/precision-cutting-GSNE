using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Helpers
{
    public class PrismUtils
    {
        public static IEventAggregator? GetEventAggregator()
        {
            // 方式1：通过 App 当前容器的静态访问（需确保 App 继承自 PrismApplication）
            if (PrismApplicationBase.Current is PrismApplication prismApp)
            {
                return prismApp.Container.Resolve<IEventAggregator>();
            }
            return null;
        }
    }
}
