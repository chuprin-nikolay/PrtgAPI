﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PrtgAPI.Request;

namespace PrtgAPI.Tests.UnitTests
{
    class MockWebClient : IWebClient
    {
        IWebResponse response;

        public MockWebClient(IWebResponse response)
        {
            this.response = response;
        }

        public Task<HttpResponseMessage> GetSync(string address, CancellationToken token)
        {
            var statusCode = GetStatusCode();

            if (token.IsCancellationRequested)
                throw new TaskCanceledException();

            var message = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response.GetResponseText(ref address)),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri(address)
                }
            };

            SetContentHeaders(message);

            return Task.FromResult(message);
        }

        public async Task<HttpResponseMessage> GetAsync(string address, CancellationToken token)
        {
            var statusCode = GetStatusCode();

            if (token.IsCancellationRequested)
                throw new TaskCanceledException();

            var stack = new System.Diagnostics.StackTrace();

            var method = stack.GetFrames().Last(f => f.GetMethod().Module.Name == "PrtgAPI.dll").GetMethod();

            var responseStr = string.Empty;

            if (method.Name.StartsWith("Stream"))
            {
                var streamer = response as IWebStreamResponse;

                if (streamer != null)
                {
                    responseStr = await streamer.GetResponseTextStream(address).ConfigureAwait(false);
                }
                else
                    throw new NotImplementedException($"Could not stream as response does not implement {nameof(IWebStreamResponse)}");
            }
            else
            {
                //If the method is in fact async, or is called as part of a streaming method, we execute the request as async
                //This implies we do not consider nested streaming methods to be an implemented scenario
                responseStr = await Task.FromResult(response.GetResponseText(ref address)).ConfigureAwait(false);
            }
            //we should check whether the method is a streamer or an async, and if its async we should to task.fromresult

            var message = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseStr),
                RequestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri(address)
                }
            };

            SetContentHeaders(message);

            return message;
        }

        private HttpStatusCode GetStatusCode()
        {
            var statusCode = HttpStatusCode.OK;

            var statusResponse = response as IWebStatusResponse;

            if (statusResponse != null)
            {
                if (statusResponse.StatusCode != 0)
                    statusCode = statusResponse.StatusCode;
            }

            return statusCode;
        }

        private void SetContentHeaders(HttpResponseMessage message)
        {
            var headerResponse = response as IWebContentHeaderResponse;

            headerResponse?.HeaderAction?.Invoke(message.Content.Headers);
        }
    }
}
