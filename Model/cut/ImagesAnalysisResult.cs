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
        public double BladeWidthMax { get; set; } = double.MinValue;
        public Mat BladeWidthMaxMat { get; set; }
        public double CollapseWidthMax { get; set; } = double.MinValue;
        public Mat CollapseWidthMaxMat { get; set; }
        public List<ImageData> ImageDatas{ get; set; } = new List<ImageData>();
    }

    public class ImageData
    {
        public double BladeWidth { get; set; }
        public double CollapseWidth { get; set; }
        public Mat Mat { get; set; }
    }
}
