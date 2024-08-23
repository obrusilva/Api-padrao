using Konekti.BD;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Padrao.Api.Configuration;
using Padrao.APi.Configuration;
using Padrao.APi.Filters;
using Padrao.APi.Formatter;
using Padrao.Domain.Interfaces;
using Padrao.Domain.Virtual;
using Padrao.Service.Interface;
using Padrao.Service.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;

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
    options.SupportedCultures = [new("pt-BR")];
});



builder.Services.AddHttpContextAccessor();
builder.Services.AddRateLimitingConfig();



//builder.Services.AddDistributedMemoryCache(); //em memoria
builder.Services.AddStackExchangeRedisOutputCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ApiPadrao";
}); // redis

builder.Services.AddOutputCache();


builder.Services.AddScoped<IResponse, Response>();
builder.Services.AddScoped<DataContext, DataContext>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IUser, UserAuthenticated>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExamplesService, ExamplesService>();
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
app.UseOutputCache();
app.UseEndpoints(endpoints => { _ = endpoints.MapControllers(); });

app.UseResponseCompression();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Padrao"));
var cultureInfo = new CultureInfo("pt-Br");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

app.Run();
