using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EspOtaUpdaterTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await UpdateFirmwareAsync();
        }

        private async Task UpdateFirmwareAsync()
        {
            try
            {

                FileOpenPicker picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".bin");
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                var firmware = await picker.PickSingleFileAsync();
                if (firmware != null)
                {
                    progressText.Text = "Starting update";
                    progressText.Visibility = Visibility.Visible;
                    progressBar.Value = 0;
                    progressBar.IsIndeterminate = true;
                    progressBar.Visibility = Visibility.Visible;

                    var uri = new Uri(uriEntry.Text);
                    var updateTask = EspOtaUWP.OtaUpdater.PostFirmwareAsync(firmware, uri, usernameEntry.Text, passwordEntry.Password);
                    updateTask.Progress += OnHttpProgress;
                    var result = await updateTask;
                    Debug.WriteLine(result);

                    progressText.Visibility = Visibility.Collapsed;
                    progressBar.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                Debug.WriteLine("There was a problem");
            }
        }

        private async void OnHttpProgress(IAsyncOperationWithProgress<HttpStatusCode, HttpProgress> asyncInfo, HttpProgress progressInfo)
        {
            Debug.WriteLine("Progress: {0} {1} {2} {3} {4} {5}",
                progressInfo.BytesReceived,
                progressInfo.BytesSent,
                progressInfo.Retries,
                progressInfo.Stage,
                progressInfo.TotalBytesToReceive,
                progressInfo.TotalBytesToSend);

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => UpdateProgressBar(progressInfo));
        }

        private void UpdateProgressBar(HttpProgress progressInfo)
        {
            progressText.Text = progressInfo.Stage.ToString();
            switch (progressInfo.Stage)
            {
                case HttpProgressStage.ReceivingContent:
                    progressBar.IsIndeterminate = false;
                    progressBar.Maximum = (double)progressInfo.TotalBytesToReceive;
                    progressBar.Value = progressInfo.BytesReceived;
                    break;

                case HttpProgressStage.SendingContent:
                    progressBar.IsIndeterminate = false;
                    progressBar.Maximum = (double)progressInfo.TotalBytesToSend;
                    progressBar.Value = progressInfo.BytesSent;
                    break;

                default:
                    progressBar.IsIndeterminate = true;
                    break;
            }
        }
    }
}
