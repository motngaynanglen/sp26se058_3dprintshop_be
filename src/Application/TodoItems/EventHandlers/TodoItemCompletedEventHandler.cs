using sp26se058_3dprintshop_be.Domain.Events;
using Microsoft.Extensions.Logging;

namespace sp26se058_3dprintshop_be.Application.TodoItems.EventHandlers;

public class TodoItemCompletedEventHandler : INotificationHandler<TodoItemCompletedEvent>
{
    private readonly ILogger<TodoItemCompletedEventHandler> _logger;

    public TodoItemCompletedEventHandler(ILogger<TodoItemCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TodoItemCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("sp26se058_3dprintshop_be Domain Event: {DomainEvent}", notification.GetType().Name);

        return Task.CompletedTask;
    }
}
