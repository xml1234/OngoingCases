using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetCore_WebApi.Model;

namespace NetCore_WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly MyContext _myContext;
        public ValuesController(MyContext myContext) {
            _myContext = myContext;
        }


        [HttpGet("Get")]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        [HttpGet("GetCityList")]
        public ActionResult<List<City>> GetCityList()
        {
            var list = _myContext.Cities.ToList();
            return list;
        }

        [HttpGet("DelCity")]
        [Authorize("Permission")]
        public int DelCity(int id)
        {

            _myContext.Cities.Remove(_myContext.Cities.Find(id));
            return _myContext.SaveChanges();
        }

        [HttpPost("AddCityEntity")]
        [Authorize]
        public int AddCityEntity(City city)
        {
            _myContext.Cities.Add(city);
            return _myContext.SaveChanges();
        }
    }
}
