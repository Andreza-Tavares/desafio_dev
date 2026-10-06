var builder = WebApplication.CreateBuilder(args);

// Força a porta fixa 5055
builder.WebHost.UseUrls("http://localhost:5055");

// Configura o CORS para permitir requisições de arquivos locais ou testadores
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Adiciona os controllers
builder.Services.AddControllers();

var app = builder.Build();

// Ativa o CORS antes das rotas
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();