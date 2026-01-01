using sp26se058_3dprintshop_be.Domain.Events;
using Microsoft.Extensions.Logging;

namespace sp26se058_3dprintshop_be.Application.TodoItems.EventHandlers;

public class TodoItemCreatedEventHandler : INotificationHandler<TodoItemCreatedEvent>
{
    private readonly ILogger<TodoItemCreatedEventHandler> _logger;

    public TodoItemCreatedEventHandler(ILogger<TodoItemCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TodoItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("sp26se058_3dprintshop_be Domain Event: {DomainEvent}", notification.GetType().Name);

        return Task.CompletedTask;
    }
}
