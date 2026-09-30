using BusinessLayer.Exceptions;

public class InvalidCallUpDataException : BaseException
{
    public InvalidCallUpDataException(string message) : base(message) { }
}
