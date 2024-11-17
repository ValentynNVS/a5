using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
*   FILE          : UI.cs
*   PROJECT       : PROG2121 - A04
*   PROGRAMMER    : Ahmed Almoune
*   FIRST VERSION : 11/17/2024
*   DESCRIPTION   :
*      The class in this file is the ui class for the server. It handles all output to the screen.
*/
namespace TCPIPServer
{
    internal class ServerUI
    {
        /*
        *  Method  : Write()
        *  Summary : take a string parameter and print it to console.
        *  Params  : 
        *     string textToPrint = the string to print.
        *  Return  :  
        *     none.
        */
        internal void Write(string textToPrint)
        {
            Console.WriteLine(textToPrint);
        }

        /*
        *  Method  : WriteInLine()
        *  Summary : take a string parameter and print it to console (inline).
        *  Params  : 
        *     string textToPrint = the string to print.
        *  Return  :  
        *     none.
        */
        internal void WriteInLine(string textToPrint)
        {
            Console.Write(textToPrint);
        }
    }
}