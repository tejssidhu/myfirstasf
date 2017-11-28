using System;
using Newtonsoft.Json;

namespace Ecommerce.API.Models
{
    public class ApiBasketAddRequest
    {
        [JsonProperty("productId")]
        public Guid ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}