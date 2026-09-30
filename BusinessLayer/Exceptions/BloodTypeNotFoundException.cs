using BusinessLayer.Exceptions;
using System;

public class BloodTypeNotFoundException : BaseException
{
    public int ID { get; }

    // Basic constructor
    public BloodTypeNotFoundException() : base("The requested blood type could not be found.") { }

    // Constructor that accepts the ID to provide a better message
    public BloodTypeNotFoundException(int id)
        : base($"No blood type was found with the ID '{id}' in our system.")
    {
        ID = id;
    }    
}