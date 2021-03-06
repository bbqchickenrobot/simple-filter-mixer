﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Nokia.Graphics.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace simple_filter_mixer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Imaging _imaging = new Imaging();
        private bool _firstTime = true;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_firstTime)
            {
                double[] screenWidthAndDisplayRatio = await ResolveScreenWidthAndDisplayRatioAsync();
                App.ScreenWidth = screenWidthAndDisplayRatio[0];
                App.DisplayRatio = screenWidthAndDisplayRatio[1];
            }

            if (App.ChosenPhoto == null)
            {
                try
                {
                    Windows.Storage.StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;

                    App.ChosenPhoto = await StorageFile.GetFileFromPathAsync(installedLocation.Path + @"\Assets\Default.jpg");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);        
                    throw;
                }
            }

            var filters = new List<IFilter>();
            Imaging.GetFilters(filters);
            string appliedFiltersString = "";
            int i = 0;

            foreach (var filter in filters)
            {
                appliedFiltersString += filter.ToString().Split('.').Last<string>();
                
                if (i < filters.Count - 1)
                {
                    appliedFiltersString += " > ";
                }

                i++;
            }

            AppliedFiltersTextBlock.Text = (appliedFiltersString.Length > 0) ? appliedFiltersString : "No filters";

            _imaging.IsRenderingChanged += OnIsRenderingChanged;

            if (FiltersPage.FiltersChanged || SettingsPage.SettingsChanged || _firstTime)
            {
                // Need to render the image
                ImageControl.Source = await _imaging.ApplyBasicFilter(filters);
                FiltersPage.FiltersChanged = false;
                SettingsPage.SettingsChanged = false;
                _firstTime = false;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _imaging.IsRenderingChanged -= OnIsRenderingChanged;
            base.OnNavigatedFrom(e);
        }

        private void OnIsRenderingChanged(object sender, bool e)
        {
            if (e)
            {
                MyProgressBar.Visibility = Visibility.Visible;
                PhotoButton.IsEnabled = false;
                FilterButton.IsEnabled = false;
                AboutButton.IsEnabled = false;
            }
            else
            {
                MyProgressBar.Visibility = Visibility.Collapsed;
                PhotoButton.IsEnabled = true;
                FilterButton.IsEnabled = true;
                AboutButton.IsEnabled = true;
            }
        }
        
#if WINDOWS_PHONE_APP
        private void OnPhotoClicked(object sender, RoutedEventArgs e)
#else
        private async void OnPhotoClicked(object sender, RoutedEventArgs e)
#endif
        {
            var picker = new FileOpenPicker();

            // Filter to include a sample subset of file types
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".png");

            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.ViewMode = PickerViewMode.Thumbnail;

#if WINDOWS_PHONE_APP
            App.ContinuationEventArgsChanged += App_ContinuationEventArgsChanged;

            picker.PickSingleFileAndContinue(); 
#else
            App.ChosenPhoto = await picker.PickSingleFileAsync();

            if (App.ChosenPhoto != null)
            {
                await _imaging.RenderPlainPhoto(ImageControl);
            }
#endif
        }

#if WINDOWS_PHONE_APP
        private async void App_ContinuationEventArgsChanged(object sender, IContinuationActivatedEventArgs e)
        {
            App.ContinuationEventArgsChanged -= App_ContinuationEventArgsChanged;

            var openFileArgs = e as FileOpenPickerContinuationEventArgs;
            var saveFileArgs = e as FileSavePickerContinuationEventArgs;

            if (openFileArgs != null && openFileArgs.Files != null && openFileArgs.Files.Count > 0)
            {
                App.ChosenPhoto = openFileArgs.Files[0];

                if (App.ChosenPhoto != null)
                {
                    await _imaging.RenderPlainPhoto(ImageControl);
                }
            }
        }
#endif

        private void OnFiltersClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(FiltersPage));
        }

        private void OnAboutClicked(object sender, RoutedEventArgs e)
        {
#if WINDOWS_PHONE_APP
            Frame.Navigate(typeof(AboutPage));
#endif
        }

        public static async System.Threading.Tasks.Task<double[]> ResolveScreenWidthAndDisplayRatioAsync()
        {
            double rawPixelsPerViewPixel = 0;
            double screenResolutionX = 0;
            double screenResolutionY = 0;
            double[] result = new double[2];

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Windows.Graphics.Display.DisplayInformation displayInformation = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
                    rawPixelsPerViewPixel = displayInformation.RawPixelsPerViewPixel;
                    result[0] = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Bounds.Width;
                    screenResolutionX = result[0] * rawPixelsPerViewPixel;
                    screenResolutionY = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Bounds.Height * rawPixelsPerViewPixel;
                });

            result[1] = screenResolutionY / screenResolutionX;
            return result;
        }
    }
}
