using BusinessLayer.Exceptions;
using System;

public class CategoryNotFoundException : BaseException
{
    public int ID { get; }

    // Basic constructor
    public CategoryNotFoundException() : base("The requested category could not be found.") { }

    // Constructor that accepts the ID to provide a better message
    public CategoryNotFoundException(int id)
        : base($"No category was found with the ID '{id}' in our system.")
    {
        ID = id;
    }
}