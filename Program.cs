using System;
using Minimal_API.ViewModels;
using Minimal_API.Data;
using Minimal_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NetDevPack.Identity.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

//dotnet add package NetDevPack.Identity
//link com a documentação -> https://github.com/NetDevPack/Security.Identity

builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
     options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"), //pegando a string de conexão com o banco
    b => b.MigrationsAssembly("Minimal _API"))); // nome do projeto

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExcluirFornecedor",
        policy => policy.RequireClaim("ExcluirFornecedor")); //apenas usuarios com claims, podem fazer exclusão. Usuario sem claims recebe um 403.
});


//Configuração do Swagger

//http://localhost:5074/Swagger/index.html
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API Sample",
        Description = "Developed by Eduardo Pires - Owner @ desenvolvedor.io",
        Contact = new OpenApiContact { Name = "Eduardo Pires", Email = "contato@eduardopires.net.br" },
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});



var app = builder.Build();
app.UseAuthConfiguration();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//para que a tabela de usuario seja criada é preciso executar a migration:

//dotnet ef migrations add NewAut -c  NetDevPackAppDbContext
//dotnet ef database update NewAut -c NetDevPackAppDbContext

//{
//   "email": "teste2@gmailcom",
// "password": "Teste@123",
//"confirmPassword": "Teste@123"
//}

app.MapPost("/registro", [AllowAnonymous] async (
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    IOptions<AppJwtSettings> appJwtSettings,
    RegisterUser registerUser) =>
{
    if (registerUser == null)
        return Results.BadRequest("Usuário não informado");

    var user = new IdentityUser
    {
        UserName = registerUser.Email,
        Email = registerUser.Email,
        EmailConfirmed = true
    };

    var result = await userManager.CreateAsync(user, registerUser.Password);

    if (!result.Succeeded)
        return Results.BadRequest(result.Errors);

    var jwt = new JwtBuilder()
                .WithUserManager(userManager)
                .WithJwtSettings(appJwtSettings.Value)
                .WithEmail(user.Email)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .BuildUserResponse();

    return Results.Ok(jwt);

}).ProducesValidationProblem()
  .Produces(StatusCodes.Status200OK)
  .Produces(StatusCodes.Status400BadRequest)
  .WithName("RegistroUsuario")
  .WithTags("Usuario");

app.MapPost("/login", [AllowAnonymous] async (
   SignInManager<IdentityUser> signInManager,
   UserManager<IdentityUser> userManager,
   IOptions<AppJwtSettings> appJwtSettings,
   LoginUser loginUser) =>
{
    if (loginUser == null)
        return Results.BadRequest("Usuário não informado");

    var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

    if (result.IsLockedOut)
        return Results.BadRequest("Usuário bloqueado");

    if (!result.Succeeded)
        return Results.BadRequest("Usuário ou senha inválidos");

    var jwt = new JwtBuilder()
                .WithUserManager(userManager)
                .WithJwtSettings(appJwtSettings.Value)
                .WithEmail(loginUser.Email)
                .WithJwtClaims()
                .WithUserClaims()
                .WithUserRoles()
                .BuildUserResponse();

    return Results.Ok(jwt);

}).ProducesValidationProblem()
 .Produces(StatusCodes.Status200OK)
 .Produces(StatusCodes.Status400BadRequest)
 .WithName("LoginUsuario")
 .WithTags("Usuario");

app.MapGet("v1/todos", [AllowAnonymous] (AppDbContext context) =>
{
    var todo = context.Todos.ToList();
    return Results.Ok(todo);
});


app.MapGet("v1/todos/{id}", [Authorize] (AppDbContext context, Guid id) =>
{
    var todo = context.Todos
    .AsNoTracking()
    .FirstOrDefault(x => x.id == id);

    if (todo == null) return Results.NotFound("Registro não encontrado.");

    return Results.Ok(todo);
});


app.MapPost("v1/todos", [Authorize] (AppDbContext context, CreateTodoViewModel model) =>
{
    var todo = model.MapTo();

    if (!model.IsValid)
        return Results.BadRequest(model.Notifications);

    context.Todos.Add(todo);
    context.SaveChanges();

    return Results.Created($"/v1/todos/{todo.id}", todo);

});

app.MapPut("v1/todos/{id}", [Authorize] async (AppDbContext context, Guid id, Todo todo) =>
{
    var todoBanco = await context.Todos
                .AsNoTracking<Todo>()
                .FirstOrDefaultAsync(x => x.id == id);

    if (todoBanco == null) return Results.NotFound();

    context.Todos.Update(todo);

    var result = context.SaveChanges();

    return result > 0 ? Results.NoContent() : Results.BadRequest("Houve um problema ao atualizar o registro.");

});


app.MapDelete("v1/todos/{id}", [Authorize] async ([FromServices] AppDbContext context, Guid id) =>
{
    var todo = await context.Todos
                .AsNoTracking<Todo>()
                .FirstOrDefaultAsync(x => x.id == id);

    if (todo == null) return Results.NotFound("Registro não encontrado.");

    context.Todos.Remove(todo);

    var result = context.SaveChanges();

    return result > 0 ? Results.NoContent() : Results.BadRequest("Houve um problema ao apagar o registro.");

}).RequireAuthorization("ExcluirFornecedor");


app.Run();
