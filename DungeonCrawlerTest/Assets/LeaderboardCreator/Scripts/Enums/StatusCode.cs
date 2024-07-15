namespace Dan.Enums
{
    public enum StatusCode
    {
        FailedToConnect = 0,
        Ok = 200,
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        Conflict = 409,
        TooManyRequests = 429,
        InternalServerError = 500,
        NotImplemented = 501,
        ServiceUnavailable = 503
    }
}