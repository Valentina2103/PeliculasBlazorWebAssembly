using BlazorPeliculasFinal.Server.Helpers;
using BlazorPeliculasFinal.Shared.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlazorPeliculasFinal.Server.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public class UsuariosController: ControllerBase
    {
        private readonly AplicationDbContext context;
        private readonly UserManager<IdentityUser> userManager;

        public UsuariosController(AplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<UsuarioDTO>>> Get([FromQuery] PaginacionDTO paginacion)
        {
            var Queryable = context.Users.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnRespuesta(Queryable, paginacion.CantidadRegistros);

            return await Queryable.Paginar(paginacion)
                .Select(x => new UsuarioDTO { Id = x.Id, Email = x.Email! }).ToListAsync();
        }

        [HttpGet("roles")]
        public async Task<ActionResult<List<RolDTO>>> Get()
        {
            return await context.Roles.Select(x => new RolDTO { Nombre = x.Name! }).ToListAsync();
        }

        [HttpPost("asignarRol")]
        public async Task<ActionResult> AsignarRolUsuario(EditarRolDTO editarRolDTO)
        {
            var Usuario = await userManager.FindByIdAsync(editarRolDTO.UsuarioId);

            if (Usuario == null)
            {
                return BadRequest("Usuario no existe");
            }

            await userManager.AddToRoleAsync(Usuario, editarRolDTO.Rol);
            return NoContent();
        }

        [HttpPost("removerRol")]
        public async Task<ActionResult> RemoverRolUsuario(EditarRolDTO editarRolDTO)
        {
            var Usuario = await userManager.FindByIdAsync(editarRolDTO.UsuarioId);

            if (Usuario == null)
            {
                return BadRequest("Usuario no existe");
            }

            await userManager.RemoveFromRoleAsync(Usuario, editarRolDTO.Rol);
            return NoContent();
        }
    }
}

