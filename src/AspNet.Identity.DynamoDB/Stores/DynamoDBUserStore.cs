using System;
using Microsoft.AspNetCore.Identity;

public class DynamoDBUserStore<TUser> 
    : IUserStore<TUser>, IDisposable where TUser : IdentityUser
{
}