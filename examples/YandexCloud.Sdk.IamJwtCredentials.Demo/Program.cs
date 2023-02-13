using Yandex.Cloud.Resourcemanager.V1;
using Yandex.Cloud;
using System.Text.Json;

namespace YandexCloud.IamJwtCredentials.Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IamJwtCredentialsConfiguration configuration = JsonSerializer.Deserialize<IamJwtCredentialsConfiguration>(File.ReadAllText("authorized_key.json"));
            var sdk = new Sdk(new IamJwtCredentialsProvider(configuration));

            var response = sdk.Services.Ydb.DatabaseService.List(new Yandex.Cloud.Ydb.V1.ListDatabasesRequest() { FolderId = "b1g7r20so8vkbaq4fr1f" });
                       
            foreach (var c in response.Databases)
            {
                Console.Out.WriteLine($"* {c.Name} ({c.Id})");
            }
        }
    }
}