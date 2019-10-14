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
            //Criar Diretório de Log
            string pastalog = @"C:\Log";
            if (!Directory.Exists(pastalog))
            {
                Directory.CreateDirectory(pastalog);
            }
            //Consulta API do Millennium, Buscando as informações da lista a faturar.
            try
            {
                var requisicaoWeb = WebRequest.CreateHttp("http://189.113.4.250:888/api/millenium!pillow/prefaturamentos/lista_fat_auto?$format=json");
                requisicaoWeb.Method = "GET";
                requisicaoWeb.Headers.Add("Authorization", $"Basic YWRtaW5pc3RyYXRvcjp2dGFUUFJAMjAxOSoq");
                requisicaoWeb.UserAgent = "RequisicaoAPIGETPrefat";
                requisicaoWeb.Timeout = 1300000;

                //Retorno da API do Millennium com a Lista a Faturar.
                using (var resposta = requisicaoWeb.GetResponse())
                {
                    var streamDados = resposta.GetResponseStream();
                    StreamReader reader = new StreamReader(streamDados);
                    object objResponse = reader.ReadToEnd();
                    var statusCodigo = ((System.Net.HttpWebResponse)resposta).StatusCode;

                    ListaPrefaturamentos Prefat = JsonConvert.DeserializeObject<ListaPrefaturamentos>(objResponse.ToString());

                    //Consultando cada número de Pre-Faturamento recebido.
                    foreach (var pref in Prefat.Value)
                    {
                        //Comunicando a API de Faturamento do Millennium.
                        try
                        {
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://189.113.4.250:888/api/millenium_log/picking/faturar");
                            request.Method = "POST";
                            request.ContentType = "application/json";
                            request.Headers.Add("Authorization", $"Basic YWRtaW5pc3RyYXRvcjp2dGFUUFJAMjAxOSoq");
                            request.UserAgent = "RequisicaoAPIPOST";
                            request.Timeout = 1300000;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                            //Enviando o número do Pre-faturamento no formato JSON para API do Millennium.
                            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                            {
                                var PostPrefat = JsonConvert.SerializeObject(pref.Numero);

                                var DadosPost = "{" + "\"numero\"" + ":" + PostPrefat + "}";

                                streamWriter.Write(DadosPost);
                                streamWriter.Flush();
                                streamWriter.Close();
                            }

                            //Comunicando com a API do Millennium e Aguardando o retorno.
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

                            //Verificando a pasta de Log. 
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                            string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                            //Verifica se o arquivo de Log não existe e inclui as informações.
                            if (!File.Exists(path))
                            {
                                string nomeArquivo1 = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                StreamWriter writer1 = new StreamWriter(nomeArquivo1);
                                writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " - " + $"Status: {(int)statusCodigo}" + " - " + $"Numero Pre-faturamento: {pref.Numero}" + " - " + $"Retorno Millennium: {statusCodigo}");
                                writer1.Close();
                            }
                            //Verifica se o arquivo de Log já existe e inclui as informações.
                            else
                            {
                                using (StreamWriter sw = File.AppendText(path))
                                {
                                    sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " - " + $"Status: {(int)statusCodigo}" + " - " + $"Numero Pre-faturamento: {pref.Numero}" + " - " + $"Retorno Millennium: {statusCodigo}");
                                    sw.Close();
                                }
                            }
                        }
                        //Recebida exceção da API do Millennium. (ERRO)
                        catch (WebException e)
                        {
                            //Verifica o Status de exceção recebida, número de erro retornado.
                            if (e.Status == WebExceptionStatus.ProtocolError)
                            {
                                //Busca a resposta da API.
                                var respostaErr = (HttpWebResponse)e.Response;

                                //Abre a resposta da API
                                using (Stream dataStreamErr = respostaErr.GetResponseStream())
                                {
                                    StreamReader readerErr = new StreamReader(dataStreamErr);
                                    string responseFromServer = readerErr.ReadToEnd();

                                    //Comunicando a API de Erro no Faturamento do Millennium.
                                    try
                                    {
                                        HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create("http://189.113.4.250:888/api/millenium!pillow/prefaturamentos/info_erro_faturamento");
                                        request1.Method = "POST";
                                        request1.ContentType = "application/json";
                                        request1.Headers.Add("Authorization", $"Basic YWRtaW5pc3RyYXRvcjp2dGFUUFJAMjAxOSoq");
                                        request1.UserAgent = "RequisicaoAPIPOST";
                                        request1.Timeout = 1300000;
                                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                                        //Enviando o número do Pre-faturamento no formato JSON para API do Millennium.
                                        using (var streamWriter = new StreamWriter(request1.GetRequestStream()))
                                        {
                                            var PostPrefat = JsonConvert.SerializeObject(pref.Numero);
                                            var PostErroFat = responseFromServer.Replace("\"", "");

                                            var DadosPost = "{" + "\"numero\"" + ":" + PostPrefat + "," + " " + "\"erro_fat_auto\"" + ":" + "\" " + PostErroFat + "\" " + "}";

                                            streamWriter.Write(DadosPost);
                                            streamWriter.Flush();
                                            streamWriter.Close();
                                        }

                                        //Comunicando com a API do Millennium e Aguardando o retorno, incluindo informações de log.
                                        using (var respostaPostErro = request1.GetResponse())
                                        {
                                            var streamPostErro = respostaPostErro.GetResponseStream();
                                            StreamReader readerPost = new StreamReader(streamPostErro);
                                            object objResponsePostErro = readerPost.ReadToEnd();
                                            var statusRespostaErro = ((System.Net.HttpWebResponse)respostaPostErro).StatusCode;
                                            var PostErroFat = responseFromServer.Replace("\"", "");

                                            //Verificando a pasta de Log. 
                                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                            string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                            //Verifica se o arquivo de Log não existe e inclui as informações.
                                            if (!File.Exists(path))
                                            {
                                                Console.WriteLine($"Log Erro - Faturamento: {pref.Numero}" + " - " + $"Status: {(int)statusRespostaErro}" + " - " + $"{statusRespostaErro}");
                                                string nomeArquivo = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                                StreamWriter writer = new StreamWriter(nomeArquivo);
                                                writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"Log Erro - Pré-Faturamento: {pref.Numero}" + " - " + $"Status: {(int)statusRespostaErro}" + " - " + $"{statusRespostaErro}" + " " + $"{PostErroFat}");
                                                writer.Close();
                                            }
                                            //Verifica se o arquivo de Log já existe e inclui as informações.
                                            else
                                            {
                                                using (StreamWriter sw = File.AppendText(path))
                                                {
                                                    Console.WriteLine($"Log Erro - Faturamento: {pref.Numero}" + " - " + $"Status: {(int)statusRespostaErro}" + " - " + $"{statusRespostaErro}");
                                                    sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"Log Erro - Pré-Faturamento: {pref.Numero}" + " - " + $"Status: {(int)statusRespostaErro}" + " - " + $"{statusRespostaErro}" + " " + $"{PostErroFat}");
                                                    sw.Close();
                                                }
                                            }
                                            streamPostErro.Close();
                                            respostaPostErro.Close();
                                        }
                                    }
                                    catch (WebException e1)
                                    {
                                        //Verifica o Status de exceção recebida, número de erro retornado.
                                        if (e1.Status == WebExceptionStatus.ProtocolError)
                                        {
                                            //Verificando a pasta de Log. 
                                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                            string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                            //Verifica se o arquivo de Log não existe e inclui as informações.
                                            if (!File.Exists(path))
                                            {
                                                var resposta3 = (HttpWebResponse)e1.Response;
                                                Console.WriteLine($"Errorcode: {(int)resposta3.StatusCode}" + " - " + $"{resposta3.StatusDescription.ToString()}");

                                                string nomeArquivo = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                                StreamWriter writer = new StreamWriter(nomeArquivo);
                                                writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta3.StatusCode})" + " " + $"(Status: {e1.Message})");
                                                writer.Close();
                                            }
                                            //Verifica se o arquivo de Log já existe e inclui as informações.
                                            else
                                            {
                                                using (StreamWriter sw = File.AppendText(path))
                                                {
                                                    var resposta3 = (HttpWebResponse)e1.Response;
                                                    Console.WriteLine($"Errorcode: {(int)resposta3.StatusCode}" + " - " + $"{resposta3.StatusDescription.ToString()}");

                                                    sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta3.StatusCode})" + " " + $"(Status: {e1.Message})");
                                                    sw.Close();
                                                }
                                            }
                                        }
                                    }

                                    respostaErr.Close();
                                }

                            }
                            //Recebida Exceção da API, porem sem retorno de Status.
                            else
                            {
                                // Verificando a pasta de Log. 
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                //Verifica se o arquivo de Log não existe e inclui as informações.
                                if (!File.Exists(path))
                                {
                                    Console.WriteLine("Error: {0}", e.Status);

                                    string nomeArquivo1 = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                    StreamWriter writer1 = new StreamWriter(nomeArquivo1);
                                    writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                                    writer1.Close();
                                }
                                //Verifica se o arquivo de Log já existe e inclui as informações.
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
            //Recebida exceção da API do Millennium. 
            catch (WebException e)
            {
                //Verifica o Status de exceção recebida, número de erro retornado.
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    //Verificando a pasta de Log. 
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                    //Verifica se o arquivo de Log não existe e inclui as informações.
                    if (!File.Exists(path))
                    {
                        var resposta2 = (HttpWebResponse)e.Response;
                        Console.WriteLine($"Errorcode: {(int)resposta2.StatusCode}" + " - " + $"{resposta2.StatusDescription.ToString()}");

                        string nomeArquivo = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                        StreamWriter writer = new StreamWriter(nomeArquivo);
                        writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta2.StatusCode})" + " " + $"(Status: {e.Message})");
                        writer.Close();
                    }
                    //Verifica se o arquivo de Log já existe e inclui as informações.
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
                //Recebida Exceção da API, porem sem retorno de Status.
                else
                {
                    //Verificando a pasta de Log. 
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string path = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                    //Verifica se o arquivo de Log não existe e inclui as informações.
                    if (!File.Exists(path))
                    {
                        Console.WriteLine("Error: {0}", e.Status);
                        string nomeArquivo = @"c:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                        StreamWriter writer = new StreamWriter(nomeArquivo);
                        writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                        writer.Close();
                    }
                    //Verifica se o arquivo de Log já existe e inclui as informações.
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
