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
            string pastalog = @"C:\Log";
            if (!Directory.Exists(pastalog))
            {
                Directory.CreateDirectory(pastalog);
            }
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

                                Console.WriteLine($"Processado - Faturamento: {pref.Numero}" + " - " + $"Status: {(int)statusResposta}" + " - " + $"{statusResposta}");
                                streamPost.Close();
                                respostaPost.Close();
                            }

                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            string path = @"c:\log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                            if (!File.Exists(path))
                            {
                                string nomeArquivo1 = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                StreamWriter writer1 = new StreamWriter(nomeArquivo1);
                                writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " - " + $"Status: {(int)statusCodigo}" + " - " + $"Numero Pre-faturamento: {pref.Numero}" + " - " + $"Retorno Millennium: {statusCodigo}");
                                writer1.Close();
                            }
                            else
                            {
                                using (StreamWriter sw = File.AppendText(path))
                                {
                                    sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " - " + $"Status: {(int)statusCodigo}" + " - " + $"Numero Pre-faturamento: {pref.Numero}" + " - " + $"Retorno Millennium: {statusCodigo}");
                                    sw.Close();
                                }
                            }
                        }
                        catch (WebException e)
                        {
                            if (e.Status == WebExceptionStatus.ProtocolError)
                            {
                                var respostaErr = (HttpWebResponse)e.Response;

                                using (Stream dataStreamErr = respostaErr.GetResponseStream())
                                {

                                    StreamReader readerErr = new StreamReader(dataStreamErr);
                                    string responseFromServer = readerErr.ReadToEnd();

                                    Console.WriteLine($"Error Code: {(int)respostaErr.StatusCode}" + " - " + $"{respostaErr.StatusDescription.ToString()}" + " - " + $"Numero Pre-faturamento: {pref.Numero}");

                                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                    string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                    if (!File.Exists(path))
                                    {
                                        string nomeArquivo1 = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                        StreamWriter writer1 = new StreamWriter(nomeArquivo1);
                                        writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " - " + $"Status: {e.Message}" + " - " + $"Numero Pre-faturamento: {pref.Numero}" + " - " + $"Retorno Millennium: {responseFromServer}");
                                        writer1.Close();
                                    }
                                    else
                                    {
                                        using (StreamWriter sw = File.AppendText(path))
                                        {
                                            sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " - " + $"Status: {e.Message}" + " - " + $"Numero Pre-faturamento: {pref.Numero}" + " - " + $"Retorno Millennium: {responseFromServer}");
                                            sw.Close();
                                        }
                                    }   
                                    respostaErr.Close();
                                }
                               
                            }
                            else
                            {
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                if (!File.Exists(path))
                                {
                                    Console.WriteLine("Error: {0}", e.Status);

                                    string nomeArquivo1 = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                    StreamWriter writer1 = new StreamWriter(nomeArquivo1);
                                    writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                                    writer1.Close();
                                }
                                else
                                {
                                    using (StreamWriter sw = File.AppendText(path))
                                    {
                                        sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                                        sw.Close();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                    if (!File.Exists(path))
                    {
                        var resposta2 = (HttpWebResponse)e.Response;
                        Console.WriteLine($"Errorcode: {(int)resposta2.StatusCode}" + " - " + $"{resposta2.StatusDescription.ToString()}");

                        string nomeArquivo = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                        StreamWriter writer = new StreamWriter(nomeArquivo);
                        writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta2.StatusCode})" + " " + $"(Status: {e.Message})");
                        writer.Close();
                    }
                    else
                    {
                        using (StreamWriter sw = File.AppendText(path))
                        {
                            var resposta2 = (HttpWebResponse)e.Response;
                            Console.WriteLine($"Errorcode: {(int)resposta2.StatusCode}" + " - " + $"{resposta2.StatusDescription.ToString()}");

                            sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta2.StatusCode})" + " " + $"(Status: {e.Message})");
                            sw.Close();
                        }
                    }
                }
                else
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                    if (!File.Exists(path))
                    {
                        Console.WriteLine("Error: {0}", e.Status);
                        string nomeArquivo = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                        StreamWriter writer = new StreamWriter(nomeArquivo);
                        writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                        writer.Close();
                    }
                    else
                    {
                        using (StreamWriter sw = File.AppendText(path))
                        {
                            sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                            sw.Close();
                        }
                    }
                    
                }
               
            }
        }
    }
}
