namespace EU.Core.EventBus.EventHandling;

public class TiobonQueryIntegrationEvent : IntegrationEvent
{
    public string TiobonId { get; private set; }

    public TiobonQueryIntegrationEvent(string Tiobonid)
        => TiobonId = Tiobonid;
}
