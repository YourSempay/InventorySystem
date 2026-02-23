namespace API.Services;

public class Exceptions
{
    // 400 — ошибки валидации
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    // 403 — нет прав
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message) { }
    }

    // 409 — конфликт
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}