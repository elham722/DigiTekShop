using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);

        string Decrypt(string encryptedText);
    }
}
