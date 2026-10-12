using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crud.Controllers
{
    [Route("api/Persondetails")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        // GET: api/Person
        [HttpGet]
        public IEnumerable<string> GetAllPerson()
        {
            return new string[] { "value1", "value2" };
        }
        // GET: api/Person/5
        [HttpGet("{id}")]
        public string GetName(int id)
        {
            return "value";
        }
        // POST: api/Person
        [HttpPost]
        public string   AddName(string name)
        {
            return name;
        }
        // PUT: api/Person/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }
        // DELETE: api/Person/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {

        }
    }
}
