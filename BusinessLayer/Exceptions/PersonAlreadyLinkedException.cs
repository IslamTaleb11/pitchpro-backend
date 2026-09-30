using BusinessLayer.Exceptions;
using System;

public class PersonAlreadyLinkedException : BaseException
{
    public int ID { get; }

    public PersonAlreadyLinkedException()
        : base("This person is already associated with a user account.") { }

    public PersonAlreadyLinkedException(int personId)
        : base($"The person with ID '{personId}' is already linked to an existing user. One person cannot have multiple accounts.")
    {
        ID = personId;
    }
}