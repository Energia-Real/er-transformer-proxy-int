namespace er_transformer_proxy_int.Model
{
    public class Response<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public Response()
        {
            Success = true;
            Message = string.Empty;
            Data = default(T);
        }

        public Response(T data)
        {
            Success = true;
            Message = string.Empty;
            Data = data;
        }

        public Response(string message)
        {
            Success = false;
            Message = message;
            Data = default(T);
        }

        public Response(string message, T data)
        {
            Success = false;
            Message = message;
            Data = data;
        }
    }
}
