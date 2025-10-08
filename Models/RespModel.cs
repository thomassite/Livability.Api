namespace Livability.Api.Models
{
    public class RespModel<T>
    {
        public bool Success { get; set; } = true;


        public string Message { get; set; }

        public string ReturnCode { get; set; }

        public T Result { get; set; }
    }
}
