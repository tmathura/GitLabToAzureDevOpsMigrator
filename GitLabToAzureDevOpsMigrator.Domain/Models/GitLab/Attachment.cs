namespace GitLabToAzureDevOpsMigrator.Domain.Models.GitLab
{
    public class Attachment
    {
        public Attachment(string name, string urlPath, Uri url)
        {
            Name = name;
            UrlPath = urlPath;
            Url = url;
        }

        public string Name { get; set; }
        public string UrlPath { get; set; }
        public Uri Url { get; set; }
    }
}