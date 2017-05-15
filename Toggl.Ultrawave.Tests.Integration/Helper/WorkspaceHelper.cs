﻿using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.Tests.Integration.Helper
{
    internal static class WorkspaceHelper
    {
        public static async Task<Workspace> Create((string email, string password) credentials)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            var newWorkspaceName = $"{Guid.NewGuid()}";
            var json = $"{{\"name\": \"{newWorkspaceName}\"}}";

            var requestMessage = AuthorizedRequestBuilder.CreateRequest(
                credentials.email, credentials.password,
                "https://toggl.space/api/v9/workspaces", HttpMethod.Post);
            requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(requestMessage);
                var responseBody = await response.Content.ReadAsStringAsync();

                var jsonSerializer = new JsonSerializer();
                return jsonSerializer.Deserialize<Workspace>(responseBody);
            }
        }
    }
}