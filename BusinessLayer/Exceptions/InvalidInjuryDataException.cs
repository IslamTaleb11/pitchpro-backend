using BusinessLayer.Exceptions;

public class InvalidInjuryDataException : BaseException
{
    public InvalidInjuryDataException(string message) : base(message) { }
}
