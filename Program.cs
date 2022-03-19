using Minimal_API.ViewModels;
using Minimal_API.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>();

var app = builder.Build();

app.MapGet("v1/todos", (AppDbContext context) =>
{
    var todo = context.Todos.ToList();
    return Results.Ok(todo);
});

app.MapPost("v1/todos", (AppDbContext context, CreateTodoViewModel model) =>
{
    var todo = model.MapTo();

    if (!model.IsValid)
        return Results.BadRequest(model.Notifications);

    context.Todos.Add(todo);
    context.SaveChanges();

    return Results.Created($"/v1/todos/{todo.id}", todo);

});

app.Run();
