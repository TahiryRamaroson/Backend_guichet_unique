using Backend_guichet_unique.Models;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;
using AutoMapper;

namespace Backend_guichet_unique.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly AuthService _authService;
		private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;

		public AuthController(AuthService authService, GuichetUniqueContext context, IMapper mapper)
		{
			_authService = authService;
			_context = context;
			_mapper = mapper;
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginDTO auth)
		{
			var hashedPassword = GetHashSha256(auth.MotDePasse);
			var user = await _context.Utilisateurs
				.Include(u => u.IdProfilNavigation)
				.FirstOrDefaultAsync(u => u.Email == auth.Email && u.MotDePasse == hashedPassword && u.Statut == 5);

			if (auth == null || user == null)
			{
				return Unauthorized();
			}

			var userDto = _mapper.Map<UtilisateurDTO>(user);

			var token = _authService.GenerateJwtToken(userDto);
			return Ok(new { Token = token });
		}

		public static string GetHashSha256(string text)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			SHA256Managed hashstring = new SHA256Managed();
			byte[] hash = hashstring.ComputeHash(bytes);
			string hashString = string.Empty;
			foreach (byte x in hash)
			{
				hashString += String.Format("{0:x2}", x);
			}
			return hashString;
		}
	}
}
