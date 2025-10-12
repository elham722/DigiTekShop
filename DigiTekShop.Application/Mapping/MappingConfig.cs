using DigiTekShop.Contracts.DTOs.Customer;
using DigiTekShop.Domain.Customer.Entities;
using Mapster;

namespace DigiTekShop.Application.Mapping;

public static class MappingConfig
{
    public static void Register(TypeAdapterConfig cfg)
    {
        // Address -> AddressDto
        cfg.NewConfig<Address, AddressDto>();

        // Customer -> CustomerView
        cfg.NewConfig<Customer, CustomerView>()
            .Map(d => d.Id, s => s.Id.Value)
            .Map(d => d.UserId, s => s.UserId)
            .Map(d => d.FullName, s => s.FullName)
            .Map(d => d.Email, s => s.Email)
            .Map(d => d.Phone, s => s.Phone)
            .Map(d => d.Addresses, s => s.Addresses);
    }
}


