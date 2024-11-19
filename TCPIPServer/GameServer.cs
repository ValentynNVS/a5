using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Runtime.InteropServices.ComTypes;

/*
*   FILE          : GameServer.cs
*   PROJECT       : PROG2121 - A05
*   PROGRAMMER    : Ahmed & Valentyn
*   FIRST VERSION : 11/11/2024
*   DESCRIPTION   :
*      The class in this file contains the server for the guessing game. It allows clients to connect over TCP/IP and 
*      play a game of word guessing.
*/
namespace TCPIPServer
{
    internal class GameServer
    {
        //to hold the details of a session /
        public struct SessionVariables
        {
            public string sessionId;
            public string scrambledString;
            public string[] wordsList;
            public int numOfWords;
        }

        public static List<SessionVariables> playerSessions = new List<SessionVariables>(); //list of active sessions
        private static ServerUI ui = new ServerUI();

        /* constants */
        const int kMaxMessageLength = 256;
        const int port = 55457;
        const string ipv4Address = "10.179.16.204";

        const int clientPort = 13001;
        const string clientIpv4Address = "10.0.0.41";
        volatile bool running = true;
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
            TcpListener server = null;
            //bool running = true;

            Action<Object> shutDownWorker = shutDownServer;
            Task shutDownTask = Task.Factory.StartNew(shutDownWorker, server);

            try
            {
                // initialize IP address
                IPAddress ipAddress = IPAddress.Parse(ipv4Address);

                // establish endpoint of connection (socket)
                server = new TcpListener(ipAddress, port);
                server.Start();

                /* enter listening loop */
                while (running)
                {
                    // wait for a connection
                    ui.Write("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();

                    // connection occurred; fire off a task to handle it
                    ui.Write("Connection happened!");
                    Action<Object> gameWorker = GuessingGame;
                    Task gameTask = Task.Factory.StartNew(gameWorker, client);
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                ui.Write("Error: " + e + e.Message);
            }
            finally
            {
                server.Stop();
            }
        }

        /*
        *  Method  : GuessingGame()
        *  Summary : handle client requests and provide appropriate response based on game logic.
        *  Params  : 
        *     Object o = the TcpClient object to communicate with client.
        *  Return  :  
        *     none.
        */
        public void GuessingGame(Object o)
        {
            /* buffer for reading request message */
            TcpClient client = (TcpClient)o;
            NetworkStream stream = client.GetStream();
            byte[] request = new byte[kMaxMessageLength];
            string message = string.Empty;
            int i;

            try
            {
                /* read request and handle it */
                while (stream.DataAvailable && (i = stream.Read(request, 0, request.Length)) != 0)
                {
                    message = System.Text.Encoding.ASCII.GetString(request, 0, i);
                    ui.Write("Received: " + message);

                    Regex startGame = new Regex(@"^CreatePlayerSession$");
                    Regex wordGuess = new Regex(@"^MakeGuess\|\S{1,10}\|\S{36}$");
                    Regex endGame = new Regex(@"^EndPlayerSession\|.{36}$");

                    /* call appropriate method based on request */
                    if (startGame.IsMatch(message))
                    {
                        message = CreateSession();
                    }
                    else if (wordGuess.IsMatch(message))
                    {
                        message = TakeGuess(message);
                    }
                    else if (endGame.IsMatch(message))
                    {
                        message = EndSession(message);
                    }
                    else //request is invalid
                    {
                        message = "BadRequest";
                    }

                    /* send response back to client */
                    byte[] response = System.Text.Encoding.ASCII.GetBytes(message);
                    stream.Write(response, 0, response.Length);
                    ui.Write("Sent: " + message);
                }
            }
            catch (Exception e)
            {
                ui.Write("Error: " + e + e.Message);
            }
            finally
            {
                if (client.Connected)
                {
                    client.Close();
                }
            }
        }

        /*
        *  Method  : CreateSession()
        *  Summary : create a new game session by parsing a string file and using its contents to create a new session instance,  
        *  Params  : 
        *     none.
        *  Return  :  
        *     string response = a string made up of the scrambled string, number of words to be found, and the session id.
        *     e.g. thisawh|6|NBIO-8346.
        */
        public string CreateSession()
        {
            string sessionId = Guid.NewGuid().ToString();
            string[] stringFiles = { "1.txt", "2.txt", "3.txt" };

            /* randomly choose a scrambled string file */
            Random randomNumber = new Random();
            int randomIndex = randomNumber.Next(0, stringFiles.Length);
            StreamReader reader = new StreamReader(ConfigurationManager.AppSettings["stringsPath"] + stringFiles[randomIndex]);

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

            /* create a session for the client */
            SessionVariables playerSession = new SessionVariables();
            playerSession.sessionId = sessionId;
            playerSession.scrambledString = scrambledString;
            playerSession.wordsList = wordsList;
            playerSession.numOfWords = numOfWords;

            playerSessions.Add(playerSession); //add session to list

            string response = scrambledString + "|" + numOfWords.ToString() + "|" + sessionId;
            return response;
        }

        /*
        *  Method  : TakeGuess()
        *  Summary : handle a client guess and update session variables accordingly.  
        *  Params  : 
        *     string message = the message from the client containing the command, guess, and session id.
        *  Return  :  
        *     string response = a string made up of: a word indicating whether the guess was found in the string or not, the 
        *     number of words left to guess.
        */
        public string TakeGuess(string message)
        {
            string[] messageComponents = message.Split('|');
            SessionVariables tempSession = new SessionVariables();
            int sessionNumber = 0;
            string response = string.Empty;

            if (!SessionsActive()) { return "SessionNotFound"; }
            
            /* search for player's session */
            for (int i = 0; i < playerSessions.Count; i++)
            {
                if (messageComponents[2] == playerSessions[i].sessionId)
                {
                    tempSession = playerSessions[i];
                    sessionNumber = i;
                    break;
                }
                else if (i == playerSessions.Count - 1) //session does not exist
                {
                    response = "SessionNotFound";
                    return response;
                }
            }

            /* determine if the guess was valid or not */
            for (int i = 0; i < tempSession.wordsList.Length; i++)
            {
                if (messageComponents[1] == tempSession.wordsList[i])
                {
                    /* update session variables */
                    tempSession.wordsList[i] = null;
                    tempSession.numOfWords--;
                    playerSessions[sessionNumber] = tempSession;

                    response = "Valid|" + tempSession.numOfWords.ToString();
                    break;
                }
                else if (i == tempSession.wordsList.Length - 1) //guess was wrong
                {
                    response = "Invalid|" + tempSession.numOfWords.ToString();
                }
            }

            return response;
        }

        /*
        *  Method  : EndSession()
        *  Summary : enable use to end active session.  
        *  Params  : 
        *     string message = the message from the client containing the command and session id.
        *  Return  :  
        *     string response = a response informing the user that the session was ended successfully.
        */
        public string EndSession(string message)
        {
            string[] messageComponents = message.Split('|');
            string response = null;

            if (!SessionsActive()) { return "SessionNotFound"; }

            /* search for player's session */
            for (int i = 0; i < playerSessions.Count; i++)
            {
                if (playerSessions[i].sessionId == messageComponents[1])
                {
                    playerSessions.Remove(playerSessions[i]);
                    response = "SessionDeleted";
                    break;
                }
                else if (i == playerSessions.Count - 1)
                {
                    response = "SessionNotFound";
                }
            }

            return response;
        }

        // tells you if there are any active sessions or not.
        public bool SessionsActive()
        {
            if (playerSessions.Count > 0) { return true; }
            else { return false; }
        }

        public void shutDownServer(object o)
        {
            TcpListener server = (TcpListener)o;
            Console.WriteLine("shutdown to stop");
            string command = Console.ReadLine();

            //TcpClient client = new TcpClient(clientIpv4Address, clientPort);
            //NetworkStream stream = client.GetStream();
            string message = "Server is shutting down!";

            if (command == "shutdown")
            {
                for (int i = 0; i < playerSessions.Count; i++)
                {
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                    //stream.Write(data, 0, data.Length);
                    ui.Write("Sent: " + message);
                }
                running = false;
            }
        }
    }
}
