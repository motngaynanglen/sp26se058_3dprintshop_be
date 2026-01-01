namespace sp26se058_3dprintshop_be.Domain.Events;

public class TodoItemDeletedEvent : BaseEvent
{
    public TodoItemDeletedEvent(TodoItem item)
    {
        Item = item;
    }

    public TodoItem Item { get; }
}
