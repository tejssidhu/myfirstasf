using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ecommerce.API.Models
{
    public class ApiCheckoutSummary
    {
        [JsonProperty("products")]
        public List<ApiCheckoutProduct> Products { get; set; }

        [JsonProperty("totalPrice")]
        public double TotalPrice { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}
