using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace 精密切割系统.Model.sqlite
{
    //4.4.0磨刀程序
    [Table("bm_sharpen_parameter")]
    class BmSharpenParameterModel
    {
        [PrimaryKey, AutoIncrement]
        [Column("id")]
        public long Id { get; set; }
        [Column("dress_data_no")]//磨刀号
        public string BladeLotID { get; set; }

        [Column("unit")]//单位 mm  iocn
        public string Unit { get; set; }

        [Column("rotate_speed")]//旋转轴转速 /min
        public string RotateSpeed { get; set; }

        [Column("cut_method")]//切割方式
        public string CutMethod { get; set; }

        [Column("cut_thickness")]//切割片厚度
        public string CutThickness { get; set; }

        [Column("cut_height")]//切割片高度
        public float CutHeight { get; set; }

        [Column("if_correct_height")]// 是否开启刀具修正后刀片测高
        public string IfCorrectHeight { get; set; } //1开启 其他不开启

        [Column("co_cut_num")]//刀片数
        public int CoCutNum { get; set; }

        [Column("co_x_distance")]//x轴行程（mm）
        public float CoXDistance { get; set; }

        [Column("co_y_distance")]//y轴行程（mm）
        public float CoYDistance { get; set; }

        [Column("co_jiao_height")]//膠 -胶片厚度
        public float CoJiaoHeight { get; set; }

        [Column("co_cut_size")]//进刀尺寸
        public float CoCutSize { get; set; }

        [Column("co_offset_x")]//offset_x
        public float CoOffsetX { get; set; }

        [Column("co_cut_direction")]//切割方向
        public string CoCutDirection { get; set; }



        [Column("mo_cut_one_speed")]//磨刀-进刀速度1
        public string MoCutOneSpeed { get; set; }
        [Column("mo_cut_one_no")]//磨刀-进刀1-刀
        public string MoCutOneNo { get; set; }

        [Column("mo_cut_two_speed")]//磨刀-进刀速度2
        public string MoCutTwoSpeed { get; set; }
        [Column("mo_cut_two_no")]//磨刀-进刀2-刀
        public string MoCutTwoNo { get; set; }

        [Column("mo_cut_three_speed")]//磨刀-进刀速度3
        public string MoCutThreeSpeed { get; set; }
        [Column("mo_cut_three_no")]//磨刀-进刀3-刀
        public string MoCutThreeNo { get; set; }

        [Column("mo_cut_four_speed")]//磨刀-进刀速度4
        public string MoCutFourSpeed { get; set; }


        [Column("mo_cut_four_no")]//磨刀-进刀4-刀
        public string MoCutFourNo { get; set; }

        [Column("mo_cut_five_speed")]//磨刀-进刀速度5
        public string MoCutFiveSpeed { get; set; }

        [Column("mo_cut_five_no")]//磨刀-进刀5-刀
        public string MoCutFiveNo { get; set; }


        [Column("mo_cut_six_speed")]//磨刀-进刀速度6
        public string MoCutSixSpeed { get; set; }
        [Column("mo_cut_six_no")]//磨刀-进刀1-6
        public string MoCutSixNo { get; set; }

        [Column("mo_cut_seven_speed")]//磨刀-进刀速度7
        public string MoCutSevenSpeed { get; set; }
        [Column("mo_cut_seven_no")]//磨刀-进刀7-刀
        public string MoCutSevenNo { get; set; }

        [Column("mo_cut_eight_speed")]//磨刀-进刀速度8
        public string MoCutEightSpeed { get; set; }
        [Column("mo_cut_eight_no")]//磨刀-进刀8-刀
        public string MoCutEightNo { get; set; }

        [Column("mo_cut_nine_speed")]//磨刀-进刀速度9
        public string MoCutNineSpeed { get; set; }


        [Column("mo_cut_nine_no")]//磨刀-进刀9-刀
        public string MoCutNineNo { get; set; }

        [Column("mo_cut_ten_speed")]//磨刀-进刀速度10
        public string MoCutTenSpeed { get; set; }

        [Column("mo_cut_ten_no")]//磨刀-进刀10-刀
        public string MoCutTenNo { get; set; }

    }
}
