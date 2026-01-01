using sp26se058_3dprintshop_be.Application.TodoLists.Commands.CreateTodoList;
using sp26se058_3dprintshop_be.Application.TodoLists.Commands.DeleteTodoList;
using sp26se058_3dprintshop_be.Domain.Entities;

namespace sp26se058_3dprintshop_be.Application.FunctionalTests.TodoLists.Commands;

using static Testing;

public class DeleteTodoListTests : BaseTestFixture
{
    [Test]
    public async Task ShouldRequireValidTodoListId()
    {
        var command = new DeleteTodoListCommand(99);
        await FluentActions.Invoking(() => SendAsync(command)).Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task ShouldDeleteTodoList()
    {
        var listId = await SendAsync(new CreateTodoListCommand
        {
            Title = "New List"
        });

        await SendAsync(new DeleteTodoListCommand(listId));

        var list = await FindAsync<TodoList>(listId);

        list.Should().BeNull();
    }
}
