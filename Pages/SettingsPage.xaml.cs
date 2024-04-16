using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OpenAI.Managers;
using OpenAI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Podkast.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private void SetOpenAIKeyButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = SetOpenAIKeyTextBox.Text;
            if (!string.IsNullOrEmpty(userInput) && userInput.StartsWith("sk-"))
            {
                Environment.SetEnvironmentVariable("OPENAI_API_KEY", SetOpenAIKeyTextBox.Text);
                var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                System.Diagnostics.Debug.WriteLine(openAiKey);
                ShowMessageDialogSuccess("Environment Variable Set Successfully");
            }
            else
            {
                ShowMessageDialogError("Invalid API Key Format");
            }
            //System.Diagnostics.Debug.WriteLine(SetOpenAIKeyTextBox.Text);
        }

        private async void ShowMessageDialogError(string message)
        {
            ContentDialog noWifiDialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "Input Error",
                Content = message,
                CloseButtonText = "Ok"
            };

            await noWifiDialog.ShowAsync();
        }

        private async void ShowMessageDialogSuccess(string message)
        {
            ContentDialog noWifiDialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "Success",
                Content = message,
                CloseButtonText = "Ok"
            };

            await noWifiDialog.ShowAsync();
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        { 
            ShowMessageDialogInfo();
        }

        private async void ShowMessageDialogInfo()
        {
            /*
            string message = "<BPS ID: GSLABGAVSVIT009 (Podcast Summarization)" + "\r\nTeam ID: 53495\r\nAkilesh S 21MIS1167 \r\nRajeev Sekar 21MIS1152 \r\nTulasi Raman R 21MIS1170";
            ContentDialog noWifiDialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "Info",
                Content = message,
                CloseButtonText = "Ok"
            };

            await noWifiDialog.ShowAsync();
            */

            // Create a RichTextBlock to hold the formatted text
            RichTextBlock richTextBlock = new RichTextBlock();

            // Create a paragraph to contain the formatted text
            Paragraph paragraph = new Paragraph();

            // Create a Run with the bold text
            Run boldRun = new Run();
            boldRun.Text = "PS ID: GSLABGAVSVIT009 (Podcast Summarization)";
            boldRun.FontWeight = FontWeights.Bold; // Using FontWeight from Windows.UI.Xaml namespace
            paragraph.Inlines.Add(boldRun);

            // Add the rest of the message as plain text
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run { Text = "Team ID: 53495" });
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run { Text = "Akilesh S 21MIS1167" });
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run { Text = "Rajeev Sekar 21MIS1152" });
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run { Text = "Tulasi Raman R 21MIS1170" });

            // Add the paragraph to the RichTextBlock
            richTextBlock.Blocks.Add(paragraph);

            // Create the ContentDialog
            ContentDialog noWifiDialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "Info",
                CloseButtonText = "Ok",
                Content = richTextBlock
            };

            // Show the ContentDialog
            await noWifiDialog.ShowAsync();
        }

    }
}
