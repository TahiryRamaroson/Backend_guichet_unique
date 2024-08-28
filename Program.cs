using Backend_guichet_unique.Models;
using Backend_guichet_unique.Services;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowSpecificOrigin",
	policy =>
	{
		policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod();
		policy.WithOrigins("http://localhost:5174").AllowAnyHeader().AllowAnyMethod();
	});
});

var cultureInfo = new CultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Add services to the container.

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["Jwt:Issuer"],
		ValidAudience = builder.Configuration["Jwt:Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
	};
});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdministrateurPolicy", policy => policy.RequireRole("Administrateur"));
	options.AddPolicy("IntervenantPolicy", policy => policy.RequireRole("Intervenant sociaux"));
	options.AddPolicy("ResponsablePolicy", policy => policy.RequireRole("Responsable guichet unique"));
});

builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "GU_API", Version = "v1" });

	// Ajouter la configuration pour le token JWT
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "Veuillez entrer le token JWT avec le mot clé 'Bearer' suivi d'un espace et du token. Exemple : 'Bearer {token}'",
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer"
	});

	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = "Bearer"
					}
				},
			new string[] {}
		}
	});
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<GuichetUniqueContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddScoped<AuthService>();
builder.Services.AddAutoMapper(typeof(ProfilMapper));
builder.Services.AddAutoMapper(typeof(UtilisateurMapper));
builder.Services.AddAutoMapper(typeof(RegionMapper));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigin");

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
