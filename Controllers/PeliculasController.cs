using AutoMapper;
using BlazorPeliculasFinal.Server.Helpers;
using BlazorPeliculasFinal.Shared.DTOs;
using BlazorPeliculasFinal.Shared.Entidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlazorPeliculasFinal.Server.Controllers
{
    [ApiController]
    [Route("api/peliculas")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Admin")]

    public class PeliculasController: ControllerBase
    {
        private readonly AplicationDbContext Context;
        private readonly iAlmacenadorArchivos AlmacenadorArchivos;
        private readonly IMapper Mapper;
        private readonly string Contenedor = "peliculas";

        public PeliculasController(AplicationDbContext context, iAlmacenadorArchivos almacenadorArchivos, IMapper mapper)
        {
            this.Context = context;
            this.AlmacenadorArchivos = almacenadorArchivos;
            this.Mapper = mapper; 
        }

        [HttpPost]
        public async Task<ActionResult<int>> Post(Pelicula pelicula)
        {
            if (!string.IsNullOrWhiteSpace(pelicula.Poster))
            {
                var Poster = Convert.FromBase64String(pelicula.Poster);
                pelicula.Poster = await AlmacenadorArchivos.GuardarArchivo(Poster, "jpg", Contenedor);
            }

            EscribirOrdenActores(pelicula);

            Context.Add(pelicula);
            await Context.SaveChangesAsync();
            return pelicula.Id;
        }

        private static void EscribirOrdenActores(Pelicula pelicula)
        {
            if (pelicula.PeliculasActor is not null)
            {
                for (var i = 0; i < pelicula.PeliculasActor.Count; i++)
                {
                    pelicula.PeliculasActor[i].Orden = i + 1;
                }
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<HomePageDTO>> Get()
        {
            var Limite = 6;

            var PeliuclasEnCartelera = await Context.Peliculas.Where(x => x.EnCartelera).Take(Limite).OrderByDescending(x => x.Lanzamiento).ToListAsync();

            var FechaActual = DateTime.Today;

            var ProximosEstrenos = await Context.Peliculas.Where(x => x.Lanzamiento > FechaActual).OrderBy(x => x.Lanzamiento).Take(Limite).ToListAsync();

            var Resultado = new HomePageDTO
            {
                PeliculasEnCartelera = PeliuclasEnCartelera,
                ProximosEstrenos = ProximosEstrenos
            };

            return Resultado;
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<PeliculaVisualizarDTO>> Get(int id)
        {
            var Pelicula = await Context.Peliculas.Where(x => x.Id == id)
                .Include(x => x.GenerosPelicula)
                    .ThenInclude(gp => gp.Genero)
                .Include(pelicula => pelicula.PeliculasActor.OrderBy(pa => pa.Orden))
                    .ThenInclude(pa => pa.Actor)
                    .FirstOrDefaultAsync();

            if(Pelicula is null)
            {
                return NotFound(id);
            }

            var PromedioVoto = 4;
            var VotoUsuaurio = 5;

            var Modelo = new PeliculaVisualizarDTO();
            Modelo.Pelicula = Pelicula;
            Modelo.Generos = Pelicula.GenerosPelicula.Select(x => x.Genero!).ToList();
            Modelo.Actores = Pelicula.PeliculasActor.Select(x => new Actor
            {
                Nombre = x.Actor!.Nombre,
                Foto = x.Actor.Foto,
                Personaje = x.Personaje,
                Id = x.ActorId
            }).ToList();

            Modelo.PromedioVotos = PromedioVoto;
            Modelo.VotoUsuario = VotoUsuaurio;
            return Modelo;
        }

        [HttpGet("actualizar/{id}")]
        public async Task<ActionResult<PeliculaActualizacionDTO>> PutGet(int id)
        {

            var PeliculaActionResult = await Get(id);

            if (PeliculaActionResult.Result is NotFoundResult)
            {
                return NotFound();
            }

            var PeliculaVisualizarDTO = PeliculaActionResult.Value;

            var GenerosSeleccionadosIds = PeliculaVisualizarDTO!.Generos.Select(x => x.Id).ToList();

            var GenerosNoSeleccionados = await Context.Generos.Where(x => !GenerosSeleccionadosIds.Contains(x.Id)).ToListAsync();

            var Modelo = new PeliculaActualizacionDTO();
            Modelo.Pelicula = PeliculaVisualizarDTO.Pelicula;
            Modelo.GenerosNoSeleccionados = GenerosNoSeleccionados;
            Modelo.GenerosSeleccionados = PeliculaVisualizarDTO.Generos;
            Modelo.Actores = PeliculaVisualizarDTO.Actores;
            return Modelo;
        }

        [HttpPut]
        public async Task<ActionResult> Put(Pelicula pelicula)
        {
            var PeliculaDB = await Context.Peliculas
                .Include(x => x.GenerosPelicula)
                .Include(x => x.PeliculasActor)
                .FirstOrDefaultAsync(x => x.Id == pelicula.Id);
        
            if(PeliculaDB is null)
            {
                return NotFound();
            }

            PeliculaDB = Mapper.Map(pelicula, PeliculaDB);

            if (!string.IsNullOrWhiteSpace(pelicula.Poster))
            {
                var PosterImagen = Convert.FromBase64String(pelicula.Poster);
                PeliculaDB.Poster = await AlmacenadorArchivos.EditarArchivo(PosterImagen, ".jpg", Contenedor, PeliculaDB.Poster!);
            }

            EscribirOrdenActores(PeliculaDB);

            await Context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var Pelicula = await Context.Peliculas.FirstOrDefaultAsync(x => x.Id == id);

            if (Pelicula is null)
            {
                return NotFound();
            }

            Context.Remove(Pelicula);
            await Context.SaveChangesAsync();
            await AlmacenadorArchivos.EliminarArchivo(Pelicula.Poster!, Contenedor);
            return NoContent();
        }
          
        [HttpGet("filtrar")]
        [AllowAnonymous]

        public async Task<ActionResult<List<Pelicula>>> Get([FromQuery] ParametrosBusquedaPeliculasDTO modelo)
        {
            var PeliculaQueryable = Context.Peliculas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(modelo.Titulo))
            {
                PeliculaQueryable = PeliculaQueryable.Where(x => x.Titulo.Contains(modelo.Titulo));
            }

            if (modelo.EnCartelera)
            {
                PeliculaQueryable = PeliculaQueryable.Where(x => x.EnCartelera);
            }

            if (modelo.Estrenos)
            {
                var Hoy = DateTime.Today;
                PeliculaQueryable = PeliculaQueryable.Where(x => x.Lanzamiento >= Hoy);
            }

            if (modelo.GeneroId != 0)
            {
                PeliculaQueryable = PeliculaQueryable
                    .Where(x => x.GenerosPelicula
                    .Select(y => y.GeneroId)
                    .Contains(modelo.GeneroId));
            }

            await HttpContext.InsertarParametrosPaginacionEnRespuesta(PeliculaQueryable, modelo.CantidadRegistros);

            var Peliculas = await PeliculaQueryable.Paginar(modelo.PaginacionDTO).ToListAsync();
            return Peliculas;
        }

    }

}
