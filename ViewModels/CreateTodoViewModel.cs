using Flunt.Notifications;
using Flunt.Validations;
using Minimal_API.Models;

namespace Minimal_API.ViewModels
{
    //dotnet add package Flunt
    public class CreateTodoViewModel : Notifiable<Notification>
    {

        public string Title { get; set; }

        public Todo MapTo()
        {
            var contract = new Contract<Notification>()
            .Requires()
            .IsNotNull(Title, "Informe o titulo da tarefa")
            .IsGreaterThan(Title, 5, "O Titulo deve conter mais que 5 caracteres");

            AddNotifications(contract);

            return new Todo(Guid.NewGuid(), Title, false);


        }

    }


}