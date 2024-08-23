using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Padrao.APi.Controllers;
using Padrao.Domain.Interfaces;
using Padrao.Service.Interface;
using System;
using System.Threading;

namespace Padrao.Api.Controllers
{
   
    public class ExamplesController(IResponse response, IExamplesService examplesService) : BaseController(response)
    {
        private readonly IExamplesService _examplesService = examplesService;
        [HttpGet]
        [EnableRateLimiting("ip")]
        [Route("rate-limiting")]
        public IActionResult RateLimitingIP()
        {
            Thread.Sleep(10000);
            return JsonResponse();
        }

        [HttpPost]
        [EnableRateLimiting("concurrency")]
        [Route("rate-limiting-post")]
        public IActionResult ReteLimitingPost()
        {
            Thread.Sleep(10000);
            return JsonResponse();
        }

        [HttpGet]
        [Route("cache")]
        [AllowAnonymous]
        [OutputCache(Duration = 20)]
        public IActionResult Cache()
        {
            var teste = _examplesService.Cache();
            return JsonResponse(teste);
        }

        [HttpGet]
        [Route("cache-string")]
        [OutputCache(Duration = 30, VaryByQueryKeys = ["key"])]
        [AllowAnonymous]
        public IActionResult Cache(string key)
        {
            return JsonResponse(new { Chave = key, Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
        }
    }
}
