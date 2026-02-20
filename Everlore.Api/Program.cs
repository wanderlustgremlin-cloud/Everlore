using Everlore.Infrastructure.Postgres;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var catalogConnectionString = builder.Configuration.GetConnectionString("everloredb")
    ?? throw new InvalidOperationException("Connection string 'everloredb' not found.");

builder.Services.AddPostgresInfrastructure(catalogConnectionString);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
