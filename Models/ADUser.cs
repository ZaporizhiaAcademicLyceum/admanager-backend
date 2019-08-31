namespace admanager_backend.Models
{
    public class ADUser
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Unit { get; set; }

        public string OU
        {
            get
            {
                return "OU=" + this.Unit + ",OU=Students,OU=Users,OU=ACADEMLY,DC=ad,DC=academlyceum,DC=zp,DC=ua";
            }
        }

        public string Department
        {
            get
            {
                return this.Unit;
            }
        }

        public string HomeDir
        {
            get
            {
                return @"\\ALEX.ad.academlyceum.zp.ua\infstud\" + this.Unit + @"\" + this.Username;
            }
        }
    }
}
