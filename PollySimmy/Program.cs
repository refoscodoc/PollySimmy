using System.Net.Sockets;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PollySimmy.DataAccess;
using MediatR;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.Simmy;
using Polly.Contrib.Simmy.Behavior;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;

var builder = WebApplication.CreateBuilder(args);

#region Policies

bool isEnabled = true;

var latencyChaosPolicy = MonkeyPolicy.InjectLatency(with =>
    with.Latency(TimeSpan.FromSeconds(5))
        .InjectionRate(0.35)
        .Enabled(isEnabled)
);

AsyncInjectOutcomePolicy<HttpResponseMessage> faultPolicy = MonkeyPolicy.InjectFaultAsync<HttpResponseMessage>(
    new HttpRequestException("Simmy threw an exception"), 
    injectionRate: .25,
    enabled: () => true
);

// Following example causes policy to execute a method to restart a virtual machine; the probability that method will be executed is 1% if enabled
var chaosPolicy = MonkeyPolicy.InjectBehaviour(with =>
    with.Behaviour( () => Console.WriteLine("Some function restarts the VM here...") )
        .InjectionRate(0.01)
        .EnabledWhen((ctx, ct) =>
        {
            // Some conditions here for checking whether or not it should restart the VM.
            return isEnabled;
        })
);

var basicCircuitBreakerPolicy = Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30), OnBreak, OnReset, OnHalfOpen);

var breaker = Policy
    .Handle<Exception>()
    .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

// ######################

var faultTwo = new Exception("Simmy's exception");
        
var circuitBreakSimmy = MonkeyPolicy.InjectExceptionAsync(with =>
    with.Fault(faultTwo)
        .InjectionRate(0.5)
        .Enabled()
);
        
var chaosLatencyPolicy = MonkeyPolicy.InjectLatencyAsync(with =>
    with.Latency(TimeSpan.FromSeconds(5))
        .InjectionRate(1)
        .Enabled()
);
        
var simmyPolicyWrap = Policy
    .WrapAsync(circuitBreakSimmy, chaosLatencyPolicy);
        
var timeoutPolicy = Policy
    .TimeoutAsync(
        TimeSpan.FromMilliseconds(10), // _settings.TimeoutWhenCallingApi,
        Polly.Timeout.TimeoutStrategy.Pessimistic
    );
var basicCircuitBreaker = Policy.Handle<Exception>()
    .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));
var circuitBreakerWrappingTimeout = basicCircuitBreaker
    .WrapAsync(timeoutPolicy);
        
var wrapperPolicy = Policy
    .Handle<Exception>()
    .FallbackAsync(
        async cancellationToken => { Console.WriteLine("fallback triggered"); })
    .WrapAsync(circuitBreakerWrappingTimeout);

#endregion

#region CircuitBreakerDelegates

void OnHalfOpen()
{
    Console.WriteLine("Circuit in test mode, one request will be allowed.");
}

void OnReset()
{
    Console.WriteLine("Circuit closed, requests flow normally.");
}

void OnBreak(DelegateResult<HttpResponseMessage> result, TimeSpan ts)
{
    Console.WriteLine("Circuit cut, requests will not flow.");
}

#endregion

builder.Services.AddSingleton(faultPolicy);
builder.Services.AddSingleton(wrapperPolicy);

builder.Services.AddHttpClient("BaconService", client =>
{
    client.BaseAddress = new Uri("https://baconipsum.com/api/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddPolicyHandler(basicCircuitBreakerPolicy);

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