using System;
using Minimal_API.ViewModels;
using Minimal_API.Data;
using Minimal_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>();

var app = builder.Build();

app.MapGet("v1/todos", (AppDbContext context) =>
{
    var todo = context.Todos.ToList();
    return Results.Ok(todo);
});


app.MapGet("v1/todos/{id}", (AppDbContext context, Guid id) =>
{
    var todo = context.Todos
    .AsNoTracking()
    .FirstOrDefault(x => x.id == id);

    if(todo==null) return Results.NotFound("Registro não encontrado.");

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

app.MapPut("v1/todos/{id}", async (AppDbContext context, Guid id, Todo todo) =>
{
    var todoBanco = await context.Todos
                .AsNoTracking<Todo>()
                .FirstOrDefaultAsync(x => x.id == id);

    if (todoBanco==null) return Results.NotFound();

    context.Todos.Update(todo);

    var result=context.SaveChanges();
 
    return result >0 ? Results.NoContent(): Results.BadRequest("Houve um problema ao atualizar o registro.");

});


app.MapDelete("v1/todos/{id}", async ([FromServices] AppDbContext context, Guid id) =>
{
    var todo = await context.Todos
                .AsNoTracking<Todo>()
                .FirstOrDefaultAsync(x => x.id == id);

    if (todo==null) return Results.NotFound("Registro não encontrado.");

    context.Todos.Remove(todo);

    var result=context.SaveChanges();
 
    return result >0 ? Results.NoContent(): Results.BadRequest("Houve um problema ao apagar o registro.");

});


app.Run();
