using System.Text;

namespace VcogBookmarkServer
{
    public class AuthOptions
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string Key { get; set; }
        public Microsoft.IdentityModel.Tokens.SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.ASCII.GetBytes(Key));
        }
    }
}