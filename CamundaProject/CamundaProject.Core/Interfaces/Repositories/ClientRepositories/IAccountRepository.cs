using CamundaProject.Core.Models.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Core.Interfaces.Repositories.ClientRepositories
{
    public interface IAccountRepository
    {
        Task<BankAccount> CreateAccountAsync(BankAccount account);
   
    }
}
