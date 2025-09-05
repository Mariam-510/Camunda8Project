using AutoMapper;
using CamundaProject.Core.Models.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Application.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<ApplicationForm, BankAccount>()
            .ForMember(dest => dest.AccountHolderName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.DepositAmount))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.AccountNumber, opt => opt.MapFrom(src => GenerateAccountNumber()))
            .ForMember(dest => dest.Id, opt => opt.Ignore()); 

        }

        private static string GenerateAccountNumber()
        {
            
            return $"ACCT-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }
}
