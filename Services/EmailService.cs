using MimeKit;
using MailKit.Net.Smtp;

namespace Backend_guichet_unique.Services
{
	public class EmailService
	{
		private readonly IConfiguration _configuration;

		public EmailService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task SendEmailAsync(string toEmail, string subject, string body)
		{
			var email = new MimeMessage();
			email.From.Add(new MailboxAddress(_configuration["SmtpSettings:SenderName"], _configuration["SmtpSettings:SenderEmail"]));
			email.To.Add(new MailboxAddress("", toEmail));
			email.Subject = subject;

			var builder = new BodyBuilder { HtmlBody = body };
			email.Body = builder.ToMessageBody();

			using var smtp = new SmtpClient();
			await smtp.ConnectAsync(_configuration["SmtpSettings:Server"], int.Parse(_configuration["SmtpSettings:Port"]), MailKit.Security.SecureSocketOptions.StartTls);
			await smtp.AuthenticateAsync(_configuration["SmtpSettings:Username"], _configuration["SmtpSettings:Password"]);
			await smtp.SendAsync(email);
			await smtp.DisconnectAsync(true);
		}
	}
}
