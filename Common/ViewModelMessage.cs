using System;

namespace Common
{
  
    public class ViewModelMessage
    {
        public string Command { get; set; }
        public string Data { get; set; }
        public ViewModelMessage(string command, string data)

        {
            this.Command = command;
            this.Data = data;
        }
    }
}