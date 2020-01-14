using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YgoProEsPatcher
{
    public static class GitAccess
    {

        public static GitHubClient githubAuthorized = new GitHubClient(new ProductHeaderValue("pics"))
        {
            //Credentials = new Credentials(Data.GetToken())
        };
        public static GitHubClient githubUnauthorized = new GitHubClient(new ProductHeaderValue("versionCheck"))
        {
            //Credentials = new Credentials(Data.GetToken())
        };
        static public List<RepositoryContent> GetRepositoryContent(string owner, string repo, string path)
        {

            List<RepositoryContent> result = new List<RepositoryContent>();
            if (path == null || path == "")
            {
                result.AddRange(githubAuthorized.Repository.Content.GetAllContents(owner, repo).Result);
            }
            else
            {
                result.AddRange(githubAuthorized.Repository.Content.GetAllContents(owner, repo, path).Result);
            }

            return result;
        }
        static public string GetURLofRepo(string owner, string repo)
        {
            Repository repository = githubAuthorized.Repository.Get(owner, repo).Result;


            return repository.CloneUrl;
        }
        static public List<string> GetAllFilesWithExtensionFromYGOPRO(string path, string extension)
        {
            List<RepositoryContent> result = GitAccess.GetRepositoryContent("Armagedon13", "YgoProEs-CDB", path);
            List<string> fileNames = new List<string>();
            foreach (var c in result)
            {
                if (c.Name.Contains(extension))
                {
                    fileNames.Add(c.Name);

                }
            }
            return fileNames;
        }
        static public List<string> GetAllFilesWithExtensionFromRepo(string owner, string repo, string path, string extension)
        {
            List<RepositoryContent> result = GitAccess.GetRepositoryContent(owner, repo, path);
            List<string> fileNames = new List<string>();
            foreach (var c in result)
            {
                if (c.Name.Contains(extension))
                {
                    fileNames.Add(c.Name);

                }
            }
            return fileNames;
        }
        static public Release GetNewestYgoProPatcherRelease()
        {

            return githubUnauthorized.Repository.Release.GetLatest("Armagedon13", "ygopropatcher").Result;
        }
        static public List<GitHubCommit> GetHeaderCommit()
        {
            List<GitHubCommit> commits = new List<GitHubCommit>
            {
                githubAuthorized.Repository.Commit.Get(Data.YgoProESOwner, "YgoproEs-Pics", "HEAD").Result,
                githubAuthorized.Repository.Commit.Get(Data.YgoProESOwner, "YgoProEs-CDB", "HEAD").Result,
                githubAuthorized.Repository.Commit.Get(Data.YgoProESOwner, "YgoproEs-Database", "HEAD").Result,
                githubAuthorized.Repository.Commit.Get(Data.YgoProESOwner, "YgoproEs-Pics-Field", "HEAD").Result,

                //githubAuthorized.Repository.Commit.Get("Szefo09","face","HEAD").Result
            };

            return commits;

        }

    }
}
