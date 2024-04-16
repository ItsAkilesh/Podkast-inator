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
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenAI.ObjectModels.ResponseModels;
using Windows.Storage.Streams;

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
        public string urlForAlbumArtGen;
        private List<ChatMessage> conversationContext = new List<ChatMessage>();
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
                AddMessageToConversation($"You: {userInput}");
                InputTextBox.Text = string.Empty;

                conversationContext.Add(ChatMessage.FromUser(userInput)); // Add user input to conversation context

                var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
                {
                    Messages = conversationContext, // Use conversation context
                    Model = Models.ChatGpt3_5Turbo,
                    MaxTokens = 800
                });

                if (completionResult != null && completionResult.Successful)
                {
                    AddMessageToConversation("Podkast AI: " + completionResult.Choices.First().Message.Content);
                    conversationContext.Add(completionResult.Choices.First().Message); // Add AI response to conversation context
                }
                else
                {
                    AddMessageToConversation("Podkast AI: Sorry, something bad happened: " + completionResult.Error?.Message);
                }
            }
        }

        private async void StartMagicButton_Click(object sender, RoutedEventArgs e)
        {
            conversationContext.Add(ChatMessage.FromUser("You are Podkast AI. An Generative AI tool used to summarize podcasts uploaded and transcripted. Keep your responses short. Do no hallucinate information that was not provided to you although attempt answering questions only if you confidently know the context."));

            string fileName = file.Name;

            byte[] sampleFileBytes;
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                using (var dataReader = new DataReader(stream))
                {
                    await dataReader.LoadAsync((uint)stream.Size);
                    System.Diagnostics.Debug.WriteLine(dataReader.ToString());
                    sampleFileBytes = new byte[stream.Size];
                    System.Diagnostics.Debug.WriteLine(sampleFileBytes.ToString());
                    dataReader.ReadBytes(sampleFileBytes);
                }
            }

            //Transcript
            var audioResult = await openAiService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest
            {
                FileName = fileName,
                File = sampleFileBytes,
                Model = Models.WhisperV1,
                ResponseFormat = StaticValues.AudioStatics.ResponseFormat.VerboseJson
            });

            if (audioResult.Successful)
            {
                System.Diagnostics.Debug.WriteLine(audioResult.ToString());


                podkastTranscript.Text = audioResult.Text;
                conversationContext.Add(ChatMessage.FromUser(audioResult.Text.ToString()));
            }
            else
            {
                if (audioResult.Error == null)
                {
                    throw new Exception("Unknown Error");
                }
                podkastTranscript.Text = ($"{audioResult.Error.Code}: {audioResult.Error.Message}");
            }

            //Summarization
            conversationContext.Add(ChatMessage.FromUser("Summarize the Podcast"));

            var summaryResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
            {
                Messages = conversationContext, // Use conversation context
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 800
            });

            if (summaryResult != null && summaryResult.Successful)
            {
                podkastSummary.Text = summaryResult.Choices.First().Message.Content.ToString();
                conversationContext.Add(summaryResult.Choices.First().Message); // Add AI response to conversation context
            }
            else
            {
                podkastSummary.Text = (summaryResult.Error?.Message).ToString();
            }

            //Title
            conversationContext.Add(ChatMessage.FromUser("What is the name of this podcast? Tell the name only"));

            var titleResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
            {
                Messages = conversationContext, // Use conversation context
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 800
            });

            if (titleResult != null && titleResult.Successful)
            {
                podkastTitle.Text = titleResult.Choices.First().Message.Content.ToString();
                conversationContext.Add(titleResult.Choices.First().Message); // Add AI response to conversation context
            }
            else
            {
                podkastTitle.Text = (titleResult.Error?.Message).ToString();
            }

            conversationContext.Add(ChatMessage.FromUser("Tell only the names of the speakers in the podcast"));

            //Hosts and Speakers results

            var hostsResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
            {
                Messages = conversationContext, // Use conversation context
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 800
            });

            if (hostsResult != null && hostsResult.Successful)
            {
                podkastHosts.Text = hostsResult.Choices.First().Message.Content.ToString();
                conversationContext.Add(hostsResult.Choices.First().Message); // Add AI response to conversation context
            }
            else
            {
                podkastHosts.Text = (hostsResult.Error?.Message).ToString();
            }

            //Context Generation

            conversationContext.Add(ChatMessage.FromUser("What is the Context of this podcast? What is the Genre of this podcast? Suggest similar podcasts to this. (Write answers to the above questions as separate paragraphs.)"));

            var contextResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
            {
                Messages = conversationContext, // Use conversation context
                Model = Models.Gpt_4_turbo,
                MaxTokens = 800
            });

            if (contextResult != null && hostsResult.Successful)
            {
                podkastSimilar.Text = contextResult.Choices.First().Message.Content.ToString();
                conversationContext.Add(contextResult.Choices.First().Message); // Add AI response to conversation context
            }
            else
            {
                podkastSimilar.Text = (contextResult.Error?.Message).ToString();
            }

            //Album art generation with Dall-E 3

            conversationContext.Add(ChatMessage.FromUser("Use the context present to generate a prompt for the album art for this podcast. Be extremely descriptive, include already known references and mention styles if necessary. Keep the prompt brief and informative"));

            var artResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest()
            {
                Messages = conversationContext, // Use conversation context
                Model = Models.Gpt_4_turbo,
                MaxTokens = 120
            });

            if (artResult != null && artResult.Successful)
            {
                String promptForImage = artResult.Choices.First().Message.Content.ToString();
                conversationContext.Add(artResult.Choices.First().Message); // Add AI response to conversation context

                var imageResult = await openAiService.Image.CreateImage(new ImageCreateRequest
                {

                    Model = Models.Dall_e_3,
                    Prompt = artResult.Choices.First().Message.Content.ToString(),
                    N = 1,
                    Size = StaticValues.ImageStatics.Size.Size1024,
                    ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Url,
                });


                urlForAlbumArtGen = string.Join("\n", imageResult.Results.Select(r => r.Url));

                System.Diagnostics.Debug.WriteLine(string.Join("\n", imageResult.Results.Select(r => r.Url)));

                // Create a HttpClient instance
                using (HttpClient client = new HttpClient())
                {
                    // Download the image bytes
                    byte[] imageBytes = await client.GetByteArrayAsync(urlForAlbumArtGen);

                    // Write the image bytes to a local file
                    await File.WriteAllBytesAsync(@"C:\Users\akile\OneDrive\Desktop\temp_image_art.png", imageBytes);

                    podkastAlbumArt.Source = new BitmapImage(new Uri(@"C:\Users\akile\OneDrive\Desktop\temp_image_art.png"));



                }



            }
        }

            private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset podcast title and hosts to default values
            podkastTitle.Text = "Title";
            podkastHosts.Text = "Hosts and Speakers";

            // Reset podcast album art to the default image
            // Ensure the image path matches your project's file structure
            podkastAlbumArt.Source = new BitmapImage(new Uri("ms-appx:///Assets/Blue-Logo.png"));

            try
            {
                // Get the file to delete synchronously
                StorageFile fileToDelete = StorageFile.GetFileFromApplicationUriAsync(new Uri(@"C:\Users\akile\OneDrive\Desktop\temp_image_art.png")).AsTask().Result;

                // Delete the file synchronously
                fileToDelete.DeleteAsync().AsTask().Wait();

                Console.WriteLine("File deleted successfully.");
            }
            catch (AggregateException ex)
            {
                foreach (var innerException in ex.InnerExceptions)
                {
                    Console.WriteLine($"Error deleting file: {innerException.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }

                // Reset transcript, summary, and context windows to default values
                podkastTranscript.Text = "Podcast Transcript goes here.";
            podkastSummary.Text = "Podcast Summary goes here.";
            podkastSimilar.Text = "The Genre of the podcast";

            // Clear conversation window
            ConversationList.Items.Clear();
            PickAFileOutputTextBlock.Text = "";

            conversationContext.Clear();
        }
        private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && !string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                SendButton_Click(this, new RoutedEventArgs());
            }
        }
        public class MessageItem
        {
            public string Text { get; set; }
            public SolidColorBrush Color { get; set; }
        }
        private void AddMessageToConversation(string message)
        {
            var messageItem = new MessageItem();
            messageItem.Text = message;
            messageItem.Color = message.StartsWith("You:") ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.White);
            ConversationList.Items.Add(messageItem);

            // handle scrolling
            ConversationScrollViewer.UpdateLayout();
            ConversationScrollViewer.ChangeView(null, ConversationScrollViewer.ScrollableHeight, null);

        }
    }
}
