using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media;

namespace 精密切割系统.Helpers
{
    internal class CommonEvent
    {

        public static void BtnScaleDown(object? sender, int type)
        {
            Border border = (Border)sender;
            if (type == 0)
            {
                // 缩小按钮内容
                var scaleTransform = (ScaleTransform)((TransformGroup)border.RenderTransform).Children[0];
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
                // 恢复发光效果（提升不透明度）
                var shadowEffect = (DropShadowEffect)border.Effect;
                shadowEffect.Opacity = 0.8; // 恢复到 80%
            }
            else
            {
                // 缩小按钮内容
                var scaleTransform = (ScaleTransform)((TransformGroup)border.RenderTransform).Children[0];
                scaleTransform.ScaleX = 0.95; // 缩小到 95%
                scaleTransform.ScaleY = 0.95;

                // 减弱发光效果（降低不透明度）
                var shadowEffect = (DropShadowEffect)border.Effect;
                shadowEffect.Opacity = 0.16; // 减弱到 20%
            }
        }
    }
}
