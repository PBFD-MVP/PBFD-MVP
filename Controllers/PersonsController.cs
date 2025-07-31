using VisitorLog_PBFD.Data;
using VisitorLog_PBFD.Models;
using Microsoft.AspNetCore.Mvc;

namespace VisitorLog_PBFD.Controllers
{
    public class PersonsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PersonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            int personId = 1;

            var person = _context.Persons.FirstOrDefault(p => p.PersonId == personId);
    
            return View(person);
        }


        [HttpPost]
        public IActionResult Index(Person person)
        {
            // Remove validation for PersonId during model state validation
            ModelState.Remove(nameof(Person.PersonId));

            if (ModelState.IsValid)
            {
                if (person.PersonId > 0)
                {
                    // Update existing person
                    var existingPerson = _context.Persons.FirstOrDefault(p => p.PersonId == person.PersonId);
                    if (existingPerson != null)
                    {
                        existingPerson.FirstName = person.FirstName;
                        existingPerson.MiddleName = person.MiddleName;
                        existingPerson.LastName = person.LastName;
                        existingPerson.Email = person.Email;
                        _context.Update(existingPerson);
                    }
                }
                else
                {
                    // Insert new person
                    _context.Persons.Add(person);
                }

                // Save changes and get the new or updated PersonId
                _context.SaveChanges();

                // Redirect to the Continents page with the correct PersonId
                return RedirectToAction("Index", "Locations", new { personId = person.PersonId, selectedLocationIds = String.Empty });
            }

            // If the model is invalid, redisplay the form
            return View(person);
        }
    }
}
