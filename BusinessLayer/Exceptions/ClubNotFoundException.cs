using BusinessLayer.Exceptions;
using System;

public class ClubNotFoundException : BaseException
{
    public int ID { get; }

    // Basic constructor
    public ClubNotFoundException() : base("The requested club could not be found.") { }

    // Constructor that accepts the ID to provide a better message
    public ClubNotFoundException(int id)
        : base($"No club was found with the ID '{id}' in our system.")
    {
        ID = id;
    }
}