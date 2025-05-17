using ASC.Model.Models;
using ASC.Web.Areas.Configuration.Models;
using AutoMapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<MasterDataKey, MasterDataKeyViewModel>();
        CreateMap<MasterDataKeyViewModel, MasterDataKey>();

        CreateMap<MasterDataValue, MasterDataValueViewModel>();
        CreateMap<MasterDataValueViewModel, MasterDataValue>();
    }
}
