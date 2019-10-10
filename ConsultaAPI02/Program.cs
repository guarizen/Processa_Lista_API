using System;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace FaturamentoAutomatico
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var requisicaoWeb = WebRequest.CreateHttp("http://189.113.4.250:888/api/millenium!pillow/prefaturamentos/lista_fat_auto?$format=json");
                requisicaoWeb.Method = "GET";
                requisicaoWeb.Headers.Add("Authorization", $"Basic YWRtaW5pc3RyYXRvcjp2dGFUUFJAMjAxOSoq");
                requisicaoWeb.UserAgent = "RequisicaoAPIGETPrefat";
                requisicaoWeb.Timeout = 1300000;

                using (var resposta = requisicaoWeb.GetResponse())
                {
                    var streamDados = resposta.GetResponseStream();
                    StreamReader reader = new StreamReader(streamDados);
                    object objResponse = reader.ReadToEnd();
                    var statusCodigo = ((System.Net.HttpWebResponse)resposta).StatusCode;

                    ListaPrefaturamentos Prefat = JsonConvert.DeserializeObject<ListaPrefaturamentos>(objResponse.ToString());

                    foreach (var pref in Prefat.Value)
                    {
                        try {
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://189.113.4.250:888/api/millenium_log/picking/faturar");
                            request.Method = "POST";
                            request.ContentType = "application/json";
                            request.Headers.Add("Authorization", $"Basic YWRtaW5pc3RyYXRvcjp2dGFUUFJAMjAxOSoq");
                            request.UserAgent = "RequisicaoAPIPOST";
                            request.Timeout = 1300000;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                            {
                                var PostPrefat = JsonConvert.SerializeObject(pref.Numero);

                                var DadosPost = "{" + "\"numero\"" + ":" + PostPrefat + "}";

                                streamWriter.Write(DadosPost);
                                streamWriter.Flush();
                                streamWriter.Close();
                            }

                            using (var respostaPost = request.GetResponse())
                            {
                                var streamPost = respostaPost.GetResponseStream();
                                StreamReader readerPost = new StreamReader(streamPost);
                                object objResponsePost = readerPost.ReadToEnd();
                                var statusResposta = ((System.Net.HttpWebResponse)respostaPost).StatusCode;
                                var post = JsonConvert.DeserializeObject<Post>(objResponsePost.ToString());

                                Console.WriteLine($"Processado - Faturamento: {pref.Numero}"+" "+ $"Status: {(int)statusResposta}" + " - " + $"{statusResposta}");
                                streamPost.Close();
                                respostaPost.Close();
                            }
                        }
                        catch (WebException e)
                        {
                            if (e.Status == WebExceptionStatus.ProtocolError)
                            {
                                var resposta2 = (HttpWebResponse)e.Response;
                                Console.WriteLine($"Errorcode: {(int)resposta2.StatusCode}" + " - " + $"{resposta2.StatusDescription.ToString()}");
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                string nomeArquivo1 = @"c:\log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                StreamWriter writer1 = new StreamWriter(nomeArquivo1);
                                writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta2.StatusCode})" + " " + $"(Status: {e.Message})");
                                writer1.Close();
                            }
                            else
                            {
                                Console.WriteLine("Error: {0}", e.Status);
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                string nomeArquivo1 = @"c:\log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                StreamWriter writer1 = new StreamWriter(nomeArquivo1);
                                writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                                writer1.Close();
                            }
                        }
                    }


                    Console.WriteLine("Codigo Retorno API: " + statusCodigo);
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string nomeArquivo = @"c:\log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                    StreamWriter writer = new StreamWriter(nomeArquivo);
                    writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Status: {(int)statusCodigo})" + " " + "Finalizada Execução!");
                    writer.Close();

                    reader.Close();
                    streamDados.Close();
                    resposta.Close();

                    Console.WriteLine("Finalizada Execução!");
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    var resposta2 = (HttpWebResponse)e.Response;
                    Console.WriteLine($"Errorcode: {(int)resposta2.StatusCode}" + " - " + $"{resposta2.StatusDescription.ToString()}");
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string nomeArquivo = @"c:\log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                    StreamWriter writer = new StreamWriter(nomeArquivo);
                    writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}"+ " " + $"(Codigo Erro: {(int)resposta2.StatusCode})" + " " + $"(Status: {e.Message})");
                    writer.Close();
                }
                else
                {
                    Console.WriteLine("Error: {0}", e.Status);
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string nomeArquivo = @"c:\log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                    StreamWriter writer = new StreamWriter(nomeArquivo);
                    writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                    writer.Close();
                }
               
            }
           // Console.ReadKey();
        }
    }
}
