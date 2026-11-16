namespace FixNear.Features.Auth.Configurations
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public string SenderName { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
