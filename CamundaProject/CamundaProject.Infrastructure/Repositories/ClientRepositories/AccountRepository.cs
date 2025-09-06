using CamundaProject.Core.Interfaces.Repositories.ClientRepositories;
using CamundaProject.Core.Models.ClientModels;
using CamundaProject.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamundaProject.Infrastructure.Repositories.ClientRepositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;
        public AccountRepository(AppDbContext context)
        {
            _context = context;

        }

        public async Task<BankAccount> CreateAccountAsync(BankAccount account)
        {
            await _context.BankAccounts.AddAsync(account);
            await _context.SaveChangesAsync();
            return account;
        }
    }
}
