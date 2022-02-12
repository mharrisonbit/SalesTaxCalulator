using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Navigation;
using SalesTaxCalculator.Interfaces;
using SalesTaxCalculator.Models;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace SalesTaxCalculator.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private INavigationService navigationService;
        private IGetDataFromApi callApi;
        private IGeolocation geoLocation;
        private IPermissions permissions;
        private IGeocoding geocoding;

        public DelegateCommand FindTaxRateBtn { get; private set; }
        public DelegateCommand FigureTaxForOrderBtn { get; private set; }

        public MainPageViewModel(INavigationService navigationService, IGetDataFromApi callApi, IGeolocation geoLocation, IPermissions permissions, IGeocoding geocoding) : base(navigationService)
        {
            this.navigationService = navigationService;
            this.callApi = callApi;
            this.geoLocation = geoLocation;
            this.permissions = permissions;
            this.geocoding = geocoding;

            userLocation = new Location();
            ReceieversPlaceMark = new Placemark();
            ShippingTaxes = new OrderTaxes();

            FindTaxRateBtn = new DelegateCommand(async () => await FindTaxRateCmd()).ObservesCanExecute(() => IsEnabled);
            FigureTaxForOrderBtn = new DelegateCommand(async () => await FigureTaxForOrderCmd()).ObservesCanExecute(() => IsEnabled);
        }

        private Location userLocation;
        private IEnumerable<Placemark> tempLocations;

        OrderTaxes _ShippingTaxes;
        public OrderTaxes ShippingTaxes
        {
            get { return _ShippingTaxes; }
            set { SetProperty(ref _ShippingTaxes, value); }
        }

        Placemark _Location;
        public Placemark Location
        {
            get { return _Location; }
            set { SetProperty(ref _Location, value); }
        }

        Placemark _ReceieversPlaceMark;
        public Placemark ReceieversPlaceMark
        {
            get { return _ReceieversPlaceMark; }
            set { SetProperty(ref _ReceieversPlaceMark, value); }
        }

        string _StreetAddressTxt;
        public string StreetAddressTxt
        {
            get { return _StreetAddressTxt; }
            set { SetProperty(ref _StreetAddressTxt, value); }
        }

        string _RecieverStreetAddressTxt;
        public string RecieverStreetAddressTxt
        {
            get { return _RecieverStreetAddressTxt; }
            set { SetProperty(ref _RecieverStreetAddressTxt, value); }
        }

        string _TotalTxt;
        public string TotalTxt
        {
            get { return _TotalTxt; }
            set { SetProperty(ref _TotalTxt, value); }
        }

        string _ShippingCostTxt;
        public string ShippingCostTxt
        {
            get { return _ShippingCostTxt; }
            set { SetProperty(ref _ShippingCostTxt, value); }
        }

        string _ItemCostTxt;
        public string ItemCostTxt
        {
            get { return _ItemCostTxt; }
            set { SetProperty(ref _ItemCostTxt, value); }
        }

        private async Task FindTaxRateCmd()
        {
            IsBusy = true;
            IsEnabled = false;
            try
            {
                if (userLocation == null)
                {
                    tempLocations = await this.geocoding.GetPlacemarksAsync(userLocation.Latitude, userLocation.Longitude);
                    Location = tempLocations.FirstOrDefault();
                }

                if (Location.CountryName == "United States")
                {
                    Location.CountryName = "US";
                }
                JObject jObject = new JObject
                {
                    { "country", Location.CountryName },
                    { "zipcode", Location.PostalCode },
                    { "city", Location.Locality }
                };

                var answer = await this.callApi.GetTaxRatesFromApi(jObject);
                var taxRate = TaxRate.FromJson(answer);
                TotalTxt = taxRate.Rate.CombinedRate;
                //TODO this is wher the output will be done now that the info has been returned. If there is anything returned.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"                MainViewModel:FigureTaxBtnCmd {ex.Message}");
            }
            IsBusy = false;
            IsEnabled = true;
        }

        private async Task FigureTaxForOrderCmd()
        {
            IsBusy = true;
            IsEnabled = false;
            //TODO use the buyers address to get tax buy calling the GetTaxRatesFromApi() and add to total
            try
            {
                ReceieversPlaceMark.CountryName = "US";

                if (Location.CountryName == "United States")
                {
                    Location.CountryName = "US";
                }
                //TODO convert the shipping to float
                float.TryParse(ShippingCostTxt, out var shippingAmountFlt);
                float.TryParse(ItemCostTxt, out var totalAmountFlt);
                ReceieversPlaceMark.AdminArea = GetStateAbrevation(ReceieversPlaceMark.AdminArea);
                JObject orderInfo = new JObject
                {
                    { "from_street", StreetAddressTxt },
                    { "from_city", Location.Locality },
                    { "from_state", Location.AdminArea},
                    { "from_zip", Location.PostalCode},
                    {"from_country", Location.CountryName},
                    { "items", new JObject
                        {
                            { "id", 3 },
                            { "quantity", 1},
                            { "unit_price", totalAmountFlt},
                            { "product_tax_code", "40030" }
                        }
                    },
                    { "to_street", RecieverStreetAddressTxt },
                    { "to_city", ReceieversPlaceMark.Locality },
                    { "to_state", ReceieversPlaceMark.AdminArea },
                    { "to_zip", ReceieversPlaceMark.PostalCode },
                    { "to_country", ReceieversPlaceMark.CountryName },
                    { "amount", totalAmountFlt },
                    { "shipping", shippingAmountFlt }
                };


                var answer = await this.callApi.GetTaxForOrderFromApi(orderInfo);
                ShippingTaxes = OrderTaxes.FromJson(answer);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"                MainViewModel:FigureTaxForOrderBtnCmd {ex.Message}");
            }
            IsBusy = false;
            IsEnabled = true;
        }

        private async Task<Location> GetLocationAsync()
        {
            try
            {
                IsEnabled = false;
                IsBusy = true;
                var status = await this.permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    var updatedStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (updatedStatus == PermissionStatus.Granted)
                    {
                        userLocation = await this.geoLocation.GetLocationAsync();
                    }
                    else
                    {
                        //returns a blank Location, if the permission has not been granted.
                        var answer = new Location();
                        return answer;
                    }
                }
                else
                {
                    await Task.Delay(500);
                    userLocation = await geoLocation.GetLocationAsync();
                }

                tempLocations = await this.geocoding.GetPlacemarksAsync(userLocation.Latitude, userLocation.Longitude);
                Location = tempLocations.FirstOrDefault();

                Location.AdminArea = GetStateAbrevation(Location.AdminArea);

                IsEnabled = true;
                IsBusy = false;
                StreetAddressTxt = $"{Location.SubThoroughfare} {Location.Thoroughfare}";

                return userLocation;
            }
            catch (Exception ex)
            {
                IsEnabled = true;
                IsBusy = false;
                Console.WriteLine(ex.Message);
                var answer = new Location();
                return answer;
            }
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            GetLocationAsync();
        }

        private string GetStateAbrevation(string state)
        {
            string stateAbrv = state switch
            {
                "Alabama" => "AL",
                "Alaska" => "AK",
                "Arizona" => "AZ",
                "Arkansas" => "AR",
                "California" => "CA",
                "Colorado" => "CO",
                "Connecticut" => "CT",
                "Delaware" => "DE",
                "Florida" => "FL",
                "Georgia" => "GA",
                "Hawaii" => "HI",
                "Idaho" => "ID",
                "Illinois" => "IL",
                "Indiana" => "IN",
                "Iowa" => "IA",
                "Kansas" => "KS",
                "Kentucky" => "KY",
                "Louisiana" => "LA",
                "Maine" => "ME",
                "Maryland" => "MD",
                "Massachusetts" => "MA",
                "Michigan" => "MI",
                "Minnesota" => "MN",
                "Mississippi" => "MS",
                "Missouri" => "MO",
                "Montana" => "MT",
                "Nebraska" => "NE",
                "Nevada" => "NV",
                "Hampshire" => "NH",
                "New Jersey" => "NJ",
                "New Mexico" => "NM",
                "New York" => "NY",
                "North Carolina" => "NC",
                "North Dakota" => "ND",
                "Ohio" => "OH",
                "Oklahoma" => "OK",
                "Pennsylvania" => "PA",
                "Oregon" => "OR",
                "Rhode Island" => "RI",
                "South Carolina" => "SC",
                "South Dakota" => "SD",
                "Tennessee" => "TN",
                "Utah" => "UT",
                "Vermont" => "VT",
                "Virginia" => "VA",
                "Washington" => "WA",
                "West Virginia" => "WV",
                "Wisconsin" => "WI",
                "Wyoming" => "WY",
                "Texas" => "TX",
                "District of Columbia" => "DC",
                _ => state,
            };
            return stateAbrv;
        }
    }
}
