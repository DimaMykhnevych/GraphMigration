using GraphMigrator.Algorithms.QueryComparison;
using GraphMigrator.Domain.Configuration;
using Neo4j.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IQueryComparisonAlgorithm, QueryComparisonAlgorithm>();

// Configuration Options
builder.Services.Configure<SourceDataSourceConfiguration>(builder.Configuration.GetSection(nameof(SourceDataSourceConfiguration)));
builder.Services.Configure<TargetDataSourceConfiguration>(builder.Configuration.GetSection(nameof(TargetDataSourceConfiguration)));
builder.Services.Configure<TargetDatbaseNames>(builder.Configuration.GetSection(nameof(TargetDatbaseNames)));

var settings = new TargetDataSourceConfiguration();
builder.Configuration.GetSection(nameof(TargetDataSourceConfiguration)).Bind(settings);

// This is to register your Neo4j Driver Object as a singleton
builder.Services.AddSingleton(GraphDatabase.Driver(settings.Connection, AuthTokens.Basic(settings.User, settings.Password)));

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    builder.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
