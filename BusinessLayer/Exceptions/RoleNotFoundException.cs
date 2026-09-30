using BusinessLayer.Exceptions;
using System;

public class RoleNotFoundException : BaseException
{
    public int ID { get; }

    // Basic constructor
    public RoleNotFoundException() : base("The specified role could not be found.") { }

    // Constructor that accepts the ID
    public RoleNotFoundException(int id)
        : base($"No role was found with the ID '{id}'. Please ensure the Role ID is correct.")
    {
        ID = id;
    }
}