using BusinessLayer.Exceptions;
using System;

public class PersonNotFoundException : BaseException
{
    public int ID { get; }

    // Basic constructor
    public PersonNotFoundException() : base("The requested person could not be found.") { }

    // Constructor that accepts the ID to provide a better message
    public PersonNotFoundException(int id)
        : base($"No person was found with the ID '{id}' in our system.")
    {
        ID = id;
    }
}