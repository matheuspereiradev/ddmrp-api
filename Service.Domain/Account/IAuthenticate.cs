using Service.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Domain.Account
{
    public interface IAuthenticate
    {
        string GenerateToken(int id, string email);
    }
}
