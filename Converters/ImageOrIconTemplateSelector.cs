using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace 精密切割系统.Converters
{
    public class ImageOrIconTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }

        public DataTemplate IconTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // 这里item就是绑定的数据，但我们这里实际上绑定的是ImagePath字符串
            // 注意：我们可能绑定整个数据上下文，但这里我们只需要ImagePath
            // 所以我们可以将ContentControl的Content绑定到ImagePath，然后在这里判断
            if (item is string imagePath)
            {
                // 使用与之前相同的逻辑：如果以'/'开头，则使用ImageTemplate，否则使用IconTemplate
                if (imagePath.StartsWith("/"))
                    return ImageTemplate;
                else
                    return IconTemplate;
            }
            return null;
        }
    }

}
