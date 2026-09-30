using BusinessLayer.Exceptions;
using System;

public class RoleClassificationNotFoundException : BaseException
{
    public int ID { get; }

    // Basic constructor
    public RoleClassificationNotFoundException() : base("The specified role classification could not be found.") { }

    // Constructor that accepts the ID
    public RoleClassificationNotFoundException(int id)
        : base($"No role classification was found with the ID '{id}'. Please ensure the Role classification ID is correct.")
    {
        ID = id;
    }
}