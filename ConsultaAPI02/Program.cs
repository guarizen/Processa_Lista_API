using System;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace ConsultaAPI02
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var requisicaoWeb = WebRequest.CreateHttp("http://189.113.4.250:888/api/millenium!pillow/movimentacao/lista_dap?filial=16&$format=json");
                requisicaoWeb.Method = "GET";
                requisicaoWeb.Headers.Add("Authorization", $"Basic YWRtaW5pc3RyYXRvcjp2dGFUUFJAMjAxOSoq");
                requisicaoWeb.UserAgent = "RequisicaoAPIGET";
                requisicaoWeb.Timeout = 1300000;

                using (var resposta = requisicaoWeb.GetResponse())
                {
                    var streamDados = resposta.GetResponseStream();
                    StreamReader reader = new StreamReader(streamDados);
                    object objResponse = reader.ReadToEnd();
                    var statusCodigo = ((System.Net.HttpWebResponse)resposta).StatusCode;

                    Pedidos Pedidov = JsonConvert.DeserializeObject<Pedidos>(objResponse.ToString());

                    foreach (var pv in Pedidov.Value)
                    {
                        try {
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://189.113.4.250:888/api/millenium/pedido_venda/reserva_estoque");
                            request.Method = "POST";
                            request.ContentType = "application/json";
                            request.Headers.Add("Authorization", $"Basic YWRtaW5pc3RyYXRvcjp2dGFUUFJAMjAxOSoq");
                            request.UserAgent = "RequisicaoAPIPOST";
                            request.Timeout = 1300000;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                            {
                                var PostPedido = JsonConvert.SerializeObject(pv.Pedidov);

                                var DadosPost = "[{" + "\"pedidov\"" + ":" + PostPedido + "}]";

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

                                Console.WriteLine($"Processado - Pedidov: {pv.Pedidov}"+" "+ $"Status: {(int)statusResposta}" + " - " + $"{statusResposta}");
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
