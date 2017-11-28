using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CheckoutService.Model;
using Ecommerce.API.Models;
using Ecommerce.ProductCatalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace Ecommerce.API.Controllers
{
    [Route("api/Checkout")]
    public class CheckoutController : Controller
    {
        private static readonly Random rnd = new Random(DateTime.UtcNow.Second);

        [Route("{userId}")]
        public async Task<ApiCheckoutSummary> Checkout(string userId)
        {
            CheckoutSummary summary = await GetCheckoutService().Checkout(userId);

            return ToApiCheckoutSummary(summary);
        }

        private ICheckoutService GetCheckoutService()
        {
            long key = LongRandom();

            return ServiceProxy.Create<ICheckoutService>(new Uri("fabric:/MyFirstASF/CheckoutService"), new ServicePartitionKey(key));
        }

        private long LongRandom()
        {
            byte[] buf = new byte[8];
            rnd.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);
            return longRand;
        }

        [Route("history/{userId}")]
        public async Task<IEnumerable<ApiCheckoutSummary>> GetHistory(string userId)
        {
            IEnumerable<CheckoutSummary> history = await GetCheckoutService().GetOrderHistory(userId);

            return history.Select(ToApiCheckoutSummary);
        }

        private ApiCheckoutSummary ToApiCheckoutSummary(CheckoutSummary model)
        {
            return new ApiCheckoutSummary
            {
                Products = model.Products.Select(p => new ApiCheckoutProduct
                {
                    ProductId = p.Product.Id,
                    ProductName = p.Product.Name,
                    Price = p.Price,
                    Quantity = p.Quantity
                }).ToList(),
                Date = model.Date,
                TotalPrice = model.TotalPrice
            };
        }
    }
}