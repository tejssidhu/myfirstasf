using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckoutService.Model;
using Ecommerce.ProductCatalog;
using Ecommerce.ProductCatalog.Model;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using UserActor.Interfaces;

namespace CheckoutService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class CheckoutService : StatefulService, ICheckoutService
    {
        public CheckoutService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[] {
                new ServiceReplicaListener(context => this.CreateServiceRemotingListener(context))
            };
        }
        
        public async Task<CheckoutSummary> Checkout(string userId)
        {
            var result = new CheckoutSummary();
            result.Date = DateTime.UtcNow;
            result.Products = new List<CheckoutProduct>();

            IUserActor userActor = GetUserActor(userId);
            Dictionary<Guid, int> basket = await userActor.GetBasket();

            IProductCatalogService catalogService = GetProductCatalogService();

            foreach (KeyValuePair<Guid, int> basketLine in basket)
            {
                Product product = await catalogService.GetProduct(basketLine.Key);
                var checkoutProduct = new CheckoutProduct
                {
                    Product = product,
                    Price = product.Price,
                    Quantity = basketLine.Value
                };
                result.Products.Add(checkoutProduct);
            }

            result.TotalPrice = result.Products.Sum(p => p.Price);

            await userActor.ClearBasket();

            await AddToHistory(result);

            return result;
        }

        private async Task AddToHistory(CheckoutSummary checkout)
        {
            var history = await StateManager.GetOrAddAsync<IReliableDictionary<DateTime, CheckoutSummary>>("history");

            using (var tx = StateManager.CreateTransaction())
            {
                await history.AddAsync(tx, checkout.Date, checkout);

                await tx.CommitAsync();
            }
        }

        private IProductCatalogService GetProductCatalogService()
        {
            return ServiceProxy.Create<IProductCatalogService>(new Uri("fabric:/MyFirstASF/Ecommerce.ProductCatalog"), new ServicePartitionKey(0));
        }

        public async Task<IEnumerable<CheckoutSummary>> GetOrderHistory(string userId)
        {
            var result = new List<CheckoutSummary>();
            var history = await StateManager.GetOrAddAsync<IReliableDictionary<DateTime, CheckoutSummary>>("history");

            using (var tx = StateManager.CreateTransaction())
            {
                var allProducts = await history.CreateEnumerableAsync(tx, EnumerationMode.Unordered);
                using (var enumerator = allProducts.GetAsyncEnumerator())
                {
                    while (await enumerator.MoveNextAsync(CancellationToken.None))
                    {
                        KeyValuePair<DateTime, CheckoutSummary> current = enumerator.Current;

                        result.Add(current.Value);
                    }
                }
            }

            return result;
        }

        private IUserActor GetUserActor(string userId)
        {
            return ActorProxy.Create<IUserActor>(new ActorId(userId), new Uri("fabric:/MyFirstASF/UserActorService"));
        }
    }
}
