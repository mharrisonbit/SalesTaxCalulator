using Prism.Ioc;
using SalesTaxCalculator.Implementations;
using SalesTaxCalculator.Interfaces;
using SalesTaxCalculator.ViewModels;
using SalesTaxCalculator.Views;
using Xamarin.Essentials.Implementation;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace SalesTaxCalculator
{
    public partial class App
    {
        public App()
        {
        }

        protected override async void OnInitialized()
        {
            InitializeComponent();

            var result = await NavigationService.NavigateAsync("MainPage");

            if (!result.Success)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Views and ViewModels
            containerRegistry.RegisterForNavigation<NavigationPage>();
            containerRegistry.RegisterForNavigation<MainPage, MainPageViewModel>();

            //Interfaces and implementations
            containerRegistry.RegisterSingleton<IGetDataFromApi, RestCalls>();
            containerRegistry.RegisterSingleton<IGeolocation, GeolocationImplementation>();
            containerRegistry.RegisterSingleton<IPermissions, PermissionsImplementation>();
            containerRegistry.RegisterSingleton<IGeocoding, GeocodingImplementation>();
        }
    }
}