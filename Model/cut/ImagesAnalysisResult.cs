using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 精密切割系统.Model.cut
{
    public class ImagesAnalysisResult
    {
        public ImageData BladeWidthMaxImage { get; set; } = new ImageData() { BladeWidth = double.MinValue};
        public ImageData CollapseWidthMaxImage { get; set; } = new ImageData() { CollapseWidth = double.MinValue };
        public List<ImageData> ImageDatas{ get; set; } = new List<ImageData>();
    }

    public class ImageData
    {
        public double BladeWidth { get; set; }
        public double CollapseWidth { get; set; }
        public Mat Mat { get; set; }
    }
}
