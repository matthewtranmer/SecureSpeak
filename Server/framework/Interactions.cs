namespace APIcontroller
{
    public struct APIRequest
    {
        public Dictionary<string, string>? request_body;
    }

    public struct APIResponse
    {
        public string? response_code;
        public string? error_message;

        public Dictionary<string, string>? response_body;

        public APIResponse(Dictionary<string, string>? response_body = null, string? error_message = null, string? response_code = "200")
        {
            this.response_code = response_code;
            this.response_body = response_body;
            this.error_message = error_message;
        }

        public APIResponse()
        {
            response_code = "200";
            error_message = null;
            response_body = null;
        }
    }

    public enum Method
    {
        GET,
        POST,
        PUT,
        DELETE
    }
}
