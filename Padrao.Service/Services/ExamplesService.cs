using Microsoft.AspNetCore.OutputCaching;
using Padrao.Domain.Interfaces;
using Padrao.Service.Interface;
using System;

namespace Padrao.Service.Services
{
    public class ExamplesService : BaseService, IExamplesService
    {
        public ExamplesService(IResponse response) : base(response)
        {

        }
        public dynamic Cache()
        {
            var teste = new { Titulo = "teste", Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            return teste;
        }
    }
}
