using System.Threading.Channels;
using MarbleCraftOMS.Core.Events;
using MarbleCraftOMS.Core.Interfaces;

namespace MarbleCraftOMS.Infrastructure.Messaging;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Channel<IDomainEvent> _channel =
        Channel.CreateUnbounded<IDomainEvent>(
            new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<IDomainEvent> Reader => _channel.Reader;

    public void Publish(IDomainEvent evt) => _channel.Writer.TryWrite(evt);
}
