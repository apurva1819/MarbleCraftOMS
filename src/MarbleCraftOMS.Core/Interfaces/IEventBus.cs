using MarbleCraftOMS.Core.Events;

namespace MarbleCraftOMS.Core.Interfaces;

public interface IEventBus
{
    void Publish(IDomainEvent evt);
}
