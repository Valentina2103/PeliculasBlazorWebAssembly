using BlazorPeliculasFinal.Shared.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.Eventing.Reader;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlazorPeliculasFinal.Server.Controllers
{
    [ApiController]
    [Route("api/cuentas")]
    public class CuentasController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IConfiguration configuration;

        public CuentasController(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
        }

        [HttpPost("crear")]
        public async Task<ActionResult<UserTokenDTO>> CreateUser([FromBody] UserInfo model)
        {
            var Usuario = new IdentityUser { UserName = model.Email, Email = model.Email };
            var Resultado = await userManager.CreateAsync(Usuario, model.Password);

            if (Resultado.Succeeded)
            {
                return await BuildToken(model);
            }
            else
            {
                return BadRequest(Resultado.Errors.First());
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserTokenDTO>> Login([FromBody] UserInfo model)
        {
            var Resultado = await signInManager.PasswordSignInAsync(model.Email,
                model.Password, isPersistent: false, lockoutOnFailure: false);

            if (Resultado.Succeeded)
            {
                return await BuildToken(model);
            }
            else
            {
                return BadRequest("Intento de Login Fallido");
            }

        }

        [HttpGet("renovarToken")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<UserTokenDTO>> Renovar()
        {
            var UserInfo = new UserInfo()
            {
                Email = HttpContext.User.Identity!.Name!
            };

            return await BuildToken(UserInfo);
        }
         
    
        private async Task<UserTokenDTO> BuildToken(UserInfo userInfo)
        {
            var Claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, userInfo.Email)
            };

            var usuario = await userManager.FindByEmailAsync(userInfo.Email);
            var roles = await userManager.GetRolesAsync(usuario!);

            foreach(var rol in roles)
            {
                Claims.Add(new Claim(ClaimTypes.Role, rol));
            }

            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["jwtkey"]!));
            var Creds = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);

            var Expiration = DateTime.UtcNow.AddYears(1);

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: Claims,
                expires: Expiration,
                signingCredentials: Creds
                );

            return new UserTokenDTO
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = Expiration
            };
        }

    }
}


