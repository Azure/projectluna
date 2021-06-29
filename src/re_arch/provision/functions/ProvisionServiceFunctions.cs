using Luna.Common.Utils;
using Luna.Gallery.Public.Client;
using Luna.Provision.Clients;
using Luna.Provision.Data;
using Luna.Publish.Public.Client;
using Luna.PubSub.Public.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Luna.Provision.Functions
{
    /// <summary>
    /// The service maintains all routings
    /// </summary>
    public class ProvisionServiceFunctions
    {
        private const string ROUTING_SERVICE_BASE_URL_CONFIG_NAME = "ROUTING_SERVICE_BASE_URL";

        private readonly ISqlDbContext _dbContext;
        private readonly ILogger<ProvisionServiceFunctions> _logger;
        private readonly IPubSubServiceClient _pubSubClient;
        private readonly IAzureKeyVaultUtils _keyVaultUtils;
        private readonly ISwaggerClient _swaggerClient;
        private readonly IProvisionStepClientFactory _provisionStepClientFactory;

        public ProvisionServiceFunctions(ISqlDbContext dbContext, 
            ILogger<ProvisionServiceFunctions> logger, 
            IAzureKeyVaultUtils keyVaultUtils,
            IPubSubServiceClient pubSubClient,
            ISwaggerClient swaggerClient,
            IProvisionStepClientFactory provisionStepClientFactory)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(dbContext));
            this._pubSubClient = pubSubClient ?? throw new ArgumentNullException(nameof(pubSubClient));
            this._keyVaultUtils = keyVaultUtils ?? throw new ArgumentNullException(nameof(keyVaultUtils));
            this._swaggerClient = swaggerClient ?? throw new ArgumentNullException(nameof(swaggerClient));
            this._provisionStepClientFactory = provisionStepClientFactory ?? throw new ArgumentNullException(nameof(provisionStepClientFactory));
        }

        [FunctionName("ProcessApplicationEvents")]
        public async Task ProcessApplicationEvents([QueueTrigger("provision-processapplicationevents")] string myQueueItem)
        {
            // Get the last applied event id
            // If there's no record in the database, it will return the default value of long type 0
            var lastAppliedEventId = await _dbContext.LunaApplicationSwaggers.
                OrderByDescending(x => x.LastAppliedEventId).
                Select(x => x.LastAppliedEventId).FirstOrDefaultAsync();

            var events = await _pubSubClient.ListEventsAsync(
                LunaEventStoreType.APPLICATION_EVENT_STORE,
                new LunaRequestHeaders(),
                eventsAfter: lastAppliedEventId);

            foreach (var ev in events)
            {
                if (ev.EventType.Equals(LunaEventType.PUBLISH_APPLICATION_EVENT))
                {
                    LunaApplication app = JsonConvert.DeserializeObject<LunaApplication>(ev.EventContent, new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.All
                    });

                    var swaggerContent = await this._swaggerClient.GenerateSwaggerAsync(app);

                    var swaggerDb = new LunaApplicationSwaggerDB()
                    {
                        ApplicationName = ev.PartitionKey,
                        SwaggerContent = swaggerContent,
                        SwaggerEventId = ev.EventSequenceId,
                        LastAppliedEventId = ev.EventSequenceId,
                        IsEnabled = true,
                        CreatedTime = DateTime.UtcNow
                    };

                    this._dbContext.LunaApplicationSwaggers.Add(swaggerDb);
                    await this._dbContext._SaveChangesAsync();
                }
                else
                {
                    var appName = ev.PartitionKey;
                    var isDeleted = ev.EventType.Equals(LunaEventType.DELETE_APPLICATION_EVENT);

                    var swaggerDb = await this._dbContext.LunaApplicationSwaggers.
                        Where(x => x.ApplicationName == appName).
                        OrderByDescending(x => x.LastAppliedEventId).FirstOrDefaultAsync();

                    if (swaggerDb != null)
                    {
                        swaggerDb.IsEnabled = !isDeleted;
                        swaggerDb.LastAppliedEventId = ev.EventSequenceId;

                        this._dbContext.LunaApplicationSwaggers.Update(swaggerDb);
                        await this._dbContext._SaveChangesAsync();
                    }
                }
            }
        }

        [FunctionName("ProcessSubscriptionEvents")]
        public async Task ProcessSubscriptionEvents([QueueTrigger("provision-processsubscriptionevents")] string myQueueItem)
        {
            // Get the last applied event id
            // If there's no record in the database, it will return the default value of long type 0
            var lastAppliedEventId = await _dbContext.LunaApplicationSwaggers.
                OrderByDescending(x => x.LastAppliedEventId).
                Select(x => x.LastAppliedEventId).FirstOrDefaultAsync();

            var events = await _pubSubClient.ListEventsAsync(
                LunaEventStoreType.SUBSCRIPTION_EVENT_STORE,
                new LunaRequestHeaders(),
                eventsAfter: lastAppliedEventId);

        }


        [FunctionName("ProcessAzureMarketplaceEvents")]
        public async Task ProcessAzureMarketplaceEvents([QueueTrigger("provision-processazuremarketplaceevents")] string myQueueItem)
        {
            if (myQueueItem.Equals(LunaEventType.PUBLISH_AZURE_MARKETPLACE_OFFER))
            {
                await ProcessMarketplaceOfferEvents();
            }
            else if (myQueueItem.Equals(LunaEventType.CREATE_AZURE_MARKETPLACE_SUBSCRIPTION))
            {
                await ProcessMarketplaceSubscriptionEvents();
            }
        }

        private async Task<List<MarketplaceSubscriptionParameter>> GetParametersAsync(MarketplaceSubProvisionJobDB job)
        {
            var content = await this._keyVaultUtils.GetSecretAsync(job.ParametersSecretName);
            var parameters = JsonConvert.DeserializeObject<List<MarketplaceSubscriptionParameter>>(content, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            return parameters;
        }

        private async Task<List<MarketplaceProvisioningStep>> GetProvisionStepConfigAsync(MarketplaceSubProvisionJobDB job)
        {
            var content = await this._keyVaultUtils.GetSecretAsync(job.ProvisionStepsSecretName);
            var steps = JsonConvert.DeserializeObject<List<MarketplaceProvisioningStep>>(content, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            });

            return steps;
        }

        private async Task<List<string>> GetProvisionStepsAsync(MarketplaceSubProvisionJobDB job)
        {
            var plan = await _dbContext.MarketplacePlans.
                SingleOrDefaultAsync(x => x.OfferId == job.OfferId && x.PlanId == job.PlanId && x.CreatedByEventId == job.PlanCreatedByEventId);

            var prop = JsonConvert.DeserializeObject<MarketplacePlanProp>(plan.Properties);
            if (job.EventType.Equals(LunaEventType.CREATE_AZURE_MARKETPLACE_SUBSCRIPTION))
            {
                return prop.OnSubscribe;
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        private bool IsJumpboxReady(List<MarketplaceSubscriptionParameter> parameters)
        {
            return JumpboxParameterConstants.HasConnectionInfo(parameters.Select(x => x.Name).ToList());
        }

        [FunctionName("ProcessProvisionJobs")]
        public async Task ProcessProvisionJobs([TimerTrigger("*/10 * * * * *", RunOnStartup = true)] TimerInfo myTimer)
        {
            var activeJobs = await _dbContext.MarketplaceSubProvisionJobs.Where(x => x.IsActive).ToListAsync();

            // process active jobs
            foreach(var job in activeJobs)
            {
                var parameters = await GetParametersAsync(job);
                var stepConfigs = await GetProvisionStepConfigAsync(job);
                var steps = await GetProvisionStepsAsync(job);

                if (job.ProvisioningStepIndex == -1)
                {
                    if (job.Mode.Equals(MarketplacePlanMode.IaaS.ToString()))
                    {
                        if (!IsJumpboxReady(parameters))
                        {
                            // prepare jumpbox
                        }
                        else
                        {
                        }
                    }
                    else
                    {

                    }

                    job.ProvisioningStepIndex = 0;
                    job.ProvisioningStepStatus = ProvisionStepStatus.NotStarted.ToString();
                }
                else
                {
                    if (job.ProvisioningStepIndex >= steps.Count)
                    {
                        var e = new LunaServerException("The provisioning step index is invalid");
                    }
                    else
                    {
                        var step = steps[job.ProvisioningStepIndex];

                        var stepConfig = stepConfigs.SingleOrDefault(x => x.Name == step);

                        if (stepConfig == null)
                        {
                            throw new LunaServerException("");
                        }

                        if (stepConfig.Properties.IsSynchronized)
                        {
                            ISyncProvisionStepClient client = this._provisionStepClientFactory.GetSyncProvisionStepClient(stepConfig);
                        }
                        else
                        {
                            IAsyncProvisionStepClient client = this._provisionStepClientFactory.GetAsyncProvisionStepClient(stepConfig);

                            if (client != null)
                            {
                                if (job.ProvisioningStepStatus.Equals(ProvisionStepStatus.NotStarted.ToString()))
                                {
                                    // TODO: should copy over
                                    var newParams = await client.StartAsync(parameters);

                                    job.ProvisioningStepStatus = ProvisionStepStatus.Running.ToString();
                                    var content = JsonConvert.SerializeObject(newParams, new JsonSerializerSettings()
                                    {
                                        TypeNameHandling = TypeNameHandling.All
                                    });
                                    await this._keyVaultUtils.SetSecretAsync(job.ParametersSecretName, content);

                                }
                                else if (job.ProvisioningStepStatus.Equals(ProvisionStepStatus.Running.ToString()))
                                {
                                    var result = await client.CheckExecutionStatusAsync(parameters);
                                    switch (result)
                                    {
                                        case ProvisionStepExecutionResult.Completed:
                                            job.ProvisioningStepStatus = ProvisionStepStatus.Completed.ToString();
                                            break;
                                        case ProvisionStepExecutionResult.Running:
                                            break;
                                        case ProvisionStepExecutionResult.Failed:
                                            job.ProvisioningStepStatus = ProvisionStepStatus.Failed.ToString();
                                            break;
                                        default:
                                            throw new LunaServerException($"invalid provision step result {result.ToString()}");
                                    }
                                }
                                else if (job.ProvisioningStepStatus.Equals(ProvisionStepStatus.Completed.ToString()))
                                {
                                    if (job.ProvisioningStepIndex + 1 < steps.Count)
                                    {
                                        job.ProvisioningStepIndex = job.ProvisioningStepIndex + 1;
                                        job.ProvisioningStepStatus = ProvisionStepStatus.NotStarted.ToString();
                                    }
                                    else
                                    {
                                        job.Status = ProvisionStatus.Completed.ToString();
                                        job.IsActive = false;
                                        job.CompletedTime = DateTime.UtcNow;
                                    }
                                }
                            }
                        }
                    }
                }

                job.LastUpdatedTime = DateTime.UtcNow;
                _dbContext.MarketplaceSubProvisionJobs.Update(job);
                await _dbContext._SaveChangesAsync();
            }

            var queuedJobs = await _dbContext.MarketplaceSubProvisionJobs.
                OrderBy(x => x.CreatedByEventId).
                Where(x => x.Status == ProvisionStatus.Queued.ToString()).
                ToListAsync();

            List<Guid> activedJobSubs = new List<Guid>();

            foreach (var job in queuedJobs)
            {
                // Only active one job at a time for a certian subscription
                if (!activeJobs.Any(x => x.SubscriptionId == job.SubscriptionId) && !activedJobSubs.Contains(job.SubscriptionId))
                {
                    // make it active
                    var plan = await _dbContext.MarketplacePlans.
                        SingleOrDefaultAsync(x => x.OfferId == job.OfferId && 
                            x.PlanId == job.PlanId && 
                            x.CreatedByEventId == job.PlanCreatedByEventId);

                    if (plan == null)
                    {
                        var error = $"Can not find marketplace plan {job.PlanId} in offer {job.OfferId}" +
                            $" created by event {job.PlanCreatedByEventId} for subscription {job.SubscriptionId}";
                        _logger.LogError(error);
                        job.Status = ProvisionStatus.Aborted.ToString();
                        job.LastErrorMessage = error;
                    }
                    else
                    {
                        job.Status = ProvisionStatus.Running.ToString();
                        job.IsActive = true;
                        activedJobSubs.Add(job.SubscriptionId);
                    }

                    job.LastUpdatedTime = DateTime.UtcNow;
                }
            }

            _dbContext.MarketplaceSubProvisionJobs.UpdateRange(queuedJobs);
            await _dbContext._SaveChangesAsync();
        }


        /// <summary>
        /// Test endpoint
        /// </summary>
        /// <param name="req">The http request</param>
        /// <returns></returns>
        [FunctionName("Test")]
        public async Task<IActionResult> Test(
        [HttpTrigger(AuthorizationLevel.Anonymous, "Get", Route = "test")]
        HttpRequest req)
        {
            var lunaHeaders = new LunaRequestHeaders(req);
            using (_logger.BeginManagementNamedScope(lunaHeaders))
            {
                _logger.LogMethodBegin(nameof(this.Test));

                try
                {
                    throw new LunaUnauthorizedUserException(ErrorMessages.CAN_NOT_PERFORM_OPERATION);
                }
                catch (Exception ex)
                {
                    return ErrorUtils.HandleExceptions(ex, this._logger, lunaHeaders.TraceId);
                }
                finally
                {
                    _logger.LogMethodEnd(nameof(this.Test));
                }
            }
        }

        #region private methods

        private async Task ProcessMarketplaceOfferEvents()
        {
            // Get the last applied event id
            // If there's no record in the database, it will return the default value of long type 0
            var lastAppliedEventId = await _dbContext.MarketplacePlans.
                OrderByDescending(x => x.CreatedByEventId).
                Select(x => x.CreatedByEventId).FirstOrDefaultAsync();

            var events = await _pubSubClient.ListEventsAsync(
                LunaEventStoreType.AZURE_MARKETPLACE_EVENT_STORE,
                new LunaRequestHeaders(),
                eventsAfter: lastAppliedEventId);

            foreach (var ev in events)
            {
                if (ev.EventType.Equals(LunaEventType.PUBLISH_AZURE_MARKETPLACE_OFFER))
                {
                    MarketplaceOffer offer = JsonConvert.DeserializeObject<MarketplaceOffer>(ev.EventContent, new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });

                    foreach (var plan in offer.Plans)
                    {
                        var planDb = new MarketplacePlanDB();
                        planDb.OfferId = offer.OfferId;
                        planDb.PlanId = plan.PlanId;
                        planDb.Mode = plan.Properties.Mode;
                        planDb.Properties = JsonConvert.SerializeObject(plan.Properties, new JsonSerializerSettings()
                        {
                            TypeNameHandling = TypeNameHandling.All
                        });
                        var parameters = new List<MarketplaceParameter>();
                        parameters.AddRange(offer.Parameters);
                        parameters.AddRange(plan.Parameters);
                        planDb.Parameters = JsonConvert.SerializeObject(parameters, new JsonSerializerSettings()
                        {
                            TypeNameHandling = TypeNameHandling.All
                        });
                        planDb.CreatedByEventId = ev.EventSequenceId;
                        planDb.ProvisioningStepsSecretName = offer.ProvisioningStepsSecretName;

                        _dbContext.MarketplacePlans.Add(planDb);
                    }

                    await _dbContext._SaveChangesAsync();
                }
            }
        }

        private async Task ProcessMarketplaceSubscriptionEvents()
        {
            // Get the last applied event id
            // If there's no record in the database, it will return the default value of long type 0
            var lastAppliedEventId = await _dbContext.MarketplaceSubProvisionJobs.
                OrderByDescending(x => x.CreatedByEventId).
                Select(x => x.CreatedByEventId).FirstOrDefaultAsync();

            var events = await _pubSubClient.ListEventsAsync(
                LunaEventStoreType.AZURE_MARKETPLACE_EVENT_STORE,
                new LunaRequestHeaders(),
                eventsAfter: lastAppliedEventId);


            foreach (var ev in events)
            {
                if (ev.EventType.Equals(LunaEventType.CREATE_AZURE_MARKETPLACE_SUBSCRIPTION))
                {
                    var sub = JsonConvert.DeserializeObject<MarketplaceSubscriptionInternal>(ev.EventContent);

                    var plan = await _dbContext.MarketplacePlans.
                        SingleOrDefaultAsync(x => x.OfferId == sub.OfferId &&
                        x.PlanId == sub.PlanId && x.CreatedByEventId == sub.PlanCreatedByEventId);

                    if (plan == null)
                    {
                        throw new LunaServerException($"Plan {sub.PlanId} in offer {sub.OfferId} created by event {sub.PlanCreatedByEventId} does not exist.");
                    }

                    var currentTime = DateTime.UtcNow;

                    var jobDb = new MarketplaceSubProvisionJobDB()
                    {
                        SubscriptionId = sub.Id,
                        OfferId = sub.OfferId,
                        PlanId = sub.PlanId,
                        PlanCreatedByEventId = sub.PlanCreatedByEventId,
                        Mode = plan.Mode,
                        Status = ProvisionStatus.Queued.ToString(),
                        EventType = ev.EventType,
                        ProvisioningStepIndex = -1,
                        IsSynchronizedStep = false,
                        ProvisioningStepStatus = ProvisionStepStatus.NotStarted.ToString(),
                        ParametersSecretName = sub.ParametersSecretName,
                        ProvisionStepsSecretName = plan.ProvisioningStepsSecretName,
                        IsActive = false,
                        RetryCount = 0,
                        CreatedByEventId = ev.EventSequenceId,
                        CreatedTime = currentTime,
                        LastUpdatedTime = currentTime,
                    };

                    using (var transaction = await _dbContext.BeginTransactionAsync())
                    {
                        // Can have only one create job for a subscription
                        if (!await _dbContext.MarketplaceSubProvisionJobs.
                            AnyAsync(x => x.SubscriptionId == sub.Id && x.EventType == ev.EventType))
                        {
                            this._dbContext.MarketplaceSubProvisionJobs.Add(jobDb);
                            await this._dbContext._SaveChangesAsync();
                        }

                        transaction.Commit();
                    }



                }
            }
        }

        #endregion

    }
}