using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebStoreApiEF.Models;
using WebStoreApiEF.Services;

namespace WebStoreApiEF.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;

        private readonly List<string> listCategories = new List<string>()
        {
            "Phones","Computers","Accessories","Printers","Cameras","Other"
        };

        // pour trouver le chemin absolut de wwwroot
        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env) 
        {
            this.context = context;
            this.env = env;
        }

        [HttpGet("Categories")]
        public IActionResult GetCategories()
        {
            return Ok(listCategories);
        }

        [HttpGet]
        public IActionResult GetProducts(string? search,string? searchCategory,int? searchMinPrice,int? searchMaxPrice,
            string? sort,string? order,int? page)
        {
            IQueryable<Product> query = context.Products;

            //search functionality 
            if (search != null)
            {
                query = query.Where(p=>p.Name.Contains(search) || p.Description.Contains(search));
            }

            if(searchCategory!= null)
            {
                query = query.Where(p => p.Category == searchCategory);
            }

            if(searchMinPrice != null)
            {
                query = query.Where(p => p.Price >= searchMinPrice);
            }

            if(searchMaxPrice != null)
            {
                query = query.Where(p=>p.Price <= searchMaxPrice);
            }

            // sort functionality 
            if (sort == null) sort = "id";
            if (order == null || order != "asc") order = "desc";
            
            if(sort.ToLower() == "name")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Name);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Name);
                }
            }
            else if (sort.ToLower() == "brand")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Brand);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Brand);
                }
            }
            else if (sort.ToLower() == "category")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Category);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Category);
                }
            }
            else if (sort.ToLower() == "price")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Price);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Price);
                }
            }
            else if (sort.ToLower() == "date")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }
            }
            else 
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Id);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Id);
                }
            }

            var products = query.ToList();


            // pagination functionality

            if (page == null || page < 1) page = 1;

            int pageSize = 5;
            int totalPages = 0;

            decimal count = query.Count();
            totalPages = (int)Math.Ceiling(count / pageSize);

            query = query.Skip((int)(page - 1) * pageSize)
                .Take(pageSize);

            var response = new
            {
                Products = products,
                TotalPages = totalPages,
                PageSize = pageSize,
                Page = page
            };

            return Ok(response);
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


        [Authorize(Roles = "admin")]
        [HttpPost]
        public IActionResult CreateProduct([FromForm]ProductDto productDto)
        {
            if (!listCategories.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "Please select a valid category");
                return BadRequest(ModelState);
            }

            if(productDto.ImageFile == null)
            {
                ModelState.AddModelError("ImageFile", "The Image File is required");
                return BadRequest(ModelState);
            }

            // save the image on the server
            string imageFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            imageFileName += Path.GetExtension(productDto.ImageFile.FileName);
            string imagesFolder = env.WebRootPath+"/images/products/";

            using (var stream = System.IO.File.Create(imagesFolder+imageFileName))
            {
                productDto.ImageFile.CopyTo(stream);
            }

            //save product in the database
            Product product = new Product()
            {
                Name = productDto.Name,
                Brand = productDto.Brand,
                Category =  productDto.Category,
                Price = productDto.Price,
                Description = productDto.Description ??  "",
                ImageFilename = imageFileName,
                CreatedAt = DateTime.Now
            };
            context.Products.Add(product);

            context.SaveChanges();

            return Ok();
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id,[FromForm]ProductDto productDto)
        {
            if (!listCategories.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "Please select a valid category");
                return BadRequest(ModelState);
            }

            var product = context.Products.Find(id);

            if (product == null)
            {
                //ModelState.AddModelError("Product", "Please select a valid Product");
                //return BadRequest(ModelState);
                return NotFound();
            }

            string imageFileName = product.ImageFilename;

           

            if(productDto.ImageFile != null)
            {
                // To save
                imageFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                imageFileName += Path.GetExtension(productDto.ImageFile.FileName);

                string imagesFolder = env.WebRootPath + "/images/products/";
                using (var stream = System.IO.File.Create(imagesFolder + imageFileName))
                {
                    productDto.ImageFile.CopyTo(stream);
                }

                //Delete the file 

                System.IO.File.Delete(imagesFolder + product.ImageFilename);
            }

            //Update the product in dataDase 

            product.Name = productDto.Name;
            product.Brand = productDto.Brand;
            product.Category = productDto.Category;
            product.Price = productDto.Price;
            product.Description = productDto.Description ?? "";
            product.ImageFilename = imageFileName;

            context.SaveChanges();
            return Ok(product);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var product = context.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            // delete the image on the server
            string imagesFolder = env.WebRootPath + "/images/products/";
            System.IO.File.Delete(imagesFolder + product.ImageFilename);

            //Delete from dataBase
            context.Products.Remove(product);
            context.SaveChanges();

            return Ok();
        }
    }
}
