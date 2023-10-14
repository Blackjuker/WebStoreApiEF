using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using WebStoreApiEF.Models;
using WebStoreApiEF.Services;

namespace WebStoreApiEF.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly EmailSender emailSender;

        //// creer une liste d'acceptable subjects
        //private readonly List<string> listSubjects = new List<string>()
        //{
        //    "Order Status","Refund Request","Job Application","Other"
        //};

        public ContactsController(ApplicationDbContext context,EmailSender emailSender)
        {
            this.context = context;
            this.emailSender = emailSender;
        }

        [HttpGet("subjects")]
        public IActionResult GetSubjects()
        {
          //  return Ok(listSubjects);

            var listSubjects = context.Subjects.ToList();
            return Ok(listSubjects);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public IActionResult GetContacts(int? page)
        {
            if(page == null || page <1)
            {
                page = 1;
            }

            int pageSize = 5;
            int totalPages = 0;

            decimal count = context.Contacts.Count();
            totalPages = (int)Math.Ceiling(count / pageSize);

            //update pour paginer avant 
           // var contacts = context.Contacts.Include(c => c.Subject).ToList();
            //update pour paginer après 
            var contacts = context.Contacts
                .Include(c => c.Subject)
                .OrderByDescending(c=>c.Id)
                .Skip((int)(page-1)*pageSize) //Skip un nombre de page
                .Take(pageSize)
                .ToList();

            //object of anymous type

            var response = new
            {
               Contacts = contacts,
               TotalPages = totalPages,
               PageSize = pageSize,
               Page=page
            };

            //if (contacts.Count==0)
            //{
            //    ModelState.AddModelError("Contacts", "Le nombre de page max est " + totalPages.ToString());
            //    return NotFound(ModelState);
            //}
            //else
            //{
                return Ok(response);
           // }

           
        }

        [Authorize(Roles = "admin")]
        [HttpGet("{id}")]
        public IActionResult GetContact(int id) 
        {
            var contact = context.Contacts.Include(c=> c.Subject).FirstOrDefault(c => c.Id == id);

            if (contact == null)
            {
                return NotFound();
            }
            return Ok(contact);
        }


        [HttpPost]
        public IActionResult CreateContact(ContactDto contactDto)
        {
            // if (!listSubjects.Contains(contactDto.Subject))
            var subject = context.Subjects.Find(contactDto.SubjectId);
            if(subject == null)
            {
                ModelState.AddModelError("Subject", "Please select a Valid Subject");
                return BadRequest(ModelState);
            }

            Contact Contact = new Contact()
            {
                FirstName = contactDto.FirstName,
                LastName = contactDto.LastName,
                Email = contactDto.Email,
                Phone = contactDto.Phone ?? "",
                Subject = subject,
                Message = contactDto.Message,
                CreatedAt = DateTime.Now,
            };

            context.Contacts.Add(Contact);
            context.SaveChanges();

            //send confirmation email

            string emailSubject = "Contact Confirmation";
            string username = contactDto.FirstName + " " + contactDto.LastName;
            string emailMessage = "Dear " + username + "\n" +
                "We received your message. Thank you for contacting us.\n" +
                "our team will contact you very soon.\n" +
                "Best Regards\n\n" +
                "Your message:\n"+ contactDto.Message;

          //  emailSender.SendSimpleMessage();

            return Ok(Contact);
        }

        //[HttpPut("{id}")]
        //public IActionResult UpdateContact(int id,ContactDto contactDto)
        //{
        //    var subject = context.Subjects.Find(contactDto.SubjectId);
        //    //if (!listSubjects.Contains(contactDto.Subject))
        //    if (subject==null)
        //    {
        //        ModelState.AddModelError("Subject", "Please select a Valid Subject");
        //        return BadRequest(ModelState);
        //    }

        //    var contact = context.Contacts.Find(id);
        //    if (contact == null)
        //    {
        //        return NotFound();
        //    }

        //    contact.FirstName = contactDto.FirstName;
        //    contact.LastName = contactDto.LastName;
        //    contact.Email = contactDto.Email;
        //    contact.Phone = contactDto.Phone ?? "";
        //    contact.Subject = subject;
        //    contact.Message = contactDto.Message;

        //    context.SaveChanges();


        //    return Ok(contact);
        //}

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteContact(int id)
        {
            //Method 1
            /*
            var contact = context.Contacts.Find(id);
            if(contact == null)
            {
                return NotFound();
            }

            context.Contacts.Remove(contact);
            context.SaveChanges();

            return Ok(); */

            // Method 2
            try
            {
                var contact = new Contact() { Id = id, Subject = new Subject() };
                context.Contacts.Remove(contact);
                context.SaveChanges();
            }catch(Exception)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
