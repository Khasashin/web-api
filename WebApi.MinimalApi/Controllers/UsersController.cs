using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto? user)
    {
        if (user is null)
            return BadRequest();
        if(!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
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
        return new CreatedAtRouteResult(nameof(GetUserById), 
            new { userId = createdUser.Id }, createdUser.Id);
    }
}