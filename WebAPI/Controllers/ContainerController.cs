using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class ContainerController : ApiController
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["SomiodDatabase"].ConnectionString;

    }
}
