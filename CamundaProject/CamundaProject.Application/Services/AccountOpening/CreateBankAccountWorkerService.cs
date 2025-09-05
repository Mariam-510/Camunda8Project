using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeebe.Client.Api.Worker;
using Zeebe.Client;
using CamundaProject.Core.Models.ClientModels;
using Zeebe.Client.Api.Responses;
using System.Text.Json;
using CamundaProject.Core.Interfaces.Repositories.ClientRepositories;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace CamundaProject.Application.Services.AccountOpening
{
    public class CreateBankAccountWorkerService : IHostedService
    {


        private readonly ILogger<CreateBankAccountWorkerService> _logger;
        private readonly IZeebeClient _zeebeClient;
        private IJobWorker? _worker;

        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _scopeFactory;


        public CreateBankAccountWorkerService(IZeebeClient zeebeClient, ILogger<CreateBankAccountWorkerService> logger, IMapper mapper, IServiceScopeFactory scopeFactory)
        {
            _zeebeClient = zeebeClient;
            _logger = logger;
           
            _mapper = mapper;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _worker = _zeebeClient.NewWorker()
               .JobType("create-account") 
               .Handler(HandleJob)
               .Name("CreateBankAccountWorker")
               .MaxJobsActive(5)
               .PollInterval(TimeSpan.FromSeconds(1))
               .Timeout(TimeSpan.FromMinutes(2))
               .Open();
            _logger.LogInformation("CreateBankAccountWorker started and listening for jobs.");
            return Task.CompletedTask;
        }


        private async Task HandleJob(IJobClient client, IJob job)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

                    var clientInfo = JsonSerializer.Deserialize<ApplicationForm>(job.Variables);

                    var account = _mapper.Map<BankAccount>(clientInfo);

                    var createdAccount = await accountRepository.CreateAccountAsync(account);
                    //createdAccount is null || createdAccount.Id == 0
                    if (true)
                    {
                        _logger.LogError("Error creating bank account. Retries left: {Retries}", job.Retries);
                        if (job.Retries > 1)
                        {
                            await client.NewFailCommand(job.Key)
                                .Retries(job.Retries - 1)
                                .ErrorMessage("CreateAccount failed.")
                                .Send();
                        }
                        else
                        {
                            _logger.LogError("Core Banking system failed");
                            await client.NewThrowErrorCommand(job.Key)
                                .ErrorCode("ACCOUNT_ERROR")
                                .ErrorMessage("Core Banking system failed")
                                .Send();


                        }
                            return;


                    }

                    var vars = new
                    {
                        isCreated = true,
                        AccountId = createdAccount.AccountNumber,

                    };

                    await client.NewCompleteJobCommand(job.Key)
                                .Variables(JsonSerializer.Serialize(vars))
                                .Send();






                    _logger.LogInformation("--------------------------------------------------------------------------------------------------------");
                    _logger.LogInformation("Bank account created successfully: {AccountNumber} for Client: {clientName}", createdAccount?.AccountNumber, createdAccount?.AccountHolderName);
                    _logger.LogInformation("--------------------------------------------------------------------------------------------------------");


                }



            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bank account.");
                await client.NewFailCommand(job.Key)
                    .Retries(job.Retries - 1)
                    .ErrorMessage(ex.Message)
                    .Send();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _worker?.Dispose();
            _logger.LogInformation("Create Account worker stopped.");
            return Task.CompletedTask;
        }
    }
}
