using Konekti.BD;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Padrao.APi.Configuration;
using Padrao.APi.Filters;
using Padrao.APi.Formatter;
using Padrao.Domain.Interfaces;
using Padrao.Domain.Virtual;
using Padrao.Service.Interface;
using Padrao.Service.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddCors();
//compressao json 
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
builder.Services.AddControllers(options => {
    options.InputFormatters.Insert(0, new TextPlainInputFormatter());
    options.ReturnHttpNotAcceptable = true;
    options.Filters.Add(new ExceptionFilter());
    options.Filters.Add(new ProducesAttribute("application/json"));
    options.Filters.Add(typeof(ValidateModelStateFilterAttribute));
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

//Desabilitar Model invalido Automatico -- classe efetua a valida��o ValidateModelStateAttribute
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

//config para usar func�oes do identity 
builder.Services.AddIdentityConfiguration(builder.Configuration);

var appTokenSection = builder.Configuration.GetSection("AppToken");
builder.Services.Configure<AppToken>(appTokenSection);

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("pt-BR");
    options.SupportedCultures = new List<CultureInfo> { new CultureInfo("pt-BR") };
});


builder.Services.AddHttpContextAccessor();
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests; //for�a o retorno 429 de too many requests
    opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{httpContext.User.Identity?.Name}_{httpContext.Request.Path}",
            factory: partion =>
            {
                return new FixedWindowRateLimiterOptions { 
                    AutoReplenishment = true,
                    PermitLimit = 120,
                    QueueLimit = 0,
                    Window =TimeSpan.FromSeconds(60)
                    };
            })
    );

    opt.AddPolicy("ip", httpContext =>
       RateLimitPartition.GetFixedWindowLimiter(
           partitionKey: $"{httpContext.Request.Path}_{httpContext.Connection.RemoteIpAddress}",
           factory: partition =>
           {
               return new FixedWindowRateLimiterOptions
               {
                   AutoReplenishment = true,
                   PermitLimit = 1,
                   QueueLimit = 0,
                   Window = TimeSpan.FromSeconds(60)
               };
           }));
    opt.AddPolicy("concurrency", httpContext =>
        RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: httpContext.Request.Path,
            factory: partition =>
            {
                return new ConcurrencyLimiterOptions { 
                    PermitLimit = 1,
                    QueueLimit = 0,
                };
            }));


    opt.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAtfer))
            await context.HttpContext.Response.WriteAsJsonAsync(new ResultJson($"O limite de requisi��es foi atingifo tente novamente daqui {retryAtfer.TotalSeconds} segundos ", null));
        else if (context.Lease.TryGetMetadata(MetadataName.ReasonPhrase, out var reasonPhrase))
            await (context.HttpContext.Response.WriteAsJsonAsync(new ResultJson($"O limite de requisi��es simult�neas foi atingido,tente novamente mais tarde", null), cancellationToken: token));
        else
            await (context.HttpContext.Response.WriteAsJsonAsync(new ResultJson($"o limite de requisi��es foi atingido, tente novamente mais tarde", null), cancellationToken: token));



    };
});


builder.Services.AddScoped<IResponse, Response>();
builder.Services.AddScoped<DataContext, DataContext>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IUser, UserAuthenticated>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSwaggerConfiguration();


var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

app.UseRateLimiter();

app.UseEndpoints(endpoints => { _ = endpoints.MapControllers(); });

app.UseResponseCompression();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Padrao"));
var cultureInfo = new CultureInfo("pt-Br");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

app.Run();
