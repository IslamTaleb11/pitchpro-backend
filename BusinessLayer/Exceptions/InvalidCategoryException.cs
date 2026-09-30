using BusinessLayer.Exceptions;
using System;

public class InvalidCategoryException : BaseException
{
    public int CategoryID { get; }

    // Basic constructor
    public InvalidCategoryException() : base("The specified category was not found.") { }

    // Constructor that accepts the ID to provide a detailed message
    public InvalidCategoryException(int categoryId)
        : base($"Category with ID {categoryId} does not exist or does not belong to this club.")
    {
        CategoryID = categoryId;
    }
}