using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebStoreApiEF.Models;
using WebStoreApiEF.Services;

namespace WebStoreApiEF.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        public OrdersController(ApplicationDbContext context) 
        {
            Context = context;
        }

        public ApplicationDbContext Context { get; }

        [Authorize]
        [HttpGet]
        public IActionResult GetOrders(int? page)
        {
            int userId = JwtReader.GetUserId(User);
            string role = Context.Users.Find(userId)?.Role ?? ""; //JwtReader.GetUserRole(User);

            IQueryable<Order> query = Context.Orders
                .Include(o => o.user)
                .Include( o => o.OrderItems)
                .ThenInclude(oi => oi.Product);
            if (role != "admin")
            {
                query = query.Where(o => o.UserId == userId); 
            }

            query = query.OrderByDescending(o => o.Id);

            // implement the pagination functionality
            if(page==null || page < 1)
            {
                page = 1;
            }

            int pageSize = 5;
            int totalPages = 0;

            decimal count = query.Count();
            totalPages = (int)Math.Ceiling(count / pageSize);

            query = query.Skip((int)(page - 1) * pageSize)
                .Take(pageSize);


            // read the orders
            var orders = query.ToList();

            foreach (var order in orders)
            {
                //get rid of the object cycle

                foreach (var item in order.OrderItems)
                {
                    item.Order = null;
                }

                order.user.Password = "";
            }

            var response = new
            {
                Orders = orders,
                TotalPages = totalPages,
                PageSize = pageSize,
                Page = page
            };

            return Ok(response);


        }

        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetOrder(int id)
        {
            int userId = JwtReader.GetUserId(User);
            string role = Context.Users.Find(userId)?.Role ?? "";

            Order? order = null;

            if (role == "admin")
            {
                order = Context.Orders
                    .Include( o => o.user)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi=>oi.Product)
                    .FirstOrDefault(o => o.Id == id);
            }
            else
            {
                order = Context.Orders
                   .Include(o => o.user)
                   .Include(o => o.OrderItems)
                   .ThenInclude(oi => oi.Product)
                   .FirstOrDefault(o => o.Id == id && o.UserId == userId); 
            }

            if (order == null)
            {
                return NotFound();
            }


            // get rid of the object cycle
            foreach(var item in order.OrderItems)
            {
                item.Order = null;
            }

            // hide the user password
            order.user.Password = "";
            return Ok(order);
        }

        [Authorize]
        [HttpPost]
        public IActionResult CreateOrder(OrderDto orderDto)
        {
            // check if payment method is valid or not 
            if (!OrderHelper.PaymentMethods.ContainsKey(orderDto.PaymentMethod))
            {
                ModelState.AddModelError("Payment Method", "Please select a valid payment method");
                return BadRequest(ModelState);
            }

            int userId = JwtReader.GetUserId(User);
            var user = Context.Users.Find(userId);

            if (user == null)
            {
                ModelState.AddModelError("Order", "Unable to create the order");
                return BadRequest(ModelState);
            }

            var productDictionary = OrderHelper.GetProductDictionary(orderDto.ProductIdentifiers);

            // create a new order
            Order order = new Order();
            order.UserId = userId;
            order.CreatedAt = DateTime.Now;
            order.ShippingFee = OrderHelper.ShippingFee;
            order.DeliveryAddress = orderDto.DeliveryAddress;
            order.PaymentMethod = orderDto.PaymentMethod;
            order.PaymentStatus = OrderHelper.PaymentStatuses[0]; // pending
            order.OrderStatus = OrderHelper.OrderStatuses[0]; //created


            foreach (var pair in productDictionary)
            {
                var productId = pair.Key;

                var product = Context.Products.Find(productId);

                if(product == null)
                {
                    ModelState.AddModelError("Product", "Product with id " + productId + " is not available");
                    return BadRequest(ModelState);
                }

                var orderItem = new OrderItem();
                orderItem.ProductId = productId;
                orderItem.Quantity = pair.Value;
                orderItem.UnitPrice = product.Price;
                
                order.OrderItems.Add(orderItem);
            }


            if(order.OrderItems.Count < 1)
            {
                ModelState.AddModelError("Order", "Unable to create the order");
                return BadRequest(ModelState); 
            }

            // Save the order on database

            Context.Orders.Add(order);

            Context.SaveChanges();

            foreach(var item in order.OrderItems)
            {
                item.Order = null;
            }

            //hide 
            order.user.Password = "";

            return Ok(order);

        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public IActionResult UpdateOrder(int id, string? paymentStatus, string? orderStatus)
        {
            if (paymentStatus == null && orderStatus == null)
            {
                // we have nothing to do 
                ModelState.AddModelError("Update Order", "There is nothing to update");
                return BadRequest(ModelState);
            }
           
            if (paymentStatus!=null && !OrderHelper.PaymentStatuses.Contains(paymentStatus))
            {
                // the payment status is not valid 
                ModelState.AddModelError("Payment Status", "The Payment Status is not valid");
                return BadRequest(ModelState);
            }

            var order = Context.Orders.Find(id);

            if (order == null)
            {
                return NotFound();
            }

            if (paymentStatus != null)
            {
                order.PaymentStatus = paymentStatus;
            }

            if (orderStatus != null)
            {
                order.OrderStatus = orderStatus;
            }

            Context.SaveChanges();

            return Ok(order);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteOrder(int id)
        {
            var order = Context.Orders.Find(id);
            if (order == null)
            {
                return NotFound();
            }

            Context.Orders.Remove(order);
            Context.SaveChanges();
            return Ok();
        }

    }
}
