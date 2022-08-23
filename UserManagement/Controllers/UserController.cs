using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagement.Core.Entities;
using UserManagement.Core.ViewModels;
using UserManagement.Helpers;
using UserManagement.Services;

namespace UserManagement.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[Controller]")]
    public class UserController : ControllerBase
    {
        private IUserService _UserService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;
        public UserController(IUserService userService, IMapper mapper, IOptions<AppSettings> appSettings)
        {
            _UserService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous] // it means this method dont need token
        [HttpPost("register")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult Register([FromBody] RegisterViewModel registerViewModel)
        {
            var regUser = _mapper.Map<User>(registerViewModel);
            try
            {
                var regResp = _UserService.Create(regUser, registerViewModel.Password);
                var regOut = _mapper.Map<UserViewModel>(regResp);
                return Ok(new ApiResponse { Status = true, Message = "User Register Successfully", Data = regOut });
            }
            catch (CustomException ex)
            {

                return BadRequest(new ApiResponse { Status = false, Message = ex.Message, Data = null });
            }
        }

        [AllowAnonymous] // it means this method dont need token
        [HttpPost("authentication")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult Authentication([FromBody] AuthenticateViewModel authenticateViewModel)
        {
            var user = _UserService.Authenticate(authenticateViewModel.Username, authenticateViewModel.Password);

            if (user == null)
            {
                return BadRequest(new ApiResponse { Status = false, Message = "UserName or Password Invalid! ", Data = null });

            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.ID.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var resp = _mapper.Map<UserTokenViewModel>(user);
            resp.Token = tokenString;

            return Ok(new ApiResponse { Status = true, Message = "Login Successfully!", Data = resp });
        }

       // [AllowAnonymous]
        [HttpGet("{id}")]

        public IActionResult GetById(int id)
        {
            var user = _UserService.GetUserById(id);
            var output = _mapper.Map<UserViewModel>(user);

            if (user == null)
            {
                return BadRequest(new ApiResponse { Status = false, Message = "UserName or Password Invalid! ", Data = null });

            }
            return Ok(new ApiResponse { Status = true, Message = "User Details!", Data = output });

        }

        [AllowAnonymous]
        [HttpGet("getAll")]
        public IActionResult GetUsers()
        {

            var userAll = _UserService.GetAll();
            var user = _mapper.Map<List<UserViewModel>>(userAll);
            if (user == null)
            {
                return BadRequest(new ApiResponse { Status = false, Message = "No data found! ", Data = user });

            }

            return Ok(new ApiResponse { Status = true, Message = "Users Details!", Data = user });

        }

       // [AllowAnonymous]
        [HttpDelete]
        [Route("delete/{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _UserService.GetUserById(id);
            var output = _mapper.Map<UserViewModel>(user);
            if (user != null)
            {
                _UserService.DeleteUser(user);
                return Ok(new ApiResponse { Status = true, Message = "Users Deleted Successfully!", Data = output });
            }
            return NotFound($"User with id:{id} was not found");
        }

       // [AllowAnonymous]
        [HttpPatch]
        [Route("update")]

        public IActionResult EditUser([FromBody] updateViewModel user)
        {
            var ExitUser = _UserService.GetUserById(user.ID);
            var output = _mapper.Map<updateViewModel>(user);
            if (ExitUser != null)
            {
                user.ID = ExitUser.ID;
                _UserService.EditUser(output);
            }
            return Ok(new ApiResponse { Status = true, Message = "Users updated Successfully!", Data = output });
         
        }


    }
}
