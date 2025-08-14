using IdeaCenterExamPrep_July2025.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace IdeaCenterExamPrep_July2025
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI3MDNmMjVjMi1hMWFhLTQ1OTAtYjEzZi0zMjQ2MWZkOGUwNjIiLCJpYXQiOiIwOC8xNC8yMDI1IDEzOjMxOjI5IiwiVXNlcklkIjoiNzE1Yzg2ZjMtNTk4Ny00YjE1LWQyYmMtMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJNYXJ5QGV4YW1wbGUuY29tIiwiVXNlck5hbWUiOiJNYXJ5IiwiZXhwIjoxNzU1MTk5ODg5LCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9.NKXfdilfYsbpiI7g5OwZOxEAt_19obiyug6IivOKY04";

        private const string LoginEmail = "Mary@example.com";
        private const string LoginPassword = "Mary123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }

        // IdeaCenter Api Tests

        [Order(1)]
        [Test]
        public void CreateIdea_WithRequiredFields_ShouldReturnSuccess()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Create Idea",
                Description = "This is a test idea description.",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]

        public void GetAllIdea()
        {
            var request = new RestRequest ("/api/Idea/All", Method.Get);    
            var response = this.client.Execute(request);

            var responseAllIdeas = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseAllIdeas, Is.Not.Empty, "There is no ideas.");

            lastCreatedIdeaId = responseAllIdeas.LastOrDefault()?.Id;
        }

        [Order(3)]
        [Test]  

        public void EditLastIdea()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Edited Idea 1",
                Description = "This is an updated test idea description.",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"), "Editing item was faled.");

        }

        [Order(4)]
        [Test]

        public void DeleteIdeaThatWasEdited()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"), "Deleting idea was unsuccessful.");

        }

        [Order(5)]
        [Test]  

        public void CreateAnIdeaWithoutRequiredFields()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "",
                Description = "",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]

        public void EditNonExistingIdea ()
        {
            string unExistingIdeaId = "12";
            var editRequest = new IdeaDTO
            {
                Title = "Edited Idea that unexist",
                Description = "This is an updated test idea that unexist.",
                Url = ""
            };

            var request = new RestRequest($"/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", unExistingIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);         

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingIdea()
        {
            string unExistingIdeaId = "12";

            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", unExistingIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"), "Deleting idea was unsuccessful.");
        }


        [OneTimeTearDown]
        public void TearDown()
        { 
           this.client?.Dispose();
        }
    }
}