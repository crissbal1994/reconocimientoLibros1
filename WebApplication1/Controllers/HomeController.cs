using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        const string subscriptionKey = "093610e086584df7ad35bd0b84e6b268";
        const string uriBase ="https://westeurope.api.cognitive.microsoft.com/vision/v2.0/recognizeText";

        public ActionResult Index()
        {

            return View();
        }

        [NonAction]
        public async Task ReadHandwrittenTextAsync(String path)
        {
            String respuesta = "";
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", subscriptionKey);

                // Request parameter.
                // Note: The request parameter changed for APIv2.
                // For APIv1, it is "handwriting=true".
                string requestParameters = "mode=Printed";

                // Assemble the URI for the REST API Call.
                string uri = uriBase + "?" + requestParameters;

                HttpResponseMessage response;

                // Two REST API calls are required to extract handwritten text.
                // One call to submit the image for processing, the other call
                // to retrieve the text found in the image.
                // operationLocation stores the REST API location to call to
                // retrieve the text.
                string operationLocation="";

                // Request body.
                // Posts a locally stored JPEG image.
                byte[] byteData = GetImageAsByteArray(path);

                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses content type "application/octet-stream".
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // The first REST call starts the async process to analyze the
                    // written text in the image.
                    response = await client.PostAsync(uri, content);
                }

                // The response contains the URI to retrieve the result of the process.
                if (response.IsSuccessStatusCode)
                    operationLocation =
                        response.Headers.GetValues("Operation-Location").FirstOrDefault();
                else
                {
                    // Display the JSON error data.
                    string errorString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("\n\nResponse:\n{0}\n",
                        JToken.Parse(errorString).ToString());
                    //return;
                }

                // The second REST call retrieves the text written in the image.
                //
                // Note: The response may not be immediately available. Handwriting
                // recognition is an async operation that can take a variable amount
                // of time depending on the length of the handwritten text. You may
                // need to wait or retry this operation.
                //
                // This example checks once per second for ten seconds.
                string contentString;
                int i = 0;
                do
                {
                    System.Threading.Thread.Sleep(1000);
                    response = await client.GetAsync(operationLocation);
                    contentString = await response.Content.ReadAsStringAsync();
                    ++i;
                }
                while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1);

                if (i == 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1)
                {
                    Console.WriteLine("\nTimeout error.\n");
                  //  return;
                }

                // Display the JSON response.
                Console.WriteLine("\nResponse:\n\n{0}\n",
                JToken.Parse(contentString).ToString());

                JObject googleSearch = JObject.Parse(contentString);

                Console.WriteLine(googleSearch);

                JToken results = googleSearch["recognitionResult"];
                Console.WriteLine(results);
                JToken lines = results["lines"];
                JArray linesArray = JArray.Parse(lines.ToString());
                Console.WriteLine(linesArray);

                List<string> lineas = new List<string>();
                foreach (JObject l in linesArray.Children<JObject>())
                {
                    string linea = l["text"].ToString();
                    lineas.Add(linea);
                }
                foreach (var texto in lineas)
                {
                    Console.WriteLine("linea:" + texto);
                }

                var cb = new SqlConnectionStringBuilder();
                cb.DataSource = "libreriadb.database.windows.net";
                cb.UserID = "libreriadb";
                cb.Password = "Libreria-db1";
                cb.InitialCatalog = "libreriadb";
                List<string> num = new List<string>();

                string patron = @"^(\d|-)?(\d|,)*\.?\d*$";
                Regex regex = new Regex(patron);
                foreach (var texto in lineas)
                {
                    foreach (Match m in regex.Matches(texto))
                    {
                        num.Add(m.ToString());
                        Console.WriteLine("Número: {0}", m.Value);
                    }
                }

                List<int> idLib = new List<int>();
                using (var conn = new SqlConnection(cb.ConnectionString))
                {
                    Console.WriteLine("Opening connection");
                    conn.Open();
                    foreach (var texto in num)
                    {
                        string[] words = texto.Split('.');
                        Console.WriteLine(words[0] + "" + words[1]);

                        using (var command = conn.CreateCommand())
                        {
                            command.CommandText = "SELECT id FROM dbo.LibroCategorias where LibroCategorias.categoria in (" + words[0] + ") and LibroCategorias.codigo in (" + words[1] + ")";

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    Console.WriteLine(reader.GetInt32(0));
                                    idLib.Add(reader.GetInt32(0));
                                }
                                // Console.WriteLine(reader.GetInt32());
                            }
                        }

                    }
                    foreach (var texto in idLib)
                    {

                        using (var command = conn.CreateCommand())
                        {
                            command.CommandText = "SELECT titulo FROM dbo.Libroes where Libroes.id in (" + texto + ")";
                           
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    Console.WriteLine(reader.GetString(0));
                                    respuesta = respuesta + " " + reader.GetString(0);
                                    // idLib.Add(reader.GetInt32(0));
                                }
                                // Console.WriteLine(reader.GetInt32());
                            }
                        }

                    }



                    Console.WriteLine("Closing connection");
                   
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
            }
            //return View();
        }

        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (FileStream fileStream =
                new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

    }
}



