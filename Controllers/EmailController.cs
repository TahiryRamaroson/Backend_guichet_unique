using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;
using Firebase.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Backend_guichet_unique.Services;

namespace Backend_guichet_unique.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class EmailController : ControllerBase
	{

		private readonly EmailService _emailService;

		public EmailController(EmailService emailService)
		{
			_emailService = emailService;
		}

		[HttpPost("notifier")]
		public async Task<ActionResult> NotifEmail(EmailFormDTO emailDto)
		{

			string toEmail = "taxramaroson2@gmail.com";
			string subject = emailDto.Objet;
			string body = emailDto.Corps;
			await _emailService.SendEmailAsync(toEmail, subject, body);


			return Ok(new {status = "200"});
		}
	}
}
