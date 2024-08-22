using Backend_guichet_unique.Models.DTO;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Backend_guichet_unique.Services
{
	public class AuthService
	{
		private readonly IConfiguration _configuration;

		public AuthService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public string GenerateJwtToken(UtilisateurDTO user)
		{
			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


			var claims = new[]
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Nom + " " + user.Prenom),
				new Claim("utilisateur", JsonSerializer.Serialize(user)),
				new Claim(ClaimTypes.Role, user.Profil.Nom),
				new Claim("profil", user.Profil.Nom),
				new Claim("idutilisateur", user.Id.ToString()),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};

			var token = new JwtSecurityToken(
			issuer: _configuration["Jwt:Issuer"],
			audience: _configuration["Jwt:Audience"],
			claims: claims,
			expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])),
			signingCredentials: credentials);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
