using MarcaAi.Backend.Data;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Services;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<TokenService>();
builder.Services.AddHttpClient<WhatsAppService>();
builder.Services.AddTransient<WhatsAppService>();
builder.Services.AddHttpClient<MenuiaService>();

// CORS básico (ajuste conforme seu front)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

// 🔹 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext (usa a ConnectionString "DefaultConnection" do appsettings.json)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AgendamentoService>();

var app = builder.Build();

// 🔹 Swagger (somente no Development, mas pode deixar sempre se quiser)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarcaAi API v1");
        c.RoutePrefix = string.Empty; // 👉 Swagger abre direto em http://localhost:5000
    });
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.MapControllers();

app.Run();
