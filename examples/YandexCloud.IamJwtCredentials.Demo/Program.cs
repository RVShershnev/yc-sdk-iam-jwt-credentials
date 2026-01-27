using System;
using System.IO;
using System.Text.Json;
using Yandex.Cloud;

namespace YandexCloud.IamJwtCredentials.Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var configuration = JsonSerializer.Deserialize<IamJwtCredentialsConfiguration>(File.ReadAllText("authorized_key.json"));
            var sdk = new Sdk(new IamJwtCredentialsProvider(configuration));

            var response = sdk.Services.Ydb.DatabaseService.List(new Yandex.Cloud.Ydb.V1.ListDatabasesRequest() { FolderId = "b1g7r20so8vkbaq4fr1f" });
                       
            foreach (var c in response.Databases)
            {
                Console.Out.WriteLine($"* {c.Name} ({c.Id})");
            }
        }
    }
}