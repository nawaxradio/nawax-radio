namespace NawaxRadio.Api.Options
{
    public class FirebaseStorageOptions
    {
        public string ProjectId { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string? CredentialsPath { get; set; }
    }
}
