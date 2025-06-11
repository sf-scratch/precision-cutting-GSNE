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
        public bool IsSuccess { get; set; }
        public List<Mat> AnalysisFailMats { get; set; }
        public ImageData BladeWidthMaxImage { get; set; }
        public ImageData CollapseWidthMaxImage { get; set; }
        public List<ImageData> ImageDatas{ get; set; }
        public List<ImageData> ConcatImages { get; set; }
        public bool IsSnakelike { get; set; }

        public ImagesAnalysisResult()
        {
            AnalysisFailMats = new List<Mat>();
            BladeWidthMaxImage = new ImageData() { BladeWidth = double.MinValue };
            CollapseWidthMaxImage = new ImageData() { CollapseWidth = double.MinValue };
            ImageDatas = new List<ImageData>();
            ConcatImages = new List<ImageData>();
        }
    }

    public class ImageData
    {
        public double BladeWidth { get; set; }
        public double CollapseWidth { get; set; }
        public bool IsSnakelike { get; set; }
        public Mat Mat { get; set; }
    }
}
