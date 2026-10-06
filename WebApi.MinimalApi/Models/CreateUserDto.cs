using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WebApi.MinimalApi.Models;

public class CreateUserDto
{
    [Required] public string Login;
    [DefaultValue("John")] public string FirstName;
    [DefaultValue("Doe")] public string LastName;
}