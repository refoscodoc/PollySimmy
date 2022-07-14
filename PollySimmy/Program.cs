using System.Net.Sockets;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PollySimmy.DataAccess;
using MediatR;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Outcomes;

var builder = WebApplication.CreateBuilder(args);

AsyncInjectOutcomePolicy<HttpResponseMessage> faultPolicy = MonkeyPolicy.InjectFaultAsync<HttpResponseMessage>(
    new HttpRequestException("Simmy threw an exception"), 
    injectionRate: .25,
    enabled: () => true
);

builder.Services.AddSingleton(faultPolicy);
//builder.Services.AddSingleton<SocketException>();
builder.Services.AddHttpClient();
builder.Services.AddMediatR(Assembly.GetExecutingAssembly());

var sqlConnectionString = builder.Configuration.GetConnectionString("DataAccessMySqlProvider");
var serverVersion = new MySqlServerVersion(new Version(10, 8, 3));

builder.Services.AddDbContext<DataContext>(options =>
    options.UseMySql(sqlConnectionString, serverVersion));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();