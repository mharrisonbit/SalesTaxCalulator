using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SalesTaxCalculator.Implementations;

namespace SalesTaxCalculator.Tests
{
    [TestFixture]
    public class GetTaxForOrderFromApi_ShouldReturnString
    {
        private JObject passingTaxRateObject;
        private JObject failingTaxRateObject;

        [SetUp]
        public void Setup()
        {
            Xamarin.Forms.Mocks.MockForms.Init();

            passingTaxRateObject = new JObject
            {
                { "from_street", "442 Mullett Br" },
                { "from_city", "Inez" },
                { "from_state", "KY"},
                { "from_zip", "41224"},
                {"from_country", "US"},
                { "items", new JObject
                    {
                        { "id", 3 },
                        { "quantity", 1},
                        { "unit_price", 100},
                        { "product_tax_code", "40030" }
                    }
                },
                { "to_street", "216 Shamrock Ln" },
                { "to_city", "Richmond"},
                { "to_state", "KY"},
                { "to_zip", "40475"},
                { "to_country", "US"},
                { "amount", 10},
                { "shipping", 10 }
            };

            failingTaxRateObject = new JObject
            {
                { "from_street", "442 Mullett Br" },
                { "from_city", "Inez" },
                { "from_state", "KY"},
                { "from_zip", "41224"},
                {"from_country", "US"},
                { "items", new JObject
                    {
                        { "id", 3 },
                        { "quantity", 1},
                        { "unit_price", 100},
                        { "product_tax_code", "40030" }
                    }
                },
                { "to_street", "216 Shamrock Ln" },
                { "to_city", "Richmond"},
                { "to_state", "KY"},
                { "to_zip", ""},
                { "to_country", "US"},
                { "amount", 10},
                { "shipping", 10 }
            };
        }

        [Test]
        public async Task PassingTest()
        {
            var classSetup = new RestCalls();
            var taxRateStringReturned = await classSetup.GetTaxForOrderFromApi(passingTaxRateObject);
            taxRateStringReturned.Should().NotBe("");
        }

        [Test]
        public async Task FailingTest()
        {
            var classSetup = new RestCalls();
            var taxRateStringReturned = await classSetup.GetTaxForOrderFromApi(failingTaxRateObject);
            taxRateStringReturned.Should().Be("");
        }
    }
}