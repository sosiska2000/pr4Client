using System;

namespace Common
{

    public class ViewModelSend
    {
        public string Message { get; set; }

        public int Id { get; set; }
        public ViewModelSend(string message, int id)
        {
            this.Message = message;
            this.Id = id;
        }
    }
}