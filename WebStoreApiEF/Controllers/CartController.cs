using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebStoreApiEF.Models;
using WebStoreApiEF.Services;

namespace WebStoreApiEF.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        public CartController(ApplicationDbContext context)
        {
            Context = context;
        }

        public ApplicationDbContext Context { get; }


        [HttpGet("PaymentMethods")]
        public IActionResult GetPaymentMethods()
        {
            return Ok(OrderHelper.PaymentMethods);
            //bOnjour
        }

        [HttpGet]
        public IActionResult GetCart(string productIdentifiers) 
        {
            CartDto cartDto = new CartDto();
            cartDto.CartItems = new List<CartItemDto>();
            cartDto.SubTotal = 0;
            cartDto.ShippingFee = OrderHelper.ShippingFee;
            cartDto.TotalPrice = 0;

            var productDictionary = OrderHelper.GetProductDictionary(productIdentifiers);

            foreach(var pair in productDictionary)
            {
                int productId = pair.Key;               
                var product = Context.Products.Find(productId);
                if(product == null)
                {
                    continue;
                }

                var cartItemDto = new CartItemDto();

                cartItemDto.product = product;
                cartItemDto.Quantity = pair.Value;

                cartDto.CartItems.Add(cartItemDto);
                cartDto.SubTotal += product.Price * cartItemDto.Quantity;
                cartDto.TotalPrice = cartDto.SubTotal + cartDto.ShippingFee;
            }

            return Ok(cartDto);
        }
    }
}
