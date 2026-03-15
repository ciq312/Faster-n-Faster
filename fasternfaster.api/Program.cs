var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(); // NSwag generates the spec

var app = builder.Build();

app.UseOpenApi(); // serves /swagger/v1/swagger.json
app.UseSwaggerUi(); // serves /swagger/index.html

app.MapControllers();

app.Run();
