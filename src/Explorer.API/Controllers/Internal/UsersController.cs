// src/Explorer.API/Controllers/Internal/UsersController.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.Core.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Internal
{
    [Route("api/internal/users")]
    [ApiController]
    public class InternalUsersController : ControllerBase
    {
        private readonly ICrudRepository<Person> _personRepository;

        public InternalUsersController(ICrudRepository<Person> personRepository)
        {
            _personRepository = personRepository;
        }

        [HttpPost("emails")]
        public ActionResult<Dictionary<long, string>> GetEmailsByPersonIds([FromBody] List<long> personIds)
        {
            try
            {
                var people = _personRepository.GetAll()
                    .Where(p => personIds.Contains(p.Id))
                    .ToList();

                var emailMap = people.ToDictionary(p => p.Id, p => p.Email);
                return Ok(emailMap);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving emails: {ex.Message}");
            }
        }

        [HttpGet("{personId:long}/email")]
        public ActionResult<string> GetPersonEmail(long personId)
        {
            try
            {
                var person = _personRepository.Get(personId);
                if (person == null)
                {
                    return NotFound("Person not found");
                }

                return Ok(person.Email);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving email: {ex.Message}");
            }
        }
    }
}