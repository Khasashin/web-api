using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    public UsersController(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] NewUserDto user)
    {
        if(!user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError(nameof(user.Login), $"Login must contain only letters and numbers.");
            return UnprocessableEntity(ModelState);
        }
        var entity = new UserEntity
        {
            Login = user.Login,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };
        
        var createdUser = userRepository.Insert(entity);
        return new CreatedAtRouteResult("GetUserById", 
            new { id = createdUser.Id }, entity);
    }
}