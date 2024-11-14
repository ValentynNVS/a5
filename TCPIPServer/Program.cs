using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

/*
*   FILE          : Program.cs
*   PROJECT       : PROG2121 - A05
*   PROGRAMMER    : Ahmed & Valentyn
*   FIRST VERSION : 11/11/2024
*   DESCRIPTION   :
*      The class in this file is used to start up the server/listener of the game.
*/
namespace TCPIPServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GameServer server = new GameServer();
            server.GuessingGame(server);
            server.StartServer();

            Console.WriteLine("Press Enter to End");
            Console.ReadLine();
        }
    }
}
