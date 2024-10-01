using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;
using Firebase.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Backend_guichet_unique.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend_guichet_unique.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class EmailController : ControllerBase
	{

		private readonly EmailService _emailService;
		private readonly GuichetUniqueContext _context;

		public EmailController(EmailService emailService, GuichetUniqueContext context)
		{
			_emailService = emailService;
			_context = context;
		}

		[HttpPost("notifier")]
		public async Task<ActionResult> NotifEmail(EmailFormDTO emailDto)
		{
			var responsables = await _context.Utilisateurs
				.Where(u => u.IdProfil == 3 && u.Statut == 5)
				.ToListAsync();

			try
			{
				foreach (var responsable in responsables)
				{
					string toEmail = responsable.Email;
					string subject = emailDto.Objet;
					string body = emailDto.Corps;
					await _emailService.SendEmailAsync(toEmail, subject, body);
				}
			}
			catch (Exception e)
			{
				return Ok(new { error = e.Message });
			}


			return Ok(new {status = "200"});
		}
	}
}
