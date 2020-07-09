using System;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Extensions.Configuration;
using System.Net;

namespace Translator.Console {

    class Program {

        private static IConfigurationRoot Configuration { get; set; }

        static async Task Main(string[] args) {

            // Setup the configuration values
            Configuration = BuildConfiguration();

            // Read in the TSV tab-separated file
            var lines = await File.ReadAllLinesAsync(@"Sample.txt");

            // Process the translations
            var result = await TranslateAsync(lines, "es");

            // TODO: Write the results out to an Output.txt file
            //File.WriteAllTextAsync("Output.txt"); // NOT Complete

            System.Console.ReadLine();
        }

        private static IConfigurationRoot BuildConfiguration() {

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>();

            return builder.Build();
        }

        private static async Task<List<string>> TranslateAsync(string[] lines, string toLang) {

            var client = new HttpClient();

            var url = $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to={toLang}";

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", Configuration["cogsvc_region"]);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Configuration["cogsvc_key"]);

            var tasks = lines.Select(async line => {

                var split = line.Split('\t');

                var result = String.Empty;

                if (split.Length == 3) {

                    var input = split[2];

                    var requestJson = JsonSerializer.Serialize(new[] { new { Text = input } });

                    var response = await client.PostAsync(url, new StringContent(requestJson, Encoding.UTF8, "application/json"));

                    var responseJson = await response.Content.ReadAsStringAsync();

                    var resultData = JsonSerializer.Deserialize<List<TranslationResult>>(responseJson);

                    result = resultData[0].translations[0].text;

                    // Write out some data to indicate status progress
                    System.Console.WriteLine($"TRANSLATION: {input} => {result}");
                }
                else {

                    result = "EXCEPTION: Line cannot be broken into columns";
                }

                return $"{line}\t{result}";
            });

            var results = await Task.WhenAll(tasks);

            return results.ToList();
        }
    }

    internal class TranslationResult {

        public Detectedlanguage detectedLanguage { get; set; }

        public Translation[] translations { get; set; }
    }

    internal class Detectedlanguage {

        public string language { get; set; }

        public float score { get; set; }
    }

    internal class Translation {

        public string text { get; set; }

        public string to { get; set; }
    }
}
