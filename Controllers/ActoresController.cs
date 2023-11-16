using AutoMapper;
using BlazorPeliculasFinal.Server.Helpers;
using BlazorPeliculasFinal.Shared.DTOs;
using BlazorPeliculasFinal.Shared.Entidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;
using System.Transactions;

namespace BlazorPeliculasFinal.Server.Controllers
{
    [ApiController]
    [Route("api/actores")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]

    public class ActoresController : ControllerBase
    {
        private readonly AplicationDbContext Context;
        private readonly iAlmacenadorArchivos AlmacenadorArchivos;
        private readonly IMapper Mapper;
        private readonly string Contenedor = "personas";

        public ActoresController(AplicationDbContext context, iAlmacenadorArchivos almacenadorArchivos, IMapper mapper)
        { 
            this.Context = context;
            this.AlmacenadorArchivos = almacenadorArchivos;
            this.Mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(Actor actor)
        {
            if (!string.IsNullOrWhiteSpace(actor.Foto))
            {
                var FotoActor = Convert.FromBase64String(actor.Foto);
                actor.Foto = await AlmacenadorArchivos.GuardarArchivo(FotoActor, "jpg", Contenedor);
            }   

            Context.Add(actor);
            await Context.SaveChangesAsync();
            return actor.Id;
        }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<Actor>>> Get([FromQuery]PaginacionDTO paginacion)
        {
            var queryable =  Context.Actores.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnRespuesta(queryable, paginacion.CantidadRegistros);
            return await queryable.OrderBy(x => x.Nombre).Paginar(paginacion).ToListAsync();
        }



        [HttpGet("{id:int}")]
        public async Task<ActionResult<Actor>> Get(int id)
        {
            var Actor = await Context.Actores.FirstOrDefaultAsync(x => x.Id == id);

            if (Actor is null)
            {
                return NotFound();
            }

            return Actor;
        }

        [HttpGet("buscar/{textoBusqueda}")]
        public async Task <ActionResult<List<Actor>>> Get(string textoBusqueda)
        {
            if (string.IsNullOrWhiteSpace(textoBusqueda))
            {
                return new List<Actor>();
            }

            textoBusqueda = textoBusqueda.ToLower();

            return await Context.Actores.Where(x => x.Nombre.ToLower().Contains(textoBusqueda)).Take(5).ToListAsync();
        }

        [HttpPut]
        public async Task<ActionResult> Put(Actor actor)
        {
            var ActorDB = await Context.Actores.FirstOrDefaultAsync(x => x.Id == actor.Id);
            
            if (ActorDB is null)
            {
                return NotFound();
            }

            ActorDB = Mapper.Map(actor, ActorDB);

            if (!string.IsNullOrEmpty(actor.Foto))
            {
                var FotoActor = Convert.FromBase64String(actor.Foto);
                ActorDB.Foto = await AlmacenadorArchivos.EditarArchivo(FotoActor, ".jpg", Contenedor, ActorDB.Foto!);

            }

            await Context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var Actor = await Context.Actores.FirstOrDefaultAsync(x => x.Id ==id);

            if (Actor is null)
            {
                return NotFound();
            }

            Context.Remove(Actor);
            await Context.SaveChangesAsync();
            await AlmacenadorArchivos.EliminarArchivo(Actor.Foto!, Contenedor);
            return NoContent();
        }

    }
}
 





