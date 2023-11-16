using BlazorPeliculasFinal.Shared.Entidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using BlazorPeliculasFinal.Shared.DTOs;

namespace BlazorPeliculasFinal.Server.Controllers
{

    [ApiController]
    [Route("api/votos")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class VotosController: ControllerBase
    {
        private readonly AplicationDbContext context;
        private readonly UserManager<IdentityUser> userManager;

        public VotosController(AplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        
    }
}
