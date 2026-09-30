using BusinessLayer.Exceptions;
using System;

public class PrimaryRoleNotFoundException : BaseException
{
    public int ID { get; }

    // Basic constructor
    public PrimaryRoleNotFoundException() : base("The specified primary role could not be found.") { }

    // Constructor that accepts the ID
    public PrimaryRoleNotFoundException(int id)
        : base($"No primary role was found with the ID '{id}'. Please ensure the Primary Role ID is correct.")
    {
        ID = id;
    }
}