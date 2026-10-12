using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;

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

    [HttpGet("{userId:guid}", Name = nameof(GetUserById))]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        
        return Ok(mapper.Map<UserDto>(user));
    }

    [HttpPost(Name = nameof(CreateUser))]
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

    [HttpPut("{userId:guid}", Name = nameof(UpdateUser))]
    [Consumes("application/json")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? user)
    {
        if (user == null || userId == Guid.Empty)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        var userEntity = mapper.Map(user, new UserEntity(userId));
        userRepository.UpdateOrInsert(userEntity, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        return NoContent();
    }
    
    [HttpPatch("{userId:guid}", Name = nameof(PartiallyUpdateUser))]
    [Consumes("application/json-patch+json")]
    [Produces("application/json", "application/xml")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchUser)
    {
        if (patchUser is null)
            return BadRequest();
        var emptyOp = patchUser.Operations.Where(op => string.IsNullOrEmpty(op.value.ToString()))
            .ToList();
        if (emptyOp.Count > 0)
        {
            foreach (var op in emptyOp)
            {
                ModelState.AddModelError(op.path, $"Patch operations must not be empty value from path {op.path}.");
            }
            return UnprocessableEntity(ModelState);
        }
        if(!patchUser.Operations.Where(op => op.path == "login")
               .All(op =>  (op.value.ToString() ?? throw new InvalidOperationException()).All(char.IsLetterOrDigit)))
        {
            ModelState.AddModelError("login", $"Login must contain only letters and numbers.");
            return UnprocessableEntity(ModelState);
        }
        if(!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        if (!TryValidateModel(patchUser))
        {
            ModelState.AddModelError(nameof(patchUser), $"UserDto can be contain only Login, FirstName and LastName");
            return UnprocessableEntity(ModelState);
        }

        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }
        var updateUserDto = mapper.Map<UpdateUserDto>(user);
        
        patchUser.ApplyTo(updateUserDto, ModelState);
        var updateUser = mapper.Map<UserEntity>(updateUserDto);
        userRepository.Update(updateUser);
        return NoContent();
    }
}