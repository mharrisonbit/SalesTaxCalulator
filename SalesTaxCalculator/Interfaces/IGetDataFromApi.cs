using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SalesTaxCalculator.Interfaces
{
    public interface IGetDataFromApi
    {
        /// <summary>
        /// This is going to get the tax rate for a users location and return it to the calling method.
        /// </summary>
        /// <param name="callData"></param>
        /// <returns>json string</returns>
        Task<string> GetTaxRatesFromApi(JObject callData);

        /// <summary>
        /// This is going to get the tax for an order and return it to the calling method.
        /// </summary>
        /// <param name="orderInfo"></param>
        /// <returns>json string</returns>
        Task<string> GetTaxForOrderFromApi(JObject orderInfo);
    }
}
