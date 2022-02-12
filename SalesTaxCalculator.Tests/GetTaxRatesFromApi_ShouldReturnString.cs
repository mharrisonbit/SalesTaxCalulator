using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SalesTaxCalculator.Implementations;
using SalesTaxCalculator.Interfaces;

namespace SalesTaxCalculator.Tests
{
    [TestFixture]
    public class GetTaxRatesFromApi_ShouldReturnString
    {
        private JObject passingTaxRateObject;
        private JObject failingTaxRateObject;

        public Mock<IGetDataFromApi> GetDataMock { get; private set; }

        [SetUp]
        public void Setup()
        {
            passingTaxRateObject = new JObject
            {
                { "country", "US" },
                { "zipcode", "41224" },
                { "city", "Inez" }
            };

            failingTaxRateObject = new JObject
            {
                { "country", "US" },
                { "zipcode", "" },
                { "city", "Inez" }
            };
        }

        [Test]
        public async Task PassingTest()
        {
            var classSetup = new RestCalls();
            var taxRateStringReturned = await classSetup.GetTaxRatesFromApi(passingTaxRateObject);
            taxRateStringReturned.Should().NotBe("");
        }

        [Test]
        public async Task FailingTest()
        {
            var classSetup = new RestCalls();
            var taxRateStringReturned = await classSetup.GetTaxRatesFromApi(failingTaxRateObject);
            taxRateStringReturned.Should().Be("");
        }
    }
}