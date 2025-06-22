using eAccountingServer.Application.Features.Banks;
using eAccountingServer.Application.Features.CashRegisters;
using eAccountingServer.Application.Features.Companies;
using eAccountingServer.Domain.Entities;
using eAccountingServer.Domain.Enums;
using Mapster;

namespace eAccountingServer.Application.Mapping;
public static class MapsterConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<CreateCashRegisterCommand, CashRegister>
            .NewConfig()
            .Map(dest => dest.CurrencyType, src => CurrencyTypeEnum.FromValue(src.CurrencyTypeValue));

        TypeAdapterConfig<UpdateCacheRegisterCommand, CashRegister>
            .NewConfig()
            .Map(dest => dest.CurrencyType, src => CurrencyTypeEnum.FromValue(src.CurrencyTypeValue));

        TypeAdapterConfig<CreateBankCommand, Bank>
           .NewConfig()
           .Map(dest => dest.CurrencyType, src => CurrencyTypeEnum.FromValue(src.CurrencyTypeValue));

        TypeAdapterConfig<UpdateBankCommand, Bank>
            .NewConfig()
            .Map(dest => dest.CurrencyType, src => CurrencyTypeEnum.FromValue(src.CurrencyTypeValue));
    }
}
