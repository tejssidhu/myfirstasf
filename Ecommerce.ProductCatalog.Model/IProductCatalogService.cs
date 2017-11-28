using Ecommerce.ProductCatalog.Model;
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.ProductCatalog
{
    public interface IProductCatalogService: IService
    {
        Task<IEnumerable<Product>> GetAllProducts();
        Task AddProduct(Product product);
        Task<Product> GetProduct(Guid id);
    }
}
