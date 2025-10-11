namespace Livability.Api.Models
{
    public static class Resp
    {
        public static RespModel<T> Ok<T>(T result, string? message = null)
            => new RespModel<T> { Success = true, Result = result, Message = message };

        public static RespModel<T> Fail<T>(string message)
            => new RespModel<T> { Success = false, Message = message };
    }
}
