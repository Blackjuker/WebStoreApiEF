using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebStoreApiEF.Models;
using WebStoreApiEF.Services;

namespace WebStoreApiEF.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public ProductsController(ApplicationDbContext context) 
        {
            this.context = context;
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = context.Products.ToList();

            return Ok(products);
        }

        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = context.Products.Find(id);

            if(product == null)
            {
                NotFound();
            }

            return Ok(product);
        }

        [HttpPost]
        public IActionResult CreateProduct(ProductDto productDto)
        {

        }
    }
}
