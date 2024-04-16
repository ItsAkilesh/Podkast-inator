using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using Microsoft.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Podkast.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PodkastPage : Page
    {
        private OpenAIService openAiService;
        private StorageFile file;
        public PodkastPage()
        {
            this.InitializeComponent();
            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = openAiKey
            });

        }
        private async void PickAFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous returned file name, if it exists, between iterations of this scenario
            PickAFileOutputTextBlock.Text = "";

            // Create a file picker
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

            // See the sample code below for how to make the window accessible from the App class.
            var window = new MainWindow();

            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

            // Initialize the file picker with the window handle (HWND).
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your file picker
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a file
            file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                PickAFileOutputTextBlock.Text = "Picked file: " + file.Name;
            }
            else
            {
                PickAFileOutputTextBlock.Text = "Operation cancelled.";
            }
        }
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = InputTextBox.Text;
            if (!string.IsNullOrEmpty(userInput))
            {
                AddMessageToConversation($"User: {userInput}");
                InputTextBox.Text = string.Empty;
                var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                {
                    Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("You are a helpful assistant."),
                    ChatMessage.FromUser(userInput)
                },
                    Model = Models.Gpt_4_1106_preview,
                    MaxTokens = 300
                });

                if (completionResult != null && completionResult.Successful)
                {
                    AddMessageToConversation("GPT: " + completionResult.Choices.First().Message.Content);
                }
                else
                {
                    AddMessageToConversation("GPT: Sorry, something bad happened: " + completionResult.Error?.Message);
                }
            }
        }
        private void AddMessageToConversation(string message)
        {
            var messageBlock = new TextBlock();
            messageBlock.Text = message;
            messageBlock.Margin = new Thickness(5);
            if (message.StartsWith("User:"))
            {
                messageBlock.Foreground = new SolidColorBrush(Colors.LightBlue);
            }
            else
            {
                messageBlock.Foreground = new SolidColorBrush(Colors.LightGreen);
            }
            ConversationList.Items.Add(message);
            ConversationList.ScrollIntoView(ConversationList.Items[ConversationList.Items.Count - 1]);
        }
    }
}
