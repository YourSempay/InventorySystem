using System;
using System.Net;

namespace MVVM.Tools;

public class ApiException: Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(HttpStatusCode code, string message) : base(message)
    {
        StatusCode = code;
    }
}