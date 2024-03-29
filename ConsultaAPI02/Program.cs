﻿using System;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Configuration;
using System.Management;

namespace FaturamentoAutomatico
{
    class Program
    {
        static void Main(string[] args)
        {
            string pastalog = @"C:\Log\";
            if (!File.Exists(pastalog))
            {
                //Criar Diretório de Log
                Directory.CreateDirectory(pastalog);
            }

            //Configurações do App.Config
            var ConnectLista = ConfigurationManager.AppSettings["ConnectLista"];
            var ConnectFaturar = ConfigurationManager.AppSettings["ConnectFaturar"];
            var ConnectFaturado = ConfigurationManager.AppSettings["ConnectFaturado"];
            var ConnectErroFat = ConfigurationManager.AppSettings["ConnectErroFat"];
            var Authorization = ConfigurationManager.AppSettings["Authorization"];
            var ControlKey = ConfigurationManager.AppSettings["ControlKey"];

            //Validação de Acesso ao Programa, Control Key.
            var hDid = string.Empty;
            var mc = new ManagementClass("Win32_LogicalDisk");
            var moc = mc.GetInstances();
            foreach (var o in moc)
            {
                var mo = (ManagementObject)o;

                hDid += (string)mo.Properties["VolumeSerialNumber"].Value;

            }
            //Adicionando String de Att. na Validação da Control Key.
            if(hDid != null)
            {
                hDid += "A9679I0";
            }

            if (hDid == ControlKey)
            {
                //Consulta API do Millennium, Buscando as Informações da Lista a Faturar.
                try
                {
                    var requisicaoWeb = WebRequest.CreateHttp($"{ConnectLista}"+ "?$format=json");
                    requisicaoWeb.Method = "GET";
                    requisicaoWeb.Headers.Add("Authorization", $"{Authorization}");
                    requisicaoWeb.UserAgent = "RequisicaoAPIGETPrefat";
                    requisicaoWeb.Timeout = 1300000;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

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
                                //Consultando API para Envio do Faturamento
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{ConnectFaturar}");
                                request.Method = "POST";
                                request.ContentType = "application/json";
                                request.Headers.Add("Authorization", $"{Authorization}");
                                request.UserAgent = "RequisicaoAPIPOST";
                                request.Timeout = 1300000;
                                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                                //Enviando o número do Pre-faturamento no formato JSON para API do Millennium.
                                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                                {
                                    var PostPrefat = JsonConvert.SerializeObject(pref.Numero);

                                    var DadosPost = "{" + "\"numero\"" + ":" + PostPrefat + "}";

                                    streamWriter.Write(DadosPost);

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
                                    streamPost.Dispose();
                                }

                                //Verificando a pasta de Log. 
                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                string path = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                //Verifica se o arquivo de Log não existe e inclui as informações.
                                if (!File.Exists(path))
                                {
                                    DirectoryInfo dir = new DirectoryInfo(pastalog);

                                    foreach (FileInfo fi in dir.GetFiles())
                                    {
                                        fi.Delete();
                                    }

                                    string nomeArquivo1 = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
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
                                    }
                                }
                                try
                                {
                                    //Consultando API para Envio da Confirmação de Faturamento
                                    HttpWebRequest requestEnv = (HttpWebRequest)WebRequest.Create($"{ConnectFaturado}");
                                    requestEnv.Method = "POST";
                                    requestEnv.ContentType = "application/json";
                                    requestEnv.Headers.Add("Authorization", $"{Authorization}");
                                    requestEnv.UserAgent = "RequisicaoAPIPOST";
                                    requestEnv.Timeout = 1300000;
                                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                                    //Enviando a Confirmação de Faturado
                                    using (var streamWriter = new StreamWriter(requestEnv.GetRequestStream()))
                                    {
                                        var PostPrefatEnv = JsonConvert.SerializeObject(pref.Prefaturamento);

                                        var DadosPostEnv = "{" + "\"prefaturamento\"" + ":" + PostPrefatEnv + "}";

                                        streamWriter.Write(DadosPostEnv);

                                    }

                                    //Comunicando com a API do Millennium Enviando a Confirmação de Faturamento (Ag. Retorno)
                                    using (var respostaPostFat = requestEnv.GetResponse())
                                    {
                                        var streamPostFat = respostaPostFat.GetResponseStream();
                                        StreamReader readerPostFat = new StreamReader(streamPostFat);
                                        object objResponsePost = readerPostFat.ReadToEnd();
                                        var statusResposta = ((System.Net.HttpWebResponse)respostaPostFat).StatusCode;
                                        var postFat = JsonConvert.DeserializeObject<Post>(objResponsePost.ToString());

                                        Console.WriteLine($"Envio Faturamento Confirmado: {pref.Numero}" + " - " + $"Status: {(int)statusResposta}" + " - " + $"{statusResposta}");
                                        streamPostFat.Dispose();
                                    }
                                    //Verificando a pasta de Log. 
                                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                    string pathLogFat = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                    //Verifica se o arquivo de Log não existe e inclui as informações.
                                    if (!File.Exists(pathLogFat))
                                    {
                                        //Listar todos os arquivos na pasta de Log e Apagar.
                                        DirectoryInfo dir = new DirectoryInfo(pastalog);

                                        foreach (FileInfo fi in dir.GetFiles())
                                        {
                                            fi.Delete();
                                        }

                                        //Criar o arquivo de Log.
                                        string nomeArquivo1 = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                        StreamWriter writer1 = new StreamWriter(nomeArquivo1);
                                        writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " - " + $"Status: {(int)statusCodigo}" + " - " + $"Pre-faturamento: {pref.Numero}" + " - " + $"Confirmação Faturado: {statusCodigo}");
                                        writer1.Close();

                                    }
                                    //Verifica se o arquivo de Log já existe e inclui as informações.
                                    else
                                    {
                                        //Incluir Log.
                                        using (StreamWriter sw = File.AppendText(pathLogFat))
                                        {
                                            sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " - " + $"Status: {(int)statusCodigo}" + " - " + $"Pre-faturamento: {pref.Numero}" + " - " + $"Confirmação Faturado: {statusCodigo}");
                                        }
                                    }
                                }
                                //Erro no retorno da API de Confirmação de Faturamento
                                catch (WebException eFat)
                                {
                                    if (eFat.Status == WebExceptionStatus.ProtocolError)
                                    {
                                        // Verificando a pasta de Log. 
                                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                        string pathErroFat = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                        //Verifica se o arquivo de Log não existe e inclui as informações.
                                        if (!File.Exists(pathErroFat))
                                        {
                                            //Listar todos os arquivos na pasta de Log e Apagar.
                                            DirectoryInfo dir = new DirectoryInfo(pastalog);

                                            foreach (FileInfo fi in dir.GetFiles())
                                            {
                                                fi.Delete();
                                            }

                                            Console.WriteLine("Error: {0}", eFat.Status);

                                            //Incluir Log.
                                            string nomeArquivoErrFat = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                            StreamWriter writer1 = new StreamWriter(nomeArquivoErrFat);
                                            writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {eFat.Message})" + " " + $"(Status: {eFat.Status})");
                                            writer1.Close();
                                        }
                                        //Verifica se o arquivo de Log já existe e inclui as informações.
                                        else
                                        {
                                            //Incluir Log.
                                            using (StreamWriter sw = File.AppendText(path))
                                            {
                                                sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {eFat.Message})" + " " + $"(Status: {eFat.Status})");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Verificando a pasta de Log. 
                                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                        string pathErroFat = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                        //Verifica se o arquivo de Log não existe e inclui as informações.
                                        if (!File.Exists(pathErroFat))
                                        {
                                            //Listar todos os arquivos na pasta de Log e Apagar.
                                            DirectoryInfo dir = new DirectoryInfo(pastalog);

                                            foreach (FileInfo fi in dir.GetFiles())
                                            {
                                                fi.Delete();
                                            }

                                            Console.WriteLine("Error: {0}", eFat.Status);

                                            //Incluir Log.
                                            string nomeArquivoErrFat = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                            StreamWriter writer1 = new StreamWriter(nomeArquivoErrFat);
                                            writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {eFat.Message})" + " " + $"(Status: {eFat.Status})");
                                            writer1.Close();
                                        }
                                        //Verifica se o arquivo de Log já existe e inclui as informações.
                                        else
                                        {
                                            //Incluir Log.
                                            using (StreamWriter sw = File.AppendText(path))
                                            {
                                                sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {eFat.Message})" + " " + $"(Status: {eFat.Status})");
                                            }
                                        }
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
                                            HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create($"{ConnectErroFat}");
                                            request1.Method = "POST";
                                            request1.ContentType = "application/json";
                                            request1.Headers.Add("Authorization", $"{Authorization}");
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
                                                string path = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                                //Verifica se o arquivo de Log não existe e inclui as informações.
                                                if (!File.Exists(path))
                                                {
                                                    //Listar todos os arquivos na pasta de Log e Apagar.
                                                    DirectoryInfo dir = new DirectoryInfo(pastalog);

                                                    foreach (FileInfo fi in dir.GetFiles())
                                                    {
                                                        fi.Delete();
                                                    }

                                                    //Incluir Log.
                                                    Console.WriteLine($"Log Erro - Faturamento: {pref.Numero}" + " - " + $"Status: {(int)statusRespostaErro}" + " - " + $"{statusRespostaErro}");
                                                    string nomeArquivo = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                                    StreamWriter writer = new StreamWriter(nomeArquivo);
                                                    writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"Log Erro - Pré-Faturamento: {pref.Numero}" + " - " + $"Status: {(int)statusRespostaErro}" + " - " + $"{statusRespostaErro}" + " " + $"{PostErroFat}");
                                                    writer.Close();
                                                }
                                                //Verifica se o arquivo de Log já existe e inclui as informações.
                                                else
                                                {
                                                    //Incluir Log.
                                                    using (StreamWriter sw = File.AppendText(path))
                                                    {
                                                        Console.WriteLine($"Log Erro - Faturamento: {pref.Numero}" + " - " + $"Status: {(int)statusRespostaErro}" + " - " + $"{statusRespostaErro}");
                                                        sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"Log Erro - Pré-Faturamento: {pref.Numero}" + " - " + $"Status: {(int)statusRespostaErro}" + " - " + $"{statusRespostaErro}" + " " + $"{PostErroFat}");
                                                    }
                                                }
                                                streamPostErro.Close();
                                            }
                                        }
                                        catch (WebException e1)
                                        {
                                            //Verifica o Status de exceção recebida, número de erro retornado.
                                            if (e1.Status == WebExceptionStatus.ProtocolError)
                                            {
                                                //Verificando a pasta de Log. 
                                                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                                string path = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                                //Verifica se o arquivo de Log não existe e inclui as informações.
                                                if (!File.Exists(path))
                                                {
                                                    //Listar todos os arquivos na pasta de Log e Apagar.
                                                    DirectoryInfo dir = new DirectoryInfo(pastalog);

                                                    foreach (FileInfo fi in dir.GetFiles())
                                                    {
                                                        fi.Delete();
                                                    }

                                                    var resposta3 = (HttpWebResponse)e1.Response;
                                                    Console.WriteLine($"Errorcode: {(int)resposta3.StatusCode}" + " - " + $"{resposta3.StatusDescription.ToString()}");

                                                    //Incluir Log.
                                                    string nomeArquivo = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                                    StreamWriter writer = new StreamWriter(nomeArquivo);
                                                    writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta3.StatusCode})" + " " + $"(Status: {e1.Message})");
                                                    writer.Close();
                                                }
                                                //Verifica se o arquivo de Log já existe e inclui as informações.
                                                else
                                                {
                                                    //Incluir Log.
                                                    using (StreamWriter sw = File.AppendText(path))
                                                    {
                                                        var resposta3 = (HttpWebResponse)e1.Response;
                                                        Console.WriteLine($"Errorcode: {(int)resposta3.StatusCode}" + " - " + $"{resposta3.StatusDescription.ToString()}");

                                                        sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta3.StatusCode})" + " " + $"(Status: {e1.Message})");
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
                                    string path = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                                    //Verifica se o arquivo de Log não existe e inclui as informações.
                                    if (!File.Exists(path))
                                    {
                                        //Listar todos os arquivos na pasta de Log e Apagar.
                                        DirectoryInfo dir = new DirectoryInfo(pastalog);

                                        foreach (FileInfo fi in dir.GetFiles())
                                        {
                                            fi.Delete();
                                        }

                                        Console.WriteLine("Error: {0}", e.Status);

                                        //Incluir Log.
                                        string nomeArquivo1 = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                                        StreamWriter writer1 = new StreamWriter(nomeArquivo1);
                                        writer1.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                                        writer1.Close();
                                    }
                                    //Verifica se o arquivo de Log já existe e inclui as informações.
                                    else
                                    {
                                        //Incluir Log.
                                        using (StreamWriter sw = File.AppendText(path))
                                        {
                                            sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                                        }
                                    }
                                }
                            }
                        }
                        streamDados.Dispose();
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
                        string path = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                        //Verifica se o arquivo de Log não existe e inclui as informações.
                        if (!File.Exists(path))
                        {
                            //Listar todos os arquivos na pasta de Log e Apagar.
                            DirectoryInfo dir = new DirectoryInfo(pastalog);

                            foreach (FileInfo fi in dir.GetFiles())
                            {
                                fi.Delete();
                            }

                            var resposta2 = (HttpWebResponse)e.Response;
                            Console.WriteLine($"Errorcode: {(int)resposta2.StatusCode}" + " - " + $"{resposta2.StatusDescription.ToString()}");

                            //Incluir Log.
                            string nomeArquivo = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                            StreamWriter writer = new StreamWriter(nomeArquivo);
                            writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta2.StatusCode})" + " " + $"(Status: {e.Message})");
                            writer.Close();
                        }
                        //Verifica se o arquivo de Log já existe e inclui as informações.
                        else
                        {
                            //Incluir Log.
                            using (StreamWriter sw = File.AppendText(path))
                            {
                                var resposta2 = (HttpWebResponse)e.Response;
                                Console.WriteLine($"Errorcode: {(int)resposta2.StatusCode}" + " - " + $"{resposta2.StatusDescription.ToString()}");

                                sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Codigo Erro: {(int)resposta2.StatusCode})" + " " + $"(Status: {e.Message})");
                            }
                        }
                    }
                    //Recebida Exceção da API, porem sem retorno de Status.
                    else
                    {
                        //Verificando a pasta de Log. 
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string path = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                        //Verifica se o arquivo de Log não existe e inclui as informações.
                        if (!File.Exists(path))
                        {
                            //Listar todos os arquivos na pasta de Log e Apagar.
                            DirectoryInfo dir = new DirectoryInfo(pastalog);

                            foreach (FileInfo fi in dir.GetFiles())
                            {
                                fi.Delete();
                            }

                            //Incluir Log.
                            Console.WriteLine("Error: {0}", e.Status);
                            string nomeArquivo = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                            StreamWriter writer = new StreamWriter(nomeArquivo);
                            writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                            writer.Close();
                        }
                        //Verifica se o arquivo de Log já existe e inclui as informações.
                        else
                        {
                            //Incluir Log.
                            using (StreamWriter sw = File.AppendText(path))
                            {
                                sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"(Erro: {e.Message})" + " " + $"(Status: {e.Status})");
                            }
                        }

                    }

                }
            }
            else
            {
                string pastalog2 = @"C:\Log\";
                //Verificando a pasta de Log. 
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";

                //Verifica se o arquivo de Log não existe e inclui as informações.
                if (!File.Exists(path))
                {
                    //Listar todos os arquivos na pasta de Log e Apagar.
                    DirectoryInfo dir = new DirectoryInfo(pastalog2);

                    foreach (FileInfo fi in dir.GetFiles())
                    {
                        fi.Delete();
                    }

                    //Incluir Log.
                    Console.WriteLine("Error: ControlKey no Arquivo de Configuração está Inválida!");
                    string nomeArquivo = @"C:\Log\" + DateTimeOffset.Now.ToString("ddMMyyyy") + ".log";
                    StreamWriter writer = new StreamWriter(nomeArquivo);
                    writer.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"Erro: ControlKey no Arquivo de Configuração está Inválida!");
                    writer.Close();
                }
                //Verifica se o arquivo de Log já existe e inclui as informações.
                else
                {
                    //Incluir Log.
                    using (StreamWriter sw = File.AppendText(path))
                    {
                        sw.WriteLine($"Data: {DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss")}" + " " + $"Erro: ControlKey no Arquivo de Configuração está Inválida!");
                    }
                }
            }
        }
    }
}
