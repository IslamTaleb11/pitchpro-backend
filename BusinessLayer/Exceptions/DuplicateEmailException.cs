using BusinessLayer.Exceptions;
using System;

public class DuplicateEmailException : BaseException
{
    public string Email { get; }

    // Basic constructor
    public DuplicateEmailException() : base("This email is already in use.") { }

    // Constructor that accepts the email to provide a better message
    public DuplicateEmailException(string email)
        : base($"The email address '{email}' is already registered in our system.")
    {
        Email = email;
    }
}