using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaVault.Models
{
    public class UserProfile
    {
        public string UserName { get; set; }
        public string PreferredTheme { get; set; }
        public string Language { get; set; }

        public UserProfile(string userName, string preferredTheme, string language)
        {
            UserName = userName;
            PreferredTheme = preferredTheme;
            Language = language;
        }
    }
}
