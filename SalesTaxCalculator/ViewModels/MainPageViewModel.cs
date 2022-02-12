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

        string _ShippingFeeTxt;
        public string ShippingFeeTxt
        {
            get { return _ShippingFeeTxt; }
            set { SetProperty(ref _ShippingFeeTxt, value); }
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

        private string GetStateAbrevation(string state)
        {
            var stateAbrv = "";
            switch (state)
            {
                case "Alabama":
                    stateAbrv = "AL";
                    break;
                case "Alaska":
                    stateAbrv = "AK";
                    break;
                case "Arizona":
                    stateAbrv = "AZ";
                    break;
                case "Arkansas":
                    stateAbrv = "AR";
                    break;
                case "California":
                    stateAbrv = "CA";
                    break;
                case "Colorado":
                    stateAbrv = "CO";
                    break;
                case "Connecticut":
                    stateAbrv = "CT";
                    break;
                case "Delaware":
                    stateAbrv = "DE";
                    break;
                case "Florida":
                    stateAbrv = "FL";
                    break;
                case "Georgia":
                    stateAbrv = "GA";
                    break;
                case "Hawaii":
                    stateAbrv = "HI";
                    break;
                case "Idaho":
                    stateAbrv = "ID";
                    break;
                case "Illinois":
                    stateAbrv = "IL";
                    break;
                case "Indiana":
                    stateAbrv = "IN";
                    break;
                case "Iowa":
                    stateAbrv = "IA";
                    break;
                case "Kansas":
                    stateAbrv = "KS";
                    break;
                case "Kentucky":
                    stateAbrv = "KY";
                    break;
                case "Louisiana":
                    stateAbrv = "LA";
                    break;
                case "Maine":
                    stateAbrv = "ME";
                    break;
                case "Maryland":
                    stateAbrv = "MD";
                    break;
                case "Massachusetts":
                    stateAbrv = "MA";
                    break;
                case "Michigan":
                    stateAbrv = "MI";
                    break;
                case "Minnesota":
                    stateAbrv = "MN";
                    break;
                case "Mississippi":
                    stateAbrv = "MS";
                    break;
                case "Missouri":
                    stateAbrv = "MO";
                    break;
                case "Montana":
                    stateAbrv = "MT";
                    break;
                case "Nebraska":
                    stateAbrv = "NE";
                    break;
                case "Nevada":
                    stateAbrv = "NV";
                    break;
                case "Hampshire":
                    stateAbrv = "NH";
                    break;
                case "New Jersey":
                    stateAbrv = "NJ";
                    break;
                case "New Mexico":
                    stateAbrv = "NM";
                    break;
                case "New York":
                    stateAbrv = "NY";
                    break;
                case "North Carolina":
                    stateAbrv = "NC";
                    break;
                case "North Dakota":
                    stateAbrv = "ND";
                    break;
                case "Ohio":
                    stateAbrv = "OH";
                    break;
                case "Oklahoma":
                    stateAbrv = "OK";
                    break;
                case "Pennsylvania":
                    stateAbrv = "PA";
                    break;
                case "Oregon":
                    stateAbrv = "OR";
                    break;
                case "Rhode Island":
                    stateAbrv = "RI";
                    break;
                case "South Carolina":
                    stateAbrv = "SC";
                    break;
                case "South Dakota":
                    stateAbrv = "SD";
                    break;
                case "Tennessee":
                    stateAbrv = "TN";
                    break;
                case "Utah":
                    stateAbrv = "UT";
                    break;
                case "Vermont":
                    stateAbrv = "VT";
                    break;
                case "Virginia":
                    stateAbrv = "VA";
                    break;
                case "Washington":
                    stateAbrv = "WA";
                    break;
                case "West Virginia":
                    stateAbrv = "WV";
                    break;
                case "Wisconsin":
                    stateAbrv = "WI";
                    break;
                case "Wyoming":
                    stateAbrv = "WY";
                    break;
                case "Texas":
                    stateAbrv = "TX";
                    break;
                case "District of Columbia":
                    stateAbrv = "DC";
                    break;
                default:
                    stateAbrv = state;
                    break;
            }
            return stateAbrv;
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            GetLocationAsync();
        }
    }
}
