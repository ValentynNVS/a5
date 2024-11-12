using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

/*
*   FILE          : GameServer.cs
*   PROJECT       : PROG2121 - A05
*   PROGRAMMER    : Ahmed & Valentyn
*   FIRST VERSION : 11/11/2024
*   DESCRIPTION   :
*      The class in this file contains the server for the guessing game. It allows clients to connect over TCP/IP and 
*      play a game of guessing words from a string of scrambled letters.
*/
namespace TCPIPServer
{
    internal class GameServer
    {
        /* constants */
        const int kMaxMessageLength = 256;

        /*
        *  Method  : StartServer()
        *  Summary : initialize the server and start listening for client requests.
        *  Params  : 
        *     none.
        *  Return  :  
        *     none.
        */
        internal void StartServer()
        { 
            // initialize port & IP address
            int port = 13000;
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

            // establish endpoint of connection (socket)
            TcpListener server = null;
            server = new TcpListener(ipAddress, port);

            server.Start();

            /* enter listening loop */
            while (true)
            {
                // wait for a connection
                Console.Write("Waiting for a connection...");
                TcpClient client = server.AcceptTcpClient();

                // connection occurred; fire off a task to handle it
                Console.Write("Connection happened!");
                Action<Object> gameWorker = GuessingGame;
                Task gameTask = Task.Factory.StartNew(gameWorker, client);
            }
            
        }

        /*
        *  Method  : GuessingGame()
        *  Summary : handle game logic.
        *  Params  : 
        *     none.
        *  Return  :  
        *     none.
        */
        public void GuessingGame(Object o)
        {
            TcpClient client = (TcpClient)o;
            string[] stringFiles = { "1.txt", "2.txt", "3.txt" };

            /* randomly choose a scrambled string file */
            Random rand = new Random();
            int randomIndex = rand.Next(0, stringFiles.Length);
            StreamReader reader =  new StreamReader("../../String files/" + stringFiles[randomIndex]);

            /* parse info from the file */
            string scrambledString = reader.ReadLine();
            int numOfWords = int.Parse(reader.ReadLine());
            string[] wordsList = new string[numOfWords];
            int i = 0;

            while ((!reader.EndOfStream) && (i < numOfWords))
            {
                wordsList[i] = reader.ReadLine();
                i++;
            }

            NetworkStream stream = client.GetStream();
            byte[] dataBytes = new byte[kMaxMessageLength];
            string message = null;

            //while ((stream.Read(dataBytes, 0, dataBytes.Length) != 0)
            //{

            //}






        }
    }

}
