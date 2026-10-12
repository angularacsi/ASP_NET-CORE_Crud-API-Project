using CrudData.Models;
using CrudData.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crud.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PeopleController : ControllerBase
    {
        private readonly IPersonRepository _personrepo;

        public PeopleController(IPersonRepository personrepo)
        {
           _personrepo = personrepo;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllPeople()
        {
            try
            {
                var people = await _personrepo.GetPeople();
                return Ok(people);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,new ResponseModel 
                { StatusCode=500,Message="Something went wrong"} );
            }
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetPersonById(int id)
        {
            var person = await _personrepo.GetPersonById(id);
            if (person == null)
            {
                return NotFound();
            }
            return Ok(person);
        }
        [HttpPost]
        public async Task<IActionResult> AddPerson(Person person)
        {
            await _personrepo.AddPerson(person);
            return Ok(new ResponseModel { StatusCode = 200, Message = "Person added successfully" });
        }
        [HttpPut]
        public async Task<IActionResult> UpdatePerson(Person person)
        {
            var existingPerson = await _personrepo.GetPersonById(person.Id);
            if (existingPerson == null)
            {
                return NotFound();
            }
            await _personrepo.UpdatePerson(person);
            return Ok(new ResponseModel { StatusCode = 200, Message = "Person updated successfully" });
        }
    }
}
