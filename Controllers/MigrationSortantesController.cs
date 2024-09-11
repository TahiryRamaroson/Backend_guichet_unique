using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_guichet_unique.Models;
using Backend_guichet_unique.Models.DTO;
using AutoMapper;

namespace Backend_guichet_unique.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MigrationSortantesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
        private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public MigrationSortantesController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<MigrationSortanteDTO>>> GetPagedMigrationSortantes(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.MigrationSortantes.Where(m => m.Statut != -5).CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var migrationSortantes = await _context.MigrationSortantes
				.Where(m => m.Statut != -5)
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
					.ThenInclude(m => m.IdMenageNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdFokontanyDestinationNavigation)
				.OrderByDescending(n => n.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var migrationSortantesDto = _mapper.Map<IEnumerable<MigrationSortanteDTO>>(migrationSortantes);
			return Ok(new { MigrationSortante = migrationSortantesDto, TotalPages = totalPages });
		}

		// GET: api/MigrationSortantes
		[HttpGet]
        public async Task<ActionResult<IEnumerable<MigrationSortanteDTO>>> GetMigrationSortantes()
        {
			var migrationSortantes = await _context.MigrationSortantes
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
					.ThenInclude(m => m.IdMenageNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdFokontanyDestinationNavigation)
				.ToListAsync();

			var migrationSortantesDto = _mapper.Map<IEnumerable<MigrationSortanteDTO>>(migrationSortantes);
			return Ok(migrationSortantesDto);
		}

        // GET: api/MigrationSortantes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MigrationSortante>> GetMigrationSortante(int id)
        {
			var migrationSortante = await _context.MigrationSortantes
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
					.ThenInclude(m => m.IdMenageNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdFokontanyDestinationNavigation)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (migrationSortante == null)
            {
                return NotFound();
            }

			var migrationSortanteDto = _mapper.Map<MigrationSortanteDTO>(migrationSortante);
			return Ok(migrationSortanteDto);
        }
        
        [HttpPost]
        public async Task<ActionResult<MigrationSortante>> PostMigrationSortante(MigrationSortanteFormDTO migrationSortanteDto)
        {
			if (migrationSortanteDto.PieceJustificative.Length < 0 || migrationSortanteDto.PieceJustificative == null)
			{
				return Ok(new { error = "La pièce justificative est obligatoire" });
			}

			if (migrationSortanteDto.PieceJustificative.Length > 10485760) // (10 MB)
			{
				return Ok(new { error = "Le fichier est trop volumineux." });
			}

			//var firebaseStorage = await new FirebaseStorage( _configuration["FirebaseStorage:Bucket"])
			//	.Child("migrationSortante")
			//	.Child(migrationSortanteDto.PieceJustificative.FileName)
			//	.PutAsync(migrationSortanteDto.PieceJustificative.OpenReadStream());

			var migrationSortante = _mapper.Map<MigrationSortante>(migrationSortanteDto);

			migrationSortante.PieceJustificative = "-----------";

			_context.MigrationSortantes.Add(migrationSortante);

			//var token = Request.Headers["Authorization"].ToString().Substring(7);
			//var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			//var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			//var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			//var historiqueApplication = new HistoriqueApplication();
			//historiqueApplication.Action = _configuration["Action:Create"];
			//historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			//historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			//historiqueApplication.DateAction = DateTime.Now;
			//historiqueApplication.IdUtilisateur = int.Parse(idu);

			//_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

            return CreatedAtAction("GetMigrationSortante", new { id = migrationSortante.Id }, migrationSortante);
        }

        private bool MigrationSortanteExists(int id)
        {
            return _context.MigrationSortantes.Any(e => e.Id == id);
        }
    }
}
