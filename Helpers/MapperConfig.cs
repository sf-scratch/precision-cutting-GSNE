using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 精密切割系统.DTOs;
using 精密切割系统.Entities;
using 精密切割系统.Model.cut;

namespace 精密切割系统.Helpers
{
    public static class MapperConfig
    {
        public static IMapper Mapper => _lazyMapper.Value;

        private static readonly Lazy<IMapper> _lazyMapper = new Lazy<IMapper>(() =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<LunguSksjDTO, LunguSksjModel>().ReverseMap();
                cfg.CreateMap<SharpenParamsEntity, SharpenParamsModel>().ReverseMap();
                cfg.CreateMap<CutParamsEntity, CutParamsModel>().ReverseMap();
                cfg.CreateMap<KnifeWearEntity, KnifeWearModel>().ReverseMap();
            });
            return config.CreateMapper();
        });
    }
}
