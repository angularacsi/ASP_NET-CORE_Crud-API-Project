using Crud.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Crud.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Crud.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PeopleEfController : ControllerBase
    {
        private readonly APiDbContext _dbContext;

        public PeopleEfController(APiDbContext dbContext)
        {
            _dbContext = dbContext;

        }
        [HttpGet]
        public async Task<IActionResult> GetAllpeople()
        {
            var people = await _dbContext.People.ToListAsync();
            return Ok(people);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPeopleById(int id)
        {
            var people = await _dbContext.People.FindAsync(id);
            if (people == null)
            {
                return NotFound();
            }
            return Ok(people);
        }

        //use stored procedure for get 
        [HttpGet("SP_GET")]
        public async Task<IActionResult> GetPeopleByID(int id)
        {
            var people = await _dbContext.People.FromSqlInterpolated($"EXEC P_Getpeople @Id={id}").ToListAsync();
            if (people == null || people.Count == 0)
            {
                return NotFound();
            }
            return Ok(people);
        }



        [HttpPost]

        public async Task<IActionResult> AddPerson(People people)
        {
            EntityEntry<People> entityEntry = await _dbContext.People.AddAsync(people);
            await _dbContext.SaveChangesAsync();
            return Ok(entityEntry.Entity);

        }
        [HttpPost("SP_ADD")]
        public async Task<IActionResult> AddPersonSP(People people)
        {
            var result = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"EXEC Addpeople @Name={people.Name}, @email={people.email}, @address={people.address}");
            if (result > 0)
            {
                return Ok("Person added successfully using stored procedure.");
            }
            else
            {
                return BadRequest("Failed to add person using stored procedure.");
            }
        }

        //update data 
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePerson(int id, People people)
        {
            var existingPerson = await _dbContext.People.FindAsync(id);
            if (existingPerson == null)
            {
                return NotFound();
            }
            existingPerson.Name = people.Name;
            existingPerson.email = people.email;
            existingPerson.address = people.address;
            await _dbContext.SaveChangesAsync();
            return Ok(existingPerson);
        }
        //delete data
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerson(int id)
        {
            var existingPerson = await _dbContext.People.FindAsync(id);
            if (existingPerson == null)
            {
                return NotFound();
            }
            _dbContext.People.Remove(existingPerson);
            await _dbContext.SaveChangesAsync();
            return NoContent();



        }

    }
}
