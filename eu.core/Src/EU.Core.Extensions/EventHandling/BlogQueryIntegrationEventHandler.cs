using EU.Core.Common;
using EU.Core.EventBus.EventHandling;
using EU.Core.IServices;
using Microsoft.Extensions.Logging;

namespace EU.Core.EventBus;

public class TiobonQueryIntegrationEventHandler : IIntegrationEventHandler<TiobonQueryIntegrationEvent>
{
    //private readonly ITiobonArticleServices _TiobonArticleServices;
    private readonly ILogger<TiobonQueryIntegrationEventHandler> _logger;

    public TiobonQueryIntegrationEventHandler(
        //ITiobonArticleServices TiobonArticleServices,
        ILogger<TiobonQueryIntegrationEventHandler> logger)
    {
        //_TiobonArticleServices = TiobonArticleServices;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(TiobonQueryIntegrationEvent @event)
    {
        _logger.LogInformation("----- Handling integration event: {IntegrationEventId} at {AppName} - ({@IntegrationEvent})", @event.Id, "EU.Core", @event);

        ConsoleHelper.WriteSuccessLine($"----- Handling integration event: {@event.Id} at EU.Core - ({@event})");

        //await _TiobonArticleServices.QueryById(@event.TiobonId.ToString());
    }

}
