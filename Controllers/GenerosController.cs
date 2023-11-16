using BlazorPeliculasFinal.Shared.Entidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlazorPeliculasFinal.Server.Controllers
{
    [Route("api/generos")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Admin")]

    public class GenerosController : ControllerBase
    {
        public readonly AplicationDbContext Context;

        public GenerosController(AplicationDbContext context)
        {
            this.Context = context;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(Genero genero)
        {
            Context.Add(genero);
            await Context.SaveChangesAsync();
            return genero.Id;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Genero>>> Get()
        {
            return await Context.Generos.ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Genero>> Get(int id)
        {
            var Genero = await Context.Generos.FirstOrDefaultAsync(x => x.Id == id);

            if (Genero is null)
            {
                return NotFound();
            }

            return Genero;
        }

        [HttpPut]
        public async Task<ActionResult> Put(Genero genero)
        {
            Context.Update(genero);
            await Context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var FilasAfectadas = await Context.Generos.Where(x => x.Id == id).ExecuteDeleteAsync();

            if (FilasAfectadas == 0)
            {
                return NotFound();
            }

            return NoContent();
        }
    }

}


